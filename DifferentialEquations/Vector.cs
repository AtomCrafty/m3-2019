using System;
using System.Drawing;

namespace M3.DifferentialEquations {
	public struct Vector {
		public static readonly Vector Zero = default;

		public readonly float X;
		public readonly float Y;

		public Vector(float x, float y) {
			X = x;
			Y = y;
		}

		public float Length => (float)Math.Sqrt(X * X + Y * Y);

		public static Vector operator +(Vector v) => v;
		public static Vector operator -(Vector v) => new Vector(-v.X, -v.Y);
		public static Vector operator +(Vector l, Vector r) => new Vector(l.X + r.X, l.Y + r.Y);
		public static Vector operator -(Vector l, Vector r) => new Vector(l.X - r.X, l.Y - r.Y);
		public static Vector operator *(Vector v, float s) => new Vector(v.X * s, v.Y * s);
		public static Vector operator *(float s, Vector v) => new Vector(v.X * s, v.Y * s);
		public static Vector operator /(Vector v, float s) => new Vector(v.X / s, v.Y / s);
		public static implicit operator PointF(Vector v) => new PointF(v.X, v.Y);
		public static implicit operator Vector(PointF p) => new Vector(p.X, p.Y);
	}
}