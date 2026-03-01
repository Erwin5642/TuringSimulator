using System.Collections;
using TuringSimulator.Core.Types;

namespace TuringSimulator.View.Machine.Halt
{
    /// <summary>
    /// Represents a visual indicator that reflects the machine halt status.
    /// Implementations may animate or transition over time.
    /// </summary>
    public interface IHaltStatusIndicator
    {
        void Initialize();
        IEnumerator Show(HaltStatus status);

        void Reset();
    }
}