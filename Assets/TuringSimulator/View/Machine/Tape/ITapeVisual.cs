using System.Collections;
using System.Collections.Generic;
using TuringSimulator.Core.Types;

namespace TuringSimulator.View.Machine.Tape
{
    /// <summary>
    /// Represents a visual component that presents tape operations
    /// of a Turing Machine, potentially animated over time.
    /// </summary>
    public interface ITapeVisual
    {
        int HeadIndex { get; }
        void Initialize();
        void SetTape(IReadOnlyList<Symbol> symbols, int headIndex);
        IEnumerator MoveHead(MoveDirection direction);
        IEnumerator ShowWrite(Symbol symbol);
        IEnumerator ShowRead();

        void Reset();
    }
}