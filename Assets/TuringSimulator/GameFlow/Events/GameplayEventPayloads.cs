using TuringSimulator.Core.Level;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

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

    public readonly struct RunStartedEventData
    {
        public RunStartedEventData(EventContextData context, string sourceState)
        {
            Context = context;
            SourceState = sourceState ?? string.Empty;
        }

        public EventContextData Context { get; }
        public string SourceState { get; }

        public override string ToString() => $"from={SourceState} ctx={Context}";
    }

    public readonly struct RunFinishedEventData
    {
        public RunFinishedEventData(EventContextData context, HaltStatus haltStatus, int stepCount)
        {
            Context = context;
            HaltStatus = haltStatus;
            StepCount = stepCount;
        }

        public EventContextData Context { get; }
        public HaltStatus HaltStatus { get; }
        public int StepCount { get; }

        public override string ToString() => $"halt={HaltStatus} steps={StepCount} ctx={Context}";
    }

    public readonly struct LevelLoadedEventData
    {
        public LevelLoadedEventData(
            EventContextData context,
            LevelDefinition level,
            string levelId,
            string levelTitle,
            int validationScenarioCount)
        {
            Context = context;
            Level = level;
            LevelId = levelId ?? string.Empty;
            LevelTitle = levelTitle ?? string.Empty;
            ValidationScenarioCount = validationScenarioCount;
        }

        public EventContextData Context { get; }
        public LevelDefinition Level { get; }
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
        public ProgramChangedEventData(EventContextData context, IProgram program, int transitionCount, int finalStateCount)
        {
            Context = context;
            Program = program;
            TransitionCount = transitionCount;
            FinalStateCount = finalStateCount;
        }

        public EventContextData Context { get; }
        public IProgram Program { get; }
        public int TransitionCount { get; }
        public int FinalStateCount { get; }

        public override string ToString() => $"transitions={TransitionCount} finals={FinalStateCount} ctx={Context}";
    }

    public readonly struct PlaybackStepEventData
    {
        public PlaybackStepEventData(EventContextData context, int stepIndex, ResultKind resultKind, StepResult step)
        {
            Context = context;
            StepIndex = stepIndex;
            ResultKind = resultKind;
            Step = step;
        }

        public EventContextData Context { get; }
        public int StepIndex { get; }
        public ResultKind ResultKind { get; }
        public StepResult Step { get; }

        public override string ToString() => $"step={StepIndex} kind={ResultKind} ctx={Context}";
    }

    public readonly struct SimulationStepProducedEventData
    {
        public SimulationStepProducedEventData(EventContextData context, int stepIndex, ResultKind resultKind, StepResult step)
        {
            Context = context;
            StepIndex = stepIndex;
            ResultKind = resultKind;
            Step = step;
        }

        public EventContextData Context { get; }
        public int StepIndex { get; }
        public ResultKind ResultKind { get; }
        public StepResult Step { get; }

        public override string ToString() => $"sim-step={StepIndex} kind={ResultKind} ctx={Context}";
    }

    public readonly struct HaltReachedEventData
    {
        public HaltReachedEventData(EventContextData context, HaltStatus haltStatus, StepResult haltStep)
        {
            Context = context;
            HaltStatus = haltStatus;
            HaltStep = haltStep;
        }

        public EventContextData Context { get; }
        public HaltStatus HaltStatus { get; }
        public StepResult HaltStep { get; }

        public override string ToString() => $"halt={HaltStatus} ctx={Context}";
    }

    public readonly struct ValidationCompletedEventData
    {
        public ValidationCompletedEventData(
            EventContextData context,
            string levelId,
            bool allPassed,
            int passedCount,
            int totalCount)
        {
            Context = context;
            LevelId = levelId ?? string.Empty;
            AllPassed = allPassed;
            PassedCount = passedCount;
            TotalCount = totalCount;
        }

        public EventContextData Context { get; }
        public string LevelId { get; }
        public bool AllPassed { get; }
        public int PassedCount { get; }
        public int TotalCount { get; }

        public override string ToString()
        {
            return $"level={LevelId} passed={PassedCount}/{TotalCount} all={AllPassed} ctx={Context}";
        }
    }

    public enum LevelOutcomeKind
    {
        Victory = 0,
        Defeat = 1,
    }

    public readonly struct LevelOutcomeEventData
    {
        public LevelOutcomeEventData(EventContextData context, string levelId, LevelOutcomeKind outcome)
        {
            Context = context;
            LevelId = levelId ?? string.Empty;
            Outcome = outcome;
        }

        public EventContextData Context { get; }
        public string LevelId { get; }
        public LevelOutcomeKind Outcome { get; }

        public override string ToString() => $"level={LevelId} outcome={Outcome} ctx={Context}";
    }

    public readonly struct MicToggleRequestedEventData
    {
        public MicToggleRequestedEventData(EventContextData context)
        {
            Context = context;
        }

        public EventContextData Context { get; }

        public override string ToString() => $"ctx={Context}";
    }

    public readonly struct ListeningStateChangedEventData
    {
        public ListeningStateChangedEventData(EventContextData context, bool isListening)
        {
            Context = context;
            IsListening = isListening;
        }

        public EventContextData Context { get; }
        public bool IsListening { get; }

        public override string ToString() => $"listening={IsListening} ctx={Context}";
    }

    public readonly struct PartialTranscriptionEventData
    {
        public PartialTranscriptionEventData(EventContextData context, string partialText)
        {
            Context = context;
            PartialText = partialText ?? string.Empty;
        }

        public EventContextData Context { get; }
        public string PartialText { get; }

        public override string ToString() => $"partial=\"{PartialText}\" ctx={Context}";
    }

    public readonly struct TranscriptionReadyEventData
    {
        public TranscriptionReadyEventData(EventContextData context, string text)
        {
            Context = context;
            Text = text ?? string.Empty;
        }

        public EventContextData Context { get; }
        public string Text { get; }

        public override string ToString() => $"text=\"{Text}\" ctx={Context}";
    }

    public readonly struct AskRequestedEventData
    {
        public AskRequestedEventData(
            EventContextData context,
            string studentId,
            string levelId,
            string question)
        {
            Context = context;
            StudentId = studentId ?? string.Empty;
            LevelId = levelId ?? string.Empty;
            Question = question ?? string.Empty;
        }

        public EventContextData Context { get; }
        public string StudentId { get; }
        public string LevelId { get; }
        public string Question { get; }

        public override string ToString() => $"student={StudentId} level={LevelId} q=\"{Question}\" ctx={Context}";
    }

    public readonly struct AskResultEventData
    {
        public AskResultEventData(EventContextData context, bool success, string reply, string error)
        {
            Context = context;
            Success = success;
            Reply = reply ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public EventContextData Context { get; }
        public bool Success { get; }
        public string Reply { get; }
        public string Error { get; }

        public override string ToString()
        {
            return Success
                ? $"success reply=\"{Reply}\" ctx={Context}"
                : $"failure error=\"{Error}\" ctx={Context}";
        }
    }

    public readonly struct ThinkingStateChangedEventData
    {
        public ThinkingStateChangedEventData(EventContextData context, bool isThinking)
        {
            Context = context;
            IsThinking = isThinking;
        }

        public EventContextData Context { get; }
        public bool IsThinking { get; }

        public override string ToString() => $"thinking={IsThinking} ctx={Context}";
    }

    public enum AgentAnimationKind
    {
        None = 0,
        Idle = 1,
        Thinking = 2,
        Talking = 3,
        Celebrate = 4,
    }

    public readonly struct AgentActionRequestedEventData
    {
        public AgentActionRequestedEventData(EventContextData context, string text, AgentAnimationKind animation)
        {
            Context = context;
            Text = text ?? string.Empty;
            Animation = animation;
        }

        public EventContextData Context { get; }
        public string Text { get; }
        public AgentAnimationKind Animation { get; }

        public override string ToString() => $"animation={Animation} text=\"{Text}\" ctx={Context}";
    }
}
