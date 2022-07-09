using System;
using UnityEngine;

public static class MathExtension {
	
	// 정확도가 낮음. 사용에 주의가 필요
	public static float ToPercent(this float value, int decimalPoint = 2) {
		if (value >= 1) {
			return 100;
		}
		
		if (decimalPoint <= 0 || decimalPoint > 10) {
			decimalPoint = 2;
		}

		var multiValue = Mathf.Pow(10, decimalPoint);
		var divideValue = decimalPoint > 2 ? 10 * (decimalPoint - 2) : 100f;
		return (float)Math.Truncate(value * multiValue) / divideValue;
	}

	public static decimal ToDecimalPercent(this decimal value, int decimalPoint = 2) {
		if (value >= 1) {
			return 100;
		}
		
		if (decimalPoint <= 0 || decimalPoint > 10) {
			decimalPoint = 2;
		}

		var multiValue = (decimal) Mathf.Pow(10, decimalPoint);
		var divideValue = decimalPoint > 2 ? 10 * (decimalPoint - 2) : 100f;
		return Math.Truncate(value * multiValue) / (decimal) divideValue;
	}
}
