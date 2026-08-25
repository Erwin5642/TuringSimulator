using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace TuringSimulator.Controller.Hands
{
    /// <summary>
    /// Maps a held hand gesture (default Shaka) onto mic Start/Stop without
    /// calling VoiceInputHandler. A hold count ignores duplicate Start/Stop
    /// if the pose retriggers.
    /// </summary>
    public sealed class HandGestureMicListener : MonoBehaviour, IHandGestureMicListener
    {
        [Header("Event Channels")]
        [SerializeField] private HandGesturePerformedEventChannel _handGestureChannel;
        [SerializeField] private MicToggleRequestedEventChannel _micToggleRequestedChannel;

        [Header("Gesture")]
        [SerializeField] private string _gestureId = "Shaka";

        private int _holdCount;

        void OnEnable()
        {
            if (_handGestureChannel != null)
                _handGestureChannel.OnRaised += HandleGesture;
        }

        void OnDisable()
        {
            if (_handGestureChannel != null)
                _handGestureChannel.OnRaised -= HandleGesture;

            if (_holdCount <= 0)
                return;

            _holdCount = 0;
            RaiseMic(MicListenMode.Stop);
        }

        public void HandleGesture(HandGesturePerformedEventData eventData)
        {
            if (!HandGestureMicMapping.TryMapListenMode(
                    eventData.GestureId,
                    eventData.Phase,
                    _gestureId,
                    out var mode))
                return;

            if (!HandGestureMicMapping.TryApplyHoldCount(ref _holdCount, mode, out var emitMode))
                return;

            RaiseMic(emitMode);
        }

        void RaiseMic(MicListenMode mode)
        {
            if (_micToggleRequestedChannel == null)
            {
                Debug.LogWarning("[HandGestureMicListener] Missing MicToggleRequested channel.", this);
                return;
            }

            var payload = new MicToggleRequestedEventData(
                EventContextFactory.Create(nameof(HandGestureMicListener), $"{_gestureId}-{mode}"),
                mode);
            _micToggleRequestedChannel.Raise(payload, this);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_handGestureChannel == null)
                Debug.LogWarning($"{name}: assign HandGesturePerformedEventChannel.", this);
            if (_micToggleRequestedChannel == null)
                Debug.LogWarning($"{name}: assign MicToggleRequestedEventChannel.", this);
            if (string.IsNullOrWhiteSpace(_gestureId))
                Debug.LogWarning($"{name}: GestureId should not be empty.", this);
        }
#endif
    }
}
