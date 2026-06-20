using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.ProgramGraph
{
    /// <summary>Symbols used when emitting complete rows from a visual block state.</summary>
    public static class TapeAlphabet
    {
        /// <summary>All tape symbols used in transition-table emission (excludes <see cref="Symbol.None"/> unless added later).</summary>
        public static readonly Symbol[] All =
        {
            Symbol.Blank,
            Symbol.Gear,
            Symbol.Mark,
            Symbol.Screw,
            Symbol.Nut,
            Symbol.None
        };
    }
}
