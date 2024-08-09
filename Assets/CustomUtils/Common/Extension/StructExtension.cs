using System;
using System.Runtime.InteropServices;

public static class StructExtension {

    public static bool TryBytes<T>(this ref T structure, out byte[] bytes) where T : struct {
        bytes = structure.ToBytes();
        return bytes != Array.Empty<byte>();
    }

    public static byte[] ToBytes<T>(this ref T structure) where T : struct {
        var size = Marshal.SizeOf<T>();
        var pointer = Marshal.AllocHGlobal(size);
        var bytes = new byte[size];

        try {
            Marshal.StructureToPtr(structure, pointer, true);
            Marshal.Copy(pointer, bytes, 0, size);
        } catch (Exception) {
            return Array.Empty<byte>();
        } finally {
            Marshal.FreeHGlobal(pointer);
        }

        return bytes;
    }

    public static bool TrySpan<T>(this ref T structure, out Span<byte> byteSpan) where T : struct {
        byteSpan = new byte[Marshal.SizeOf<T>()];
        return MemoryMarshal.TryWrite(byteSpan, ref structure);
    }
    
    public static Span<byte> ToSpan<T>(this ref T structure) where T : struct {
        Span<byte> bytes = new byte[Marshal.SizeOf<T>()];
        MemoryMarshal.Write(bytes, ref structure);
        return bytes;
    }

    public static bool TryMemory<T>(this ref T structure, out Memory<byte> byteMemory) where T : struct {
        byteMemory = MemoryMarshal.AsMemory<byte>(structure.ToBytes());
        return byteMemory.IsEmpty == false;
    }
    
    public static Memory<byte> ToMemory<T>(this ref T structure) where T : struct => MemoryMarshal.AsMemory<byte>(structure.ToBytes());

    public static T? ToStruct<T>(this ref Memory<byte> memory) where T : struct {
        try {
            return MemoryMarshal.Read<T>(memory.Span);
        } catch (Exception) {
            return null;
        }
    }

    public static T? ToStruct<T>(this ref Span<byte> byteSpan) where T : struct {
        try {
            return MemoryMarshal.Read<T>(byteSpan);
        } catch (Exception) {
            return null;
        }
    }

    public static T? ToStruct<T>(this byte[] bytes) where T : struct {
        if (bytes.Length <= 0) {
            return null;
        }

        var pointer = Marshal.AllocHGlobal(bytes.Length);
        try {
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            return Marshal.PtrToStructure<T>(pointer);
        } catch (Exception) {
            return null;
        } finally {
            Marshal.FreeHGlobal(pointer);
        }
    }
}