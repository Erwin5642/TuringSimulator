using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Program Changed", fileName = "ProgramChangedChannel")]
    public sealed class ProgramChangedEventChannel : EventChannelSO<ProgramChangedEventData>
    {
    }
}
