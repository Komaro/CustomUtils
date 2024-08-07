using System;
using System.Runtime.InteropServices;

public static class UnsafeExtension {

    public static unsafe byte[] ToBytesUnsafe<T>(this ref T value) where T : unmanaged {
        var bytes = new byte[Marshal.SizeOf<T>()];
        fixed (byte* pointer = bytes) {
            *(T*)pointer = value;
        }

        return bytes;
    }
    
    public static unsafe Span<byte> ToSpanUnsafe<T>(this ref T value) where T : unmanaged {
        var bytes = new byte[Marshal.SizeOf<T>()];
        fixed (byte* pointer = bytes) {
            *(T*)pointer = value;
        }

        return new Span<byte>(bytes);
    }

    public static unsafe Memory<byte> ToMemoryUnsafe<T>(this ref T value) where T : unmanaged {
        var bytes = new byte[Marshal.SizeOf<T>()];
        fixed (byte* pointer = bytes) {
            *(T*) pointer = value;
        }

        return new Memory<byte>(bytes);
    }

    public static bool TryUnmanagedType<T>(this byte[] bytes, out T? value) where T : unmanaged {
        try {
            value = bytes.ToUnmanagedType<T>();
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        value = null;
        return false;
    }
    
    public static unsafe T ToUnmanagedType<T>(this byte[] bytes) where T : unmanaged {
        if (bytes.Length != sizeof(T)) {
            throw new ArgumentException($"Target type size and the size of the {nameof(bytes)} do not match");
        }
    
        T value;
        fixed (byte* pointer = bytes) {
            value = *(T*) pointer;
        }
        
        return value;
    }

    public static unsafe T ToUnmanagedType<T>(this ref Span<byte> byteSpan) where T : unmanaged {
        if (byteSpan.Length != sizeof(T)) {
            throw new ArgumentException($"Target type size and the size of the {nameof(byteSpan)} do not match");
        }
    
        T value;
        fixed (byte* pointer = byteSpan) {
            value = *(T*) pointer;
        }
        
        return value;
    }
    
    public static unsafe T ToUnmanagedType<T>(this ref Memory<byte> byteMemory) where T : unmanaged {
        if (byteMemory.Length != sizeof(T)) {
            throw new ArgumentException($"Target type size and the size of the {nameof(byteMemory)} do not match");
        }

        T value;
        fixed (byte* pointer = byteMemory.Span) {
            value = *(T*) pointer;
        }
        
        return value;
    }
}

