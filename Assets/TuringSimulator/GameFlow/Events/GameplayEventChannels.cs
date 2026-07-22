using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Run Requested", fileName = "RunRequestedChannel")]
    public sealed class RunRequestedEventChannel : EventChannelSO<RunRequestedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Run Started", fileName = "RunStartedChannel")]
    public sealed class RunStartedEventChannel : EventChannelSO<RunStartedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Run Finished", fileName = "RunFinishedChannel")]
    public sealed class RunFinishedEventChannel : EventChannelSO<RunFinishedEventData>
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

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Simulation Step Produced", fileName = "SimulationStepProducedChannel")]
    public sealed class SimulationStepProducedEventChannel : EventChannelSO<SimulationStepProducedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Halt Reached", fileName = "HaltReachedChannel")]
    public sealed class HaltReachedEventChannel : EventChannelSO<HaltReachedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Validation Completed", fileName = "ValidationCompletedChannel")]
    public sealed class ValidationCompletedEventChannel : EventChannelSO<ValidationCompletedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Level Outcome", fileName = "LevelOutcomeChannel")]
    public sealed class LevelOutcomeEventChannel : EventChannelSO<LevelOutcomeEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Mic Toggle Requested", fileName = "MicToggleRequestedChannel")]
    public sealed class MicToggleRequestedEventChannel : EventChannelSO<MicToggleRequestedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Listening State Changed", fileName = "ListeningStateChangedChannel")]
    public sealed class ListeningStateChangedEventChannel : EventChannelSO<ListeningStateChangedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Partial Transcription", fileName = "PartialTranscriptionChannel")]
    public sealed class PartialTranscriptionEventChannel : EventChannelSO<PartialTranscriptionEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Transcription Ready", fileName = "TranscriptionReadyChannel")]
    public sealed class TranscriptionReadyEventChannel : EventChannelSO<TranscriptionReadyEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Ask Requested", fileName = "AskRequestedChannel")]
    public sealed class AskRequestedEventChannel : EventChannelSO<AskRequestedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Ask Result", fileName = "AskResultChannel")]
    public sealed class AskResultEventChannel : EventChannelSO<AskResultEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Thinking State Changed", fileName = "ThinkingStateChangedChannel")]
    public sealed class ThinkingStateChangedEventChannel : EventChannelSO<ThinkingStateChangedEventData>
    {
    }

    [CreateAssetMenu(menuName = "TuringSimulator/Events/Agent Action Requested", fileName = "AgentActionRequestedChannel")]
    public sealed class AgentActionRequestedEventChannel : EventChannelSO<AgentActionRequestedEventData>
    {
    }
}
