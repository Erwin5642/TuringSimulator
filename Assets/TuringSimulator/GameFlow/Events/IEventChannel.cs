using System;

namespace TuringSimulator.GameFlow.Events
{
    public interface IUntypedEventChannel
    {
        event Action<object> OnRaisedUntyped;
    }

    public interface IEventChannel<TPayload>
    {
        event Action<TPayload> OnRaised;

        void Raise(TPayload payload, UnityEngine.Object source = null);
    }
}
