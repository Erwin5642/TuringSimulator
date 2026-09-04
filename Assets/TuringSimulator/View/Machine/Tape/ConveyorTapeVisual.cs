using System;
using System.Collections;
using System.Collections.Generic;
using TuringSimulator.Core.Types;
using TuringSimulator.GameFlow.Events;
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

        [Header("Feedback")]
        [SerializeField]
        [Tooltip("Optional. Assign TapeStepFeedback for read sounds and write/delete particles.")]
        private MonoBehaviour stepFeedback;

        [SerializeField]
        [Tooltip("Optional. Raised when the tape starts and finishes sliding. EventChannelActionListener can Play/Stop move audio here.")]
        private TapeMovedEventChannel tapeMovedChannel;

        [SerializeField]
        [Tooltip("Optional. Raised at the start and end of a read beat. Filter IsMatch True/False for match vs mismatch cues.")]
        private TapeReadEventChannel tapeReadChannel;

        [SerializeField]
        [Tooltip("Optional. Raised at the start and end of a write/delete beat. Filter Effect Write or Delete.")]
        private TapeWriteEventChannel tapeWriteChannel;

        private readonly List<TapeCellView> _pool = new();
        private readonly List<TapeCellView> _cells = new();
        private readonly List<TapeCellView> _grown = new();
        private readonly Dictionary<int, Symbol> _tape = new();
        private Vector3 _cellsRootOrigin;
        private int _originHeadIndex;
        private int _firstTapeIndex;
        private ITapeStepFeedback _resolvedFeedback;
        private bool _feedbackResolved;
        private bool _moveCueActive;
        private bool _readCueActive;
        private bool _writeCueActive;
        private Symbol _activeReadSymbol;
        private Symbol _activeUpcomingWriteSymbol;
        private TapeWriteKind _activeWriteKind;
        private Symbol _activeWrittenSymbol;

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

            RaiseTapeMoved(TapeMovePhase.Started, direction);
            _moveCueActive = true;
            try
            {
                yield return MoveCellRoot(offset);
                HeadIndex = nextHead;
            }
            finally
            {
                if (_moveCueActive)
                {
                    _moveCueActive = false;
                    RaiseTapeMoved(TapeMovePhase.Finished, direction);
                }
            }

            Debug.Log($"[ConveyorTape] Tape moved to {direction}");
        }

        public IEnumerator ShowWrite(Symbol symbol)
        {
            var before = _tape.TryGetValue(HeadIndex, out var existing) ? existing : Symbol.Blank;
            _tape[HeadIndex] = symbol;
            EnsureCellForTapeIndex(HeadIndex);
            if (ConveyorTapeWindow.TryGetCellIndex(
                    _firstTapeIndex, HeadIndex, _cells.Count, out int cellIndex))
            {
                _cells[cellIndex].SetSymbol(symbol);
            }

            Debug.Log($"[ConveyorTape] Symbol {symbol} written at tape index {HeadIndex}");

            var effect = TapeStepFeedbackRules.ResolveWriteEffect(before, symbol);
            if (!TryMapWriteKind(effect, out var kind))
            {
                yield return null;
                yield break;
            }

            RaiseTapeWrite(TapeWritePhase.Started, kind, symbol);
            _writeCueActive = true;
            _activeWriteKind = kind;
            _activeWrittenSymbol = symbol;
            try
            {
                var feedback = ResolveFeedback();
                if (feedback != null)
                    yield return feedback.PlayWrite(effect, HeadCellWorldPosition());
                else
                    yield return null;
            }
            finally
            {
                FinishWriteCueIfActive();
            }
        }

        public IEnumerator ShowRead(Symbol readSymbol, Symbol writeSymbol)
        {
            EnsureCellForTapeIndex(HeadIndex);
            RaiseTapeRead(TapeReadPhase.Started, readSymbol, writeSymbol);
            _readCueActive = true;
            _activeReadSymbol = readSymbol;
            _activeUpcomingWriteSymbol = writeSymbol;
            try
            {
                var feedback = ResolveFeedback();
                if (feedback != null)
                    yield return feedback.PlayRead(readSymbol, writeSymbol, HeadCellWorldPosition());
            }
            finally
            {
                FinishReadCueIfActive();
            }
        }

        public void Reset()
        {
            FinishMoveCueIfActive();
            FinishReadCueIfActive();
            FinishWriteCueIfActive();
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

        private void RaiseTapeMoved(TapeMovePhase phase, MoveDirection direction)
        {
            if (tapeMovedChannel == null)
                return;

            tapeMovedChannel.Raise(
                new TapeMovedEventData(
                    EventContextFactory.Create(nameof(ConveyorTapeVisual), HeadIndex.ToString()),
                    phase,
                    direction,
                    HeadCellWorldPosition()),
                this);
        }

        private void RaiseTapeRead(TapeReadPhase phase, Symbol readSymbol, Symbol writeSymbol)
        {
            if (tapeReadChannel == null)
                return;

            tapeReadChannel.Raise(
                new TapeReadEventData(
                    EventContextFactory.Create(nameof(ConveyorTapeVisual), HeadIndex.ToString()),
                    phase,
                    readSymbol,
                    writeSymbol,
                    TapeStepFeedbackRules.IsReadMatch(readSymbol, writeSymbol),
                    HeadCellWorldPosition()),
                this);
        }

        private void RaiseTapeWrite(TapeWritePhase phase, TapeWriteKind effect, Symbol symbol)
        {
            if (tapeWriteChannel == null)
                return;

            tapeWriteChannel.Raise(
                new TapeWriteEventData(
                    EventContextFactory.Create(nameof(ConveyorTapeVisual), HeadIndex.ToString()),
                    phase,
                    effect,
                    symbol,
                    HeadCellWorldPosition()),
                this);
        }

        private static bool TryMapWriteKind(TapeWriteEffectKind effect, out TapeWriteKind kind)
        {
            switch (effect)
            {
                case TapeWriteEffectKind.Write:
                    kind = TapeWriteKind.Write;
                    return true;
                case TapeWriteEffectKind.Delete:
                    kind = TapeWriteKind.Delete;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private void FinishMoveCueIfActive()
        {
            if (!_moveCueActive)
                return;

            _moveCueActive = false;
            RaiseTapeMoved(TapeMovePhase.Finished, MoveDirection.Stay);
        }

        private void FinishReadCueIfActive()
        {
            if (!_readCueActive)
                return;

            _readCueActive = false;
            RaiseTapeRead(TapeReadPhase.Finished, _activeReadSymbol, _activeUpcomingWriteSymbol);
        }

        private void FinishWriteCueIfActive()
        {
            if (!_writeCueActive)
                return;

            _writeCueActive = false;
            RaiseTapeWrite(TapeWritePhase.Finished, _activeWriteKind, _activeWrittenSymbol);
        }

        private ITapeStepFeedback ResolveFeedback()
        {
            if (_feedbackResolved)
                return _resolvedFeedback;

            _feedbackResolved = true;
            _resolvedFeedback = stepFeedback as ITapeStepFeedback;
            if (_resolvedFeedback == null)
                _resolvedFeedback = GetComponent<TapeStepFeedback>();
            return _resolvedFeedback;
        }

        private Vector3 HeadCellWorldPosition()
        {
            if (ConveyorTapeWindow.TryGetCellIndex(
                    _firstTapeIndex, HeadIndex, _cells.Count, out int cellIndex))
                return _cells[cellIndex].transform.position;

            return cellsRoot != null ? cellsRoot.position : transform.position;
        }

        private void OnValidate()
        {
            if (stepFeedback == null)
            {
                var found = GetComponent<TapeStepFeedback>();
                if (found != null)
                    stepFeedback = found;
            }

            if (cellsRoot == null)
                Debug.LogWarning($"{name}: missing Cell Root.", this);
            if (stepFeedback != null && stepFeedback is not ITapeStepFeedback)
                Debug.LogWarning($"{name}: Step Feedback must implement {nameof(ITapeStepFeedback)}.", this);
        }
    }
}
