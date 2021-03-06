﻿using System;
using System.Collections.Generic;
using Xunit;

namespace SudokuSpice.RuleBased.Rules.Test
{
    public class DynamicRuleKeeperTest
    {
        [Fact]
        public void Constructor_UpdatesPossibles()
        {
            var puzzle = new Puzzle(new int?[,] {
                {           1, null /* 4 */, null /* 3 */,            2},
                {null /* 2 */, null /* 3 */,            1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {           3,            2,            4,            1}
            });
            var possibleValues = new PossibleValues(puzzle);
            var rules = new List<ISudokuRule>
            {
                new RowUniquenessRule(puzzle, possibleValues.AllPossible),
                new ColumnUniquenessRule(puzzle, possibleValues.AllPossible),
                new BoxUniquenessRule(puzzle, possibleValues.AllPossible, true)
            };
            var ruleKeeper = new DynamicRuleKeeper(puzzle, possibleValues, rules);
            Assert.NotNull(ruleKeeper);
            Assert.Equal(new BitVector(0b11000), possibleValues[new Coordinate(0, 1)]);
            Assert.Equal(new BitVector(0b01000), possibleValues[new Coordinate(0, 2)]);
            Assert.Equal(new BitVector(0b10100), possibleValues[new Coordinate(1, 0)]);
            Assert.Equal(new BitVector(0b11000), possibleValues[new Coordinate(1, 1)]);
            Assert.Equal(new BitVector(0b11000), possibleValues[new Coordinate(1, 3)]);
            Assert.Equal(new BitVector(0b10000), possibleValues[new Coordinate(2, 0)]);
            Assert.Equal(new BitVector(0b10010), possibleValues[new Coordinate(2, 1)]);
            Assert.Equal(new BitVector(0b01100), possibleValues[new Coordinate(2, 2)]);
            Assert.Equal(new BitVector(0b01000), possibleValues[new Coordinate(2, 3)]);
        }

        [Fact]
        public void Constructor_WhenSquareHasNoPossibleValues_Throws()
        {
            var puzzle = new Puzzle(
                new int?[,] {
                    {           1,    3 /* 4 */, null /* 3 */,            2},
                    {null /* 2 */, null /* 3 */,            1, null /* 4 */},
                    {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                    {           3,            2,            4,            1}
                });
            var possibleValues = new PossibleValues(puzzle);
            var rules = new List<ISudokuRule>
                {
                    new RowUniquenessRule(puzzle, possibleValues.AllPossible),
                    new ColumnUniquenessRule(puzzle, possibleValues.AllPossible),
                    new BoxUniquenessRule(puzzle, possibleValues.AllPossible, true)
                };
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            {
                var ruleKeeper = new DynamicRuleKeeper(puzzle, possibleValues, rules);
            });
            Assert.Contains("Puzzle could not be solved with the given values", ex.Message);
        }

        [Fact]
        public void CopyWithNewReferences_CreatesDeepCopy()
        {
            var puzzle = new Puzzle(new int?[,] {
                {           1, null /* 4 */, null /* 3 */,            2},
                {null /* 2 */, null /* 3 */,            1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {           3,            2,            4,            1}
            });
            var possibleValues = new PossibleValues(puzzle);
            var rules = new List<ISudokuRule>
            {
                new RowUniquenessRule(puzzle, possibleValues.AllPossible),
                new ColumnUniquenessRule(puzzle, possibleValues.AllPossible),
                new BoxUniquenessRule(puzzle, possibleValues.AllPossible, true)
            };
            var ruleKeeper = new DynamicRuleKeeper(puzzle, possibleValues, rules);

            var puzzleCopy = new Puzzle(puzzle);
            var possibleValuesCopy = new PossibleValues(possibleValues);
            ISudokuRuleKeeper ruleKeeperCopy = ruleKeeper.CopyWithNewReferences(puzzleCopy, possibleValuesCopy);

            IReadOnlyList<ISudokuRule> rulesCopy = ruleKeeperCopy.GetRules();
            Assert.Equal(rules.Count, rulesCopy.Count);
            for (int i = 0; i < rules.Count; i++)
            {
                Assert.NotSame(rules[i], rulesCopy[i]);
                Type originalType = rules[i].GetType();
                Type copiedType = rulesCopy[i].GetType();
                Assert.Equal(originalType, copiedType);
            }
            var coord = new Coordinate(0, 1);
            int val = 4;
            Assert.True(ruleKeeperCopy.TrySet(coord, val));
            Assert.Equal(new BitVector(0b11000), possibleValues[new Coordinate(1, 1)]);
            Assert.Equal(new BitVector(0b01000), possibleValuesCopy[new Coordinate(1, 1)]);
        }

        [Fact]
        public void TrySet_WithValidValue_Succeeds()
        {
            var puzzle = new Puzzle(new int?[,] {
                {           1, null /* 4 */, null /* 3 */,            2},
                {null /* 2 */, null /* 3 */,            1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {           3,            2,            4,            1}
            });
            var possibleValues = new PossibleValues(puzzle);
            var rules = new List<ISudokuRule>
                {
                    new RowUniquenessRule(puzzle, possibleValues.AllPossible),
                    new ColumnUniquenessRule(puzzle, possibleValues.AllPossible),
                    new BoxUniquenessRule(puzzle, possibleValues.AllPossible, true)
                };
            var ruleKeeper = new DynamicRuleKeeper(puzzle, possibleValues, rules);
            var coord = new Coordinate(1, 1);
            int val = 3;
            Assert.True(ruleKeeper.TrySet(coord, val));
            puzzle[coord] = val;
            Assert.Equal(new BitVector(0b10000), possibleValues[new Coordinate(0, 1)]);
            Assert.Equal(new BitVector(0b10100), possibleValues[new Coordinate(1, 0)]);
            Assert.Equal(new BitVector(0b10000), possibleValues[new Coordinate(1, 3)]);
            Assert.Equal(new BitVector(0b10010), possibleValues[new Coordinate(2, 1)]);
        }

        [Fact]
        public void TrySet_WithValueThatCausesNoPossiblesForOtherSquare_FailsAndLeavesUnchanged()
        {
            var puzzle = new Puzzle(new int?[,] {
                {           1, null /* 4 */, null /* 3 */,            2},
                {null /* 2 */, null /* 3 */,            1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {           3,            2,            4,            1}
            });
            var possibleValues = new PossibleValues(puzzle);
            var rules = new List<ISudokuRule>
                {
                    new RowUniquenessRule(puzzle, possibleValues.AllPossible),
                    new ColumnUniquenessRule(puzzle, possibleValues.AllPossible),
                    new BoxUniquenessRule(puzzle, possibleValues.AllPossible, true)
                };
            var ruleKeeper = new DynamicRuleKeeper(puzzle, possibleValues, rules);
            IDictionary<Coordinate, BitVector> initialPossibleValues = _RetrieveAllUnsetPossibleValues(puzzle, possibleValues);
            var coord = new Coordinate(1, 0);
            int val = 4;
            Assert.False(ruleKeeper.TrySet(coord, val));

            foreach (Coordinate c in puzzle.GetUnsetCoords())
            {
                Assert.Equal(initialPossibleValues[c], possibleValues[in c]);
            }
        }

        [Fact]
        public void TrySet_WithInvalidValue_FailsAndLeavesUnchanged()
        {
            var puzzle = new Puzzle(new int?[,] {
                {           1, null /* 4 */, null /* 3 */,            2},
                {null /* 2 */, null /* 3 */,            1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {           3,            2,            4,            1}
            });
            var possibleValues = new PossibleValues(puzzle);
            var rules = new List<ISudokuRule>
                {
                    new RowUniquenessRule(puzzle, possibleValues.AllPossible),
                    new ColumnUniquenessRule(puzzle, possibleValues.AllPossible),
                    new BoxUniquenessRule(puzzle, possibleValues.AllPossible, true)
                };
            var ruleKeeper = new DynamicRuleKeeper(puzzle, possibleValues, rules);
            IDictionary<Coordinate, BitVector> initialPossibleValues = _RetrieveAllUnsetPossibleValues(puzzle, possibleValues);
            var coord = new Coordinate(1, 1);
            int val = 2;
            Assert.False(ruleKeeper.TrySet(coord, val));

            foreach (Coordinate c in puzzle.GetUnsetCoords())
            {
                Assert.Equal(initialPossibleValues[c], possibleValues[in c]);
            }
        }

        [Fact]
        public void Revert_RevertsSpecifiedCoordinate()
        {
            var puzzle = new Puzzle(new int?[,] {
                {           1, null /* 4 */, null /* 3 */,            2},
                {null /* 2 */, null /* 3 */,            1, null /* 4 */},
                {null /* 4 */, null /* 1 */, null /* 2 */, null /* 3 */},
                {           3,            2,            4,            1}
            });
            var possibleValues = new PossibleValues(puzzle);
            var rules = new List<ISudokuRule>
                {
                    new RowUniquenessRule(puzzle, possibleValues.AllPossible),
                    new ColumnUniquenessRule(puzzle, possibleValues.AllPossible),
                    new BoxUniquenessRule(puzzle, possibleValues.AllPossible, true)
                };
            var ruleKeeper = new DynamicRuleKeeper(puzzle, possibleValues, rules);
            IDictionary<Coordinate, BitVector> initialPossibleValues = _RetrieveAllUnsetPossibleValues(puzzle, possibleValues);
            var coord = new Coordinate(1, 1);
            int val = 3;
            Assert.True(ruleKeeper.TrySet(in coord, val));
            puzzle[in coord] = val;

            puzzle[in coord] = null;
            ruleKeeper.Unset(in coord, val);

            foreach (Coordinate c in puzzle.GetUnsetCoords())
            {
                Assert.Equal(initialPossibleValues[c], possibleValues[in c]);
            }
        }

        private IDictionary<Coordinate, BitVector> _RetrieveAllUnsetPossibleValues(
            Puzzle puzzle, PossibleValues possibleValues)
        {
            var unsetPossibleValues = new Dictionary<Coordinate, BitVector>();
            foreach (Coordinate c in puzzle.GetUnsetCoords())
            {
                unsetPossibleValues[c] = possibleValues[in c];
            }
            return unsetPossibleValues;
        }
    }
}
