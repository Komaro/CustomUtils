public static class ByteExtension {
    
    public static string ToHex(this byte bytes) => $"0x{bytes:X2}";
}
