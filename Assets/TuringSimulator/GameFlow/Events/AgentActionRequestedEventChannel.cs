using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Agent Action Requested", fileName = "AgentActionRequestedChannel")]
    public sealed class AgentActionRequestedEventChannel : EventChannelSO<AgentActionRequestedEventData>
    {
    }
}
