using System;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Tape;

namespace TuringSimulator.Core.Simulation
{
    public class SimulationRunner : ISimulationRunner
    {
        private readonly SimulationEngine _engine;
        private readonly SimulationBuffer _buffer;
        private SimulationTape _tape;
        private IProgram _program;
        private CancellationTokenSource _cts;
        public SimulationRunner(SimulationBuffer buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _engine = new SimulationEngine();
        }

        public void SetTape(SimulationTape tape)
        {
            _tape = tape ?? throw new ArgumentNullException(nameof(tape));
        }

        public void SetProgram(IProgram program)
        {
            _program = program ?? throw new ArgumentNullException(nameof(program));
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            if (_program == null)
                throw new InvalidOperationException("A program must be set before calling this method.");
            if (_tape == null)
                throw new InvalidOperationException("A tape must be set before calling this method.");
            
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await Task.Run(() => _engine.Run(_program, _tape, _buffer, _cts.Token), _cts.Token);
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        public void Clear()
        {
            _tape.Clear();
            _buffer.Clear();
        }
    }
}