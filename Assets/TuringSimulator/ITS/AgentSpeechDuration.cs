using UnityEngine;

/// <summary>
/// Estimates how long spoken text would take at a given reading rate.
/// Used as a load-hang budget helper; Wit TTS owns real playback duration.
/// </summary>
public static class AgentSpeechDuration
{
    public static float EstimateSeconds(
        string text,
        float charsPerSecond,
        float minSeconds,
        float maxSeconds)
    {
        var rate = Mathf.Max(1f, charsPerSecond);
        var min = Mathf.Max(0.1f, minSeconds);
        var max = Mathf.Max(min, maxSeconds);
        var length = string.IsNullOrEmpty(text) ? 0 : text.Trim().Length;
        if (length <= 0)
            return min;

        return Mathf.Clamp(length / rate, min, max);
    }
}
