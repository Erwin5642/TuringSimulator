using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Tape;

namespace TuringSimulator.Core.Validation
{
    public class ValidationRunner : IValidationRunner
    {
        private ValidationTest[] _tests = Array.Empty<ValidationTest>();
        private IProgram _program;
        private CancellationTokenSource _cts;
        private bool[] _results = Array.Empty<bool>();
        private ValidationResult[] _validationResults = Array.Empty<ValidationResult>();

        public bool AllPassed => _results.All(r => r);
        public System.Collections.Generic.IReadOnlyList<ValidationResult> Results => _validationResults;
        
        public void SetTests(ValidationTest[] tests)
        {
            if (tests == null || tests.Length == 0) throw new ArgumentNullException(nameof(tests));

            _tests = tests;
            _results = new bool[tests.Length];
            _validationResults = new ValidationResult[tests.Length];

            for (int i = 0; i < tests.Length; i++)
            {
                var test = tests[i];
                _validationResults[i] = new ValidationResult
                {
                    TestIndex = i,
                    ScenarioId = string.IsNullOrWhiteSpace(test.scenarioId)
                        ? $"test_{i + 1}"
                        : test.scenarioId,
                    ExpectedStatus = test.expectedStatus,
                    ExpectedTape = new SimulationTape(test.expectedHeadIndex, test.expectedSymbols),
                    Passed = false,
                };
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
            if (_tests == null || _tests.Length == 0)
                throw new InvalidOperationException("Tests must be set before calling this method.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var tasks = new Task[_tests.Length];

            for (int i = 0; i < _tests.Length; i++)
            {
                int index = i;
                tasks[index] = RunCase(index, _cts.Token);
            }

            await Task.WhenAll(tasks);
        }
        
        public void Cancel()
        {
            _cts?.Cancel();
        }

        async Task RunCase(int index, CancellationToken token)
        {
            var test = _tests[index];
            var inputTape = new SimulationTape(test.headIndex, test.initialSymbols);
            var expectedTape = new SimulationTape(test.expectedHeadIndex, test.expectedSymbols);
            var runner = new SimulationRunner(new SimulationBuffer());
            var result = await runner.Run(new SimulationRunRequest(_program, inputTape), token);
            var passed = result.HaltStatus == test.expectedStatus &&
                         result.FinalTape.StructuralEquals(expectedTape.Snapshot());

            _results[index] = passed;
            _validationResults[index].ActualStatus = result.HaltStatus;
            _validationResults[index].ActualTape = inputTape;
            _validationResults[index].Passed = passed;
            _validationResults[index].Error = passed
                ? string.Empty
                : "Actual status or tape differs from the expected result.";
        }
    }
}