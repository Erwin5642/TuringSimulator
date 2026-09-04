using TuringSimulator.Core.Types;

namespace TuringSimulator.View.Machine.Tape
{
    public static class TapeStepFeedbackRules
    {
        public static bool IsPhysicalMaterial(Symbol symbol) =>
            symbol != Symbol.Blank && symbol != Symbol.None;

        public static bool IsReadMatch(Symbol readSymbol, Symbol writeSymbol) =>
            readSymbol == writeSymbol;

        public static TapeWriteEffectKind ResolveWriteEffect(Symbol before, Symbol after)
        {
            if (before == after)
                return TapeWriteEffectKind.None;

            if (!IsPhysicalMaterial(after))
                return IsPhysicalMaterial(before)
                    ? TapeWriteEffectKind.Delete
                    : TapeWriteEffectKind.None;

            return TapeWriteEffectKind.Write;
        }
    }
}
