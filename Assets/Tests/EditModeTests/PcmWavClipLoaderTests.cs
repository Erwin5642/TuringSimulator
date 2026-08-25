using System;
using NUnit.Framework;
using UnityEngine;

namespace EditModeTests
{
    public class PcmWavClipLoaderTests
    {
        [Test]
        public void Load_Pcm16MonoWav_CreatesClip()
        {
            var wav = BuildPcm16MonoWav(8000, new short[] { 0, 16384, -16384, 0 });
            var clip = PcmWavClipLoader.Load(wav, "tts-test");

            Assert.That(clip.frequency, Is.EqualTo(8000));
            Assert.That(clip.channels, Is.EqualTo(1));
            Assert.That(clip.samples, Is.EqualTo(4));
            Assert.That(PcmWavClipLoader.Preview(wav), Does.StartWith("52 49 46 46"));
        }

        [Test]
        public void Load_RejectsNonWaveHeader()
        {
            Assert.Throws<InvalidOperationException>(() =>
                PcmWavClipLoader.Load(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, "bad"));
        }

        static byte[] BuildPcm16MonoWav(int sampleRate, short[] samples)
        {
            var dataSize = samples.Length * 2;
            var bytes = new byte[44 + dataSize];
            WriteAscii(bytes, 0, "RIFF");
            BitConverter.GetBytes(36 + dataSize).CopyTo(bytes, 4);
            WriteAscii(bytes, 8, "WAVE");
            WriteAscii(bytes, 12, "fmt ");
            BitConverter.GetBytes(16).CopyTo(bytes, 16);
            BitConverter.GetBytes((short)1).CopyTo(bytes, 20);
            BitConverter.GetBytes((short)1).CopyTo(bytes, 22);
            BitConverter.GetBytes(sampleRate).CopyTo(bytes, 24);
            BitConverter.GetBytes(sampleRate * 2).CopyTo(bytes, 28);
            BitConverter.GetBytes((short)2).CopyTo(bytes, 32);
            BitConverter.GetBytes((short)16).CopyTo(bytes, 34);
            WriteAscii(bytes, 36, "data");
            BitConverter.GetBytes(dataSize).CopyTo(bytes, 40);
            Buffer.BlockCopy(samples, 0, bytes, 44, dataSize);
            return bytes;
        }

        static void WriteAscii(byte[] dest, int offset, string text)
        {
            for (var i = 0; i < text.Length; i++)
                dest[offset + i] = (byte)text[i];
        }
    }
}
