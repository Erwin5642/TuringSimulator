using System.Collections;
using TuringSimulator.GameFlow.Events;

namespace TuringSimulator.View.Machine.Outcome
{
    /// <summary>
    /// Visual indicator for level victory/defeat outcome.
    /// </summary>
    public interface ILevelOutcomeIndicator
    {
        void Initialize();
        void Reset();
        IEnumerator Show(LevelOutcomeKind outcome);
    }
}
