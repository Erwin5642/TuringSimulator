using System;
using System.Linq;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Types;

namespace Tests.EditModeTests.Mocks
{
    /// <summary>
    /// Factory for generating test Turing Machine programs for unit tests.
    /// Methods are named to describe the program's behavior or language.
    /// </summary>
    public static class TestProgramFactory
    {
        // ───────── Acceptance / Rejection ─────────

        /// <summary>
        /// Halts immediately and accepts any input.
        /// </summary>
        public static IProgram AlwaysAccepts()
            => new TableProgramBuilder(startState: 0)
                .AddFinalState(0)
                .Build();

        /// <summary>
        /// Never has a final state, so it rejects any input immediately.
        /// </summary>
        public static IProgram AlwaysRejects()
            => new TableProgramBuilder(startState: 0).Build();

        /// <summary>
        /// Accepts only blank input (empty tape).
        /// </summary>
        public static IProgram AcceptsOnlyEmptyInput()
        {
            var builder = new TableProgramBuilder(startState: 0);
            return builder
                .AddFinalState(1)
                .AddTransition(
                    0, Symbol.Blank,
                    new Transition(1, Symbol.Blank, MoveDirection.Stay))
                .Build();
        }

        // ───────── Halting / Stress Programs ─────────

        /// <summary>
        /// Loops forever on any input without halting.
        /// </summary>
        public static IProgram NeverHalts()
        {
            var builder = new TableProgramBuilder(startState: 0);
            return builder
                .AddTransition(0, Symbol.Zero, new Transition(0, Symbol.Zero, MoveDirection.Stay))
                .AddTransition(0, Symbol.One, new Transition(0, Symbol.One, MoveDirection.Stay))
                .AddTransition(0, Symbol.Blank, new Transition(0, Symbol.Blank, MoveDirection.Stay))
                .Build();
        }

        /// <summary>
        /// Moves right indefinitely on any input without halting.
        /// </summary>
        public static IProgram MovesRightIndefinitely()
        {
            var builder = new TableProgramBuilder(startState: 0);
            return builder
                .AddTransition(0, Symbol.Zero, new Transition(0, Symbol.Zero, MoveDirection.Right))
                .AddTransition(0, Symbol.One, new Transition(0, Symbol.One, MoveDirection.Right))
                .AddTransition(0, Symbol.Blank, new Transition(0, Symbol.Blank, MoveDirection.Right))
                .Build();
        }

        // ───────── Output / Transformation Programs ─────────

        /// <summary>
        /// Writes a single given symbol and halts immediately.
        /// </summary>
        public static IProgram ProducesSingleSymbol(Symbol symbol)
        {
            var builder = new TableProgramBuilder(startState: 0);
            return builder
                .AddFinalState(1)
                .AddTransition(
                    0, Symbol.Blank,
                    new Transition(1, symbol, MoveDirection.Stay))
                .Build();
        }

        /// <summary>
        /// Writes the given binary string on a blank tape and halts.
        /// </summary>
        /// <param name="str">Binary string to write (only '0' and '1').</param>
        /// <returns>A halting program that writes <paramref name="str"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="str"/> contains characters other than '0' or '1'.</exception>
        public static IProgram ProducesBinaryString(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            var builder = new TableProgramBuilder(startState: 0);
            int currentState = 0;

            foreach (var symbolToWrite in str.Select(c => c switch
            {
                '0' => Symbol.Zero,
                '1' => Symbol.One,
                _ => throw new ArgumentException(
                    $"Invalid character '{c}'. Only '0' and '1' are allowed.", nameof(str))
            }))
            {
                builder.AddTransition(
                    currentState, Symbol.Blank,
                    new Transition(currentState + 1, symbolToWrite, MoveDirection.Right)
                );

                currentState++;
            }

            builder.AddFinalState(currentState);
            return builder.Build();
        }

        /// <summary>
        /// Inverts binary symbols (0 ↔ 1) on the tape and halts.
        /// </summary>
        public static IProgram InvertsBinaryAndHalts()
        {
            var builder = new TableProgramBuilder(startState: 0);
            return builder
                .AddFinalState(1)
                .AddTransition(0, Symbol.Zero, new Transition(0, Symbol.One, MoveDirection.Right))
                .AddTransition(0, Symbol.One, new Transition(0, Symbol.Zero, MoveDirection.Right))
                .AddTransition(0, Symbol.Blank, new Transition(1, Symbol.Blank, MoveDirection.Stay))
                .Build();
        }

        // ───────── Tape / Movement Programs ─────────

        /// <summary>
        /// Writes '1', moves right, then moves left once, then halts.
        /// </summary>
        public static IProgram WritesThenMovesLeftOnce()
        {
            var builder = new TableProgramBuilder(startState: 0);
            return builder
                .AddFinalState(2)
                .AddTransition(0, Symbol.Blank, new Transition(1, Symbol.One, MoveDirection.Right))
                .AddTransition(1, Symbol.Blank, new Transition(2, Symbol.Blank, MoveDirection.Left))
                .Build();
        }

        // ───────── Language Recognition Programs ─────────

        /// <summary>
        /// Accepts binary strings with an even number of '1's.
        /// </summary>
        public static IProgram AcceptsBinaryWithEvenOnes()
        {
            var builder = new TableProgramBuilder(startState: 0);

            return builder
                // Even state
                .AddFinalState(0)
                .AddTransition(0, Symbol.One, new Transition(1, Symbol.One, MoveDirection.Right))
                .AddTransition(0, Symbol.Zero, new Transition(0, Symbol.Zero, MoveDirection.Right))
                .AddTransition(0, Symbol.Blank, new Transition(0, Symbol.Blank, MoveDirection.Stay))

                // Odd state
                .AddTransition(1, Symbol.One, new Transition(0, Symbol.One, MoveDirection.Right))
                .AddTransition(1, Symbol.Zero, new Transition(1, Symbol.Zero, MoveDirection.Right))
                .AddTransition(1, Symbol.Blank, new Transition(1, Symbol.Blank, MoveDirection.Stay))
                .Build();
        }

        /// <summary>
        /// Accepts only binary palindromes.
        /// </summary>
        public static IProgram AcceptsOnlyBinaryPalindromes()
        {
            var b = new TableProgramBuilder(startState: 0);

            return b
                .AddFinalState(6)
                // Scan left → right
                .AddTransition(0, Symbol.Zero, new Transition(1, Symbol.Mark, MoveDirection.Right))
                .AddTransition(0, Symbol.One, new Transition(2, Symbol.Mark, MoveDirection.Right))
                .AddTransition(0, Symbol.Blank, new Transition(6, Symbol.Blank, MoveDirection.Stay))
                // Find matching 0
                .AddTransition(1, Symbol.Zero, new Transition(1, Symbol.Zero, MoveDirection.Right))
                .AddTransition(1, Symbol.One, new Transition(1, Symbol.One, MoveDirection.Right))
                .AddTransition(1, Symbol.Blank, new Transition(3, Symbol.Blank, MoveDirection.Left))
                .AddTransition(3, Symbol.Zero, new Transition(4, Symbol.Mark, MoveDirection.Left))
                // Find matching 1
                .AddTransition(2, Symbol.Zero, new Transition(2, Symbol.Zero, MoveDirection.Right))
                .AddTransition(2, Symbol.One, new Transition(2, Symbol.One, MoveDirection.Right))
                .AddTransition(2, Symbol.Blank, new Transition(5, Symbol.Blank, MoveDirection.Left))
                .AddTransition(5, Symbol.One, new Transition(4, Symbol.Mark, MoveDirection.Left))
                // Return to start
                .AddTransition(4, Symbol.Zero, new Transition(4, Symbol.Zero, MoveDirection.Left))
                .AddTransition(4, Symbol.One, new Transition(4, Symbol.One, MoveDirection.Left))
                .AddTransition(4, Symbol.Mark, new Transition(0, Symbol.Mark, MoveDirection.Right))
                .Build();
        }
    }
}