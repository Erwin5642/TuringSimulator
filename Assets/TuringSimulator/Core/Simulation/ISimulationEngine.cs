using System.Collections;
using System.Threading;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Tape;


namespace TuringSimulator.Core.Simulation
{
    /// <summary>
    /// The "Producer". Responsible for running the Turing logic
    /// Should be used by a ISimulationRunner
    /// </summary>
    public interface ISimulationEngine
    {
        /// <summary>
        /// Begins the simulation calculation process as a main-thread coroutine.
        /// Yields once per step so callers can stay on the Unity main thread.
        /// </summary>
        /// <param name="program">The logic to execute.</param>
        /// <param name="initialTape">The starting data.</param>
        /// <param name="buffer">The shared storage where results are pushed.</param>
        /// <param name="ct">The cancellation token to stop simulation</param>
        IEnumerator Run(IProgram program, ITape initialTape, ISimulationBuffer buffer, CancellationToken ct);
    }
}
