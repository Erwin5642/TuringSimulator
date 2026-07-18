using TuringSimulator.Core.Types;
using TuringSimulator.Core.Simulation.Step;

namespace TuringSimulator.GameFlow.Events
{
    public readonly struct EventContextData
    {
        public EventContextData(string sourceName, string correlationId, long utcUnixMs)
        {
            SourceName = sourceName ?? string.Empty;
            CorrelationId = correlationId ?? string.Empty;
            UtcUnixMs = utcUnixMs;
        }

        public string SourceName { get; }
        public string CorrelationId { get; }
        public long UtcUnixMs { get; }

        public override string ToString() => $"{SourceName}:{CorrelationId}@{UtcUnixMs}";
    }

    public readonly struct RunRequestedEventData
    {
        public RunRequestedEventData(EventContextData context, string requestedState)
        {
            Context = context;
            RequestedState = requestedState ?? string.Empty;
        }

        public EventContextData Context { get; }
        public string RequestedState { get; }

        public override string ToString() => $"state={RequestedState} ctx={Context}";
    }

    public readonly struct LevelLoadedEventData
    {
        public LevelLoadedEventData(
            EventContextData context,
            string levelId,
            string levelTitle,
            int validationScenarioCount)
        {
            Context = context;
            LevelId = levelId ?? string.Empty;
            LevelTitle = levelTitle ?? string.Empty;
            ValidationScenarioCount = validationScenarioCount;
        }

        public EventContextData Context { get; }
        public string LevelId { get; }
        public string LevelTitle { get; }
        public int ValidationScenarioCount { get; }

        public override string ToString()
        {
            return $"level={LevelId} title={LevelTitle} tests={ValidationScenarioCount} ctx={Context}";
        }
    }

    public readonly struct ProgramChangedEventData
    {
        public ProgramChangedEventData(EventContextData context, int transitionCount, int finalStateCount)
        {
            Context = context;
            TransitionCount = transitionCount;
            FinalStateCount = finalStateCount;
        }

        public EventContextData Context { get; }
        public int TransitionCount { get; }
        public int FinalStateCount { get; }

        public override string ToString() => $"transitions={TransitionCount} finals={FinalStateCount} ctx={Context}";
    }

    public readonly struct PlaybackStepEventData
    {
        public PlaybackStepEventData(EventContextData context, int stepIndex, ResultKind resultKind)
        {
            Context = context;
            StepIndex = stepIndex;
            ResultKind = resultKind;
        }

        public EventContextData Context { get; }
        public int StepIndex { get; }
        public ResultKind ResultKind { get; }

        public override string ToString() => $"step={StepIndex} kind={ResultKind} ctx={Context}";
    }

    public readonly struct HaltReachedEventData
    {
        public HaltReachedEventData(EventContextData context, HaltStatus haltStatus)
        {
            Context = context;
            HaltStatus = haltStatus;
        }

        public EventContextData Context { get; }
        public HaltStatus HaltStatus { get; }

        public override string ToString() => $"halt={HaltStatus} ctx={Context}";
    }
}
