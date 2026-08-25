using TuringSimulator.Core.Types;

namespace TuringSimulator.View.Machine.Tape
{
    public enum TapeDebugKey
    {
        None = 0,
        MoveLeft,
        MoveRight,
        ArmWrite,
        WriteBlank,
        WriteGear,
        WriteNut,
        WriteScrew,
        Cancel
    }

    public readonly struct TapeDebugHotkeyOutcome
    {
        public TapeDebugHotkeyOutcome(bool awaitingWrite, MoveDirection? move, Symbol? write)
        {
            AwaitingWrite = awaitingWrite;
            Move = move;
            Write = write;
        }

        public bool AwaitingWrite { get; }
        public MoveDirection? Move { get; }
        public Symbol? Write { get; }
    }

    public static class TapeDebugHotkeyMapping
    {
        public static TapeDebugHotkeyOutcome Reduce(bool awaitingWrite, TapeDebugKey key)
        {
            if (key == TapeDebugKey.None)
                return new TapeDebugHotkeyOutcome(awaitingWrite, null, null);

            if (awaitingWrite)
            {
                if (key == TapeDebugKey.Cancel || key == TapeDebugKey.ArmWrite)
                    return new TapeDebugHotkeyOutcome(false, null, null);

                if (TryResolveWriteSymbol(key, out var symbol))
                    return new TapeDebugHotkeyOutcome(false, null, symbol);

                return new TapeDebugHotkeyOutcome(true, null, null);
            }

            if (key == TapeDebugKey.ArmWrite)
                return new TapeDebugHotkeyOutcome(true, null, null);

            if (TryResolveMove(key, out var direction))
                return new TapeDebugHotkeyOutcome(false, direction, null);

            return new TapeDebugHotkeyOutcome(false, null, null);
        }

        public static bool TryResolveMove(TapeDebugKey key, out MoveDirection direction)
        {
            switch (key)
            {
                case TapeDebugKey.MoveLeft:
                    direction = MoveDirection.Left;
                    return true;
                case TapeDebugKey.MoveRight:
                    direction = MoveDirection.Right;
                    return true;
                default:
                    direction = MoveDirection.Stay;
                    return false;
            }
        }

        public static bool TryResolveWriteSymbol(TapeDebugKey key, out Symbol symbol)
        {
            switch (key)
            {
                case TapeDebugKey.WriteBlank:
                    symbol = Symbol.Blank;
                    return true;
                case TapeDebugKey.WriteGear:
                    symbol = Symbol.Gear;
                    return true;
                case TapeDebugKey.WriteNut:
                    symbol = Symbol.Nut;
                    return true;
                case TapeDebugKey.WriteScrew:
                    symbol = Symbol.Screw;
                    return true;
                default:
                    symbol = Symbol.None;
                    return false;
            }
        }
    }
}
