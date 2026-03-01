using System.Collections;
using System.Collections.Generic;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    public class ConveyorTapeVisual : MonoBehaviour, ITapeVisual
    {
        [Header("Layout")]
        [SerializeField] private Transform cellsRoot;
        [SerializeField] private float cellSpacing = 1f;
        [SerializeField] private float moveDuration = 0.25f;

        private readonly List<TapeCellView> _cells = new();
        private readonly Dictionary<int, Symbol> _tape = new ();
        private int CenterIndex => _cells.Count / 2;

        public int HeadIndex { get; private set; }

        public void Initialize()
        {
            _cells.AddRange(cellsRoot.GetComponentsInChildren<TapeCellView>());
        }

        public void SetTape(IReadOnlyList<Symbol> symbols, int headIndex)
        {
            _tape.Clear();
            for (int i = 0; i < symbols.Count; i++)
            {
                _tape[i] = symbols[i];
            }
            HeadIndex = headIndex;
            RefreshTape();
        }

        public IEnumerator MoveHead(MoveDirection direction)
        {
            float offset = direction == MoveDirection.Right ? -cellSpacing : cellSpacing;

            yield return MoveCells(offset);

            HeadIndex += direction == MoveDirection.Right ? 1 : -1;

            RefreshTape();
            Debug.Log($"[ConveyorTape] Tape moved to {direction}");
        }

        public IEnumerator ShowWrite(Symbol symbol)
        {
            _tape[HeadIndex] = symbol;
            Debug.Log($"[ConveyorTape] Symbol {symbol} written at tape index {HeadIndex}");
            yield return null;
        }

        public IEnumerator ShowRead()
        {
            yield break;
        }

        private void RefreshTape()
        {
            int center = CenterIndex;

            for (int i = 0; i < _cells.Count; i++)
            {
                int realIndex = HeadIndex + (i - center);

                Symbol symbol = (realIndex >= 0 && realIndex < _tape.Count)
                    ? _tape[realIndex]
                    : Symbol.Blank;

                _cells[i].SetSymbol(symbol);

                _cells[i].transform.localPosition =
                    Vector3.right * ((i - center) * cellSpacing);
            }
        }

        private IEnumerator MoveCells(float deltaX)
        {
            float elapsed = 0f;
            var startPositions = new Vector3[_cells.Count];

            for (int i = 0; i < _cells.Count; i++)
                startPositions[i] = _cells[i].transform.localPosition;

            while (elapsed < moveDuration)
            {
                float t = elapsed / moveDuration;

                for (int i = 0; i < _cells.Count; i++)
                    _cells[i].transform.localPosition =
                        startPositions[i] + Vector3.right * (deltaX * t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < _cells.Count; i++)
                _cells[i].transform.localPosition =
                    startPositions[i] + Vector3.right * deltaX;
        }

        public void Reset()
        {
            HeadIndex = 0;  
            _tape.Clear();
        }
    }
}
