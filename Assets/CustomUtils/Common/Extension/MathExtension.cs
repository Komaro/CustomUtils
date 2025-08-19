using System;
using UnityEngine;

public static class MathExtension {
	
	public static float ToPercent(this float value, int decimalPoint = 2) {
		if (value >= 1) {
			return 100f;
		}

		decimalPoint = Math.Clamp(decimalPoint, 0, 10);
		return Mathf.Floor(value * Mathf.Pow(10, decimalPoint + 2)) / Mathf.Pow(10, decimalPoint);
	}
}
