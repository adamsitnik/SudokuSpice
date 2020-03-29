﻿using SudokuSpice.Data;
using System;
using System.Collections.Generic;
using Xunit;

namespace SudokuSpice.Rules.Test
{
    public class RowUniquenessRuleTest
    {
        [Fact]
        public void Constructor_FiltersCorrectly()
        {
            var puzzle = new Puzzle(new int?[,] {
                {           1, null /* 4 */, null /* 3 */,            2},
                {null /* 2 */, null /* 3 */,            1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {           3,            2,            4,            1}
            });
            var rule = new RowUniquenessRule(puzzle);
            Assert.Equal(new BitVector(0b1100), rule.GetPossibleValues(new Coordinate(0, 0)));
            Assert.Equal(new BitVector(0b1110), rule.GetPossibleValues(new Coordinate(1, 0)));
            Assert.Equal(new BitVector(0b1111), rule.GetPossibleValues(new Coordinate(2, 0)));
            Assert.Equal(new BitVector(0b0000), rule.GetPossibleValues(new Coordinate(3, 0)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(9)]
        [InlineData(25)]
        public void Constructor_AcceptsValidPuzzleSizes(int size)
        {
            var matrix = new int?[size, size];
            var puzzle = new Puzzle(matrix);
            var rule = new RowUniquenessRule(puzzle);
            Assert.NotNull(rule);
        }

        [Fact]
        public void Constructor_WithDuplicateValueInRow_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                var rule = new RowUniquenessRule(
                    new Puzzle(
                        new int?[,] {
                            {           1, /* INCORRECT */ 1, null /* 3 */,            2},
                            {null /* 2 */,      null /* 3 */,            1, null /* 4 */},
                            {null /* 4 */,      null /* 1 */, null /* 2 */, null /* 3 */},
                            {           3,                 2,            4,            1}
                        }));
            });
            Assert.Contains("Puzzle has duplicate value in row", ex.Message);
        }

        [Fact]
        public void CopyWithNewReference_CreatesDeepCopy()
        {
            var puzzle = new Puzzle(new int?[,] {
                {           1, null /* 4 */, null /* 3 */,            2},
                {null /* 2 */, null /* 3 */,            1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {           3,            2,            4,            1}
            });
            var rule = new RowUniquenessRule(puzzle);

            var puzzleCopy = new Puzzle(puzzle);
            var ruleCopy = rule.CopyWithNewReference(puzzleCopy);
            int val = 2;
            var coord = new Coordinate(2, 2);
            ruleCopy.Update(coord, val, new CoordinateTracker(puzzle.Size));
            Assert.NotEqual(rule.GetPossibleValues(coord), ruleCopy.GetPossibleValues(coord));

            puzzleCopy[coord] = val;
            var secondCoord = new Coordinate(2, 3);
            var secondVal = 3;
            var coordTracker = new CoordinateTracker(puzzle.Size);
            ruleCopy.Update(secondCoord, secondVal, coordTracker);
            var originalCoordTracker = new CoordinateTracker(puzzle.Size);
            rule.Update(secondCoord, secondVal, originalCoordTracker);
            Assert.Equal(
                new HashSet<Coordinate> { new Coordinate(2, 0), new Coordinate(2, 1) },
                new HashSet<Coordinate>(coordTracker.GetTrackedCoords().ToArray()));
            Assert.Equal(
                new HashSet<Coordinate> { new Coordinate(2, 0), new Coordinate(2, 1), new Coordinate(2, 2) },
                new HashSet<Coordinate>(originalCoordTracker.GetTrackedCoords().ToArray()));
        }

        [Fact]
        public void Update_UpdatesSpecifiedRow()
        {
            var puzzle = new Puzzle(new int?[,] {
                {1, null /* 4 */, null /* 3 */, 2},
                {null /* 2 */, null /* 3 */, 1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {3, 2, 4, 1}
            });
            var rule = new RowUniquenessRule(puzzle);
            var coordTracker = new CoordinateTracker(puzzle.Size);
            var coord = new Coordinate(1, 1);
            var val = 3;
            rule.Update(coord, val, coordTracker);
            Assert.Equal(
                new HashSet<Coordinate> { new Coordinate(1, 0), new Coordinate(1, 3) },
                new HashSet<Coordinate>(coordTracker.GetTrackedCoords().ToArray()));
            Assert.Equal(new BitVector(0b1100), rule.GetPossibleValues(new Coordinate(0, 0)));
            Assert.Equal(new BitVector(0b1010), rule.GetPossibleValues(new Coordinate(1, 0)));
            Assert.Equal(new BitVector(0b1111), rule.GetPossibleValues(new Coordinate(2, 0)));
            Assert.Equal(new BitVector(0b0000), rule.GetPossibleValues(new Coordinate(3, 0)));
        }

        [Fact]
        public void Revert_WithoutAffectedList_RevertsSpecifiedRow()
        {
            var puzzle = new Puzzle(new int?[,] {
                {1, null /* 4 */, null /* 3 */, 2},
                {null /* 2 */, null /* 3 */, 1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {3, 2, 4, 1}
            });
            var rule = new RowUniquenessRule(puzzle);
            var initialPossibleValuesByRow = _GetPossibleValuesByRow(puzzle.Size, rule);
            var coord = new Coordinate(1, 1);
            var val = 3;
            rule.Update(coord, val, new CoordinateTracker(puzzle.Size));

            rule.Revert(coord, val);

            for (int row = 0; row < initialPossibleValuesByRow.Count; row++)
            {
                Assert.Equal(
                    initialPossibleValuesByRow[row], rule.GetMissingValuesForRow(row));
            }
        }

        [Fact]
        public void Revert_RevertsSpecifiedRow()
        {
            var puzzle = new Puzzle(new int?[,] {
                {1, null /* 4 */, null /* 3 */, 2},
                {null /* 2 */, null /* 3 */, 1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {3, 2, 4, 1}
            });
            var rule = new RowUniquenessRule(puzzle);
            var initialPossibleValuesByRow = _GetPossibleValuesByRow(puzzle.Size, rule);
            var updatedCoordTracker = new CoordinateTracker(puzzle.Size);
            var revertedCoordTracker = new CoordinateTracker(puzzle.Size);
            var coord = new Coordinate(1, 1);
            var val = 3;
            rule.Update(coord, val, updatedCoordTracker);

            rule.Revert(coord, val, revertedCoordTracker);
            
            Assert.Equal(
                updatedCoordTracker.GetTrackedCoords().ToArray(),
                revertedCoordTracker.GetTrackedCoords().ToArray());
            for (int row = 0; row < initialPossibleValuesByRow.Count; row++)
            {
                Assert.Equal(
                    initialPossibleValuesByRow[row],
                    rule.GetMissingValuesForRow(row));
            }
        }

        [Fact]
        public void GetPossibleRowValues_IsCorrect()
        {
            var puzzle = new Puzzle(new int?[,] {
                {1, null /* 4 */, null /* 3 */, 2},
                {null /* 2 */, null /* 3 */, 1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {3, 2, 4, 1}
            });
            var rule = new RowUniquenessRule(puzzle);
            Assert.Equal(new BitVector(0b1100), rule.GetMissingValuesForRow(0));
            Assert.Equal(new BitVector(0b1110), rule.GetMissingValuesForRow(1));
            Assert.Equal(new BitVector(0b1111), rule.GetMissingValuesForRow(2));
            Assert.Equal(new BitVector(0b0000), rule.GetMissingValuesForRow(3));
        }

        [Fact]
        public void GetPossibleValues_MatchesGetPossibleRowValues()
        {
            var puzzle = new Puzzle(new int?[,] {
                {1, null /* 4 */, null /* 3 */, 2},
                {null /* 2 */, null /* 3 */, 1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {3, 2, 4, 1}
            });
            var rule = new RowUniquenessRule(puzzle);
            var possibleValuesByRow = _GetPossibleValuesByRow(puzzle.Size, rule);

            for (int row = 0; row < possibleValuesByRow.Count; row++)
            {
                for (int col = 0; col < puzzle.Size; col++)
                {
                    Assert.Equal(
                        possibleValuesByRow[row],
                        rule.GetPossibleValues(new Coordinate(row, col)));
                }
            }
        }

        private IList<BitVector> _GetPossibleValuesByRow(int numRows, RowUniquenessRule rule)
        {
            var possibleRowValues = new List<BitVector>();
            for (int row = 0; row < numRows; row++)
            {
                possibleRowValues.Add(rule.GetMissingValuesForRow(row));
            }
            return possibleRowValues;
        }
    }
}
