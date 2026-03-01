/*using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tests.EditModeTests.Mocks;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Types;

namespace Tests.EditModeTests
{
    public class SimulationEngineTests
    {
        [Test]
        public async Task Simulation_Accepts_SimpleProgram()
        {
            // Arrange
            var builder = new TableProgramBuilder(startState: 0);
            var program = builder
                .AddFinalState(1)
                .AddTransition(0, Symbol.One, new Transition(1, Symbol.One, MoveDirection.Stay))
                .Build();

            var tape = new TestSimulationTape(Symbol.One);
            var buffer = new SimulationBuffer();
            var engine = new SimulationEngine();

            // Act
            await engine.RunSimulationAsync(
                program,
                tape,
                buffer,
                CancellationToken.None);

            // Assert
            Assert.AreEqual(HaltStatus.Accept, buffer.Status);
        }

        [Test]
        public async Task Simulation_RewritesSymbol_AndAccepts()
        {
            // Program:
            // If we read One, rewrite to Zero and halt in accept state.
            var builder = new TableProgramBuilder(startState: 0);
            var program = builder
                .AddFinalState(1)
                .AddTransition(0, Symbol.One, new Transition(1, Symbol.Zero, MoveDirection.Stay))
                .Build();

            var tape = new TestSimulationTape(Symbol.One);
            var buffer = new SimulationBuffer();
            var engine = new SimulationEngine();

            await engine.RunSimulationAsync(program, tape, buffer, CancellationToken.None);

            Assert.AreEqual(HaltStatus.Accept, buffer.Status);
            Assert.AreEqual(Symbol.Zero, tape.Read());
        }

        [Test]
        public async Task Simulation_Rejects_WhenNoTransitionExists()
        {
            var builder = new TableProgramBuilder(startState: 0);
            var program = builder
                .AddFinalState(1)
                // No transition
                .Build();

            var tape = new TestSimulationTape(Symbol.Zero);
            var buffer = new SimulationBuffer();
            var engine = new SimulationEngine();

            await engine.RunSimulationAsync(program, tape, buffer, CancellationToken.None);

            Assert.AreEqual(HaltStatus.Reject, buffer.Status);
        }

        [Test]
        public async Task Simulation_RewritesAllSymbolsUntilBlank()
        {
            // Program:
            // State 0:
            //   One   -> write Zero, move right
            //   Blank -> accept
            var builder = new TableProgramBuilder(startState: 0);
            var program = builder
                .AddFinalState(1)
                .AddTransition(0, Symbol.One, new Transition(0, Symbol.Zero, MoveDirection.Right))
                .AddTransition(0, Symbol.Blank, new Transition(1, Symbol.Blank, MoveDirection.Stay))
                .Build();

            var tape = new TestSimulationTape(Symbol.One, Symbol.One, Symbol.One);
            var buffer = new SimulationBuffer();
            var engine = new SimulationEngine();

            await engine.RunSimulationAsync(program, tape, buffer, CancellationToken.None);

            Assert.AreEqual(HaltStatus.Accept, buffer.Status);

            // Validate entire tape
            tape.ResetHead();
            Assert.AreEqual(Symbol.Zero, tape.Read());
            tape.Move(MoveDirection.Right);
            Assert.AreEqual(Symbol.Zero, tape.Read());
            tape.Move(MoveDirection.Right);
            Assert.AreEqual(Symbol.Zero, tape.Read());
        }

        [Test]
        public async Task Simulation_CanBeCancelled()
        {
            var builder = new TableProgramBuilder(startState: 0);
            var program = builder
                .AddTransition(0, Symbol.One, new Transition(0, Symbol.One, MoveDirection.Stay))
                .Build();

            var tape = new TestSimulationTape(Symbol.One);
            var buffer = new SimulationBuffer();
            var engine = new SimulationEngine();

            using var cts = new CancellationTokenSource();
            
            cts.Cancel();
            
            var task = engine.RunSimulationAsync(program, tape, buffer, cts.Token);

            await task;

            Assert.AreEqual(HaltStatus.Aborted, buffer.Status);
        }

        [Test]
        public async Task Engine_CanRunMultipleSimulationsConcurrently()
        {
            var engine = new SimulationEngine();

            var builder = new TableProgramBuilder(startState: 0);
            var program = builder
                .AddTransition(0, Symbol.One,
                    new Transition(0, Symbol.One, MoveDirection.Stay))
                .Build();
            
            var tasks = new List<Task>();
            var buffers = new List<BlockingSimulationBuffer>();
            for (var i = 0; i < 20; i++)
            {
                var tape = new TestSimulationTape(Symbol.One);

                var buffer = new BlockingSimulationBuffer();
                buffers.Add(buffer);

                 tasks.Add(engine.RunSimulationAsync(program, tape, buffer, CancellationToken.None));
            }
            
            // Ensure BOTH simulations are running
            await Task.WhenAll(buffers.Select(b => b.Started));

            // Optional observability assertions
            Assert.That(engine.IsRunning, Is.True);
            Assert.That(engine.ActiveSimulations, Is.EqualTo(20));

            // Allow completion
            foreach (var buffer in buffers) buffer.Release();

            await Task.WhenAll(tasks);
            
            Assert.That(engine.IsRunning, Is.False);
        }
    }
}*/