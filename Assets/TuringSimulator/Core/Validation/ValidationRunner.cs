using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.Core.Validation
{
    public class ValidationRunner : IValidationRunner
    {
        // Simulation
        private ISimulationEngine _engine;
        private ISimulationBuffer[] _buffers;
        private IProgram _program;
        private CancellationTokenSource _cts;
        
        // Output from the simulation
        private SimulationTape[] _tapes;
        
        // Result expected from the tests
        private SimulationTape[] _expectedTapes;
        private HaltStatus[] _expectedStatuses;
        
        // Final result
        private bool[] _results;

        public bool AllPassed => _results.All(r => r);

        public ValidationRunner()
        {
            _engine = new SimulationEngine();
        }
        
        public void SetTests(ValidationTest[] tests)
        {
            if (tests == null || tests.Length == 0) throw new ArgumentNullException(nameof(tests));

            _tapes = new SimulationTape[tests.Length];
            _buffers = new ISimulationBuffer[tests.Length];
            _expectedTapes = new SimulationTape[tests.Length];
            _expectedStatuses = new HaltStatus[tests.Length];
            _results = new bool[tests.Length];

            for (int i = 0; i < tests.Length; i++)
            {
                var test = tests[i];
                _buffers[i] = new SimulationBuffer();
                _tapes[i] = new SimulationTape(test.headIndex, test.initialSymbols);
                _expectedTapes[i] = new SimulationTape(test.expectedHeadIndex, test.expectedSymbols);
                _expectedStatuses[i] = test.expectedStatus;
            }
        }

        public void SetProgram(IProgram program)
        {
            _program = program ?? throw new ArgumentNullException(nameof(program));
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            if (_program == null)
                throw new InvalidOperationException("A program must be set before calling this method.");
            if (_tapes == null || _buffers == null)
                throw new InvalidOperationException("Tests must be set before calling this method.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var tasks = new Task[_tapes.Length];

            for (int i = 0; i < _tapes.Length; i++)
            {
                int index = i;
                tasks[index] = Task.Run(async () =>
                {
                    await _engine.Run(_program, _tapes[index], _buffers[index], _cts.Token);
                    
                    _results[index] = _buffers[index].Status == _expectedStatuses[index]
                                       && _tapes[index].Snapshot().StructuralEquals(_expectedTapes[index].Snapshot());

                }, _cts.Token);
            }

            await Task.WhenAll(tasks);

            Debug.Log("[Validation] Simulation results");
            foreach (var res in _results)
            {
                Debug.Log(res);    
            }

            foreach (var buffer in _buffers)
            {
                Debug.Log(buffer.Status);
                var i = 0;
                while (buffer.TryGetStep(i++, out var step))
                {
                    Debug.Log(step);
                }
            }

            foreach (var output in _tapes)
            {
                foreach (var (index, symbol) in output.Snapshot().Cells)
                {
                    Debug.Log($"{index}, {symbol}");
                }
            }

            Debug.Log("[Validation] Expected: ");
            foreach (var status in _expectedStatuses)
            {
                Debug.Log(status);  
            }
            
            foreach (var expected in _expectedTapes)
            {
                foreach (var (index, symbol) in expected.Snapshot().Cells)
                {
                    Debug.Log($"{index}, {symbol}");
                }
            }
        }
        
        public void Cancel()
        {
            _cts?.Cancel();
        }
    }
}