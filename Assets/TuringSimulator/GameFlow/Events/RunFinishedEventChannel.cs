using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Run Finished", fileName = "RunFinishedChannel")]
    public sealed class RunFinishedEventChannel : EventChannelSO<RunFinishedEventData>
    {
    }
}
