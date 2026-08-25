using TuringSimulator.GameFlow.Events;

namespace TuringSimulator.Controller.Hands
{
    public interface IHandGestureMicListener
    {
        void HandleGesture(HandGesturePerformedEventData eventData);
    }
}
