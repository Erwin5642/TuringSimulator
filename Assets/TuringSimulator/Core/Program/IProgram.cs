using System.Collections.Generic;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Program
{
    /// <summary>
    /// Represents an immutable Turing machine program.
    /// </summary>
    public interface IProgram
    {
        int StartState { get; }

        IReadOnlyCollection<int> States { get; }

        IReadOnlyCollection<int> FinalStates { get; }

        bool IsFinalState(int state);

        /// <summary>
        /// Attempts to get the transition for the given state and head symbol.
        /// Returns false if no transition is defined.
        /// </summary>
        bool TryGetTransition(
            int currentState,
            Symbol headSymbol,
            out Transition transition);

        /// <summary>
        /// Returns true if a transition exists for the given state and symbol.
        /// </summary>
        bool HasTransition(int currentState, Symbol headSymbol);
    }
}