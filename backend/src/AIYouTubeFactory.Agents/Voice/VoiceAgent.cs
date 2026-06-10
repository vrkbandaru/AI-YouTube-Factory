using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace AIYouTubeFactory.Agents.Voice;

public class VoiceAgent : IVoiceAgent
{
    private readonly string _speechKey;
    private readonly string _speechRegion;

    public VoiceAgent(string speechKey, string speechRegion)
    {
        _speechKey    = speechKey;
        _speechRegion = speechRegion;
    }

    public async Task<List<AudioSegment>> GenerateVoiceAsync(
        Storyboard storyboard,
        string outputDir,
        string voiceName,
        string voiceStyle,
        float speechRate,
        IProgress<VideoProgressUpdate>? progress = null)
    {
        Directory.CreateDirectory(outputDir);
        var segments = new List<AudioSegment>();
        var scenes   = storyboard.Scenes;
        int total    = scenes.Count;

        // ── If no speech key configured, generate silence for every scene ─────
        bool hasSpeechKey = !string.IsNullOrWhiteSpace(_speechKey)
                         && _speechKey != "YOUR-AZURE-SPEECH-KEY";

        progress?.Report(new VideoProgressUpdate
        {
            Stage           = "Voice Agent",
            Message         = hasSpeechKey
                ? $"🎙️ Generating voice narration with {voiceName}..."
                : "⚠️ No Azure Speech key — using silence placeholders",
            ProgressPercent = 41,
            Status          = VideoGenerationStatus.GeneratingVoice
        });

        if (!hasSpeechKey)
        {
            // No key — add silent segments so video composition still works
            foreach (var s in scenes)
                AddSilentSegment(segments, s);
            return segments;
        }

        // ── Speech config — output as WAV (16kHz mono) ────────────────────────
        var config = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
        config.SpeechSynthesisVoiceName = voiceName;
        // IMPORTANT: Use Riff16Khz16BitMonoPcm — it writes a proper WAV that
        // FFmpeg can decode without issues. Audio48Khz192KBitRateMonoMp3 was
        // causing empty files when written via AudioConfig.FromWavFileOutput.
        config.SetSpeechSynthesisOutputFormat(
            SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

        for (int i = 0; i < total; i++)
        {
            var scene    = scenes[i];
            // Always output as .wav — FFmpeg handles it natively
            var wavPath  = Path.Combine(outputDir, $"audio_{scene.Index:D3}.wav");

            if (string.IsNullOrWhiteSpace(scene.Narration))
            {
                AddSilentSegment(segments, scene);
                continue;
            }

            // Skip if already generated (resume support)
            if (File.Exists(wavPath) && new FileInfo(wavPath).Length > 1000)
            {
                var existDur = GetWavDuration(wavPath);
                scene.GeneratedAudioPath   = wavPath;
                scene.AudioDurationSeconds = existDur;
                segments.Add(new AudioSegment
                {
                    SceneIndex      = scene.Index,
                    Text            = scene.Narration,
                    FilePath        = wavPath,
                    DurationSeconds = existDur,
                    FileSizeBytes   = new FileInfo(wavPath).Length
                });
                continue;
            }

            int pct = 41 + (int)((double)i / total * 19);
            progress?.Report(new VideoProgressUpdate
            {
                Stage           = "Voice Agent",
                Message         = $"🎙️ Scene {i + 1}/{total}: {scene.Title}",
                ProgressPercent = pct,
                Status          = VideoGenerationStatus.GeneratingVoice
            });

            try
            {
                var ssml = BuildSSML(scene.Narration, voiceName, voiceStyle, speechRate);

                // Write directly to WAV file
                using var audioConfig = AudioConfig.FromWavFileOutput(wavPath);
                using var synthesizer = new SpeechSynthesizer(config, audioConfig);
                var result            = await synthesizer.SpeakSsmlAsync(ssml);

                if (result.Reason == ResultReason.SynthesizingAudioCompleted
                    && File.Exists(wavPath)
                    && new FileInfo(wavPath).Length > 100)
                {
                    var duration = GetWavDuration(wavPath);
                    scene.GeneratedAudioPath   = wavPath;
                    scene.AudioDurationSeconds = duration;

                    segments.Add(new AudioSegment
                    {
                        SceneIndex      = scene.Index,
                        Text            = scene.Narration,
                        FilePath        = wavPath,
                        DurationSeconds = duration,
                        FileSizeBytes   = new FileInfo(wavPath).Length
                    });

                    progress?.Report(new VideoProgressUpdate
                    {
                        Stage           = "Voice Agent",
                        Message         = $"✅ Scene {i + 1}/{total}: {duration:F1}s audio",
                        ProgressPercent = pct,
                        Status          = VideoGenerationStatus.GeneratingVoice
                    });
                }
                else
                {
                    // Use SpeechSynthesisCancellationDetails (not CancellationDetails)
                    // because result is SpeechSynthesisResult, not RecognitionResult
                    string cancelDetails;
                    if (result.Reason == ResultReason.Canceled)
                    {
                        var details  = SpeechSynthesisCancellationDetails.FromResult(result);
                        cancelDetails = $"{details.Reason}: {details.ErrorDetails}";
                    }
                    else
                    {
                        cancelDetails = $"Unexpected reason: {result.Reason}";
                    }

                    progress?.Report(new VideoProgressUpdate
                    {
                        Stage           = "Voice Agent",
                        Message         = $"⚠️ Scene {i + 1} TTS failed: {cancelDetails}",
                        ProgressPercent = pct,
                        Status          = VideoGenerationStatus.GeneratingVoice
                    });
                    AddSilentSegment(segments, scene);
                }
            }
            catch (Exception ex)
            {
                progress?.Report(new VideoProgressUpdate
                {
                    Stage           = "Voice Agent",
                    Message         = $"⚠️ Scene {i + 1} exception: {ex.Message}",
                    ProgressPercent = pct,
                    Status          = VideoGenerationStatus.GeneratingVoice
                });
                AddSilentSegment(segments, scene);
            }
        }

        // Sync scene durations to actual audio
        foreach (var seg in segments.Where(s => s.DurationSeconds > 0))
        {
            var scene = scenes.FirstOrDefault(s => s.Index == seg.SceneIndex);
            if (scene != null) scene.AudioDurationSeconds = seg.DurationSeconds;
        }

        progress?.Report(new VideoProgressUpdate
        {
            Stage           = "Voice Agent",
            Message         = $"✅ Voice done — {segments.Count(s => !string.IsNullOrEmpty(s.FilePath))} with audio",
            ProgressPercent = 60,
            Status          = VideoGenerationStatus.GeneratingVoice
        });

        return segments;
    }

    private static string BuildSSML(string text, string voice, string style, float rate)
    {
        var rateStr = rate switch { < 0.8f => "slow", > 1.2f => "fast", _ => "medium" };

        text = text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&apos;");

        // Truncate very long narrations (Azure TTS limit ~3000 chars)
        if (text.Length > 2800) text = text[..2800] + "...";

        return $"""
            <speak version="1.0"
                   xmlns="http://www.w3.org/2001/10/synthesis"
                   xmlns:mstts="https://www.w3.org/2001/mstts"
                   xml:lang="en-US">
              <voice name="{voice}">
                <mstts:express-as style="{style}" styledegree="1.2">
                  <prosody rate="{rateStr}">
                    {text}
                  </prosody>
                </mstts:express-as>
              </voice>
            </speak>
            """;
    }

    private static void AddSilentSegment(List<AudioSegment> segments, StoryboardScene scene)
    {
        segments.Add(new AudioSegment
        {
            SceneIndex      = scene.Index,
            Text            = scene.Narration,
            FilePath        = "",                  // empty = FFmpeg inserts silence
            DurationSeconds = Math.Max(scene.DurationSeconds, 5)
        });
    }

    // ── Read WAV duration from header without external library ─────────────────
    private static double GetWavDuration(string wavPath)
    {
        try
        {
            if (!File.Exists(wavPath)) return 5.0;
            using var fs = File.OpenRead(wavPath);
            using var br = new BinaryReader(fs);

            // Skip RIFF header (4) + file size (4) + "WAVE" (4)
            fs.Seek(12, SeekOrigin.Begin);

            // Walk chunks to find "fmt " and "data"
            int sampleRate = 44100, channels = 1, bitsPerSample = 16;
            long dataSize  = 0;

            while (fs.Position < fs.Length - 8)
            {
                var chunkId   = new string(br.ReadChars(4));
                var chunkSize = br.ReadInt32();
                var chunkStart = fs.Position;

                if (chunkId == "fmt ")
                {
                    br.ReadInt16();                          // audio format
                    channels      = br.ReadInt16();
                    sampleRate    = br.ReadInt32();
                    br.ReadInt32();                          // byte rate
                    br.ReadInt16();                          // block align
                    bitsPerSample = br.ReadInt16();
                }
                else if (chunkId == "data")
                {
                    dataSize = chunkSize;
                    break;
                }

                fs.Seek(chunkStart + chunkSize, SeekOrigin.Begin);
            }

            if (sampleRate <= 0 || channels <= 0 || bitsPerSample <= 0) return 5.0;
            return (double)dataSize / (sampleRate * channels * (bitsPerSample / 8.0));
        }
        catch
        {
            return 5.0;
        }
    }
}
