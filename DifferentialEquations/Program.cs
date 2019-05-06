using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;

namespace M3.DifferentialEquations {

	public delegate T MapFunc<T>(T x);
	public delegate T TimeFunc<T>(float t, T y);

	public static class Program {
		public static void Main(string[] args) {
			TimeFunc<Vector> f = (t, y) => new Vector(6 * t, (float)Math.Exp(t));
			MapFunc<Vector> df = v => new Vector(6, v.Y);

			var plot = new Plot(0, 20, 50, -2, 18, 50, 20);
			plot.DrawVectorField(df, Color.Aqua);
			plot.DrawCurve(f, Vector.Zero, 0, 3, 0.1f, Color.Black);
			plot.DrawErrorMargin(f, Vector.Zero, t => 0.1f * t * t, 0, 3, 0.01f, Color.FromArgb(40, 255, 0, 0));
			plot.Save("test.png");
			Process.Start("test.png");
		}

		public static void DrawVectorField(this Plot plot, MapFunc<Vector> func, Color color) {
			int xMin = (int)Math.Ceiling(plot.XMin);
			int xMax = (int)Math.Floor(plot.XMax);
			int yMin = (int)Math.Ceiling(plot.YMin);
			int yMax = (int)Math.Floor(plot.YMax);
			for(int x = xMin; x <= xMax; x++) {
				for(int y = yMin; y <= yMax; y++) {
					var p = new Vector(x, y);
					var v = func(p) / 10;
					plot.PlotCurve(new PointF[] { p, p + v }, color);
				}
			}
		}

		public static void DrawCurve(this Plot plot, TimeFunc<Vector> func, Vector y, float a, float b, float step, Color color) {
			int steps = (int)Math.Ceiling((b - a) / step);
			var curve = Enumerable.Range(0, steps).Select(i => (PointF)func(a + step * i, y)).Concat(new[] { (PointF)func(b, y) });
			plot.PlotCurve(curve, color);
		}

		public static void DrawCurve(this Plot plot, TimeFunc<float> func, float y, float a, float b, float step, Color color) {
			int steps = (int)Math.Ceiling((b - a) / step);
			var curve = Enumerable.Range(0, steps).Select(i => new PointF(a + step * i, func(a + step * i, y))).Concat(new[] { new PointF(b, func(b, y)) });
			plot.PlotCurve(curve, color);
		}

		public static void DrawErrorMargin(this Plot plot, TimeFunc<Vector> func, Vector y, Func<float, float> error, float a, float b, float step, Color color) {
			int steps = (int)Math.Ceiling((b - a) / step);
			var region = new Region();
			region.MakeEmpty();
			var path = new GraphicsPath();
			for(int i = 0; i < steps; i++) {
				float t = a + step * i;
				var v = func(t, y);
				float e = error(t);
				path.Reset();
				path.AddEllipse(v.X - e, v.Y - e, e + e, e + e);
				region.Union(path);
				//plot.FillRegion(region, color);
				//region.MakeEmpty();
			}
			plot.FillRegion(region, color);
		}

		public static void DrawDrawErrorMargin(this Plot plot, TimeFunc<float> func, float y, Func<float, float> error, float a, float b, float step, Color color) {
			int steps = (int)Math.Ceiling((b - a) / step);
			var curve = Enumerable.Range(0, steps).Select(i => new PointF(a + step * i, func(a + step * i, y))).Concat(new[] { new PointF(b, func(b, y)) }).ToList();
			var lower = Enumerable.Range(0, steps).Select(i => new PointF(a + step * i, curve[i].Y - error(a + step * i))).Concat(new[] { new PointF(b, curve.Last().Y - error(b)) }).ToList();
			var upper = Enumerable.Range(0, steps).Select(i => new PointF(a + step * i, curve[i].Y + error(a + step * i))).Concat(new[] { new PointF(b, curve.Last().Y + error(b)) }).ToList();
			plot.PlotCurve(lower, Color.Red);
			plot.PlotCurve(upper, Color.Red);
			plot.FillPolygon(lower.Concat(((IEnumerable<PointF>)upper).Reverse()), color);
		}
	}
}
