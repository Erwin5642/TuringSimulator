using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Hand Gesture Performed", fileName = "HandGesturePerformedChannel")]
    public sealed class HandGesturePerformedEventChannel : EventChannelSO<HandGesturePerformedEventData>
    {
    }
}
