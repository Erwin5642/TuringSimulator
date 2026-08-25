using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    public sealed class SceneReloadRequestedListener : MonoBehaviour, ISceneReloadRequestedListener, ISceneReloadAction
    {
        [Header("Event Channels")]
        [SerializeField] private SceneReloadRequestedEventChannel _sceneReloadRequestedChannel;

        bool _isReloading;

        void OnEnable()
        {
            if (_sceneReloadRequestedChannel != null)
                _sceneReloadRequestedChannel.OnRaised += HandleReloadRequested;
        }

        void OnDisable()
        {
            if (_sceneReloadRequestedChannel != null)
                _sceneReloadRequestedChannel.OnRaised -= HandleReloadRequested;
        }

        public void HandleReloadRequested(SceneReloadRequestedEventData eventData)
        {
            ReloadCurrentScene();
        }

        public void ReloadCurrentScene()
        {
            SceneReload.TryBeginReload(ref _isReloading);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_sceneReloadRequestedChannel == null)
                Debug.LogWarning($"{name}: assign SceneReloadRequestedEventChannel.", this);
        }
#endif
    }
}
