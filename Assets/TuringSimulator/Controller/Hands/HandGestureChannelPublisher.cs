using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace TuringSimulator.Controller.Hands
{
    /// <summary>
    /// Bridges XR hand gesture detectors (e.g. sample StaticHandGesture UnityEvents)
    /// into the HandGesturePerformed event channel for AgentActionMapper rules.
    /// </summary>
    public sealed class HandGestureChannelPublisher : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Stable id used by AgentActionMapper MatchProperty (e.g. ThumbsUp, ShakaVoice).")]
        [SerializeField] private string _gestureId = "ThumbsUp";

        [Header("Event Channel")]
        [SerializeField] private HandGesturePerformedEventChannel _handGestureChannel;

        /// <summary>
        /// Wire from StaticHandGesture.gesturePerformed (or any UnityEvent).
        /// </summary>
        public void PublishPerformed()
        {
            Raise(HandGesturePhase.Performed);
        }

        /// <summary>
        /// Wire from StaticHandGesture.gestureEnded (or any UnityEvent).
        /// </summary>
        public void PublishEnded()
        {
            Raise(HandGesturePhase.Ended);
        }

        void Raise(HandGesturePhase phase)
        {
            if (_handGestureChannel == null)
            {
                Debug.LogWarning(
                    $"[HandGestureChannelPublisher] Missing channel for gesture '{_gestureId}'.",
                    this);
                return;
            }

            if (string.IsNullOrWhiteSpace(_gestureId))
            {
                Debug.LogWarning("[HandGestureChannelPublisher] GestureId is empty.", this);
                return;
            }

            var payload = new HandGesturePerformedEventData(
                EventContextFactory.Create(nameof(HandGestureChannelPublisher), $"{_gestureId}-{phase}"),
                _gestureId.Trim(),
                phase);

            _handGestureChannel.Raise(payload, this);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_handGestureChannel == null)
                Debug.LogWarning($"{name}: assign HandGesturePerformedEventChannel.", this);
            if (string.IsNullOrWhiteSpace(_gestureId))
                Debug.LogWarning($"{name}: GestureId should not be empty.", this);
        }
#endif
    }
}
