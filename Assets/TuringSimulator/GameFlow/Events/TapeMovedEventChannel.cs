using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Tape Moved", fileName = "TapeMovedChannel")]
    public sealed class TapeMovedEventChannel : EventChannelSO<TapeMovedEventData>
    {
    }
}
