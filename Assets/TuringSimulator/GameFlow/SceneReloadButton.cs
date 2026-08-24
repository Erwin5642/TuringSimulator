using UnityEngine;
using UnityEngine.UI;

namespace TuringSimulator.GameFlow
{
    [RequireComponent(typeof(Button))]
    public sealed class SceneReloadButton : MonoBehaviour, ISceneReloadAction
    {
        private Button _button;
        private bool _isReloading;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button ??= GetComponent<Button>();
            _button.onClick.AddListener(ReloadCurrentScene);
        }

        private void OnDisable()
        {
            _button?.onClick.RemoveListener(ReloadCurrentScene);
        }

        public void ReloadCurrentScene()
        {
            if (!SceneReload.TryBeginReload(ref _isReloading))
                return;

            if (_button != null)
                _button.interactable = false;
        }
    }
}
