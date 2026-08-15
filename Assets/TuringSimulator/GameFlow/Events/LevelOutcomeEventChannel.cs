using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Level Outcome", fileName = "LevelOutcomeChannel")]
    public sealed class LevelOutcomeEventChannel : EventChannelSO<LevelOutcomeEventData>
    {
    }
}
