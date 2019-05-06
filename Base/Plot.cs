using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace M3 {
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

		public void PlotCurve(IEnumerable<PointF> points, Color color) {
			_graphics.DrawLines(new Pen(color, 3), points.Select(ToImageSpace).ToArray());
		}

		public void PlotFunction(Func<float, float> func, Color color, DashStyle dash = DashStyle.Solid) {
			var curve = Enumerable.Range(0, Width + 1).Select(i => new PointF(i * XRange / (Width + 1) + XMin, func(i / XScale + XMin).Clamp(2 * YMin - YMax, 2 * YMax + YMin))).Select(ToImageSpace).ToArray();
			_graphics.DrawLines(new Pen(color, 3) { DashStyle = dash }, curve);
		}

		public void DrawBezier(PointF[] controlPoints, Color color, DashStyle dash = DashStyle.Solid, int resolution = 100) {
			int n = controlPoints.Length - 1;
			var curve = new PointF[resolution + 1];

			for(int i = 0; i < n; i++) {
				_graphics.DrawLine(Pens.CadetBlue, ToImageSpace(controlPoints[i]), ToImageSpace(controlPoints[i + 1]));
			}

			for(int k = 0; k <= resolution; k++) {
				float t = (float)k / resolution;
				var prev = controlPoints;
				for(int s = n; s > 0; s--) {
					// s: number of segments
					var lerped = new PointF[s];
					for(int i = 0; i < s; i++) {
						var sp = prev[i];
						var ep = prev[i + 1];
						var cp = Lerp(sp, ep, t);
						//_graphics.DrawLine(Pens.CadetBlue, ToImageSpace(sp), ToImageSpace(ep));
						//PlotPoints(new[] { cp }, Color.CadetBlue);
						lerped[i] = cp;
					}
					prev = lerped;
				}
				curve[k] = prev[0];
			}

			_graphics.DrawLines(new Pen(color, 3) { DashStyle = dash }, curve.Select(ToImageSpace).ToArray());
		}

		public void FillPolygon(IEnumerable<PointF> points, Color color) {
			_graphics.FillPolygon(new SolidBrush(color), points.Select(ToImageSpace).ToArray());
		}

		public void FillRegion(Region region, Color color) {
			var tl = new PointF(XMin, YMax);
			var tr = new PointF(XMax, YMax);
			var bl = new PointF(XMin, YMin);
			var br = new PointF(XMax, YMin);
			var size = new SizeF(tr.X - bl.X, tr.Y - bl.Y);
			region.Transform(new Matrix(new RectangleF(bl, size), new[] { ToImageSpace(bl), ToImageSpace(br), ToImageSpace(tl) }));
			_graphics.FillRegion(new SolidBrush(color), region);
		}

		public void Save(string path) {
			using(var copy = new Bitmap(_bitmap.Width + Border * 2, _bitmap.Height + Border * 2))
			using(var gfx = Graphics.FromImage(copy)) {
				gfx.Clear(Color.White);
				gfx.DrawImageUnscaled(_bitmap, Border, Border);
				gfx.DrawRectangle(new Pen(Color.Black, 3), Border, Border, Width, Height);
				//copy.RotateFlip(RotateFlipType.RotateNoneFlipY);
				copy.Save(path);
			}
		}

		public void Clear() {
			_graphics.Clear(Color.White);
		}

		public PointF ToImageSpace(PointF point) {
			float x = (point.X - XMin) * XScale;
			float y = (YMax - point.Y) * YScale;
			return new PointF(x, y);
		}

		public static PointF Lerp(PointF a, PointF b, float p) => new PointF(a.X + (b.X - a.X) * p, a.Y + (b.Y - a.Y) * p);
	}
}