﻿using System;
using Xunit;

namespace SudokuSpice.RuleBased.Test
{
    public class PuzzleGeneratorTest
    {
        [Fact]
        public void Constructor_WithValidArgs_Works()
        {
            var generator = new PuzzleGenerator<Puzzle>(
                () => new Puzzle(9), puzzle => new PuzzleSolver(puzzle));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(4, 10)]
        [InlineData(9, 30)]
        public void Generate_CreatesPuzzleWithUniqueSolution(int size, int numToSet)
        {
            var generator = new PuzzleGenerator<Puzzle>(
                () => new Puzzle(size), puzzle => new PuzzleSolver(puzzle));

            Puzzle puzzle = generator.Generate(numToSet, TimeSpan.FromSeconds(60));

            Assert.Equal(size * size - numToSet, puzzle.NumEmptySquares);
            var solver = new PuzzleSolver(puzzle);
            SolveStats stats = solver.GetStatsForAllSolutions();
            Assert.Equal(1, stats.NumSolutionsFound);
        }

        [Fact]
        public void Generate_WithShortTimeout_ThrowsTimeoutException()
        {
            var generator = new PuzzleGenerator<Puzzle>(
                () => new Puzzle(9), puzzle => new PuzzleSolver(puzzle));

            Assert.Throws<TimeoutException>(
                () => generator.Generate(17, TimeSpan.FromMilliseconds(1)));
        }
    }
}
