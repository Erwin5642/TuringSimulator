using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Listening State Changed", fileName = "ListeningStateChangedChannel")]
    public sealed class ListeningStateChangedEventChannel : EventChannelSO<ListeningStateChangedEventData>
    {
    }
}
