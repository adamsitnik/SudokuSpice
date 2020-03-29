﻿using SudokuSpice.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SudokuSpice.Rules
{
    /// <summary>
    /// Rules that each row contains all unique values.
    /// </summary>
    public class RowUniquenessRule : ISudokuRule, IMissingRowValuesTracker
    {
        private readonly Puzzle _puzzle;
        private readonly BitVector[] _unsetRowValues;

        public RowUniquenessRule(Puzzle puzzle)
        {
            _puzzle = puzzle;
            _unsetRowValues = new BitVector[puzzle.Size];
            BitVector allPossible = BitVector.CreateWithSize(puzzle.Size);
            for (int i = 0; i < puzzle.Size; i++)
            {
                _unsetRowValues[i] = allPossible;
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
                    if (!_unsetRowValues[row].IsBitSet(bit))
                    {
                        throw new ArgumentException($"Puzzle has duplicate value in row at ({row}, {col}).");
                    }
                    _unsetRowValues[row].UnsetBit(bit);
                }
            }
        }

        private RowUniquenessRule(RowUniquenessRule existing, Puzzle puzzle)
        {
            _puzzle = puzzle;
            _unsetRowValues = (BitVector[])existing._unsetRowValues.Clone();
        }

        public ISudokuRule CopyWithNewReference(Puzzle puzzle)
        {
            return new RowUniquenessRule(this, puzzle);
        }

        public BitVector GetPossibleValues(in Coordinate c) => _unsetRowValues[c.Row];

        public BitVector GetMissingValuesForRow(int row) => _unsetRowValues[row];

        public void Revert(in Coordinate c, int val)
        {
            Debug.Assert(!_puzzle[in c].HasValue, "Cannot call ISudokuRule.Revert for a set puzzle coordinate");
            _unsetRowValues[c.Row].SetBit(val - 1);
        }

        public void Revert(in Coordinate c, int val, CoordinateTracker coordTracker)
        {
            this.Revert(in c, val);
            _AddUnsetFromRow(in c, coordTracker);
        }

        public void Update(in Coordinate c, int val, CoordinateTracker coordTracker)
        {
            Debug.Assert(!_puzzle[in c].HasValue, "Cannot call ISudokuRule.Update for a set puzzle coordinate");
            _unsetRowValues[c.Row].UnsetBit(val - 1);
            _AddUnsetFromRow(in c, coordTracker);
        }

        private void _AddUnsetFromRow(in Coordinate c, CoordinateTracker coordTracker)
        {
            for (int col = 0; col < _puzzle.Size; col++)
            {
                if (col != c.Column && !_puzzle[c.Row, col].HasValue)
                {
                    coordTracker.AddOrTrackIfUntracked(new Coordinate(c.Row, col));
                }
            }
        }
    }
}
