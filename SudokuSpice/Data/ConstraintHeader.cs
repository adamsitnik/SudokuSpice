﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SudokuSpice.Data
{
    public class ConstraintHeader
    {
        internal ExactCoverMatrix Matrix { get; private set; }
        [DisallowNull]
        internal SquareLink? FirstLink { get; private set; }
        internal int Count { get; private set; }

        internal bool IsSatisfied { get; private set; }
        internal ConstraintHeader NextHeader { get; set; }
        internal ConstraintHeader PreviousHeader { get; set; }

        internal ConstraintHeader(ExactCoverMatrix matrix) {
            Matrix = matrix;
            NextHeader = PreviousHeader = this;
        }

        public static ConstraintHeader CreateConnectedHeader(ExactCoverMatrix matrix, ReadOnlySpan<PossibleSquareValue> possibleSquares)
        {
            var header = new ConstraintHeader(matrix);
            matrix.Attach(header);
            foreach (var possibleSquare in possibleSquares)
            {
                SquareLink.CreateConnectedLink(possibleSquare, header);
            }
            if (header.Count == 0)
            {
                throw new ArgumentException(
                    $"Must provide at least one {nameof(PossibleSquareValue)} when creating a {nameof(ConstraintHeader)}.");
            }
            return header;
        }

        internal bool TrySatisfyFrom(SquareLink sourceLink)
        {
            Debug.Assert(!IsSatisfied, $"Constraint was already satisfied when selecting square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex}.");
            Debug.Assert(FirstLink != null, $"Tried to satisfy constraint via square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex} but {nameof(FirstLink)} was null.");
            Debug.Assert(GetLinks().Contains(sourceLink), $"Constraint was missing possible square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex} when satisfying constraint.");
            IsSatisfied = true;
            var link = sourceLink.Down;
            while (link != sourceLink)
            {
                if (!link.PossibleSquare.TryDrop())
                {
                    link = link.Up;
                    while (link != sourceLink)
                    {
                        link.PossibleSquare.Return();
                        link = link.Up;
                    }
                    IsSatisfied = false;
                    return false;
                }
                link = link.Down;
            }
            NextHeader.PreviousHeader = PreviousHeader;
            PreviousHeader.NextHeader = NextHeader;
            if (Matrix.FirstHeader == this)
            {
                Matrix.FirstHeader = NextHeader;
            }
            return true;
        }

        internal void UnsatisfyFrom(SquareLink sourceLink)
        {
            Debug.Assert(IsSatisfied, $"Constraint was not satisfied when deselecting square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex}.");
            Debug.Assert(GetLinks().Contains(sourceLink), $"Constraint was missing possible square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex} when unsatisfying constraint.");
            var link = sourceLink.Up;
            while (link != sourceLink)
            {
                link.PossibleSquare.Return();
                link = link.Up;
            }
            IsSatisfied = false;
            NextHeader.PreviousHeader = this;
            PreviousHeader.NextHeader = this;
        }

        internal bool TryDetach(SquareLink link)
        {
            Debug.Assert(
                GetLinks().Contains(link),
                $"Can't remove missing possible square {link.PossibleSquare.Square.Coordinate}, value: {link.PossibleSquare.ValueIndex} from constraint.");
            if (Count == 1)
            {
                return false;
            }
            if (FirstLink == link)
            {
                FirstLink = link.Down;
            }
            link.Down.Up = link.Up;
            link.Up.Down = link.Down;
            Count--;
            return true;
        }

        internal void Attach(SquareLink link)
        {
            if (FirstLink is null)
            {
                FirstLink = link;
            }
            else
            {
                link.Down = FirstLink;
                link.Up = FirstLink.Up;
                FirstLink.Up.Down = link;
                FirstLink.Up = link;
            }
            Count++;
        }

        internal void Reattach(SquareLink link)
        {
            link.Down.Up = link;
            link.Up.Down = link;
            Count++;
        }

        internal IEnumerable<SquareLink> GetLinks()
        {
            if (FirstLink == null)
            {
                yield break;
            }
            var link = FirstLink;
            do
            {
                yield return link;
                link = link.Down;
            } while (link != FirstLink);
        }
    }
}