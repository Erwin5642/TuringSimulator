using System;
using ITS;
using TuringSimulator.Core.ProgramGraph;
using TuringSimulator.Core.Tape;

namespace TuringSimulator.GameFlow
{
    /// <summary>
    /// Rebuilds ITS bench payloads only when tape or program dirty flags are set.
    /// </summary>
    public sealed class ItsBenchStateCache : IItsBenchStateCache
    {
        readonly Func<TapeSnapshot> _captureTape;
        readonly Func<ProgramGraphSnapshot> _captureProgram;

        bool _tapeDirty = true;
        bool _programDirty = true;
        AskTapeDto _tapeCache;
        AskProgramDto _programCache;

        public ItsBenchStateCache(
            Func<TapeSnapshot> captureTape,
            Func<ProgramGraphSnapshot> captureProgram)
        {
            _captureTape = captureTape ?? throw new ArgumentNullException(nameof(captureTape));
            _captureProgram = captureProgram ?? throw new ArgumentNullException(nameof(captureProgram));
        }

        public void MarkTapeDirty() => _tapeDirty = true;

        public void MarkProgramDirty() => _programDirty = true;

        public AskTapeDto GetTapeForAsk()
        {
            if (!_tapeDirty)
                return _tapeCache;

            var snapshot = _captureTape();
            _tapeCache = snapshot == null ? null : ItsBenchTapeCompact.FromSnapshot(snapshot);
            _tapeDirty = false;
            return _tapeCache;
        }

        public AskProgramDto GetProgramForAsk()
        {
            if (!_programDirty)
                return _programCache;

            var snapshot = _captureProgram();
            _programCache = snapshot == null
                ? null
                : ItsBenchProgramSerializer.FromSnapshot(snapshot);
            _programDirty = false;
            return _programCache;
        }
    }
}
