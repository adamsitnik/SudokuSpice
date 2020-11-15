﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SudokuSpice.ConstraintBased
{
    /// <summary>
    /// Represents a column from an exact-cover matrix.
    /// </summary>
    /// <seealso href="https://en.wikipedia.org/wiki/Exact_cover">Exact cover (Wikipedia)</seealso>
    public class ConstraintHeader<TPuzzle> where TPuzzle : IReadOnlyPuzzle
    {
        internal ExactCoverMatrix<TPuzzle> Matrix { get; private set; }
        [DisallowNull]
        internal Link<TPuzzle>? FirstLink { get; private set; }
        internal int Count { get; private set; }

        internal bool IsSatisfied { get; private set; }
        internal ConstraintHeader<TPuzzle> NextHeader { get; set; }
        internal ConstraintHeader<TPuzzle> PreviousHeader { get; set; }

        internal ConstraintHeader(ExactCoverMatrix<TPuzzle> matrix)
        {
            Matrix = matrix;
            NextHeader = PreviousHeader = this;
        }

        internal ConstraintHeader<TPuzzle> CopyToMatrix(ExactCoverMatrix<TPuzzle> matrix)
        {
            Debug.Assert(FirstLink != null, $"Can't copy a header with a null {nameof(FirstLink)}.");
            Debug.Assert(!IsSatisfied, $"Can't copy a header that's already satisfied.");
            var copy = new ConstraintHeader<TPuzzle>(matrix);
            foreach (Link<TPuzzle>? link in GetLinks())
            {
                Square<TPuzzle>? square = matrix.GetSquare(link.PossibleSquare.Square.Coordinate);
                Debug.Assert(square != null,
                    $"Tried to copy a square link for a null square at {link.PossibleSquare.Square.Coordinate}.");
                PossibleValue<TPuzzle>? possibleSquare = square.GetPossibleValue(link.PossibleSquare.ValueIndex);
                Debug.Assert(possibleSquare != null, "Tried to link header to null possible square value.");
                _ = Link<TPuzzle>.CreateConnectedLink(possibleSquare, copy);
            }
            return copy;
        }

        /// <summary>
        /// Creates a fully connected header that can be satisfied by any of the given
        /// <paramref name="possibleSquares"/>. Adds and attaches necessary links to connect the
        /// matrix.
        /// </summary>
        /// <param name="matrix">That matrix that this header should be attached to.</param>
        /// <param name="possibleSquares">
        /// The possible square values that would satisfy this header.
        /// </param>
        /// <returns>The newly constructed header.</returns>
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
            Justification = "Static factory method is cleaner than using constructor and separate method.")]
        public static ConstraintHeader<TPuzzle> CreateConnectedHeader(
            ExactCoverMatrix<TPuzzle> matrix, ReadOnlySpan<PossibleValue<TPuzzle>> possibleSquares)
        {
            var header = new ConstraintHeader<TPuzzle>(matrix);
            matrix.Attach(header);
            foreach (PossibleValue<TPuzzle>? possibleSquare in possibleSquares)
            {
                _ = Link<TPuzzle>.CreateConnectedLink(possibleSquare, header);
            }
            if (header.Count == 0)
            {
                throw new ArgumentException(
                    $"Must provide at least one {nameof(PossibleValue<TPuzzle>)} when creating a {nameof(ConstraintHeader<TPuzzle>)}.");
            }
            return header;
        }

        internal bool TrySatisfyFrom(Link<TPuzzle> sourceLink)
        {
            Debug.Assert(!IsSatisfied, $"Constraint was already satisfied when selecting square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex}.");
            Debug.Assert(FirstLink != null, $"Tried to satisfy constraint via square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex} but {nameof(FirstLink)} was null.");
            Debug.Assert(GetLinks().Contains(sourceLink), $"Constraint was missing possible square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex} when satisfying constraint.");
            IsSatisfied = true;
            Link<TPuzzle>? link = sourceLink.Down;
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

        internal void UnsatisfyFrom(Link<TPuzzle> sourceLink)
        {
            Debug.Assert(IsSatisfied, $"Constraint was not satisfied when deselecting square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex}.");
            Debug.Assert(GetLinks().Contains(sourceLink), $"Constraint was missing possible square {sourceLink.PossibleSquare.Square.Coordinate}, value: {sourceLink.PossibleSquare.ValueIndex} when unsatisfying constraint.");
            Link<TPuzzle>? link = sourceLink.Up;
            while (link != sourceLink)
            {
                link.PossibleSquare.Return();
                link = link.Up;
            }
            IsSatisfied = false;
            NextHeader.PreviousHeader = this;
            PreviousHeader.NextHeader = this;
        }

        internal bool TryDetach(Link<TPuzzle> link)
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

        internal void Attach(Link<TPuzzle> link)
        {
            if (FirstLink is null)
            {
                FirstLink = link;
            } else
            {
                link.Down = FirstLink;
                link.Up = FirstLink.Up;
                FirstLink.Up.Down = link;
                FirstLink.Up = link;
            }
            Count++;
        }

        internal void Reattach(Link<TPuzzle> link)
        {
            link.Down.Up = link;
            link.Up.Down = link;
            Count++;
        }

        internal IEnumerable<Link<TPuzzle>> GetLinks()
        {
            if (FirstLink == null)
            {
                yield break;
            }
            Link<TPuzzle>? link = FirstLink;
            do
            {
                yield return link;
                link = link.Down;
            } while (link != FirstLink);
        }
    }
}
