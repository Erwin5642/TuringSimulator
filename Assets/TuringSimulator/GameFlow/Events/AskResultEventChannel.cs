using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Ask Result", fileName = "AskResultChannel")]
    public sealed class AskResultEventChannel : EventChannelSO<AskResultEventData>
    {
    }
}
