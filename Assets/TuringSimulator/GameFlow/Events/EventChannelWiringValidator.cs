using System.Collections.Generic;
using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    /// <summary>
    /// Validates the core demo-path channel wiring for the incremental refactor.
    /// Attach to the Systems root in BasicScene and assign the required channels.
    /// </summary>
    public sealed class EventChannelWiringValidator : MonoBehaviour
    {
        [Header("Required channels")]
        [SerializeField] private RunRequestedEventChannel _runRequestedChannel;
        [SerializeField] private RunStartedEventChannel _runStartedChannel;
        [SerializeField] private RunFinishedEventChannel _runFinishedChannel;
        [SerializeField] private LevelLoadedEventChannel _levelLoadedChannel;
        [SerializeField] private ProgramChangedEventChannel _programChangedChannel;
        [SerializeField] private PlaybackStepEventChannel _playbackStepChannel;
        [SerializeField] private SimulationStepProducedEventChannel _simulationStepProducedChannel;
        [SerializeField] private HaltReachedEventChannel _haltReachedChannel;
        [SerializeField] private ValidationCompletedEventChannel _validationCompletedChannel;
        [SerializeField] private LevelOutcomeEventChannel _levelOutcomeChannel;
        [SerializeField] private MicToggleRequestedEventChannel _micToggleRequestedChannel;
        [SerializeField] private ListeningStateChangedEventChannel _listeningStateChannel;
        [SerializeField] private PartialTranscriptionEventChannel _partialTranscriptionChannel;
        [SerializeField] private TranscriptionReadyEventChannel _transcriptionReadyChannel;
        [SerializeField] private AskRequestedEventChannel _askRequestedChannel;
        [SerializeField] private AskResultEventChannel _askResultChannel;
        [SerializeField] private ThinkingStateChangedEventChannel _thinkingStateChannel;
        [SerializeField] private AgentActionRequestedEventChannel _agentActionRequestedChannel;

        [Header("Validation")]
        [SerializeField] private bool _logWarningsOnStart = true;

        private void Start()
        {
            if (_logWarningsOnStart)
                LogValidation();
        }

        [ContextMenu("Validate Event Channels")]
        public void LogValidation()
        {
            var issues = ValidateChannels();
            if (issues.Count == 0)
            {
                Debug.Log("[EventChannelWiring] Channel wiring is complete.", this);
                return;
            }

            for (var i = 0; i < issues.Count; i++)
                Debug.LogWarning($"[EventChannelWiring] {issues[i]}", this);
        }

        public IReadOnlyList<string> ValidateChannels()
        {
            var issues = new List<string>();
            Require(_runRequestedChannel, nameof(_runRequestedChannel), issues);
            Require(_runStartedChannel, nameof(_runStartedChannel), issues);
            Require(_runFinishedChannel, nameof(_runFinishedChannel), issues);
            Require(_levelLoadedChannel, nameof(_levelLoadedChannel), issues);
            Require(_programChangedChannel, nameof(_programChangedChannel), issues);
            Require(_playbackStepChannel, nameof(_playbackStepChannel), issues);
            Require(_simulationStepProducedChannel, nameof(_simulationStepProducedChannel), issues);
            Require(_haltReachedChannel, nameof(_haltReachedChannel), issues);
            Require(_validationCompletedChannel, nameof(_validationCompletedChannel), issues);
            Require(_levelOutcomeChannel, nameof(_levelOutcomeChannel), issues);
            Require(_micToggleRequestedChannel, nameof(_micToggleRequestedChannel), issues);
            Require(_listeningStateChannel, nameof(_listeningStateChannel), issues);
            Require(_partialTranscriptionChannel, nameof(_partialTranscriptionChannel), issues);
            Require(_transcriptionReadyChannel, nameof(_transcriptionReadyChannel), issues);
            Require(_askRequestedChannel, nameof(_askRequestedChannel), issues);
            Require(_askResultChannel, nameof(_askResultChannel), issues);
            Require(_thinkingStateChannel, nameof(_thinkingStateChannel), issues);
            Require(_agentActionRequestedChannel, nameof(_agentActionRequestedChannel), issues);
            return issues;
        }

        static void Require(Object value, string fieldName, ICollection<string> issues)
        {
            if (value == null)
                issues.Add($"{fieldName} is not assigned in the Inspector.");
        }
    }
}
