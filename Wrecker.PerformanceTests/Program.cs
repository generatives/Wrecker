using BenchmarkDotNet.Running;
using System;

namespace Wrecker.PerformanceTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<VoxelMeshingTests>();
        }
    }
}
