using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Halt Reached", fileName = "HaltReachedChannel")]
    public sealed class HaltReachedEventChannel : EventChannelSO<HaltReachedEventData>
    {
    }
}
