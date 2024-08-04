using System;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct TcpSendSession {

    public readonly uint sessionId;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct TcpResponseSession {

    public readonly TCP_ERROR error;
    
    public TcpResponseSession(TCP_ERROR error) => this.error = error;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct TcpSendHeader {

    public readonly uint sessionId;
    public readonly uint dataLength;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct TcpResponseHeader {
    
    public readonly TCP_ERROR error;
}

[StructLayout(LayoutKind.Sequential)]
public struct TcpRequestPing {

    public readonly int count;
}

public static class StructExtension {

    public static bool TryBytes<T>(this ref T structure, out byte[] bytes) where T : struct {
        bytes = structure.ToBytes();
        return bytes != Array.Empty<byte>();
    }

    public static byte[] ToBytes<T>(this ref T structure) where T : struct {
        var size = Marshal.SizeOf(structure);
        var pointer = Marshal.AllocHGlobal(size);
        var bytes = new byte[size];

        try {
            MemoryMarshal.Read<T>(bytes);
            Marshal.StructureToPtr(structure, pointer, true);
            Marshal.Copy(pointer, bytes, 0, size);
        } catch (Exception) {
            return Array.Empty<byte>();
        } finally {
            Marshal.FreeHGlobal(pointer);
        }

        return bytes;
    }

    public static bool TrySpan<T>(this ref T structure, out Span<byte> bytes) where T : struct {
        bytes = new byte[Marshal.SizeOf(structure)];
        return MemoryMarshal.TryWrite(bytes, ref structure);
    }
    
    public static Span<byte> ToSpan<T>(this ref T structure) where T : struct {
        Span<byte> bytes = new byte[Marshal.SizeOf(structure)];
        MemoryMarshal.Write(bytes, ref structure);
        return bytes;
    }

    public static T? ToStruct<T>(this ref Memory<byte> memory) where T : struct {
        try {
            return MemoryMarshal.Read<T>(memory.Span);
        } catch (Exception) {
            return null;
        }
    }

    public static T? ToStruct<T>(this ref Span<byte> span) where T : struct {
        try {
            return MemoryMarshal.Read<T>(span);
        } catch (Exception) {
            return null;
        }
    }

    public static T? ToStruct<T>(this byte[] bytes) where T : struct {
        var pointer = Marshal.AllocHGlobal(bytes.Length);
        try {
            Marshal.Copy(pointer, bytes, 0, bytes.Length);
            return Marshal.PtrToStructure<T>(pointer);
        } catch (Exception) {
            return null;
        } finally {
            Marshal.FreeHGlobal(pointer);
        }
    }
}

public enum TCP_ERROR {
    NONE = 0,
    
    // Session
    DUPLICATE_SESSION = 100,
    INVALID_SESSION = 102,
    
    // Data
    MISSING_DATA = 200,
    
    // Progress
    EXCEPTION_PROGRESS = 300,
    
    TEST = 1000,
}
