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
        [SerializeField] private LevelLoadedEventChannel _levelLoadedChannel;
        [SerializeField] private ProgramChangedEventChannel _programChangedChannel;
        [SerializeField] private PlaybackStepEventChannel _playbackStepChannel;
        [SerializeField] private HaltReachedEventChannel _haltReachedChannel;

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
            Require(_levelLoadedChannel, nameof(_levelLoadedChannel), issues);
            Require(_programChangedChannel, nameof(_programChangedChannel), issues);
            Require(_playbackStepChannel, nameof(_playbackStepChannel), issues);
            Require(_haltReachedChannel, nameof(_haltReachedChannel), issues);
            return issues;
        }

        static void Require(Object value, string fieldName, ICollection<string> issues)
        {
            if (value == null)
                issues.Add($"{fieldName} is not assigned in the Inspector.");
        }
    }
}
