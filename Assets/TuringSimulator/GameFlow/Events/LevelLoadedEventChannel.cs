using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Level Loaded", fileName = "LevelLoadedChannel")]
    public sealed class LevelLoadedEventChannel : EventChannelSO<LevelLoadedEventData>
    {
    }
}
