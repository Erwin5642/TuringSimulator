using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Run Started", fileName = "RunStartedChannel")]
    public sealed class RunStartedEventChannel : EventChannelSO<RunStartedEventData>
    {
    }
}
