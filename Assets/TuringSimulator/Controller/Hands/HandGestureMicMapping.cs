using System;
using TuringSimulator.GameFlow.Events;

namespace TuringSimulator.Controller.Hands
{
    public static class HandGestureMicMapping
    {
        public static bool TryMapListenMode(
            string gestureId,
            HandGesturePhase phase,
            string expectedGestureId,
            out MicListenMode mode)
        {
            mode = default;
            if (string.IsNullOrWhiteSpace(gestureId) || string.IsNullOrWhiteSpace(expectedGestureId))
                return false;

            if (!string.Equals(gestureId.Trim(), expectedGestureId.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            mode = phase == HandGesturePhase.Ended ? MicListenMode.Stop : MicListenMode.Start;
            return true;
        }

        public static bool TryApplyHoldCount(ref int holdCount, MicListenMode mode, out MicListenMode emitMode)
        {
            emitMode = mode;
            if (holdCount < 0)
                holdCount = 0;

            switch (mode)
            {
                case MicListenMode.Start:
                    holdCount++;
                    return holdCount == 1;
                case MicListenMode.Stop:
                    if (holdCount > 0)
                        holdCount--;
                    return holdCount == 0;
                default:
                    return false;
            }
        }
    }
}
