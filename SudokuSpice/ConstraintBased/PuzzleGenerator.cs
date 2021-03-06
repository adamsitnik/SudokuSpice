﻿using SudokuSpice.ConstraintBased.Constraints;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SudokuSpice.ConstraintBased
{
    public class PuzzleGenerator<TPuzzle> where TPuzzle : IPuzzle
    {
        private readonly Random _random = new Random();
        private readonly Func<TPuzzle> _puzzleFactory;
        private readonly IReadOnlyList<IConstraint> _constraints;

        /// <summary>
        /// Creates a puzzle generator to create puzzles with custom rules and type.
        /// </summary>
        /// <param name="puzzleFactory">
        /// A function that constructs an empty <see cref="IPuzzle"/> of the desired type and shape.
        /// </param>
        /// <param name="solverFactory">
        /// A function that constructs a <see cref="SquareTracker{TPuzzle}"/> for the desired puzzle type.
        /// This allows callers to use non-standard rules and heuristics.
        /// </param>
        public PuzzleGenerator(Func<TPuzzle> puzzleFactory, IReadOnlyList<IConstraint> constraints)
        {
            _puzzleFactory = puzzleFactory;
            _constraints = constraints;
        }

        /// <summary>
        /// Generates a puzzle that has a unique solution with the given number of squares set.
        /// </summary>
        /// <remarks>
        /// Be careful calling this with low values, as it can take a very long time to generate
        /// unique puzzles as numSquaresToSet approaches the minimum number of clues necessary to
        /// provide a unique puzzle for this generator's size.
        /// </remarks>
        /// <param name="numSquaresToSet">
        /// The number of squares that will be preset in the generated puzzle.
        /// </param>
        /// <param name="timeout">
        /// The maximum timeout during which this function can search for a unique puzzle.
        /// Especially useful when trying to generate puzzles with low
        /// <paramref name="numSquaresToSet"/>.
        /// </param>
        /// <returns>
        /// A puzzle of type <c>TPuzzle</c> with a unique solution and
        /// <paramref name="numSquaresToSet"/> preset squares.
        /// </returns>
        /// <exception cref="TimeoutException">
        /// Thrown if no valid unique puzzle is found within the specified
        /// <paramref name="timeout"/>.
        /// </exception>
        public TPuzzle Generate(int numSquaresToSet, TimeSpan timeout)
        {
            int timeoutMillis = Math.Max(1, Convert.ToInt32(timeout.TotalMilliseconds));
            using var timeoutCancellationSource = new CancellationTokenSource();
            var puzzleTask = new Task<TPuzzle>(() => _Generate(
                numSquaresToSet, timeoutCancellationSource.Token), timeoutCancellationSource.Token);
            timeoutCancellationSource.CancelAfter(timeoutMillis);
            // This throws InvalidOperationException if the task has been canceled already (i.e. the
            // timeout has finished).
            puzzleTask.RunSynchronously();

            if (puzzleTask.IsCompletedSuccessfully)
            {
                return puzzleTask.Result;
            }
            if (puzzleTask.IsCanceled)
            {
                throw new TimeoutException(
                    $"Failed to generate a puzzle of type {typeof(TPuzzle).Name} and {nameof(numSquaresToSet)} {numSquaresToSet} within {timeout}.");
            }
            if (puzzleTask.Exception is null)
            {
                throw new ApplicationException("Something went wrong while trying to generate the puzzle.");
            }
            throw puzzleTask.Exception;
        }

        private TPuzzle _Generate(int numSquaresToSet, CancellationToken cancellationToken)
        {
            TPuzzle puzzle = _puzzleFactory.Invoke();
            bool foundValidPuzzle = false;
            while (!foundValidPuzzle)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _FillPuzzle(puzzle);
                var setCoordinates = new CoordinateTracker(puzzle.Size);
                _TrackAllCoordinates(setCoordinates, puzzle.Size);
                foundValidPuzzle = _TryUnsetSquaresWhileSolvable(
                        numSquaresToSet, setCoordinates, puzzle,
                        cancellationToken);
            }
            return puzzle;
        }

        private bool _TryUnsetSquaresWhileSolvable(
            int totalNumSquaresToSet, CoordinateTracker setCoordinatesToTry,
            TPuzzle puzzle, CancellationToken cancellationToken)
        {
            if (puzzle.NumSetSquares == totalNumSquaresToSet)
            {
                return true;
            }
            while (setCoordinatesToTry.NumTracked > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Coordinate randomCoord = _GetRandomTrackedCoordinate(setCoordinatesToTry);
                int? previousValue = puzzle[in randomCoord];
                setCoordinatesToTry.Untrack(in randomCoord);
                if (!_TryUnsetSquareAt(randomCoord, puzzle))
                {
                    continue;
                }
                if (_TryUnsetSquaresWhileSolvable(
                    totalNumSquaresToSet,
                    new CoordinateTracker(setCoordinatesToTry), puzzle, cancellationToken))
                {
                    return true;
                }
                // Child update failed :( replace value and try a different coordinate.
                puzzle[in randomCoord] = previousValue;
            }
            return false;
        }

        private Coordinate _GetRandomTrackedCoordinate(CoordinateTracker tracker) => tracker.GetTrackedCoords()[_random.Next(0, tracker.NumTracked)];

        private void _FillPuzzle(TPuzzle puzzle)
        {
            var solver = new PuzzleSolver<TPuzzle>(_constraints);
            solver.SolveRandomly(puzzle);
        }

        private bool _TryUnsetSquareAt(in Coordinate c, TPuzzle puzzle)
        {
            // Unset without checks when there can't be conflicts.
            if (puzzle.NumEmptySquares < 3)
            {
                puzzle[in c] = null;
                return true;
            }
            int? previousValue = puzzle[in c];
            puzzle[in c] = null;
            var solver = new PuzzleSolver<TPuzzle>(_constraints);
            SolveStats solveStats = solver.GetStatsForAllSolutions(puzzle);
            if (solveStats.NumSolutionsFound == 1)
            {
                return true;
            }
            puzzle[in c] = previousValue;
            return false;
        }

        private static void _TrackAllCoordinates(CoordinateTracker tracker, int puzzleSize)
        {
            for (int row = 0; row < puzzleSize; row++)
            {
                for (int col = 0; col < puzzleSize; col++)
                {
                    _ = tracker.AddOrTrackIfUntracked(new Coordinate(row, col));
                }
            }
        }
    }
}
