using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Interpolation {
	public static class Program {
		public static void Main(string[] args) {
			Func<float, float> f1 = f => (float)Math.Sin(f);
			Func<float, float> f2 = f => (float)Math.Cos(f);
			Func<float, float> f3 = f => (float)Math.Tan(f);
			Func<float, float> f4 = f => 1 / (1 + f * f);

			Approximate(f1, Enumerable.Range(0, 9).Select(i => i * 2f - 8f).ToArray());
		}

		public static void Approximate(Func<float, float> func, IEnumerable<float> samplePoints) {
			var fixPoints = samplePoints.Select(x => new PointF(x, func(x))).ToArray();
			//fixPoints = new[] {
			//	new PointF(0, +0f),
			//	new PointF(1, +1f),
			//	new PointF(2, -1f),
			//	new PointF(3, +2f),
			//	new PointF(4, +0f),
			//	new PointF(5, +1f),
			//};

			var plot = new Plot(-10.2f, 10.2f, 100, -2.2f, 2.2f, 100, 40);
			plot.PlotFunction(func, Color.Gray);
			//plot.PlotFunction(Lagrange(fixPoints), Color.DodgerBlue);
			//plot.PlotFunction(Newton(fixPoints), Color.LimeGreen, DashStyle.Dot);
			plot.PlotFunction(LinearSpline(fixPoints), Color.Red);
			plot.PlotFunction(CubicSpline(fixPoints), Color.LimeGreen, DashStyle.Dash);
			plot.PlotFunction(CubicSpline(fixPoints), Color.LimeGreen, DashStyle.Dash);
			plot.PlotPoints(fixPoints, Color.Gray);
			plot.Save("interpolation.png");
			Process.Start("interpolation.png");
		}

		public static Func<float, float> Lagrange(PointF[] fixPoints) {
			return f => Enumerable.Range(0, fixPoints.Length).Sum(i => fixPoints[i].Y * L(i, fixPoints.Length)(f));

			Func<float, float> L(int k, int n) => x =>
				Enumerable.Range(0, n)
					.Where(i => i != k)
					.Select(i => (x - fixPoints[i].X) / (fixPoints[k].X - fixPoints[i].X))
					.Product();
		}

		public static Func<float, float> Newton(PointF[] fixPoints) {
			int n = fixPoints.Length - 1;
			var x = fixPoints.Select(p => p.X).ToArray();
			var y = fixPoints.Select(p => p.Y).ToArray();
			var γ = CalcCoefficients();

			//Console.WriteLine(string.Join(" + ", Enumerable.Range(0, n + 1).Select(i => γ[i] + string.Join("", Enumerable.Range(0, i).Select(j => $"(x - x_{j})")))));

			return f => Enumerable.Range(0, n + 1).Select(i => γ[i] * Enumerable.Range(0, i).Select(j => f - x[j]).Product()).Sum();

			float[] CalcCoefficients() {
				var res = new float[n + 1];
				var step = y;

				res[0] = step[0];
				for(int i = 1; i < n + 1; i++) {
					int stage = n + 2 - step.Length;
					var next = new float[step.Length - 1];
					for(int j = 0; j < next.Length; j++) {
						next[j] = (step[j + 1] - step[j]) / (x[j + stage] - x[j]);
					}

					step = next;
					res[i] = step[0];
				}

				return res;
			}
		}

		public static Func<float, float> LinearSpline(PointF[] fixpoints) {
			(int startIndex, int endIndex, float start, float end)[] segments = Enumerable.Range(0, fixpoints.Length - 1).Select(i => (i, i + 1, fixpoints[i].X, fixpoints[i + 1].X)).ToArray();

			return f => {
				foreach(var segment in segments) {
					if(f < segment.start || f >= segment.end) continue;

					float x1 = segment.start;
					float x2 = segment.end;
					float y1 = fixpoints[segment.startIndex].Y;
					float y2 = fixpoints[segment.endIndex].Y;

					return (x2 - f) / (x2 - x1) * y1 + (f - x1) / (x2 - x1) * y2;
				}

				if(f < fixpoints.First().X) return fixpoints.First().Y;
				if(f >= fixpoints.Last().X) return fixpoints.Last().Y;
				throw new Exception("Should never end up here");
			};
		}

		public static Func<float, float> CubicSpline(PointF[] fixpoints) {
			int n = fixpoints.Length - 1;
			var x = fixpoints.Select(point => point.X).ToArray();
			var y = fixpoints.Select(point => point.Y).ToArray();

			(int startIndex, int endIndex, float start, float end, float length)[] segments =
				Enumerable.Range(0, n).Select(i => (i, i + 1, x[i], x[i + 1], x[i + 1] - x[i])).ToArray();

			var h = segments.Select(segment => segment.length).ToArray();

			var solution = new float[n + 1];
			var coefficients = new float[n + 1, n + 1];
			// natural constraints
			coefficients[0, 0] = 1;
			coefficients[n, n] = 1;

			/** // hermite constraint (can't use this because it requires the first derivative)
			 * coefficients[0, 0] = h[0] / 3;
			 * coefficients[0, 1] = h[0] / 6;
			 * coefficients[n, n] = h[n - 1] / 3;
			 * coefficients[n, n - 1] = h[n - 1] / 6;
			 * solution[0] = (y[1] - y[0]) / h[0] - fd(fixpoints.First().X);
			 * solution[n] = fd(fixpoints.Last().X) - (y[n] - y[n - 1]) / h[n - 1];
			 */

			// calculate coefficient matrix and solution vector
			for(int row = 1; row < n; row++) {
				coefficients[row, row - 1] = h[row - 1] / 6;
				coefficients[row, row] = (h[row - 1] + h[row]) / 3;
				coefficients[row, row + 1] = h[row] / 6;

				solution[row] = (y[row + 1] - y[row]) / h[row] - (y[row] - y[row - 1]) / h[row - 1];
			}

#if DEBUG
			for(int i = 0; i < coefficients.GetLength(0); i++) {
				for(int j = 0; j < coefficients.GetLength(1); j++) {
					Console.Write($"{coefficients[i, j] * 6:0.00}".Replace(',', '.').TrimEnd('0').TrimEnd('.').PadRight(6));
				}
				Console.WriteLine(" ->  " + solution[i]);
			}
			Console.WriteLine();
#endif

			var moments = Solve(coefficients, solution);

			return f => {
				foreach(var segment in segments) {
					if(f < segment.start || f >= segment.end) continue;

					float l = segment.length;
					float x1 = segment.start;
					float x2 = segment.end;
					float y1 = fixpoints[segment.startIndex].Y;
					float y2 = fixpoints[segment.endIndex].Y;
					float m1 = moments[segment.startIndex];
					float m2 = moments[segment.endIndex];

					float h1 = h[segment.startIndex];

					float c = (y2 - y1) / h1 - h1 / 6 * (m2 - m1);
					float d = y1 - h1 * h1 / 6 * m1;

					float t1 = x2 - f;
					float t2 = f - x1;
					return (t1 * t1 * t1 / l * m1 + t2 * t2 * t2 / l * m2) / 6 + (f - x1) * c + d;
				}

				if(f < fixpoints.First().X) return fixpoints.First().Y;
				if(f >= fixpoints.Last().X) return fixpoints.Last().Y;
				throw new Exception("Should never end up here");
			};
		}

		public static float[] Solve(float[,] m, float[] b) {
			var inverse = Matrix.Invert(m);
			var bMatrix = new float[b.Length, 1];
			for(int i = 0; i < b.Length; i++) {
				bMatrix[i, 0] = b[i];
			}
			var rMatrix = Matrix.Multiply(inverse, bMatrix);
			var result = new float[b.Length];
			for(int i = 0; i < b.Length; i++) {
				result[i] = rMatrix[i, 0];
			}
			return result;
		}
	}

	public class Plot {
		public readonly float XMin, XMax, XScale, XRange;
		public readonly float YMin, YMax, YScale, YRange;
		public readonly int Border, Width, Height;

		private readonly Bitmap _bitmap;
		private readonly Graphics _graphics;

		public Plot(float xMin, float xMax, float xScale, float yMin, float yMax, float yScale, int border) {
			XMin = xMin;
			XMax = xMax;
			XScale = xScale;
			XRange = xMax - xMin;
			YMin = yMin;
			YMax = yMax;
			YScale = yScale;
			YRange = yMax - yMin;
			Border = border;
			Width = (int)((XMax - XMin) * XScale);
			Height = (int)((YMax - YMin) * YScale);
			_bitmap = new Bitmap(Width, Height);
			_graphics = Graphics.FromImage(_bitmap);
			_graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Clear();
			DrawGrid();
		}

		private void DrawGrid() {
			int xMin = (int)Math.Ceiling(XMin);
			int xMax = (int)Math.Floor(XMax);
			int yMin = (int)Math.Ceiling(YMin);
			int yMax = (int)Math.Floor(YMax);
			for(int i = xMin; i <= xMax; i++) {
				var start = ToImageSpace(new PointF(i, YMin));
				var end = ToImageSpace(new PointF(i, YMax));
				_graphics.DrawLine(new Pen(Color.WhiteSmoke, 3) { DashStyle = DashStyle.Dash }, start, end);
			}

			for(int i = yMin; i <= yMax; i++) {
				var start = ToImageSpace(new PointF(XMin, i));
				var end = ToImageSpace(new PointF(XMax, i));
				_graphics.DrawLine(new Pen(Color.WhiteSmoke, 3) { DashStyle = DashStyle.Dash }, start, end);
			}
		}

		public void PlotPoints(IEnumerable<PointF> points, Color color, float diameter = 12) {
			foreach(var point in points.Select(ToImageSpace)) {
				_graphics.FillEllipse(new SolidBrush(color), point.X - diameter / 2, point.Y - diameter / 2, diameter, diameter);
			}
		}

		public void PlotFunction(Func<float, float> func, Color color, DashStyle dash = DashStyle.Solid) {
			var curve = Enumerable.Range(0, Width + 1).Select(i => new PointF(i * XRange / (Width + 1) + XMin, func(i / XScale + XMin).Clamp(2 * YMin - YMax, 2 * YMax + YMin))).Select(ToImageSpace).ToArray();
			_graphics.DrawLines(new Pen(color, 3) { DashStyle = dash }, curve);
		}

		public void Save(string path) {
			using(var copy = new Bitmap(_bitmap.Width + Border * 2, _bitmap.Height + Border * 2))
			using(var gfx = Graphics.FromImage(copy)) {
				gfx.Clear(Color.White);
				gfx.DrawImageUnscaled(_bitmap, Border, Border);
				gfx.DrawRectangle(new Pen(Color.Black, 3), Border, Border, Width, Height);
				copy.RotateFlip(RotateFlipType.RotateNoneFlipY);
				copy.Save(path);
			}
		}

		public void Clear() {
			_graphics.Clear(Color.White);
		}

		public PointF ToImageSpace(PointF point) {
			float x = (point.X - XMin) * XScale;
			float y = (point.Y - YMin) * YScale;
			return new PointF(x, y);
		}
	}

	public static class Extensions {
		public static float Clamp(this float value, float min, float max) => Math.Min(Math.Max(value, min), max);
		public static float Product(this IEnumerable<float> enumerable) => enumerable.Aggregate(1f, (f1, f2) => f1 * f2);

		public static float[] GetRow(this float[,] m, int row) {
			var buffer = new float[m.GetLength(1)];
			for(int i = 0; i < buffer.Length; i++) {
				buffer[i] = m[row, i];
			}
			return buffer;
		}
	}

	public static class Matrix {

		public static float[,] Identity(int n) {
			var result = new float[n, n];
			for(int i = 0; i < n; ++i)
				result[i, i] = 1.0f;

			return result;
		}

		public static float[,] Multiply(float[,] matrixA, float[,] matrixB) {
			int aRows = matrixA.GetLength(0); int aCols = matrixA.GetLength(1);
			int bRows = matrixB.GetLength(0); int bCols = matrixB.GetLength(1);
			if(aCols != bRows)
				throw new Exception("Non-conformable matrices in Multiply");

			float[,] result = new float[aRows, bCols];

			for(int i = 0; i < aRows; ++i) // each row of A
				for(int j = 0; j < bCols; ++j) // each col of B
					for(int k = 0; k < aCols; ++k) // could use k less-than bRows
						result[i, j] += matrixA[i, k] * matrixB[k, j];

			return result;
		}

		public static float[,] Invert(float[,] matrix) {
			int n = matrix.GetLength(0);
			if(matrix.GetLength(1) != n) throw new Exception("Unable to invert non-square matrix");
			var result = (float[,])matrix.Clone();

			var lum = Decompose(matrix, out int[] perm, out int _);
			if(lum == null) throw new Exception("Unable to compute inverse");

			var b = new float[n];
			for(int i = 0; i < n; ++i) {
				for(int j = 0; j < n; ++j) {
					if(i == perm[j])
						b[j] = 1.0f;
					else
						b[j] = 0.0f;
				}

				var x = HelperSolve(lum, b);

				for(int j = 0; j < n; ++j)
					result[j, i] = x[j];
			}
			return result;
		}

		public static float[] HelperSolve(float[,] luMatrix, float[] b) {
			// before calling this helper, permute b using the perm array
			// from Decompose that generated luMatrix
			int n = luMatrix.GetLength(0);
			float[] x = new float[n];
			b.CopyTo(x, 0);

			for(int i = 1; i < n; ++i) {
				float sum = x[i];
				for(int j = 0; j < i; ++j)
					sum -= luMatrix[i, j] * x[j];
				x[i] = sum;
			}

			x[n - 1] /= luMatrix[n - 1, n - 1];
			for(int i = n - 2; i >= 0; --i) {
				float sum = x[i];
				for(int j = i + 1; j < n; ++j)
					sum -= luMatrix[i, j] * x[j];
				x[i] = sum / luMatrix[i, i];
			}

			return x;
		}

		public static float[,] Decompose(float[,] matrix, out int[] perm, out int toggle) {
			// Doolittle LUP decomposition with partial pivoting.
			// rerturns: result is L (with 1s on diagonal) and U;
			// perm holds row permutations; toggle is +1 or -1 (even or odd)
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1); // assume square
			if(rows != cols)
				throw new Exception("Attempt to decompose a non-square m");

			int n = rows; // convenience

			var result = (float[,])matrix.Clone();

			perm = new int[n]; // set up row permutation result
			for(int i = 0; i < n; ++i) { perm[i] = i; }

			toggle = 1; // toggle tracks row swaps.
						// +1 -greater-than even, -1 -greater-than odd. used by MatrixDeterminant

			for(int j = 0; j < n - 1; ++j) // each column
			{
				float colMax = Math.Abs(result[j, j]); // find largest val in col
				int pRow = j;
				//for (int i = j + 1; i less-than n; ++i)
				//{
				//  if (result[i,j] greater-than colMax)
				//  {
				//    colMax = result[i,j];
				//    pRow = i;
				//  }
				//}

				// reader Matt V needed this:
				for(int i = j + 1; i < n; ++i) {
					if(Math.Abs(result[i, j]) > colMax) {
						colMax = Math.Abs(result[i, j]);
						pRow = i;
					}
				}
				// Not sure if this approach is needed always, or not.

				if(pRow != j) // if largest value not on pivot, swap rows
				{
					SwapRows(result, pRow, j);

					int tmp = perm[pRow]; // and swap perm info
					perm[pRow] = perm[j];
					perm[j] = tmp;

					toggle = -toggle; // adjust the row-swap toggle
				}

				// --------------------------------------------------
				// This part added later (not in original)
				// and replaces the 'return null' below.
				// if there is a 0 on the diagonal, find a good row
				// from i = j+1 down that doesn't have
				// a 0 in column j, and swap that good row with row j
				// --------------------------------------------------

				if(Math.Abs(result[j, j]) < 0.00001) {
					// find a good row to swap
					int goodRow = -1;
					for(int row = j + 1; row < n; ++row) {
						if(Math.Abs(result[row, j]) > 0.00001)
							goodRow = row;
					}

					if(goodRow == -1)
						throw new Exception("Cannot use Doolittle's method");

					// swap rows so 0.0 no longer on diagonal
					SwapRows(result, goodRow, j);

					int tmp = perm[goodRow]; // and swap perm info
					perm[goodRow] = perm[j];
					perm[j] = tmp;

					toggle = -toggle; // adjust the row-swap toggle
				}
				// --------------------------------------------------
				// if diagonal after swap is zero . .
				//if (Math.Abs(result[j,j]) less-than 1.0E-20) 
				//  return null; // consider a throw

				for(int i = j + 1; i < n; ++i) {
					result[i, j] /= result[j, j];
					for(int k = j + 1; k < n; ++k) {
						result[i, k] -= result[i, j] * result[j, k];
					}
				}


			} // main j column loop

			return result;
		}

		public static void SwapRows(float[,] m, int r1, int r2) {
			for(int i = 0; i < m.GetLength(1); i++) {
				float t = m[r1, i];
				m[r1, i] = m[r2, i];
				m[r2, i] = t;
			}
		}
	}
}