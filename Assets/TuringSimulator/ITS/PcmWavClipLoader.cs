using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Loads PCM WAV bytes into an AudioClip. Android TTS synthesizeToFile writes WAVE.
/// </summary>
public static class PcmWavClipLoader
{
    public static AudioClip Load(byte[] bytes, string clipName)
    {
        if (bytes == null || bytes.Length < 44)
            throw new InvalidOperationException($"WAV too small ({bytes?.Length ?? 0} bytes).");

        var riff = Encoding.ASCII.GetString(bytes, 0, 4);
        var wave = Encoding.ASCII.GetString(bytes, 8, 4);
        if (riff != "RIFF" || wave != "WAVE")
            throw new InvalidOperationException($"Not RIFF/WAVE (header={Preview(bytes)}).");

        var offset = 12;
        var channels = 0;
        var sampleRate = 0;
        var bitsPerSample = 0;
        var format = 0;
        var dataOffset = -1;
        var dataSize = 0;

        while (offset + 8 <= bytes.Length)
        {
            var chunkId = Encoding.ASCII.GetString(bytes, offset, 4);
            var chunkSize = BitConverter.ToInt32(bytes, offset + 4);
            offset += 8;
            if (chunkSize < 0 || offset + chunkSize > bytes.Length)
                throw new InvalidOperationException($"WAV chunk '{chunkId}' overruns buffer.");

            if (chunkId == "fmt ")
            {
                format = BitConverter.ToInt16(bytes, offset);
                channels = BitConverter.ToInt16(bytes, offset + 2);
                sampleRate = BitConverter.ToInt32(bytes, offset + 4);
                bitsPerSample = BitConverter.ToInt16(bytes, offset + 14);
            }
            else if (chunkId == "data")
            {
                dataOffset = offset;
                dataSize = chunkSize;
                break;
            }

            offset += chunkSize;
            if ((chunkSize & 1) != 0)
                offset++;
        }

        if (dataOffset < 0)
            throw new InvalidOperationException("WAV has no data chunk.");
        if (channels <= 0 || sampleRate <= 0)
            throw new InvalidOperationException($"Bad fmt (channels={channels} rate={sampleRate}).");

        var samples = DecodeSamples(bytes, dataOffset, dataSize, format, bitsPerSample);
        var frameCount = samples.Length / channels;
        if (frameCount <= 0)
            throw new InvalidOperationException("WAV decoded to zero frames.");

        var clip = AudioClip.Create(
            string.IsNullOrEmpty(clipName) ? "tts" : clipName,
            frameCount,
            channels,
            sampleRate,
            false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static string Preview(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return "empty";

        var n = Math.Min(16, bytes.Length);
        var sb = new StringBuilder(n * 3);
        for (var i = 0; i < n; i++)
        {
            if (i > 0)
                sb.Append(' ');
            sb.Append(bytes[i].ToString("X2"));
        }

        return sb.ToString();
    }

    static float[] DecodeSamples(byte[] bytes, int dataOffset, int dataSize, int format, int bitsPerSample)
    {
        var end = Math.Min(bytes.Length, dataOffset + dataSize);
        if (format == 3 && bitsPerSample == 32)
        {
            var count = (end - dataOffset) / 4;
            var samples = new float[count];
            Buffer.BlockCopy(bytes, dataOffset, samples, 0, count * 4);
            return samples;
        }

        if (format != 1 || bitsPerSample != 16)
            throw new InvalidOperationException($"Unsupported WAV format={format} bits={bitsPerSample}.");

        var sampleCount = (end - dataOffset) / 2;
        var pcm = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
            pcm[i] = BitConverter.ToInt16(bytes, dataOffset + i * 2) / 32768f;
        return pcm;
    }
}
