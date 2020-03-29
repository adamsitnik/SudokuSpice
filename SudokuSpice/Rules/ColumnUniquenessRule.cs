﻿using SudokuSpice.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SudokuSpice.Rules
{
    /// <summary>
    /// Restricts that each column contains all unique values.
    /// </summary>
    public class ColumnUniquenessRule : ISudokuRule, IMissingColumnValuesTracker
    {
        private readonly Puzzle _puzzle;
        private readonly BitVector[] _unsetColumnValues;

        public ColumnUniquenessRule(Puzzle puzzle)
        {
            _puzzle = puzzle;
            _unsetColumnValues = new BitVector[puzzle.Size];
            BitVector allPossible = BitVector.CreateWithSize(puzzle.Size);
            for (int i = 0; i < puzzle.Size; i++)
            {
                _unsetColumnValues[i] = allPossible;
            }
            for (int row = 0; row < puzzle.Size; row++)
            {
                for (int col = 0; col < puzzle.Size; col++)
                {
                    var val = puzzle[row, col];
                    if (!val.HasValue)
                    {
                        continue;
                    }
                    int bit = val.Value - 1;
                    if (!_unsetColumnValues[col].IsBitSet(bit))
                    {
                        throw new ArgumentException($"Puzzle has duplicate value in column at ({row}, {col}).");
                    }
                    _unsetColumnValues[col].UnsetBit(bit);
                }
            }
        }

        private ColumnUniquenessRule(ColumnUniquenessRule existing, Puzzle puzzle)
        {
            _puzzle = puzzle;
            _unsetColumnValues = (BitVector[])existing._unsetColumnValues.Clone();
        }

        public ISudokuRule CopyWithNewReference(Puzzle puzzle)
        {
            return new ColumnUniquenessRule(this, puzzle);
        }

        public BitVector GetPossibleValues(in Coordinate c) => _unsetColumnValues[c.Column];
        
        public BitVector GetMissingValuesForColumn(int col) => _unsetColumnValues[col];

        public void Revert(in Coordinate c, int val)
        {
            Debug.Assert(!_puzzle[in c].HasValue, "Cannot call ISudokuRule.Revert for a set puzzle coordinate");
            _unsetColumnValues[c.Column].SetBit(val - 1);
        }

        public void Revert(in Coordinate c, int val, CoordinateTracker coordTracker)
        {
            this.Revert(in c, val);
            _AddUnsetFromColumn(in c, coordTracker);
        }

        public void Update(in Coordinate c, int val, CoordinateTracker coordTracker)
        {
            Debug.Assert(!_puzzle[in c].HasValue, "Cannot call ISudokuRule.Update for a set puzzle coordinate");
            _unsetColumnValues[c.Column].UnsetBit(val - 1);
            _AddUnsetFromColumn(in c, coordTracker);
        }

        private void _AddUnsetFromColumn(in Coordinate c, CoordinateTracker coordTracker)
        {
            for (int row = 0; row < _puzzle.Size; row++)
            {
                if (row != c.Row && !_puzzle[row, c.Column].HasValue)
                {
                    coordTracker.AddOrTrackIfUntracked(new Coordinate(row, c.Column));
                }
            }
        }
    }
}
