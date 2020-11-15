﻿namespace SudokuSpice.ConstraintBased
{
    /// <summary>
    /// Indicates if a <see cref="PossibleValue{TPuzzle}"/> is still possible, selected, or dropped.
    /// </summary>
    public enum PossibleSquareState
    {
        /// <summary>
        /// This <see cref="PossibleValue{TPuzzle}"/> is still possible.
        /// </summary>
        UNKNOWN,
        /// <summary>
        /// This <see cref="PossibleValue{TPuzzle}"/> has been selected.
        /// </summary>
        SELECTED,
        /// <summary>
        /// This <see cref="PossibleValue{TPuzzle}"/> has been dropped, i.e. it is no longer possible
        /// with the currently selected values.
        /// </summary>
        DROPPED
    }
}
