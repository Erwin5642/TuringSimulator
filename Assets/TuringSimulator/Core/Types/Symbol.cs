using System;

namespace TuringSimulator.Core.Types
{
    public enum Symbol
    {
        None,
        Blank,
        Zero,
        Mark,
        One
    }

    public static class SymbolExtensions
    {
        /// <summary>
        /// Converts a Symbol enum to a character for display or testing.
        /// </summary>
        public static char ToChar(this Symbol symbol) => symbol switch
        {
            Symbol.Zero => '0',
            Symbol.One => '1',
            Symbol.Blank => '_',
            Symbol.Mark => 'M',
            Symbol.None => '?',
            _ => throw new ArgumentOutOfRangeException(nameof(symbol), $"Unknown symbol: {symbol}")
        };
        
        
        /// <summary>
        /// Converts a character to a Symbol enum.
        /// </summary>
        public static Symbol FromChar(this char c) => c switch
        {
            '0' => Symbol.Zero,
            '1' => Symbol.One,
            '_' => Symbol.Blank,
            'M' => Symbol.Mark,
            '?' => Symbol.None,
            _ => throw new ArgumentException($"Invalid character '{c}' for Symbol conversion", nameof(c))
        };
    }
}