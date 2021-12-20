using System;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using Existantial.Common;
using Fast;

namespace Existantial
{
    public partial class RayTraceLinq
    {
        [Benchmark]
        public void FastLinq()
        {
            const int width = 80;
            const int height = 60;

            var rayTracer = new FastRayTracer(width, height,
                (int x, int y, Color color) => {});
            rayTracer.Render(rayTracer.DefaultScene);
        }
    }
}
