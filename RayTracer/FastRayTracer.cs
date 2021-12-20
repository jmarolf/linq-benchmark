using System.Linq;
using Fast.Collections;
using Fast.Collections.Generic;
using Fast.Linq;

namespace Fast {
    using System;
    using Existantial.Common;
    
    class Scene
    {
	    public FastList<SceneObject> Things;
	    public FastList<Light> Lights;
	    public Camera Camera;

	    public IFastEnumerable<ISect, int> Intersect(Ray r)
	    {
		    var list = new FastList<ISect>();
		    var enumerator = Things.Start;
		    while (Things.TryGetNext(ref enumerator, out var thing))
		    {
			    list.Add(thing.Intersect(r));
		    }

		    return list;
	    }
    }
    
    public class FastRayTracer
    {
        private int screenWidth;
        private int screenHeight;
        private const int MaxDepth = 5;

        public Action<int, int, Color> setPixel;
        
        public FastRayTracer(int screenWidth, int screenHeight, Action<int, int, Color> setPixel)
        {
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.setPixel = setPixel;
        }
        
        class TraceRayArgs
        {
            public readonly Ray Ray;
            public readonly Scene Scene;
            public readonly int Depth;

            public TraceRayArgs(Ray ray, Scene scene, int depth) { Ray = ray; Scene = scene; Depth = depth; }
        }

        internal void Render(Scene scene)
        {
            var pixelsQuery = FastEnumerable.Range(0, screenHeight)
				.Select(y => (y, recenterY: -(y - (screenHeight / 2.0)) / (2.0 * screenHeight)))
				.Select(@t => FastEnumerable.Range(0, screenWidth)
					.Select(x => (x, recenterX: (x - (screenWidth / 2.0)) / (2.0 * screenWidth)))
					.Select(@t1 =>
					(
						@t1,
						point: Vector.Norm(Vector.Plus(scene.Camera.Forward,
							Vector.Plus(Vector.Times(@t1.recenterX, scene.Camera.Right),
								Vector.Times(@t.recenterY, scene.Camera.Up))))
					))
					.Select(@t1 => (@t1, ray: new Ray() {Start = scene.Camera.Pos, Dir = @t1.point}))
					.Select(@t1 =>
					(
						@t1,
						computeTraceRay: (Func<Func<TraceRayArgs, Color>, Func<TraceRayArgs, Color>>) (f =>
							traceRayArgs =>
								((traceRayArgs.Scene.Things.Select(thing => thing.Intersect(traceRayArgs.Ray)))
									.Where(isect => isect != null)
									.OrderBy(isect => isect.Dist)
									.Select(isect => (isect, d: isect.Ray.Dir))
									.Select(@t2 => 
									(
										@t2,
										pos: Vector.Plus(Vector.Times(@t2.isect.Dist, @t2.isect.Ray.Dir),
											@t2.isect.Ray.Start)
									))
									.Select(@t2 => (@t2, normal: @t2.@t2.isect.Thing.Normal(@t2.pos)))
									.Select(@t2 => 
									(
										@t2,
										reflectDir: Vector.Minus(@t2.@t2.@t2.d,
											Vector.Times(2 * Vector.Dot(@t2.normal, @t2.@t2.@t2.d), @t2.normal))
									))
									.Select(@t2 =>
									(
										@t2,
										naturalColors: traceRayArgs.Scene.Lights
											.Select(light =>
												(light, ldis: Vector.Minus(light.Pos, @t2.@t2.@t2.pos)))
											.Select(@t3 => (@t3, livec: Vector.Norm(@t3.ldis)))
											.Select(@t3 =>
											(
												@t3, testRay: new Ray() {Start = @t2.@t2.@t2.pos, Dir = @t3.livec}
											))
											.Select(@t3 =>
											(
												@t3,
												testIsects:
													(traceRayArgs.Scene.Things.Select(thing =>
														thing.Intersect(@t3.testRay)))
													.Where(inter => inter != null)
													.OrderBy(inter => inter.Dist)
											))
											.Select(@t3 => (@t3, testIsect: @t3.testIsects.FirstOrDefault()))
											.Select(@t3 =>
											(
												@t3, neatIsect: @t3.testIsect == null ? 0 : @t3.testIsect.Dist
											))
											.Select(@t3 => 
											(
												@t3,
												isInShadow:
													!((@t3.neatIsect > Vector.Mag(@t3.@t3.@t3.@t3.@t3.@t3.ldis)) ||
													  (@t3.neatIsect == 0))
											))
											.Where(@t3 => !@t3.isInShadow)
											.Select(@t3 => 
											(
												@t3,
												illum: Vector.Dot(@t3.@t3.@t3.@t3.@t3.@t3.livec, @t2.@t2.normal)
											))
											.Select(@t3 => 
											(
												@t3,
												lcolor: @t3.illum > 0
													? Color.Times(@t3.illum,
														@t3.@t3.@t3.@t3.@t3.@t3.@t3.@t3.light.Color)
													: Color.Make(0, 0, 0)
											))
											.Select(@t3 => 
											(
												@t3,
												specular: Vector.Dot(@t3.@t3.@t3.@t3.@t3.@t3.@t3.@t3.livec,
													Vector.Norm(@t2.reflectDir))
											))
											.Select(@t3 => 
											(
												@t3,
												scolor: @t3.specular > 0
													? Color.Times(
														Math.Pow(@t3.specular,
															@t2.@t2.@t2.@t2.isect.Thing.Surface.Roughness),
														@t3.@t3.@t3.@t3.@t3.@t3.@t3.@t3.@t3.@t3.light.Color)
													: Color.Make(0, 0, 0)
											))
											.Select(@t3 =>
												Color.Plus(
													Color.Times(
														@t2.@t2.@t2.@t2.isect.Thing.Surface.Diffuse(@t2.@t2.@t2.pos),
														@t3.@t3.@t3.lcolor),
													Color.Times(
														@t2.@t2.@t2.@t2.isect.Thing.Surface.Specular(@t2.@t2.@t2.pos),
														@t3.scolor)))
									))
									.Select(@t2 => 
									(
										@t2,
										reflectPos: Vector.Plus(@t2.@t2.@t2.@t2.pos,
											Vector.Times(.001, @t2.@t2.reflectDir))
									))
									.Select(@t2 => 
									(
										@t2,
										reflectColor: traceRayArgs.Depth >= MaxDepth
											? Color.Make(.5, .5, .5)
											: Color.Times(
												@t2.@t2.@t2.@t2.@t2.@t2.isect.Thing.Surface.Reflect(@t2.reflectPos),
												f(new TraceRayArgs(
													new Ray()
													{
														Start = @t2.reflectPos, Dir = @t2.@t2.@t2.reflectDir
													}, traceRayArgs.Scene, traceRayArgs.Depth + 1)))
									))
									.Select(@t2 => @t2.@t2.@t2.naturalColors.Aggregate(@t2.reflectColor,
										(color, natColor) => Color.Plus(color, natColor))))
								.DefaultIfEmpty(Color.Background)
								.First())
					))
					.Select(@t1 => (@t1, traceRay: Y(@t1.computeTraceRay)))
					.Select(@t1 => 
					(
						X: @t1.@t1.@t1.@t1.@t1.x,
						Y: @t.y,
						Color: @t1.traceRay(new TraceRayArgs(@t1.@t1.@t1.ray, scene, 0))
					)));

            var pixelEnumerator = pixelsQuery.Start;
            while (pixelsQuery.TryGetNext(ref pixelEnumerator, out var row))
            {
	            var rowEnumerator = row.Start;
	            while (row.TryGetNext(ref rowEnumerator, out var pixel))
	            {
		            setPixel(pixel.X, pixel.Y, pixel.Color);
	            }
            }
        }

        public static Func<T, U> Y<T, U>(Func<Func<T, U>, Func<T, U>> f)
        {
	        Func<Wrap<Func<T, U>>, Func<T, U>> g = wx => f(wx.It(wx));
	        return g(new Wrap<Func<T, U>>(wx => f(y => wx.It(wx)(y))));
        }
        
        private class Wrap<T>
        {
	        public readonly Func<Wrap<T>, T> It;
	        public Wrap(Func<Wrap<T>, T> it) { It = it; }
        }

        private static Scene GetDefaultScene()
        {
	        var things = new  FastList<SceneObject>();
	        things.Add(
		    new Plane() {
		        Norm = Vector.Make(0,1,0),
		        Offset = 0,
		        Surface = Surfaces.CheckerBoard
	        });
	        things.Add(
		        new Sphere() {
			        Center = Vector.Make(0,1,0),
			        Radius = 1,
			        Surface = Surfaces.Shiny
		        });
	        things.Add(
		        new Sphere() {
			        Center = Vector.Make(-1,.5,1.5),
			        Radius = .5,
			        Surface = Surfaces.Shiny
		        });
	        
	        var lights = new FastList<Light> ();
	        lights.Add(new Light() {
			        Pos = Vector.Make(-2,2.5,0),
			        Color = Color.Make(.49,.07,.07)
		        });
	        lights.Add(new Light() {
		        Pos = Vector.Make(1.5,2.5,1.5),
		        Color = Color.Make(.07,.07,.49)
	        });
	        lights.Add(new Light() {
		        Pos = Vector.Make(1.5,2.5,-1.5),
		        Color = Color.Make(.07,.49,.071)
	        });
	        lights.Add(new Light() {
		        Pos = Vector.Make(0,3.5,0),
		        Color = Color.Make(.21,.21,.35)
	        });
	        
	        return new Scene()
	        {
		        Things = things,
		        Lights = lights,
		        Camera = Camera.Create(Vector.Make(3, 2, 4), Vector.Make(-1, .5, 0))
	        };
        }

        internal readonly Scene DefaultScene = GetDefaultScene();
    }
}