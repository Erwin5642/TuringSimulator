using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Thinking State Changed", fileName = "ThinkingStateChangedChannel")]
    public sealed class ThinkingStateChangedEventChannel : EventChannelSO<ThinkingStateChangedEventData>
    {
    }
}
