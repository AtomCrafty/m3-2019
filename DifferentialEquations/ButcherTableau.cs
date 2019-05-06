using System;
using System.Security.Cryptography.X509Certificates;

namespace M3.DifferentialEquations {
	public class ButcherTableau {
		public readonly int N;
		public readonly float[,] Alpha;
		public readonly float[] Beta;
		public readonly float[] BetaStar;
		public readonly float[] Gamma;

		public ButcherTableau(int n) {
			N = n;
			Alpha = new float[n, n];
			Beta = new float[n];
			Gamma = new float[n];
		}

		public ButcherTableau(int n, float[,] alpha, float[] beta, float[] gamma) {
			N = n;
			Alpha = (float[,])alpha.Clone();
			Beta = (float[])beta.Clone();
			BetaStar = new float[N];
			Gamma = (float[])gamma.Clone();
			CheckDimension();
		}

		public ButcherTableau(int n, float[,] alpha, float[] beta, float[] betaStar, float[] gamma) {
			N = gamma.Length;
			Alpha = (float[,])alpha.Clone();
			Beta = (float[])beta.Clone();
			BetaStar = (float[])betaStar.Clone();
			Gamma = (float[])gamma.Clone();
			CheckDimension();
		}

		private void CheckDimension() {
			if(Alpha.GetLength(0) != N || Alpha.GetLength(1) != N)
				throw new ArgumentException("Incompatible dimensions", nameof(Alpha));
			if(Beta.Length != N)
				throw new ArgumentException("Incompatible dimensions", nameof(Beta));
			if(BetaStar.Length != N)
				throw new ArgumentException("Incompatible dimensions", nameof(BetaStar));
			if(Gamma.Length != N)
				throw new ArgumentException("Incompatible dimensions", nameof(Gamma));
		}

		public bool IsTriangular {
			get {
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				for(int row = 0; row < N; row++) {
					for(int col = row; col < N; col++) {
						if(Alpha[row, col] != 0) return false;
					}
				}
				return true;
			}
		}

		public bool IsConsistent {
			get {
				for(int row = 0; row < N; row++) {
					float sum = 0;
					for(int col = 0; col < N; col++) {
						sum += Alpha[row, col];
					}
					if(Math.Abs(sum - Gamma[row]) > 0.001) return false;
				}
				return true;
			}
		}




		// Explicit methods
		public static ButcherTableau ExplicitEuler => new ButcherTableau(1, new[,] { { 0f } }, new[] { 1f }, new[] { 0f });
		public static ButcherTableau RungeKutta4 => new ButcherTableau(4, new[,] { { 0f, 0f, 0f, 0f }, { .5f, 0f, 0f, 0f }, { 0f, .5f, 0f, 0f }, { 0f, 0f, 1f, 0f } }, new[] { 1 / 6f, 1 / 3f, 1 / 3f, 1 / 6f }, new[] { 0f, .5f, .5f, 1f });
		public static ButcherTableau RungeKuttaThreeEighths => new ButcherTableau(1, new[,] { { 0f } }, new[] { 1f }, new[] { 0f });
		public static ButcherTableau GeneralizedMidpoint(float a) => new ButcherTableau(2, new[,] { { 0f, 0f }, { a, 0f } }, new[] { 1 - 1 / (2 * a), 1 / (2 * a) }, new[] { 0f, a });
		public static ButcherTableau MidpointMethod => GeneralizedMidpoint(.5f);
		public static ButcherTableau RalstonMethod => GeneralizedMidpoint(2 / 3f);

		// Implicit methods
		public static ButcherTableau ImplicitEuler => new ButcherTableau(1, new[,] { { 1f } }, new[] { 1f }, new[] { 1f });
		public static ButcherTableau ImplicitMidpoint => new ButcherTableau(1, new[,] { { .5f } }, new[] { 1f }, new[] { .5f });
		public static ButcherTableau TrapezoidalRule => new ButcherTableau(2, new[,] { { 0f, 0f }, { .5f, .5f } }, new[] { .5f, .5f }, new[] { 1f, 0f }, new[] { 0f, 1f });
		public static ButcherTableau GaussLegendreMethod1 => ImplicitMidpoint;
		public static ButcherTableau GaussLegendreMethod2 => new ButcherTableau(2, new[,] { { .25f, .25f - Sqrt3 / 6f }, { .25f + Sqrt3 / 6f, .25f } }, new[] { .5f, .5f }, new[] { .5f + Sqrt3 / 2f, .5f - Sqrt3 / 2f }, new[] { .5f - Sqrt3 / 6f, .5f + Sqrt3 / 6f });
		public static ButcherTableau GaussLegendreMethod3 => new ButcherTableau(3, new[,] {
			{ 5/36f, 2/9f-Sqrt15/15f, 5/36f-Sqrt15/30f },
			{ 5/36f+Sqrt15/24f, 2/9f, 5/36f-Sqrt15/24f },
			{ 5/36f-Sqrt15/30f, 2/9f+Sqrt15/15f, 5/36f }
		}, new[] { 5 / 18f, 4 / 9f, 5 / 18f }, new[] { .5f - Sqrt15 / 10f, .5f, .5f + Sqrt15 / 10f });

		private static readonly float Sqrt3 = (float)Math.Sqrt(3);
		private static readonly float Sqrt15 = (float)Math.Sqrt(15);
	}
}
