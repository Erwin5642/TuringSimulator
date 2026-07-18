using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Run Requested", fileName = "RunRequestedChannel")]
    public sealed class RunRequestedEventChannel : EventChannelSO<RunRequestedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Level Loaded", fileName = "LevelLoadedChannel")]
    public sealed class LevelLoadedEventChannel : EventChannelSO<LevelLoadedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Program Changed", fileName = "ProgramChangedChannel")]
    public sealed class ProgramChangedEventChannel : EventChannelSO<ProgramChangedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Playback Step", fileName = "PlaybackStepChannel")]
    public sealed class PlaybackStepEventChannel : EventChannelSO<PlaybackStepEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Halt Reached", fileName = "HaltReachedChannel")]
    public sealed class HaltReachedEventChannel : EventChannelSO<HaltReachedEventData>
    {
    }
}
