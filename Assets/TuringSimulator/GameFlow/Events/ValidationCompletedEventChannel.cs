using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Validation Completed", fileName = "ValidationCompletedChannel")]
    public sealed class ValidationCompletedEventChannel : EventChannelSO<ValidationCompletedEventData>
    {
    }
}
