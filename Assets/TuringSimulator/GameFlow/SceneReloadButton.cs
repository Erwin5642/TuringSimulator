using UnityEngine;
using UnityEngine.SceneManagement;
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
            if (_isReloading)
                return;

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.buildIndex < 0)
            {
                Debug.LogError("[SceneReloadButton] The active scene is not in Build Settings.");
                return;
            }

            _isReloading = true;
            if (_button != null)
                _button.interactable = false;

            TuringBootstrap.Instance?.PrepareForSceneReload();
            SceneManager.LoadSceneAsync(activeScene.buildIndex, LoadSceneMode.Single);
        }
    }
}
