/*using System.Collections;
using System.Collections.Generic;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine.Tape;
using UnityEngine;

namespace Tests.PlayModeTests.Mocks
{
    public class FakeTapeVisual : MonoBehaviour, ITapeVisual
    {
        private Symbol[] _symbols;
        public int HeadIndex { get; private set; }

        public void Initialize(Symbol[] symbols, int headIndex)
        {
            _symbols = symbols;
            HeadIndex = headIndex;
        }

        IEnumerator ITapeVisual.ShowWrite(Symbol symbol)
        {
            return ShowWrite(symbol);
        }

        IEnumerator ITapeVisual.ShowRead()
        {
            return ShowRead();
        }

        public void Initialize(IReadOnlyList<Symbol> symbols, int headIndex)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator ShowRead() { yield return null; }

        IEnumerator ITapeVisual.MoveHead(MoveDirection direction)
        {
            return MoveHead(direction);
        }

        public IEnumerator ShowWrite(Symbol symbol)
        {
            _symbols[HeadIndex] = symbol;
            yield return null;
        }

        public IEnumerator MoveHead(MoveDirection direction)
        {
            HeadIndex += (int)direction;
            yield return null;
        }

        public Symbol GetSymbolAt(int index) => _symbols[index];
    }
}*/