using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Tape Write", fileName = "TapeWriteChannel")]
    public sealed class TapeWriteEventChannel : EventChannelSO<TapeWriteEventData>
    {
    }
}
