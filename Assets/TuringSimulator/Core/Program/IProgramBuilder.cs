using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Program
{
    /// <summary>
    /// Responsible for constructing a Turing machine program.
    /// The builder is mutable; the built program is immutable.
    /// </summary>
    public interface IProgramBuilder
    {
        int StartState { get; }

        IProgramBuilder AddState(int state, bool isFinal = false);

        IProgramBuilder AddFinalState(int state);
        
        IProgramBuilder RemoveTransition(int state, Symbol headSymbol);

        IProgramBuilder AddTransition(
            int currentState,
            Symbol headSymbol,
            Transition transition);

        IProgram Build();
    }
}