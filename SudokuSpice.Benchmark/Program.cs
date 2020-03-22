﻿using BenchmarkDotNet.Running;

namespace SudokuSpice.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromTypes(new[] {
                typeof(PuzzleBenchmarker),
                typeof(LegacyCsvBenchmarker),
                typeof(GeneratorBenchmarker)
            }).Run(args);
        }
    }
}
