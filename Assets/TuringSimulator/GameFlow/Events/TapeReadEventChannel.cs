using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Tape Read", fileName = "TapeReadChannel")]
    public sealed class TapeReadEventChannel : EventChannelSO<TapeReadEventData>
    {
    }
}
