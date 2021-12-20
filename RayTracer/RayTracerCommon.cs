namespace Existantial.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	
	static class Surfaces
	{
		// Only works with X-Z plane.
		public static readonly Surface CheckerBoard =
			new Surface() {
				Diffuse = pos => ((Math.Floor(pos.Z) + Math.Floor(pos.X)) % 2 != 0)
									? Color.Make(1, 1, 1)
									: Color.Make(0, 0, 0),
				Specular = pos => Color.Make(1, 1, 1),
				Reflect = pos => ((Math.Floor(pos.Z) + Math.Floor(pos.X)) % 2 != 0)
									? .1
									: .7,
				Roughness = 150
			};


		public static readonly Surface Shiny =
			new Surface() {
				Diffuse = pos => Color.Make(1, 1, 1),
				Specular = pos => Color.Make(.5, .5, .5),
				Reflect = pos => .6,
				Roughness = 50
			};
	}

	class Vector
	{
		public readonly double X;
		public readonly double Y;
		public readonly double Z;

		public Vector(double x, double y, double z) { X = x; Y = y; Z = z; }

		public static Vector Make(double x, double y, double z) { return new Vector(x, y, z); }
		public static Vector Times(double n, Vector v)
		{
			return new Vector(v.X * n, v.Y * n, v.Z * n);
		}
		public static Vector Minus(Vector v1, Vector v2)
		{
			return new Vector(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
		}
		public static Vector Plus(Vector v1, Vector v2)
		{
			return new Vector(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
		}
		public static double Dot(Vector v1, Vector v2)
		{
			return (v1.X * v2.X) + (v1.Y * v2.Y) + (v1.Z * v2.Z);
		}
		public static double Mag(Vector v) { return Math.Sqrt(Dot(v, v)); }
		public static Vector Norm(Vector v)
		{
			double mag = Mag(v);
			double div = mag == 0 ? double.PositiveInfinity : 1 / mag;
			return Times(div, v);
		}
		public static Vector Cross(Vector v1, Vector v2)
		{
			return new Vector(((v1.Y * v2.Z) - (v1.Z * v2.Y)),
							  ((v1.Z * v2.X) - (v1.X * v2.Z)),
							  ((v1.X * v2.Y) - (v1.Y * v2.X)));
		}
		public static bool Equals(Vector v1, Vector v2)
		{
			return (v1.X == v2.X) && (v1.Y == v2.Y) && (v1.Z == v2.Z);
		}
	}

	public class Color
	{
		public double R;
		public double G;
		public double B;

		public Color(double r, double g, double b) { R = r; G = g; B = b; }

		public static Color Make(double r, double g, double b) { return new Color(r, g, b); }

		public static Color Times(double n, Color v)
		{
			return new Color(n * v.R, n * v.G, n * v.B);
		}
		public static Color Times(Color v1, Color v2)
		{
			return new Color(v1.R * v2.R, v1.G * v2.G, v1.B * v2.B);
		}

		public static Color Plus(Color v1, Color v2)
		{
			return new Color(v1.R + v2.R, v1.G + v2.G, v1.B + v2.B);
		}
		public static Color Minus(Color v1, Color v2)
		{
			return new Color(v1.R - v2.R, v1.G - v2.G, v1.B - v2.B);
		}

		public static readonly Color Background = Make(0, 0, 0);
		public static readonly Color DefaultColor = Make(0, 0, 0);

		private double Legalize(double d)
		{
			return d > 1 ? 1 : d;
		}

		public override string ToString()
		{
			return string.Format("[{0},{1},{2}]", R, G, B);
		}
	}

	class Ray
	{
		public Vector Start;
		public Vector Dir;
	}

	class ISect
	{
		public SceneObject Thing;
		public Ray Ray;
		public double Dist;
	}

	class Surface
	{
		public Func<Vector, Color> Diffuse;
		public Func<Vector, Color> Specular;
		public Func<Vector, double> Reflect;
		public double Roughness;
	}

	class Camera
	{
		public Vector Pos;
		public Vector Forward;
		public Vector Up;
		public Vector Right;

		public static Camera Create(Vector pos, Vector lookAt)
		{
			Vector forward = Vector.Norm(Vector.Minus(lookAt, pos));
			Vector down = new Vector(0, -1, 0);
			Vector right = Vector.Times(1.5, Vector.Norm(Vector.Cross(forward, down)));
			Vector up = Vector.Times(1.5, Vector.Norm(Vector.Cross(forward, right)));

			return new Camera() { Pos = pos, Forward = forward, Up = up, Right = right };
		}
	}

	class Light
	{
		public Vector Pos;
		public Color Color;
	}

	abstract class SceneObject
	{
		public Surface Surface;
		public abstract ISect Intersect(Ray ray);
		public abstract Vector Normal(Vector pos);
	}

	class Sphere : SceneObject
	{
		public Vector Center;
		public double Radius;

		public override ISect Intersect(Ray ray)
		{
			Vector eo = Vector.Minus(Center, ray.Start);
			double v = Vector.Dot(eo, ray.Dir);
			double dist;
			if (v < 0)
			{
				dist = 0;
			}
			else
			{
				double disc = Math.Pow(Radius, 2) - (Vector.Dot(eo, eo) - Math.Pow(v, 2));
				dist = disc < 0 ? 0 : v - Math.Sqrt(disc);
			}
			if (dist == 0)
				return null;
			return new ISect() {
				Thing = this,
				Ray = ray,
				Dist = dist
			};
		}

		public override Vector Normal(Vector pos)
		{
			return Vector.Norm(Vector.Minus(pos, Center));
		}
	}

	class Plane : SceneObject
	{
		public Vector Norm;
		public double Offset;

		public override ISect Intersect(Ray ray)
		{
			double denom = Vector.Dot(Norm, ray.Dir);
			if (denom > 0)
				return null;
			return new ISect() {
				Thing = this,
				Ray = ray,
				Dist = (Vector.Dot(Norm, ray.Start) + Offset) / (-denom)
			};
		}

		public override Vector Normal(Vector pos)
		{
			return Norm;
		}
	}
}