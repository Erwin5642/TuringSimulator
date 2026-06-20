/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Types;
using Tests.EditModeTests.Mocks;
using TuringSimulator.Core.Simulation;

namespace Tests.EditModeTests
{
    [TestFixture]
    public class ProgramCorrectnessTests
    {
        // ───────── Test Case Source ─────────
        private static IEnumerable<TestCaseData> ProgramTestCases()
        {
            // Always accept programs
            yield return new TestCaseData(TestProgramFactory.AlwaysAccepts(), "")
                .SetName("AlwaysAccepts_HaltsImmediately");

            // Always reject programs
            yield return new TestCaseData(TestProgramFactory.AlwaysRejects(), "")
                .SetName("AlwaysRejects_NeverHalts");

            // Blank-only input programs
            yield return new TestCaseData(TestProgramFactory.AcceptsOnlyEmptyInput(), "")
                .SetName("AcceptsOnlyEmptyInput_BlankAccept");

            // Producing output
            yield return new TestCaseData(TestProgramFactory.ProducesSingleSymbol(Symbol.Gear), "")
                .SetName("ProducesSingleSymbol_WritesGear");
            
            yield return new TestCaseData(TestProgramFactory.ProducesSingleSymbol(Symbol.Screw), "")
                .SetName("ProducesSingleSymbol_WritesScrew");

            yield return new TestCaseData(TestProgramFactory.ProducesBinaryString("GSGS"), "")
                .SetName("ProducesTapeString_GSGS");

            yield return new TestCaseData(TestProgramFactory.InvertsBinaryAndHalts(), "GSGS")
                .SetName("InvertsGearScrew_GSGS");

            yield return new TestCaseData(TestProgramFactory.WritesThenMovesLeftOnce(), "")
                .SetName("WritesThenMovesLeftOnce_HeadMovesLeft");

            // Language recognition
            yield return new TestCaseData(TestProgramFactory.AcceptsBinaryWithEvenOnes(), "SGSG")
                .SetName("AcceptsEvenScrewCount_SGSG");

            yield return new TestCaseData(TestProgramFactory.AcceptsBinaryWithEvenOnes(), "SGS")
                .SetName("AcceptsEvenScrewCount_SGS");

            yield return new TestCaseData(TestProgramFactory.AcceptsOnlyBinaryPalindromes(), "SGSGS")
                .SetName("AcceptsPalindrome_SGSGS");

            yield return new TestCaseData(TestProgramFactory.AcceptsOnlyBinaryPalindromes(), "GSG")
                .SetName("AcceptsPalindrome_GSG");

            // Stress / non-halting
            yield return new TestCaseData(TestProgramFactory.NeverHalts(), "")
                .SetName("NeverHalts_LoopsForever");

            yield return new TestCaseData(TestProgramFactory.MovesRightIndefinitely(), "")
                .SetName("MovesRightIndefinitely_HeadMovesRight");
        }
        
        // ───────── Simulation Buffer Helper ─────────
        private async Task<(string tape, HaltStatus status)> RunProgramAsync(IProgram program, string input = "")
        {
            var buffer = new SimulationBuffer();
            var tape = new TestSimulationTape(input.Select(c => c.FromChar()).ToArray());
            var engine = new SimulationEngine();

            await engine.RunSimulationAsync(program, tape, buffer, CancellationToken.None);

            return (tape.ToArray().Select(s => s.ToChar()).ToString(), buffer.Status);
        }

        // ───────── Main Test ─────────
        [Test, TestCaseSource(nameof(ProgramTestCases))]
        public async Task FactoryPrograms_RunCorrectly(IProgram program, string input)
        {
            var (tapeOutput, status) = await RunProgramAsync(program, input);

            // Determine expected behavior based on program type
            var typeName = program.GetType().Name;

            if (program == TestProgramFactory.AlwaysAccepts() ||
                program == TestProgramFactory.AcceptsOnlyEmptyInput())
            {
                Assert.AreEqual(HaltStatus.Accept, status);
            }
            else if (program == TestProgramFactory.AlwaysRejects())
            {
                Assert.AreNotEqual(HaltStatus.Accept, status);
            }
            else if (program == TestProgramFactory.NeverHalts() ||
                     program == TestProgramFactory.MovesRightIndefinitely())
            {
                Assert.AreEqual(HaltStatus.StepLimitExceeded, status);
            }
            else if (program == TestProgramFactory.ProducesSingleSymbol(Symbol.Gear))
            {
                Assert.AreEqual(HaltStatus.Accept, status);
                Assert.AreEqual("G", tapeOutput);
            }
            else if (program == TestProgramFactory.ProducesSingleSymbol(Symbol.Screw))
            {
                Assert.AreEqual(HaltStatus.Accept, status);
                Assert.AreEqual("S", tapeOutput);
            }
            else if (program == TestProgramFactory.ProducesBinaryString("GSGS"))
            {
                Assert.AreEqual(HaltStatus.Accept, status);
                Assert.AreEqual("GSGS", tapeOutput);
            }
            else if (program == TestProgramFactory.InvertsBinaryAndHalts())
            {
                Assert.AreEqual(HaltStatus.Accept, status);
                // input GSGS → SGSG (gear ↔ screw)
                Assert.AreEqual("SGSG", tapeOutput);
            }
            else if (program == TestProgramFactory.WritesThenMovesLeftOnce())
            {
                Assert.AreEqual(HaltStatus.Accept, status);
                Assert.AreEqual("S", tapeOutput);
            }
            else if (program == TestProgramFactory.AcceptsBinaryWithEvenOnes())
            {
                if (input.Count(c => char.ToUpperInvariant(c) == 'S') % 2 == 0)
                    Assert.AreEqual(HaltStatus.Accept, status);
                else
                    Assert.AreNotEqual(HaltStatus.Accept, status);
            }
            else if (program == TestProgramFactory.AcceptsOnlyBinaryPalindromes())
            {
                var isPalindrome = input.SequenceEqual(input.Reverse());
                if (isPalindrome)
                    Assert.AreEqual(HaltStatus.Accept, status);
                else
                    Assert.AreNotEqual(HaltStatus.Accept, status);
            }
        }
    }
}
*/
