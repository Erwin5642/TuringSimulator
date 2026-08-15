using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Mic Toggle Requested", fileName = "MicToggleRequestedChannel")]
    public sealed class MicToggleRequestedEventChannel : EventChannelSO<MicToggleRequestedEventData>
    {
    }
}
