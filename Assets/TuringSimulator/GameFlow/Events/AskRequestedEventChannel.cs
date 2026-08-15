using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Ask Requested", fileName = "AskRequestedChannel")]
    public sealed class AskRequestedEventChannel : EventChannelSO<AskRequestedEventData>
    {
    }
}
