using System;
using System.Collections;
using System.Collections.Generic;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    public class ConveyorTapeVisual : MonoBehaviour, ITapeVisual
    {
        [Header("Layout")]
        [SerializeField]
        [Tooltip("Parent of all Tape Cells. This transform slides left/right and keeps that offset; the Tape conveyor stays still.")]
        private Transform cellsRoot;

        [SerializeField] private float cellSpacing = 1f;
        [SerializeField] private float moveDuration = 0.25f;

        private readonly List<TapeCellView> _pool = new();
        private readonly List<TapeCellView> _cells = new();
        private readonly List<TapeCellView> _grown = new();
        private readonly Dictionary<int, Symbol> _tape = new();
        private Vector3 _cellsRootOrigin;
        private int _originHeadIndex;
        private int _firstTapeIndex;

        public int HeadIndex { get; private set; }

        public void Initialize()
        {
            if (cellsRoot == null)
                throw new InvalidOperationException(
                    $"{nameof(ConveyorTapeVisual)} on '{name}' is missing Cell Root.");

            _cellsRootOrigin = cellsRoot.localPosition;
            CollectPool();
            RestorePool();
            BindWindow(_originHeadIndex);
            LayoutCells();
            RefreshSymbols();
        }

        public void SetTape(IReadOnlyList<Symbol> symbols, int headIndex)
        {
            if (symbols == null)
                throw new ArgumentNullException(nameof(symbols));

            _tape.Clear();
            for (int i = 0; i < symbols.Count; i++)
                _tape[i] = symbols[i];

            HeadIndex = headIndex;
            RestorePool();
            ResetCellRootPosition();
            BindWindow(headIndex);
            LayoutCells();
            RefreshSymbols();
        }

        public IEnumerator MoveHead(MoveDirection direction)
        {
            float offset = ConveyorTapeWindow.MoveRootDeltaX(direction, cellSpacing);
            if (offset == 0f)
                yield break;

            int nextHead = HeadIndex + (int)direction;
            EnsureCellForTapeIndex(nextHead);
            yield return MoveCellRoot(offset);

            HeadIndex = nextHead;
            Debug.Log($"[ConveyorTape] Tape moved to {direction}");
        }

        public IEnumerator ShowWrite(Symbol symbol)
        {
            _tape[HeadIndex] = symbol;
            EnsureCellForTapeIndex(HeadIndex);
            if (ConveyorTapeWindow.TryGetCellIndex(
                    _firstTapeIndex, HeadIndex, _cells.Count, out int cellIndex))
            {
                _cells[cellIndex].SetSymbol(symbol);
            }

            Debug.Log($"[ConveyorTape] Symbol {symbol} written at tape index {HeadIndex}");
            yield return null;
        }

        public IEnumerator ShowRead()
        {
            yield break;
        }

        public void Reset()
        {
            HeadIndex = 0;
            _tape.Clear();
            RestorePool();
            ResetCellRootPosition();
            if (_cells.Count == 0)
                return;

            BindWindow(0);
            LayoutCells();
            RefreshSymbols();
        }

        private void CollectPool()
        {
            _pool.Clear();
            cellsRoot.GetComponentsInChildren(true, _pool);
            if (_pool.Count == 0)
                throw new InvalidOperationException(
                    $"{nameof(ConveyorTapeVisual)} Cell Root '{cellsRoot.name}' has no {nameof(TapeCellView)} children.");

            _pool.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
        }

        private void RestorePool()
        {
            for (int i = 0; i < _grown.Count; i++)
            {
                var grown = _grown[i];
                if (grown == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(grown.gameObject);
                else
                    DestroyImmediate(grown.gameObject);
            }

            _grown.Clear();
            _cells.Clear();
            _cells.AddRange(_pool);
        }

        private void BindWindow(int originHeadIndex)
        {
            _originHeadIndex = originHeadIndex;
            _firstTapeIndex = ConveyorTapeWindow.FirstTapeIndex(originHeadIndex, _cells.Count);
        }

        private void EnsureCellForTapeIndex(int tapeIndex)
        {
            while (ConveyorTapeWindow.TryGetGrowDirection(
                       _firstTapeIndex, _cells.Count, tapeIndex, out var side))
            {
                Grow(side);
            }
        }

        private void Grow(MoveDirection side)
        {
            var template = _pool[0];
            var clone = Instantiate(template.gameObject, cellsRoot, false);
            var view = clone.GetComponent<TapeCellView>();
            int tapeIndex;
            if (side == MoveDirection.Left)
            {
                tapeIndex = _firstTapeIndex - 1;
                _firstTapeIndex = tapeIndex;
                _cells.Insert(0, view);
            }
            else
            {
                tapeIndex = ConveyorTapeWindow.LastTapeIndex(_firstTapeIndex, _cells.Count) + 1;
                _cells.Add(view);
            }

            clone.name = $"TapeCell ({tapeIndex})";
            clone.transform.localPosition = ConveyorTapeWindow.CellLocalPositionForTapeIndex(
                tapeIndex, _originHeadIndex, cellSpacing);
            _grown.Add(view);
            view.SetSymbol(
                _tape.TryGetValue(tapeIndex, out var symbol) ? symbol : Symbol.Blank);
        }

        private void LayoutCells()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                int tapeIndex = ConveyorTapeWindow.TapeIndexForCell(_firstTapeIndex, i);
                _cells[i].transform.localPosition =
                    ConveyorTapeWindow.CellLocalPositionForTapeIndex(
                        tapeIndex, _originHeadIndex, cellSpacing);
            }
        }

        private void RefreshSymbols()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].SetSymbol(
                    ConveyorTapeWindow.ResolveCellSymbol(_tape, _firstTapeIndex, i));
            }
        }

        private void ResetCellRootPosition()
        {
            if (cellsRoot != null)
                cellsRoot.localPosition = _cellsRootOrigin;
        }

        private IEnumerator MoveCellRoot(float deltaX)
        {
            Vector3 start = cellsRoot.localPosition;
            Vector3 end = start + Vector3.right * deltaX;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                float t = elapsed / moveDuration;
                cellsRoot.localPosition = Vector3.Lerp(start, end, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            cellsRoot.localPosition = end;
        }

        private void OnValidate()
        {
            if (cellsRoot == null)
                Debug.LogWarning($"{name}: missing Cell Root.", this);
        }
    }
}
