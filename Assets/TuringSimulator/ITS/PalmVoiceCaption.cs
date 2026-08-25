using System;
using TuringSimulator.GameFlow.Events;

/// <summary>
/// Display-only palm caption rules. Never mix <see cref="StoppedCue"/> into
/// text committed to ITS / TTS.
/// </summary>
public static class PalmVoiceCaption
{
    public const string StoppedCue = "Cambio";

    public static bool TryMatchGesture(
        string gestureId,
        HandGesturePhase phase,
        string expectedGestureId,
        out bool show)
    {
        show = false;
        if (string.IsNullOrWhiteSpace(gestureId) || string.IsNullOrWhiteSpace(expectedGestureId))
            return false;

        if (!string.Equals(gestureId.Trim(), expectedGestureId.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        show = phase == HandGesturePhase.Performed;
        return true;
    }

    public static string FormatLive(string recordedText) =>
        recordedText?.Trim() ?? string.Empty;

    public static string AppendStoppedCue(string recordedText, string cue = StoppedCue)
    {
        var live = FormatLive(recordedText);
        var marker = string.IsNullOrWhiteSpace(cue) ? StoppedCue : cue.Trim();
        if (string.IsNullOrEmpty(live))
            return marker;

        if (live.EndsWith(marker, StringComparison.Ordinal))
            return live;

        return $"{live}\n{marker}";
    }
}
