using System;

namespace TuringSimulator.GameFlow.Events
{
    public interface IEventChannel<TPayload>
    {
        event Action<TPayload> OnRaised;

        void Raise(TPayload payload, UnityEngine.Object source = null);
    }
}
