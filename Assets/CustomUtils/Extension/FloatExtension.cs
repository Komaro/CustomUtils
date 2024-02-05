using System.Linq;
using UnityEngine;

public static class FloatExtension {

    public static Vector2 ToVector2(this float[] array, Vector2 @default = default) => new (
        array?.ElementAtOrDefault(0) ?? @default.x,
        array?.ElementAtOrDefault(1) ?? @default.y);

    public static Vector3 ToVector3(this float[] array, Vector3 @default = default) => new (
        array?.ElementAtOrDefault(0) ?? @default.x,
        array?.ElementAtOrDefault(1) ?? @default.y,
        array?.ElementAtOrDefault(2) ?? @default.z);

    public static Vector4 ToVector4(this float[] array, Vector4 @default = default) => new(
        array?.ElementAtOrDefault(0) ?? @default.x,
        array?.ElementAtOrDefault(1) ?? @default.y,
        array?.ElementAtOrDefault(2) ?? @default.z,
        array?.ElementAtOrDefault(3) ?? @default.w);
}