using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace TuringSimulator.Controller.Hands
{
    /// <summary>
    /// Maps a configured hand gesture (default ThumbsDown) onto a scene reload
    /// request so the current level can be restarted while debugging.
    /// </summary>
    public sealed class HandGestureRestartListener : MonoBehaviour, IHandGestureRestartListener
    {
        [Header("Event Channels")]
        [SerializeField] private HandGesturePerformedEventChannel _handGestureChannel;
        [SerializeField] private SceneReloadRequestedEventChannel _sceneReloadRequestedChannel;

        [Header("Gesture")]
        [SerializeField] private string _gestureId = "ThumbsDown";

        [Header("Enable")]
        [SerializeField] private bool _enableInEditor = true;
        [SerializeField] private bool _enableInDevelopmentBuilds = true;

        bool IsEnabled =>
            (Application.isEditor && _enableInEditor) ||
            (!Application.isEditor && Debug.isDebugBuild && _enableInDevelopmentBuilds);

        void OnEnable()
        {
            if (_handGestureChannel != null)
                _handGestureChannel.OnRaised += HandleGesture;
        }

        void OnDisable()
        {
            if (_handGestureChannel != null)
                _handGestureChannel.OnRaised -= HandleGesture;
        }

        public void HandleGesture(HandGesturePerformedEventData eventData)
        {
            if (!IsEnabled)
                return;

            if (!HandGestureRestartMapping.ShouldReload(eventData.GestureId, eventData.Phase, _gestureId))
                return;

            RaiseReload();
        }

        void RaiseReload()
        {
            if (_sceneReloadRequestedChannel == null)
            {
                Debug.LogWarning("[HandGestureRestartListener] Missing SceneReloadRequested channel.", this);
                return;
            }

            var payload = new SceneReloadRequestedEventData(
                EventContextFactory.Create(nameof(HandGestureRestartListener), $"{_gestureId}-reload"));
            _sceneReloadRequestedChannel.Raise(payload, this);
            Debug.Log($"[HandGestureRestartListener] Scene reload requested by gesture '{_gestureId}'.", this);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_handGestureChannel == null)
                Debug.LogWarning($"{name}: assign HandGesturePerformedEventChannel.", this);
            if (_sceneReloadRequestedChannel == null)
                Debug.LogWarning($"{name}: assign SceneReloadRequestedEventChannel.", this);
            if (string.IsNullOrWhiteSpace(_gestureId))
                Debug.LogWarning($"{name}: GestureId should not be empty.", this);
        }
#endif
    }
}
