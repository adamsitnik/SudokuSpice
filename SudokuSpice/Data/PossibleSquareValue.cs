﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SudokuSpice.Data
{
    public class PossibleSquareValue
    {
        internal readonly Square Square;
        internal PossibleSquareState State;
        [DisallowNull]
        internal SquareLink? FirstLink;
        public int Value { get; }

        public PossibleSquareValue(Square square, int value)
        {
            Square = square;
            Value = value;
            State = PossibleSquareState.UNKNOWN;
        }

        internal bool TrySelect()
        {
            Debug.Assert(State == PossibleSquareState.UNKNOWN, $"PossibleSquare at {Square.Coordinate} with value {Value} was selected while in state {State}.");
            Debug.Assert(FirstLink != null, $"PossibleSquare at {Square.Coordinate} with value {Value} was selected while FirstLink was null.");
            if (!_TryUpdateLinks(link => link.TrySatisfyConstraint(), link => link.UnsatisfyConstraint()))
            {
                return false;
            }
            State = PossibleSquareState.SELECTED;
            return true;
        }

        internal void Deselect()
        {
            Debug.Assert(State == PossibleSquareState.SELECTED, $"PossibleSquare at {Square.Coordinate} with value {Value} was deselected while in state {State}.");
            Debug.Assert(FirstLink != null, $"PossibleSquare at {Square.Coordinate} with value {Value} was deselected while FirstLink was null.");
            State = PossibleSquareState.UNKNOWN;
            _RevertLinks(link => link.UnsatisfyConstraint());
        }

        internal bool TryDrop()
        {
            Debug.Assert(State == PossibleSquareState.UNKNOWN, $"PossibleSquare at {Square.Coordinate} with value {Value} was cleared while in state {State}.");
            Debug.Assert(FirstLink != null, $"PossibleSquare at {Square.Coordinate} with value {Value} was cleared while FirstLink was null.");
            if (Square.NumPossibleValues == 1)
            {
                return false;
            }
            if (!_TryUpdateLinks(link => link.TryRemoveFromConstraint(), link => link.ReturnToConstraint()))
            {
                return false;
            }
            Square.NumPossibleValues--;
            State = PossibleSquareState.DROPPED;
            return true;
        }

        internal void Return()
        {
            Debug.Assert(State == PossibleSquareState.DROPPED, $"PossibleSquare at {Square.Coordinate} with value {Value} was returned while in state {State}.");
            Debug.Assert(FirstLink != null, $"PossibleSquare at {Square.Coordinate} with value {Value} was returned while FirstLink was null.");
            State = PossibleSquareState.UNKNOWN;
            Square.NumPossibleValues++;
            _RevertLinks(link => link.ReturnToConstraint());
        }

        internal int GetMinConstraintCount()
        {
            Debug.Assert(FirstLink != null, $"Called {nameof(GetMinConstraintCount)} on PossibleSquare at {Square.Coordinate} with value {Value} while FirstLink was null.");
            int minCount = FirstLink.Constraint.Count;
            var link = FirstLink.Right;
            while (link != FirstLink)
            {
                if (link.Constraint.Count < minCount)
                {
                    minCount = link.Constraint.Count;
                }
                link = link.Right;
            }
            return minCount;
        }

        private bool _TryUpdateLinks(Func<SquareLink, bool> tryFn, Action<SquareLink> undoFn)
        {
            Debug.Assert(FirstLink != null, $"PossibleSquare at {Square.Coordinate} with value {Value} called {nameof(_TryUpdateLinks)} while FirstLink was null.");
            var link = FirstLink;
            do
            {
                if (!tryFn(link))
                {
                    while (link != FirstLink)
                    {
                        link = link.Left;
                        undoFn(link);
                    }
                    return false;
                }
                link = link.Right;
            } while (link != FirstLink);
            return true;
        }

        private void _RevertLinks(Action<SquareLink> fn)
        {
            Debug.Assert(FirstLink != null, $"PossibleSquare at {Square.Coordinate} with value {Value} called {nameof(_RevertLinks)} while FirstLink was null.");
            var lastLink = FirstLink.Left;
            var link = lastLink;
            do
            {
                fn(link);
                link = link.Left;
            } while (link != lastLink);
        }
    }
}
