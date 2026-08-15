using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Playback Step", fileName = "PlaybackStepChannel")]
    public sealed class PlaybackStepEventChannel : EventChannelSO<PlaybackStepEventData>
    {
    }
}
