using System;
using TuringSimulator.GameFlow.Events;

namespace TuringSimulator.Controller.Hands
{
    public static class HandGestureRestartMapping
    {
        public static bool ShouldReload(
            string gestureId,
            HandGesturePhase phase,
            string expectedGestureId)
        {
            if (string.IsNullOrWhiteSpace(gestureId) || string.IsNullOrWhiteSpace(expectedGestureId))
                return false;

            if (!string.Equals(gestureId.Trim(), expectedGestureId.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            return phase == HandGesturePhase.Performed;
        }
    }
}
