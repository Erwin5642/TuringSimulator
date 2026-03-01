using System.Collections.Generic;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Program
{
    internal sealed class TableProgram : IProgram
    {
        private readonly Dictionary<(int State, Symbol Symbol), Transition> _transitions;
        private readonly HashSet<int> _states;
        private readonly HashSet<int> _finalStates;

        public int StartState { get; }

        public IReadOnlyCollection<int> States => _states;
        public IReadOnlyCollection<int> FinalStates => _finalStates;

        internal TableProgram(
            int startState,
            Dictionary<(int, Symbol), Transition> transitions,
            HashSet<int> states,
            HashSet<int> finalStates)
        {
            StartState = startState;
            _transitions = transitions;
            _states = states;
            _finalStates = finalStates;
        }

        public bool IsFinalState(int state)
        {
            return _finalStates.Contains(state);
        }

        public bool TryGetTransition(
            int currentState,
            Symbol headSymbol,
            out Transition transition)
        {
            return _transitions.TryGetValue((currentState, headSymbol), out transition);
        }

        public bool HasTransition(int currentState, Symbol headSymbol)
        {
            return _transitions.ContainsKey((currentState, headSymbol));
        }
    }
}