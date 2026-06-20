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
                .AddTransition(0, Symbol.Gear, new Transition(0, Symbol.Gear, MoveDirection.Stay))
                .AddTransition(0, Symbol.Nut, new Transition(0, Symbol.Nut, MoveDirection.Stay))
                .AddTransition(0, Symbol.Screw, new Transition(0, Symbol.Screw, MoveDirection.Stay))
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
                .AddTransition(0, Symbol.Gear, new Transition(0, Symbol.Gear, MoveDirection.Right))
                .AddTransition(0, Symbol.Nut, new Transition(0, Symbol.Nut, MoveDirection.Right))
                .AddTransition(0, Symbol.Screw, new Transition(0, Symbol.Screw, MoveDirection.Right))
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
        /// Writes the given tape string on a blank tape and halts.
        /// </summary>
        /// <param name="str">Characters G/g (gear), N/n (nut), S/s (screw).</param>
        public static IProgram ProducesBinaryString(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            var builder = new TableProgramBuilder(startState: 0);
            int currentState = 0;

            foreach (var symbolToWrite in str.Select(ParseTapeChar))
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

        static Symbol ParseTapeChar(char c) => char.ToUpperInvariant(c) switch
        {
            'G' => Symbol.Gear,
            'N' => Symbol.Nut,
            'S' => Symbol.Screw,
            _ => throw new ArgumentException(
                $"Invalid character '{c}'. Use G (gear), N (nut), or S (screw).")
        };

        /// <summary>
        /// Inverts gear ↔ screw on the tape (nut unchanged) and halts.
        /// </summary>
        public static IProgram InvertsBinaryAndHalts()
        {
            var builder = new TableProgramBuilder(startState: 0);
            return builder
                .AddFinalState(1)
                .AddTransition(0, Symbol.Gear, new Transition(0, Symbol.Screw, MoveDirection.Right))
                .AddTransition(0, Symbol.Screw, new Transition(0, Symbol.Gear, MoveDirection.Right))
                .AddTransition(0, Symbol.Nut, new Transition(0, Symbol.Nut, MoveDirection.Right))
                .AddTransition(0, Symbol.Blank, new Transition(1, Symbol.Blank, MoveDirection.Stay))
                .Build();
        }

        // ───────── Tape / Movement Programs ─────────

        /// <summary>
        /// Writes screw, moves right, then moves left once, then halts.
        /// </summary>
        public static IProgram WritesThenMovesLeftOnce()
        {
            var builder = new TableProgramBuilder(startState: 0);
            return builder
                .AddFinalState(2)
                .AddTransition(0, Symbol.Blank, new Transition(1, Symbol.Screw, MoveDirection.Right))
                .AddTransition(1, Symbol.Blank, new Transition(2, Symbol.Blank, MoveDirection.Left))
                .Build();
        }

        // ───────── Language Recognition Programs ─────────

        /// <summary>
        /// Accepts tapes with an even number of screws (S); gear/nut do not toggle parity.
        /// </summary>
        public static IProgram AcceptsBinaryWithEvenOnes()
        {
            var builder = new TableProgramBuilder(startState: 0);

            return builder
                // Even state
                .AddFinalState(0)
                .AddTransition(0, Symbol.Screw, new Transition(1, Symbol.Screw, MoveDirection.Right))
                .AddTransition(0, Symbol.Gear, new Transition(0, Symbol.Gear, MoveDirection.Right))
                .AddTransition(0, Symbol.Nut, new Transition(0, Symbol.Nut, MoveDirection.Right))
                .AddTransition(0, Symbol.Blank, new Transition(0, Symbol.Blank, MoveDirection.Stay))

                // Odd state
                .AddTransition(1, Symbol.Screw, new Transition(0, Symbol.Screw, MoveDirection.Right))
                .AddTransition(1, Symbol.Gear, new Transition(1, Symbol.Gear, MoveDirection.Right))
                .AddTransition(1, Symbol.Nut, new Transition(1, Symbol.Nut, MoveDirection.Right))
                .AddTransition(1, Symbol.Blank, new Transition(1, Symbol.Blank, MoveDirection.Stay))
                .Build();
        }

        /// <summary>
        /// Accepts only palindromes over gear and screw (two-symbol alphabet).
        /// </summary>
        public static IProgram AcceptsOnlyBinaryPalindromes()
        {
            var b = new TableProgramBuilder(startState: 0);

            return b
                .AddFinalState(6)
                // Scan left → right
                .AddTransition(0, Symbol.Gear, new Transition(1, Symbol.Mark, MoveDirection.Right))
                .AddTransition(0, Symbol.Screw, new Transition(2, Symbol.Mark, MoveDirection.Right))
                .AddTransition(0, Symbol.Blank, new Transition(6, Symbol.Blank, MoveDirection.Stay))
                // Find matching gear
                .AddTransition(1, Symbol.Gear, new Transition(1, Symbol.Gear, MoveDirection.Right))
                .AddTransition(1, Symbol.Screw, new Transition(1, Symbol.Screw, MoveDirection.Right))
                .AddTransition(1, Symbol.Blank, new Transition(3, Symbol.Blank, MoveDirection.Left))
                .AddTransition(3, Symbol.Gear, new Transition(4, Symbol.Mark, MoveDirection.Left))
                // Find matching screw
                .AddTransition(2, Symbol.Gear, new Transition(2, Symbol.Gear, MoveDirection.Right))
                .AddTransition(2, Symbol.Screw, new Transition(2, Symbol.Screw, MoveDirection.Right))
                .AddTransition(2, Symbol.Blank, new Transition(5, Symbol.Blank, MoveDirection.Left))
                .AddTransition(5, Symbol.Screw, new Transition(4, Symbol.Mark, MoveDirection.Left))
                // Return to start
                .AddTransition(4, Symbol.Gear, new Transition(4, Symbol.Gear, MoveDirection.Left))
                .AddTransition(4, Symbol.Screw, new Transition(4, Symbol.Screw, MoveDirection.Left))
                .AddTransition(4, Symbol.Mark, new Transition(0, Symbol.Mark, MoveDirection.Right))
                .Build();
        }
    }
}
