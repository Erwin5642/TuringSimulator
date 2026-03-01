using System;
using System.Collections.Generic;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Program
{
    public sealed class TableProgramBuilder : IProgramBuilder
    {
        private readonly Dictionary<(int, Symbol), Transition> _transitions = new();
        private readonly HashSet<int> _states = new();
        private readonly HashSet<int> _finalStates = new();

        public int StartState { get; }

        public TableProgramBuilder(int startState)
        {
            StartState = startState;
            _states.Add(startState);
        }

        public IProgramBuilder AddState(int state, bool isFinal = false)
        {
            _states.Add(state);

            if (isFinal)
                _finalStates.Add(state);

            return this;
        }

        public IProgramBuilder AddFinalState(int state)
        {
            _states.Add(state);
            _finalStates.Add(state);
            return this;
        }

        public IProgramBuilder RemoveTransition(int state, Symbol headSymbol)
        {
            var key = (state, headSymbol);
            _transitions.Remove(key);
            return this;
        }

        public IProgramBuilder AddTransition(
            int currentState,
            Symbol headSymbol,
            Transition transition)
        {
            var key = (currentState, headSymbol);

            if (!_transitions.TryAdd(key, transition))
                throw new InvalidOperationException(
                    $"Transition already defined for state {currentState} and symbol {headSymbol}");

            _states.Add(currentState);
            _states.Add(transition.ToState);

            return this;
        }

        public IProgram Build()
        {
            Validate();

            // Freeze collections
            return new TableProgram(
                StartState,
                new Dictionary<(int, Symbol), Transition>(_transitions),
                new HashSet<int>(_states),
                new HashSet<int>(_finalStates));
        }

        private void Validate()
        {
            if (!_states.Contains(StartState))
                throw new InvalidOperationException("Start state must be part of the state set.");
        }
    }
}
