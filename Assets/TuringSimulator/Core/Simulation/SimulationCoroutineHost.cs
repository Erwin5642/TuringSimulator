using UnityEngine;

namespace TuringSimulator.Core.Simulation
{
    /// <summary>
    /// Hidden host so game flow can drive main-thread simulation coroutines.
    /// </summary>
    public sealed class SimulationCoroutineHost : MonoBehaviour
    {
        static SimulationCoroutineHost _instance;

        public static SimulationCoroutineHost InstanceOrNull => _instance;

        public static SimulationCoroutineHost Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                var go = new GameObject("[SimulationCoroutineHost]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SimulationCoroutineHost>();
                return _instance;
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
