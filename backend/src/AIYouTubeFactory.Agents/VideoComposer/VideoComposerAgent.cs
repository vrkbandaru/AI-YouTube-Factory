using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using FFMpegCore;
using FFMpegCore.Enums;
using System.Text;

namespace AIYouTubeFactory.Agents.VideoComposer;

public class VideoComposerAgent : IVideoComposerAgent
{
    private static readonly Dictionary<VideoResolution, (int W, int H)> Resolutions = new()
    {
        { VideoResolution.HD720p,  (1280, 720)  },
        { VideoResolution.HD1080p, (1920, 1080) },
        { VideoResolution.UHD4K,   (3840, 2160) }
    };

    private readonly string _ffmpegBinaryFolder;

    public VideoComposerAgent(string ffmpegBinaryFolder = "")
    {
        _ffmpegBinaryFolder = ffmpegBinaryFolder;
        ConfigureFFmpeg(ffmpegBinaryFolder);
    }

    // ── Fix 1: Robust FFmpeg path configuration ───────────────────────────────
    private static void ConfigureFFmpeg(string binaryFolder)
    {
        // Priority 1: explicitly configured folder
        if (!string.IsNullOrWhiteSpace(binaryFolder) && Directory.Exists(binaryFolder))
        {
            GlobalFFOptions.Configure(o => o.BinaryFolder = binaryFolder);
            return;
        }

        // Priority 2: check common Windows WinGet install paths
        var wingetBases = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WinGet", "Packages"),
            @"C:\ProgramData\Microsoft\Windows\AppV\Client\Packages"
        };

        foreach (var baseDir in wingetBases.Where(Directory.Exists))
        {
            var ffmpegDir = Directory.GetDirectories(baseDir, "Gyan.FFmpeg*", SearchOption.TopDirectoryOnly)
                .SelectMany(d => Directory.GetDirectories(d, "*full_build*", SearchOption.AllDirectories))
                .Select(d => Path.Combine(d, "bin"))
                .FirstOrDefault(Directory.Exists);

            if (ffmpegDir != null)
            {
                GlobalFFOptions.Configure(o => o.BinaryFolder = ffmpegDir);
                return;
            }
        }

        // Priority 3: common manual install paths
        var commonPaths = new[]
        {
            @"C:\ffmpeg\bin",
            @"C:\ffmpeg\ffmpeg\bin",
            @"C:\Program Files\ffmpeg\bin",
            @"C:\Tools\ffmpeg\bin",
        };

        foreach (var p in commonPaths.Where(Directory.Exists))
        {
            GlobalFFOptions.Configure(o => o.BinaryFolder = p);
            return;
        }

        // Priority 4: rely on system PATH (ffmpeg installed globally)
        // GlobalFFOptions stays default — FFMpegCore will search PATH
    }

    public async Task<GeneratedVideo> ComposeVideoAsync(
        Storyboard storyboard,
        List<AudioSegment> audioSegments,
        SubtitleFile? subtitleFile,
        string outputDir,
        VideoResolution resolution,
        IProgress<VideoProgressUpdate>? progress = null)
    {
        Directory.CreateDirectory(outputDir);
        var (width, height) = Resolutions[resolution];
        var safeTitle  = SanitizeFileName(storyboard.VideoTitle);
        var outputPath = Path.Combine(outputDir, $"{safeTitle}.mp4");
        var tempDir    = Path.Combine(outputDir, "temp");
        Directory.CreateDirectory(tempDir);

        // ── Verify FFmpeg is accessible before starting ───────────────────────
        if (!await IsFFmpegAvailableAsync())
        {
            return new GeneratedVideo
            {
                Title        = storyboard.VideoTitle,
                Status       = VideoGenerationStatus.Failed,
                ErrorMessage = "FFmpeg executable not found. " +
                               "Please verify the path in appsettings.Development.json → FFmpeg:BinaryFolder. " +
                               $"Configured path: '{_ffmpegBinaryFolder}'. " +
                               "Also try running 'ffmpeg -version' in a new terminal to confirm it works."
            };
        }

        progress?.Report(new VideoProgressUpdate
        {
            Stage           = "Video Composer",
            Message         = $"🎬 Composing {storyboard.Scenes.Count} scenes → {width}×{height} MP4...",
            ProgressPercent = 66,
            Status          = VideoGenerationStatus.ComposingVideo
        });

        try
        {
            var sceneClips = new List<string>();

            for (int i = 0; i < storyboard.Scenes.Count; i++)
            {
                var scene    = storyboard.Scenes[i];
                var segment  = audioSegments.FirstOrDefault(a => a.SceneIndex == scene.Index);
                var clipPath = Path.Combine(tempDir, $"clip_{scene.Index:D3}.mp4");

                int pct = 66 + (int)((double)i / storyboard.Scenes.Count * 22);
                progress?.Report(new VideoProgressUpdate
                {
                    Stage           = "Video Composer",
                    Message         = $"🎬 Scene {i + 1}/{storyboard.Scenes.Count}: {scene.Title}",
                    ProgressPercent = pct,
                    Status          = VideoGenerationStatus.ComposingVideo
                });

                var success = await ComposeSceneClipAsync(
                    scene, segment, clipPath, width, height);

                if (success && File.Exists(clipPath) && new FileInfo(clipPath).Length > 0)
                    sceneClips.Add(clipPath);
                else
                    progress?.Report(new VideoProgressUpdate
                    {
                        Stage   = "Video Composer",
                        Message = $"⚠️ Scene {i + 1} clip failed or empty, skipping",
                        ProgressPercent = pct,
                        Status  = VideoGenerationStatus.ComposingVideo
                    });
            }

            if (!sceneClips.Any())
                throw new InvalidOperationException(
                    "No scene clips were created. Check FFmpeg path and image/audio files.");

            progress?.Report(new VideoProgressUpdate
            {
                Stage           = "Video Composer",
                Message         = $"🔗 Concatenating {sceneClips.Count} clips...",
                ProgressPercent = 89,
                Status          = VideoGenerationStatus.ComposingVideo
            });

            await ConcatenateClipsAsync(sceneClips, outputPath, subtitleFile?.FilePath);

            // Verify output file has actual content
            var fileInfo = new FileInfo(outputPath);
            if (!fileInfo.Exists || fileInfo.Length < 1024)
                throw new InvalidOperationException(
                    $"Output video is empty or too small ({fileInfo.Length} bytes). " +
                    "FFmpeg may have failed silently — check that all scene images exist.");

            // Generate thumbnail
            var thumbnailPath = Path.Combine(outputDir, "thumbnail.jpg");
            await GenerateThumbnailAsync(outputPath, thumbnailPath);

            var mediaInfo = await FFProbe.AnalyseAsync(outputPath);

            CleanupTemp(tempDir);

            var video = new GeneratedVideo
            {
                Title           = storyboard.VideoTitle,
                FilePath        = outputPath,
                FileName        = Path.GetFileName(outputPath),
                FileSizeBytes   = fileInfo.Length,
                DurationSeconds = mediaInfo.Duration.TotalSeconds,
                Resolution      = $"{width}x{height}",
                SceneCount      = storyboard.Scenes.Count,
                HasSubtitles    = subtitleFile != null,
                SubtitlePath    = subtitleFile?.FilePath,
                ThumbnailPath   = File.Exists(thumbnailPath) ? thumbnailPath : null,
                Status          = VideoGenerationStatus.Completed
            };

            progress?.Report(new VideoProgressUpdate
            {
                Stage           = "Video Composer",
                Message         = $"✅ Video ready! {FormatSize(fileInfo.Length)}, {mediaInfo.Duration:mm\\:ss}",
                ProgressPercent = 100,
                Status          = VideoGenerationStatus.Completed
            });

            return video;
        }
        catch (Exception ex)
        {
            CleanupTemp(tempDir);
            return new GeneratedVideo
            {
                Title        = storyboard.VideoTitle,
                Status       = VideoGenerationStatus.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    // ── Fix: Verify FFmpeg binary exists and is executable ────────────────────
    private static async Task<bool> IsFFmpegAvailableAsync()
    {
        try
        {
            var tcs  = new TaskCompletionSource<bool>();
            var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = "ffmpeg",
                    Arguments              = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                },
                EnableRaisingEvents = true
            };
            proc.Exited += (_, _) => tcs.TrySetResult(proc.ExitCode == 0);
            proc.Start();
            await Task.WhenAny(tcs.Task, Task.Delay(5000));
            return proc.HasExited && proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> ComposeSceneClipAsync(
        StoryboardScene scene,
        AudioSegment? segment,
        string outputPath,
        int width, int height)
    {
        try
        {
            var imagePath = scene.GeneratedImagePath;
            var audioPath = segment?.FilePath;
            var duration  = segment?.DurationSeconds > 0
                ? segment.DurationSeconds
                : (scene.AudioDurationSeconds > 0 ? scene.AudioDurationSeconds : scene.DurationSeconds);

            if (duration <= 0) duration = 5;

            bool hasImage = !string.IsNullOrEmpty(imagePath) && File.Exists(imagePath);
            bool hasAudio = !string.IsNullOrEmpty(audioPath) && File.Exists(audioPath);

            var args = new StringBuilder();

            // Input: image or colour background
            if (hasImage)
                args.Append($"-loop 1 -framerate 30 -t {duration:F3} -i \"{imagePath}\" ");
            else
                args.Append($"-f lavfi -t {duration:F3} " +
                            $"-i \"color=c=0x1a1a2e:size={width}x{height}:rate=30\" ");

            // Input: audio or silence
            if (hasAudio)
                args.Append($"-i \"{audioPath}\" ");
            else
                args.Append($"-f lavfi -t {duration:F3} -i \"anullsrc=r=44100:cl=mono\" ");

            // Scale + pad to exact resolution
            var scaleFilter =
                $"scale={width}:{height}:force_original_aspect_ratio=decrease," +
                $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:black," +
                $"setsar=1,format=yuv420p";

            // Fade in/out
            var fadeFilter = scene.TransitionType == "fade"
                ? $",fade=t=in:st=0:d=0.4,fade=t=out:st={Math.Max(0, duration - 0.4):F2}:d=0.4"
                : "";

            args.Append($"-vf \"{scaleFilter}{fadeFilter}\" ");
            args.Append("-c:v libx264 -preset fast -crf 23 ");
            args.Append("-c:a aac -b:a 128k -ar 44100 ");
            args.Append($"-t {duration:F3} ");
            args.Append("-map 0:v:0 -map 1:a:0 ");
            args.Append("-shortest ");
            args.Append($"-y \"{outputPath}\"");

            await RunFFmpegAsync(args.ToString());
            return File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task ConcatenateClipsAsync(
        List<string> clips,
        string outputPath,
        string? subtitlePath)
    {
        var concatFile = Path.Combine(Path.GetDirectoryName(outputPath)!, "concat_list.txt");
        var lines      = clips.Select(c => $"file '{c.Replace(@"\", "/")}'");
        await File.WriteAllLinesAsync(concatFile, lines);

        var args = new StringBuilder();
        args.Append($"-f concat -safe 0 -i \"{concatFile}\" ");
        args.Append("-c:v libx264 -preset fast -crf 22 ");
        args.Append("-c:a aac -b:a 128k -ar 44100 ");
        args.Append("-movflags +faststart ");

        // Burn-in subtitles if available
        if (!string.IsNullOrEmpty(subtitlePath) && File.Exists(subtitlePath))
        {
            // Escape Windows path for FFmpeg subtitle filter
            var escapedSub = subtitlePath
                .Replace("\\", "/")
                .Replace(":", "\\:");
            args.Append($"-vf \"subtitles='{escapedSub}':force_style=" +
                        "'FontSize=20,FontName=Arial,PrimaryColour=&Hffffff," +
                        "OutlineColour=&H000000,Outline=2,Shadow=1'\" ");
        }

        args.Append($"-y \"{outputPath}\"");

        await RunFFmpegAsync(args.ToString());

        try { File.Delete(concatFile); } catch { }
    }

    private static async Task GenerateThumbnailAsync(string videoPath, string thumbPath)
    {
        try
        {
            var args = $"-i \"{videoPath}\" -ss 00:00:03 -vframes 1 " +
                       $"-vf \"scale=1280:720:force_original_aspect_ratio=decrease\" " +
                       $"-y \"{thumbPath}\"";
            await RunFFmpegAsync(args);
        }
        catch { /* thumbnail optional */ }
    }

    private static async Task RunFFmpegAsync(string args)
    {
        var tcs  = new TaskCompletionSource<bool>();
        var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "ffmpeg",
                Arguments              = args,
                RedirectStandardError  = true,
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            },
            EnableRaisingEvents = true
        };

        var errorOutput = new StringBuilder();
        proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) errorOutput.AppendLine(e.Data); };

        proc.Exited += (_, _) =>
        {
            if (proc.ExitCode != 0)
                tcs.TrySetException(new Exception(
                    $"FFmpeg exited with code {proc.ExitCode}: {errorOutput}"));
            else
                tcs.TrySetResult(true);
        };

        proc.Start();
        proc.BeginErrorReadLine();

        // Timeout: 10 minutes per operation
        var timeout = Task.Delay(TimeSpan.FromMinutes(10));
        var done    = await Task.WhenAny(tcs.Task, timeout);

        if (done == timeout)
        {
            try { proc.Kill(); } catch { }
            throw new TimeoutException("FFmpeg operation timed out after 10 minutes.");
        }

        await tcs.Task; // rethrow any exception
    }

    private static void CleanupTemp(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray())
                   .Replace(' ', '_')[..Math.Min(name.Length, 80)];
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024        => $"{bytes}B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1}KB",
        _             => $"{bytes / (1024.0 * 1024):F1}MB"
    };
}
