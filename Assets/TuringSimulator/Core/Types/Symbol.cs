using System;

namespace TuringSimulator.Core.Types
{
    /// <summary>
    /// Tape symbols. Explicit numeric values preserve Unity asset serialization
    /// (legacy Zero=2, Mark=3, One=4 → Gear, Mark, Screw).
    /// </summary>
    public enum Symbol
    {
        None = 0,
        Blank = 1,
        Gear = 2,
        Mark = 3,
        Screw = 4,
        Nut = 5,
    }

    public static class SymbolExtensions
    {
        /// <summary>
        /// Converts a Symbol enum to a character for display or testing.
        /// </summary>
        public static char ToChar(this Symbol symbol) => symbol switch
        {
            Symbol.Gear => 'G',
            Symbol.Nut => 'N',
            Symbol.Screw => 'S',
            Symbol.Blank => '_',
            Symbol.Mark => 'M',
            Symbol.None => '?',
            _ => throw new ArgumentOutOfRangeException(nameof(symbol), $"Unknown symbol: {symbol}")
        };


        /// <summary>
        /// Converts a character to a Symbol enum.
        /// </summary>
        public static Symbol FromChar(this char c) => char.ToUpperInvariant(c) switch
        {
            'G' => Symbol.Gear,
            'N' => Symbol.Nut,
            'S' => Symbol.Screw,
            '_' => Symbol.Blank,
            'M' => Symbol.Mark,
            '?' => Symbol.None,
            _ => throw new ArgumentException($"Invalid character '{c}' for Symbol conversion", nameof(c))
        };
    }
}
