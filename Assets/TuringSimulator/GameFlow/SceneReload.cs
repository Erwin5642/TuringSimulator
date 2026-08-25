using UnityEngine;
using UnityEngine.SceneManagement;

namespace TuringSimulator.GameFlow
{
    public static class SceneReload
    {
        public static bool TryBeginReload(ref bool isReloading)
        {
            if (isReloading)
                return false;

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.buildIndex < 0)
            {
                Debug.LogError("[SceneReload] The active scene is not in Build Settings.");
                return false;
            }

            isReloading = true;
            TuringBootstrap.Instance?.PrepareForSceneReload();
            SceneManager.LoadSceneAsync(activeScene.buildIndex, LoadSceneMode.Single);
            return true;
        }
    }
}
