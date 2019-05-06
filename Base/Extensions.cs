using System;
using System.Collections.Generic;
using System.Linq;

namespace M3 {
	public static class Extensions {
		public static float Clamp(this float value, float min, float max) => Math.Min(Math.Max(value, min), max);
		public static float Product(this IEnumerable<float> enumerable) => enumerable.Aggregate(1f, (f1, f2) => f1 * f2);
	}
}
