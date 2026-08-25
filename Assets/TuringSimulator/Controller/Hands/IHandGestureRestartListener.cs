using TuringSimulator.GameFlow.Events;

namespace TuringSimulator.Controller.Hands
{
    public interface IHandGestureRestartListener
    {
        void HandleGesture(HandGesturePerformedEventData eventData);
    }
}
