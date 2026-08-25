using TuringSimulator.GameFlow.Events;

namespace TuringSimulator.GameFlow
{
    public interface ISceneReloadRequestedListener
    {
        void HandleReloadRequested(SceneReloadRequestedEventData eventData);
    }
}
