/*using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tests.EditModeTests.Mocks;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace Tests.EditModeTests
{
    public class SimulationBufferTests
    {
        [TestCase("")]
        [TestCase("0")]
        [TestCase("101")]
        [TestCase("0101")]
        public async Task ProducesBinaryString_WritesAndMovesRightCorrectly(string input)
        {
            var program = TestProgramFactory.ProducesBinaryString(input);

            var buffer = new SimulationBuffer();
            var tape = new TestSimulationTape();
            var engine = new SimulationEngine();

            // Act
            await engine.RunSimulationAsync(program, tape, buffer, CancellationToken.None);

            // Assert — global halt condition
            Assert.That(buffer.Status, Is.EqualTo(HaltStatus.Accept));

            int expectedHeadPosition = 0;
            int writeIndex = 0;
            bool haltObserved = false;

            for (int i = 0; buffer.TryGetStep(i, out var step); i++)
            {
                switch (step.Kind)
                {
                    case ResultKind.Diff:
                    {
                        var diff = step.AsDiff();

                        // Head was where we expected
                        Assert.That(diff.HeadIndexBefore, Is.EqualTo(expectedHeadPosition),
                            $"Unexpected head position before step {i}");

                        // Correct symbol written
                        var expectedSymbol = input[writeIndex] == '0'
                            ? Symbol.Zero
                            : Symbol.One;

                        Assert.That(diff.SymbolAfter, Is.EqualTo(expectedSymbol),
                            $"Wrong symbol written at step {i}");

                        // Must overwrite Blank only
                        Assert.That(diff.SymbolBefore, Is.EqualTo(Symbol.Blank),
                            $"Overwriting non-blank cell at step {i}");

                        // Must move Right
                        Assert.That(diff.DirectionMoved, Is.EqualTo(MoveDirection.Right),
                            $"Invalid movement at step {i}");

                        // Head position updated correctly
                        Assert.That(diff.HeadIndexAfter, Is.EqualTo(expectedHeadPosition + 1),
                            $"Head moved incorrectly at step {i}");

                        expectedHeadPosition++;
                        writeIndex++;
                        break;
                    }

                    case ResultKind.Halt:
                        haltObserved = true;
                        Assert.That(step.AsHalt(), Is.EqualTo(HaltStatus.Accept));
                        break;

                    default:
                        Assert.Fail($"Unexpected step kind {step.Kind}");
                        break;
                }
            }

            // Final invariants
            Assert.That(haltObserved, Is.True, "Machine never halted");
            Assert.That(writeIndex, Is.EqualTo(input.Length),
                "Not all input symbols were written");
        }
    }
}*/