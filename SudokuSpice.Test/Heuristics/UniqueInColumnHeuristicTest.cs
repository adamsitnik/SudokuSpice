﻿using System.Collections.Generic;
using Xunit;

namespace SudokuSpice
{
    public class UniqueInColumnHeuristicTest
    {
        [Fact]
        public void UpdateAll_ModifiesRelevantPossibles()
        {
            var puzzle = new Puzzle(new int?[,] {
                {           1, null /* 4 */, null /* 3 */,            2},
                {null /* 2 */, null /* 3 */, null /* 1 */,            4},
                {null /* 4 */, null /* 1 */, null /* 2 */,            3},
                {           3,            2, null /* 4 */,            1}
            });
            var restricts = RestrictUtils.CreateStandardRestricts(puzzle);
            var heuristic = new UniqueInColumnHeuristic(puzzle, (ColumnRestrict)restricts[1]);
            RestrictUtils.RestrictAllUnsetPossibleValues(puzzle, restricts);
            Assert.Equal(0b1100, puzzle.GetPossibleValues(0, 1)); // Pre-modified
            Assert.Equal(0b1001, puzzle.GetPossibleValues(2, 1)); // Pre-modified
            Assert.Equal(0b0101, puzzle.GetPossibleValues(1, 2)); // Pre-modified
            Assert.Equal(0b1010, puzzle.GetPossibleValues(2, 2)); // Pre-modified
            heuristic.UpdateAll();
            Assert.Equal(0b0010, puzzle.GetPossibleValues(1, 0));
            Assert.Equal(0b1000, puzzle.GetPossibleValues(2, 0));
            Assert.Equal(0b1000, puzzle.GetPossibleValues(0, 1)); // Modified
            Assert.Equal(0b0100, puzzle.GetPossibleValues(1, 1));
            Assert.Equal(0b0001, puzzle.GetPossibleValues(2, 1)); // Modified
            Assert.Equal(0b0100, puzzle.GetPossibleValues(0, 2));
            Assert.Equal(0b0001, puzzle.GetPossibleValues(1, 2)); // Modified
            Assert.Equal(0b0010, puzzle.GetPossibleValues(2, 2)); // Modified
            Assert.Equal(0b1000, puzzle.GetPossibleValues(3, 2));
        }
    }
}