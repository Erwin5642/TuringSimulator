using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Run Requested", fileName = "RunRequestedChannel")]
    public sealed class RunRequestedEventChannel : EventChannelSO<RunRequestedEventData>
    {
    }
}
