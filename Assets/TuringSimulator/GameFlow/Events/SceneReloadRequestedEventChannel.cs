using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Scene Reload Requested", fileName = "SceneReloadRequestedChannel")]
    public sealed class SceneReloadRequestedEventChannel : EventChannelSO<SceneReloadRequestedEventData>
    {
    }
}
