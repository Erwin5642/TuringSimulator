using System;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Controller
{
    public interface IProgramEditController
    {
        IProgram Current { get; }

        bool CanEdit { get; }

        event Action<IProgram> OnProgramChanged;

        event Action<bool> EditingAvailabilityChanged;

        void AddTransition(int state, Symbol read, Transition transition);

        void RemoveTransition(int state, Symbol read);

        void AddFinalState(int state);

        void Clear();

        void ReplaceProgramBuilder(TableProgramBuilder newBuilder);

        void Enable();

        void Disable();
    }
}
