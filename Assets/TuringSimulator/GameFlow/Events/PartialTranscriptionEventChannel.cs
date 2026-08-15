using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Partial Transcription", fileName = "PartialTranscriptionChannel")]
    public sealed class PartialTranscriptionEventChannel : EventChannelSO<PartialTranscriptionEventData>
    {
    }
}
