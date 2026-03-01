using System;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Controller
{
    public interface IProgramEditController
    {
        IProgram Current {get; }

        event Action<IProgram> OnProgramChanged;
        
        void AddTransition(int state, Symbol read, Transition transition);
        void RemoveTransition(int state, Symbol read);

        void AddFinalState(int state);

        void Clear();

        void Enable();
        
        void Disable();
    }
}