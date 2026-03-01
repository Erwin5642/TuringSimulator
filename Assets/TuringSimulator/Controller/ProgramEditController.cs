using System;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.Controller
{
    public sealed class ProgramEditController : IProgramEditController
    {
        private bool _blockEdition;
        private IProgramBuilder _builder;
        private IProgram _current;

        public IProgram Current => _current;

        public event Action<IProgram> OnProgramChanged;

        public ProgramEditController()
        {
            _builder = new TableProgramBuilder(0);
            Rebuild();
        }

        public void AddTransition(
            int state,
            Symbol read,
            Transition transition)
        {
            if (_blockEdition) return;
            
            _builder.AddTransition(state, read, transition);
            Debug.Log($"[EditProgramController] Added transition to program ({state}, {read}) -> {transition.ToState} {transition.SymbolToWrite} {transition.DirectionToMove}");
            Rebuild();
        }

        public void RemoveTransition(int state, Symbol read)
        {
            if (_blockEdition) return;
            
            _builder.RemoveTransition(state, read);
            Rebuild();
        }

        public void AddFinalState(int state)
        {
            if (_blockEdition) return;
            
            _builder.AddFinalState(state);
            Rebuild();
        }

        public void Clear()
        {
            if (_blockEdition) return;
            
            _builder = new TableProgramBuilder(0);
            Rebuild();
        }

        public void Enable()
        {
            _blockEdition = false;
            
            Rebuild();
        }

        public void Disable()
        {
            _blockEdition = true;
            
            Rebuild();
        }

        private void Rebuild()
        {
            _current = _builder.Build();
            OnProgramChanged?.Invoke(_current);
        }
    }
}