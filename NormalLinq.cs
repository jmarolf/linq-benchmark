using System;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using Existantial.Common;
using Existantial.Linq;

namespace Existantial
{
    public partial class RayTraceLinq
    {
        [Benchmark(Baseline = true)]
        public void NormalLinq()
        {
            const int width = 80;
            const int height = 60;

            var rayTracer = new LinqRayTracer(width, height,
                (int x, int y, Color color) => {});
            rayTracer.Render(rayTracer.DefaultScene);
        }
    }
}