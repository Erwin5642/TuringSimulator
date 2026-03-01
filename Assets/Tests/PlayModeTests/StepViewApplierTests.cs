/*
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tests.EditModeTests.Mocks;
using Tests.PlayModeTests.Mocks;
using TuringSimulator.Controller.Syncronizer;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace Tests.PlayModeTests
{
    public class StepViewApplierTests
    {
        private readonly SimulationBuffer _buffer = new SimulationBuffer();
        private readonly FakeView _view = new FakeView();
        private readonly SimulationEngine _engine = new SimulationEngine();
        private readonly TestSimulationTape _simulationTape = new TestSimulationTape();
        
        [SetUp]
        public void Setup()
        {
            _buffer.Clear();
            
            _ = _engine.RunSimulationAsync(
                TestProgramFactory.ProducesBinaryString("010110101"),
                _simulationTape,
                _buffer,
                CancellationToken.None);
        }
        
        [Test]
        public async Task StepViewApplier_CorrectlySyncronized()
        {
            var applier = new StepViewApplier(_buffer, _view);

            while (await applier.TryStepForward() != null) ;
            
            var viewTape = _view.GetTape();
            
            Assert.AreEqual(_buffer.Status, _view.Status);
            Assert.AreEqual(_simulationTape.ToArray(), viewTape);
        }
    }
}
*/
