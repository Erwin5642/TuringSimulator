using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Tape;

namespace TuringSimulator.Core.Simulation
{
    /// <summary>
    ///  A simulation runner controls the simulation engine and store the data needed for it 
    /// </summary>
    public interface ISimulationRunner
    {
        void SetTape(SimulationTape tape);
        void SetProgram(IProgram program);

        Task Start(CancellationToken cancellationToken = default);
        void Cancel();

        void Clear();
    }
}