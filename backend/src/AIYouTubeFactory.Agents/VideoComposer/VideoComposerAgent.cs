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

    // ── Store the FULL PATH to ffmpeg.exe and ffprobe.exe ─────────────────────
    private readonly string _ffmpegExe;
    private readonly string _ffprobeExe;

    public VideoComposerAgent(string ffmpegBinaryFolder = "")
    {
        var folder  = ResolveFFmpegFolder(ffmpegBinaryFolder);
        _ffmpegExe  = string.IsNullOrEmpty(folder) ? "ffmpeg"  : Path.Combine(folder, "ffmpeg.exe");
        _ffprobeExe = string.IsNullOrEmpty(folder) ? "ffprobe" : Path.Combine(folder, "ffprobe.exe");

        // Tell FFMpegCore where the binaries are
        if (!string.IsNullOrEmpty(folder))
            GlobalFFOptions.Configure(o => o.BinaryFolder = folder);

        Console.WriteLine($"[VideoComposer] Using ffmpeg: {_ffmpegExe}");
        Console.WriteLine($"[VideoComposer] ffmpeg.exe exists: {File.Exists(_ffmpegExe)}");
    }

    // ── Resolve full ffmpeg folder using multiple strategies ──────────────────
    private static string ResolveFFmpegFolder(string configuredPath)
    {
        // Strategy 1: Explicitly configured path — check ffmpeg.exe is actually there
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var exePath = Path.Combine(configuredPath, "ffmpeg.exe");
            if (File.Exists(exePath))
            {
                Console.WriteLine($"[VideoComposer] FFmpeg found at configured path: {exePath}");
                return configuredPath;
            }
            Console.WriteLine($"[VideoComposer] WARNING: ffmpeg.exe NOT at: {exePath}");
        }

        // Strategy 2: Auto-scan WinGet packages folder
        var localAppData    = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var wingetPackages  = Path.Combine(localAppData, "Microsoft", "WinGet", "Packages");

        if (Directory.Exists(wingetPackages))
        {
            var found = Directory
                .GetDirectories(wingetPackages, "Gyan.FFmpeg*", SearchOption.TopDirectoryOnly)
                .SelectMany(d => Directory.GetDirectories(d, "*build*", SearchOption.AllDirectories))
                .Select(d => Path.Combine(d, "bin"))
                .FirstOrDefault(d => File.Exists(Path.Combine(d, "ffmpeg.exe")));

            if (found != null)
            {
                Console.WriteLine($"[VideoComposer] FFmpeg auto-discovered (WinGet): {found}");
                return found;
            }
        }

        // Strategy 3: Common manual install locations
        var candidates = new[]
        {
            @"C:\ffmpeg\bin",
            @"C:\ffmpeg\ffmpeg\bin",
            @"C:\Program Files\ffmpeg\bin",
            @"C:\Program Files (x86)\ffmpeg\bin",
            @"C:\Tools\ffmpeg\bin",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ffmpeg", "bin"),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(Path.Combine(path, "ffmpeg.exe")))
            {
                Console.WriteLine($"[VideoComposer] FFmpeg found at candidate: {path}");
                return path;
            }
        }

        // Strategy 4: Rely on system PATH
        Console.WriteLine("[VideoComposer] FFmpeg not found locally — using system PATH");
        return string.Empty;
    }

    public async Task<GeneratedVideo> ComposeVideoAsync(
        Storyboard storyboard,
        List<AudioSegment> audioSegments,
        SubtitleFile? subtitleFile,
        string outputDir,
        VideoResolution resolution,
        IProgress<VideoProgressUpdate>? progress = null)
    {
        // Ensure outputDir is absolute to avoid ffmpeg resolving relative paths incorrectly
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);
        var (width, height) = Resolutions[resolution];
        var safeTitle  = SanitizeFileName(storyboard.VideoTitle);
        var outputPath = Path.Combine(outputDir, $"{safeTitle}.mp4");
        var tempDir    = Path.Combine(outputDir, "temp");
        Directory.CreateDirectory(tempDir);

        // ── Verify ffmpeg.exe is accessible ──────────────────────────────────
        var (isAvailable, ffmpegVersion) = await CheckFFmpegAsync();
        if (!isAvailable)
        {
            var msg = _ffmpegExe == "ffmpeg"
                ? "FFmpeg not found on system PATH. Install FFmpeg and add it to PATH, " +
                  "or set FFmpeg:BinaryFolder in appsettings.Development.json."
                : $"FFmpeg executable not found at: {_ffmpegExe}\n" +
                  $"Please verify the path in appsettings.Development.json → FFmpeg:BinaryFolder.\n" +
                  $"Current value: '{Path.GetDirectoryName(_ffmpegExe)}'";

            return new GeneratedVideo
            {
                Title        = storyboard.VideoTitle,
                Status       = VideoGenerationStatus.Failed,
                ErrorMessage = msg
            };
        }

        Console.WriteLine($"[VideoComposer] FFmpeg OK: {ffmpegVersion}");

        progress?.Report(new VideoProgressUpdate
        {
            Stage           = "Video Composer",
            Message         = $"🎬 Composing {storyboard.Scenes.Count} scenes → {width}×{height}...",
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

                var success = await ComposeSceneClipAsync(scene, segment, clipPath, width, height);

                if (success && File.Exists(clipPath) && new FileInfo(clipPath).Length > 0)
                    sceneClips.Add(clipPath);
                else
                    progress?.Report(new VideoProgressUpdate
                    {
                        Stage           = "Video Composer",
                        Message         = $"⚠️ Scene {i + 1} clip empty/failed, skipping",
                        ProgressPercent = pct,
                        Status          = VideoGenerationStatus.ComposingVideo
                    });
            }

            if (!sceneClips.Any())
                throw new InvalidOperationException(
                    "No scene clips were created. Check FFmpeg path and scene image/audio files.");

            progress?.Report(new VideoProgressUpdate
            {
                Stage           = "Video Composer",
                Message         = $"🔗 Concatenating {sceneClips.Count} clips...",
                ProgressPercent = 89,
                Status          = VideoGenerationStatus.ComposingVideo
            });

            await ConcatenateClipsAsync(sceneClips, outputPath, subtitleFile?.FilePath);

            var fileInfo = new FileInfo(outputPath);
            if (!fileInfo.Exists || fileInfo.Length < 1024)
                throw new InvalidOperationException(
                    $"Output video is empty ({fileInfo.Length} bytes). " +
                    "FFmpeg may have encountered an error — check the console logs.");

            // Generate thumbnail
            var thumbPath = Path.Combine(outputDir, "thumbnail.jpg");
            await GenerateThumbnailAsync(outputPath, thumbPath);

            // Get duration via ffprobe
            var duration = await GetVideoDurationAsync(outputPath);

            CleanupTemp(tempDir);

            var video = new GeneratedVideo
            {
                Title           = storyboard.VideoTitle,
                FilePath        = outputPath,
                FileName        = Path.GetFileName(outputPath),
                FileSizeBytes   = fileInfo.Length,
                DurationSeconds = duration,
                Resolution      = $"{width}x{height}",
                SceneCount      = storyboard.Scenes.Count,
                HasSubtitles    = subtitleFile != null,
                SubtitlePath    = subtitleFile?.FilePath,
                ThumbnailPath   = File.Exists(thumbPath) ? thumbPath : null,
                Status          = VideoGenerationStatus.Completed
            };

            progress?.Report(new VideoProgressUpdate
            {
                Stage           = "Video Composer",
                Message         = $"✅ Video ready! {FormatSize(fileInfo.Length)}, {TimeSpan.FromSeconds(duration):mm\\:ss}",
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

    // ── Check ffmpeg works using FULL PATH ────────────────────────────────────
    private async Task<(bool ok, string version)> CheckFFmpegAsync()
    {
        try
        {
            var output = new StringBuilder();
            var proc   = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = _ffmpegExe,   // FULL PATH — not just "ffmpeg"
                    Arguments              = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                }
            };
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
            var firstLine = output.ToString().Split('\n').FirstOrDefault() ?? "";
            return (proc.ExitCode == 0, firstLine.Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<bool> ComposeSceneClipAsync(
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

            if (hasImage)
                args.Append($"-loop 1 -framerate 30 -t {duration:F3} -i \"{imagePath}\" ");
            else
                args.Append($"-f lavfi -t {duration:F3} -i \"color=c=0x1a1a2e:size={width}x{height}:rate=30\" ");

            if (hasAudio)
                args.Append($"-i \"{audioPath}\" ");
            else
                args.Append($"-f lavfi -t {duration:F3} -i \"anullsrc=r=44100:cl=mono\" ");

            var scaleFilter =
                $"scale={width}:{height}:force_original_aspect_ratio=decrease," +
                $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:black," +
                $"setsar=1,format=yuv420p";

            var fadeFilter = scene.TransitionType == "fade"
                ? $",fade=t=in:st=0:d=0.4,fade=t=out:st={Math.Max(0, duration - 0.4):F2}:d=0.4"
                : "";

            args.Append($"-vf \"{scaleFilter}{fadeFilter}\" ");
            args.Append("-c:v libx264 -preset fast -crf 23 ");
            args.Append("-c:a aac -b:a 128k -ar 44100 ");
            args.Append($"-t {duration:F3} ");
            args.Append("-map 0:v:0 -map 1:a:0 -shortest ");
            args.Append($"-y \"{outputPath}\"");

            await RunFFmpegAsync(args.ToString());
            return File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VideoComposer] Scene clip failed: {ex.Message}");
            return false;
        }
    }

    private async Task ConcatenateClipsAsync(
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

        if (!string.IsNullOrEmpty(subtitlePath) && File.Exists(subtitlePath))
        {
            var escapedSub = subtitlePath.Replace("\\", "/").Replace(":", "\\:");
            args.Append($"-vf \"subtitles='{escapedSub}':force_style=" +
                        "'FontSize=20,FontName=Arial,PrimaryColour=&Hffffff," +
                        "OutlineColour=&H000000,Outline=2,Shadow=1'\" ");
        }

        args.Append($"-y \"{outputPath}\"");

        await RunFFmpegAsync(args.ToString());
        try { File.Delete(concatFile); } catch { }
    }

    private async Task GenerateThumbnailAsync(string videoPath, string thumbPath)
    {
        try
        {
            var args = $"-i \"{videoPath}\" -ss 00:00:03 -vframes 1 " +
                       $"-vf \"scale=1280:720:force_original_aspect_ratio=decrease\" " +
                       $"-y \"{thumbPath}\"";
            await RunFFmpegAsync(args);
        }
        catch { /* thumbnail is optional */ }
    }

    private async Task<double> GetVideoDurationAsync(string videoPath)
    {
        try
        {
            // Use ffprobe with full path
            var args = $"-v error -show_entries format=duration " +
                       $"-of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"";

            var output = new StringBuilder();
            var proc   = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = _ffprobeExe,  // FULL PATH
                    Arguments              = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                }
            };
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            proc.Start();
            proc.BeginOutputReadLine();
            await proc.WaitForExitAsync();

            if (double.TryParse(output.ToString().Trim(), out var d)) return d;
        }
        catch { }

        // Fallback: estimate from file size
        return 0;
    }

    // ── Run ffmpeg using FULL PATH ─────────────────────────────────────────────
    private async Task RunFFmpegAsync(string args)
    {
        var errorOutput = new StringBuilder();
        var proc        = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = _ffmpegExe,   // FULL PATH — key fix
                Arguments              = args,
                RedirectStandardError  = true,
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            },
            EnableRaisingEvents = true
        };

        var tcs = new TaskCompletionSource<bool>();
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) errorOutput.AppendLine(e.Data); };
        proc.Exited += (_, _) =>
        {
            if (proc.ExitCode != 0)
                tcs.TrySetException(new Exception(
                    $"FFmpeg failed (exit {proc.ExitCode}):\n{errorOutput}"));
            else
                tcs.TrySetResult(true);
        };

        proc.Start();
        proc.BeginErrorReadLine();

        var timeout = Task.Delay(TimeSpan.FromMinutes(10));
        var done    = await Task.WhenAny(tcs.Task, timeout);

        if (done == timeout)
        {
            try { proc.Kill(); } catch { }
            throw new TimeoutException("FFmpeg timed out after 10 minutes.");
        }

        await tcs.Task;
    }

    private static void CleanupTemp(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "video";
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Replace(' ', '_');
        return sanitized.Length <= 80 ? sanitized : sanitized[..80];
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024        => $"{bytes}B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1}KB",
        _             => $"{bytes / (1024.0 * 1024):F1}MB"
    };
}
