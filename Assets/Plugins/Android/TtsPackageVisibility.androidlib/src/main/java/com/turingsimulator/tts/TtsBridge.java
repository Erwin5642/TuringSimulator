package com.turingsimulator.tts;

import android.media.AudioAttributes;
import android.media.AudioManager;
import android.os.Bundle;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import android.speech.tts.Voice;

import java.io.File;
import java.util.List;
import java.util.Locale;
import java.util.Set;

/**
 * JNI-friendly helpers for Unity. UtteranceProgressListener is an abstract class,
 * so Unity's AndroidJavaProxy cannot attach it directly.
 */
public final class TtsBridge
{
    private TtsBridge() {}

    public interface Callbacks
    {
        void onUtteranceStart(String utteranceId);
        void onUtteranceDone(String utteranceId);
        void onUtteranceError(String utteranceId, int errorCode);
    }

    public static boolean applyMediaAudioAttributes(TextToSpeech tts)
    {
        if (tts == null)
            return false;

        AudioAttributes attrs = new AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_MEDIA)
            .setContentType(AudioAttributes.CONTENT_TYPE_SPEECH)
            .build();
        return tts.setAudioAttributes(attrs) == TextToSpeech.SUCCESS;
    }

    public static void setCallbacks(TextToSpeech tts, final Callbacks callbacks)
    {
        if (tts == null)
            return;

        tts.setOnUtteranceProgressListener(new UtteranceProgressListener()
        {
            @Override
            public void onStart(String utteranceId)
            {
                if (callbacks != null)
                    callbacks.onUtteranceStart(utteranceId);
            }

            @Override
            public void onDone(String utteranceId)
            {
                if (callbacks != null)
                    callbacks.onUtteranceDone(utteranceId);
            }

            @Override
            public void onError(String utteranceId)
            {
                if (callbacks != null)
                    callbacks.onUtteranceError(utteranceId, -1);
            }

            @Override
            public void onError(String utteranceId, int errorCode)
            {
                if (callbacks != null)
                    callbacks.onUtteranceError(utteranceId, errorCode);
            }
        });
    }

    public static String describeEngines(TextToSpeech tts)
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            sb.append("default=").append(tts.getDefaultEngine());
            List<TextToSpeech.EngineInfo> engines = tts.getEngines();
            if (engines == null)
            {
                sb.append(" count=0");
                return sb.toString();
            }

            sb.append(" count=").append(engines.size());
            for (int i = 0; i < engines.size(); i++)
            {
                TextToSpeech.EngineInfo info = engines.get(i);
                sb.append(" [").append(info.name).append("|").append(info.label).append("]");
            }
        }
        catch (Throwable t)
        {
            sb.append(" error=").append(t.getMessage());
        }
        return sb.toString();
    }

    public static String describeLanguageAvailability(TextToSpeech tts)
    {
        String[] tags = new String[] { "pt-BR", "pt", "en-US", "en" };
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < tags.length; i++)
        {
            if (i > 0)
                sb.append(' ');
            int result = -99;
            try
            {
                result = tts.isLanguageAvailable(Locale.forLanguageTag(tags[i]));
            }
            catch (Throwable t)
            {
                sb.append(tags[i]).append("=throw:").append(t.getMessage());
                continue;
            }
            sb.append(tags[i]).append('=').append(result);
        }
        return sb.toString();
    }

    public static String describeVoices(TextToSpeech tts)
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            Set<Voice> voices = tts.getVoices();
            if (voices == null)
                return "voices=null";

            sb.append("count=").append(voices.size());
            int shown = 0;
            for (Voice voice : voices)
            {
                Locale locale = voice.getLocale();
                String tag = locale != null ? locale.toLanguageTag() : "?";
                boolean pt = tag.regionMatches(true, 0, "pt", 0, 2);
                if (!pt && shown >= 8)
                    continue;

                sb.append(" [").append(voice.getName())
                    .append("|").append(tag)
                    .append("|q=").append(voice.getQuality())
                    .append("]");
                shown++;
                if (shown >= 12)
                    break;
            }
        }
        catch (Throwable t)
        {
            sb.append(" error=").append(t.getMessage());
        }
        return sb.toString();
    }

    public static int synthesizeToFile(TextToSpeech tts, String text, String path, String utteranceId)
    {
        if (tts == null || text == null || path == null)
            return TextToSpeech.ERROR;

        File file = new File(path);
        File parent = file.getParentFile();
        if (parent != null && !parent.exists() && !parent.mkdirs())
            return TextToSpeech.ERROR;

        if (file.exists() && !file.delete())
            return TextToSpeech.ERROR;

        Bundle params = new Bundle();
        params.putInt(TextToSpeech.Engine.KEY_PARAM_STREAM, AudioManager.STREAM_MUSIC);
        return tts.synthesizeToFile(text, params, file, utteranceId);
    }

    public static int speak(TextToSpeech tts, String text, String utteranceId)
    {
        if (tts == null || text == null)
            return TextToSpeech.ERROR;

        Bundle params = new Bundle();
        params.putInt(TextToSpeech.Engine.KEY_PARAM_STREAM, AudioManager.STREAM_MUSIC);
        return tts.speak(text, TextToSpeech.QUEUE_FLUSH, params, utteranceId);
    }
}
