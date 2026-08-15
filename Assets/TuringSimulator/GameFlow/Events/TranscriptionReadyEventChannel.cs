using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Transcription Ready", fileName = "TranscriptionReadyChannel")]
    public sealed class TranscriptionReadyEventChannel : EventChannelSO<TranscriptionReadyEventData>
    {
    }
}
