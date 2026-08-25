using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Voice Capture Stopped", fileName = "VoiceCaptureStoppedChannel")]
    public sealed class VoiceCaptureStoppedEventChannel : EventChannelSO<VoiceCaptureStoppedEventData>
    {
    }
}
