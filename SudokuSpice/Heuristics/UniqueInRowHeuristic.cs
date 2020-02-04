﻿using System.Linq;

namespace SudokuSpice
{
    public class UniqueInRowHeuristic : IHeuristic
    {
        private readonly Puzzle _puzzle;
        private readonly RowRestrict _restrict;
        private readonly int[] _possiblesToCheckInRow;

        public UniqueInRowHeuristic(Puzzle puzzle, RowRestrict restrict)
        {
            _puzzle = puzzle;
            _restrict = restrict;
            _possiblesToCheckInRow = new int[puzzle.Size];
        }

        public void UpdateAll()
        {
            for (int row = 0; row < _puzzle.Size; row++)
            {
                _PreparePossiblesToCheckInRow(row);
                _CheckRow(row);
            }
        }

        private void _PreparePossiblesToCheckInRow(int row)
        {
            _possiblesToCheckInRow[row] = _restrict.GetPossibleRowValues(row);
            for (int col = 0; col < _puzzle.Size; col++)
            {
                if (_puzzle[row, col].HasValue)
                {
                    continue;
                }
                var modifiedPossibles = _puzzle.GetPossibleValues(row, col);
                if (modifiedPossibles.CountSetBits() == 1)
                {
                    BitVectorUtils.UnsetBit(modifiedPossibles.GetSetBits().First(), ref _possiblesToCheckInRow[row]);
                }
            }
        }

        private void _CheckRow(int row)
        {
            foreach (var possible in _possiblesToCheckInRow[row].GetSetBits())
            {
                bool isUniqueCoordForPossible = false;
                int uniqueCol = -1;
                for (int col = 0; col < _puzzle.Size; col++)
                {
                    if (_puzzle[row, col].HasValue)
                    {
                        continue;
                    }
                    if (_puzzle.GetPossibleValues(row, col).IsBitSet(possible))
                    {
                        if (isUniqueCoordForPossible)
                        {
                            isUniqueCoordForPossible = false;
                            break;
                        }
                        isUniqueCoordForPossible = true;
                        uniqueCol = col;
                    }
                }
                if (!isUniqueCoordForPossible)
                {
                    continue;
                }
                var possibles = 0;
                BitVectorUtils.SetBit(possible, ref possibles);
                _puzzle.SetPossibleValues(row, uniqueCol, possibles);
            }
        }
    }
}