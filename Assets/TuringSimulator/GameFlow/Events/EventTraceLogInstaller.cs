using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    /// <summary>
    /// Optional scene component that configures the global event trace buffer.
    /// Add it to a bootstrap object when event tracing needs inspector-driven defaults.
    /// </summary>
    public sealed class EventTraceLogInstaller : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private int _capacity = 128;
        [SerializeField] private bool _clearOnAwake = true;

        private void Awake()
        {
            EventTraceLog.Configure(_capacity, _enabled);
            if (_clearOnAwake)
                EventTraceLog.Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_capacity < 8)
                _capacity = 8;
        }
#endif
    }
}
