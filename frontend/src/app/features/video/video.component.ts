import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ContentStateService } from '../../core/services/content-state.service';
import { SignalRService } from '../../core/services/signalr.service';
import { ApiService } from '../../core/services/api.service';
import {
  ContentGenerationResult,
  YouTubeVideoScript,
  VideoProgressUpdate
} from '../../core/models/content.models';
import { environment } from '../../../environments/environment';
import { Subscription } from 'rxjs';

interface VideoJob {
  videoSessionId:   string;
  scriptTitle:      string;
  scriptIndex:      number;
  status:           'pending' | 'running' | 'completed' | 'failed';
  progressPercent:  number;
  stage:            string;
  message:          string;
  videoId?:         string;
  durationSeconds?: number;
  fileSizeBytes?:   number;
  resolution?:      string;
  downloadUrl?:     string;
  subtitleUrl?:     string;
  thumbnailUrl?:    string;
  errorMessage?:    string;
}

@Component({
  selector: 'app-video',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatSlideToggleModule, MatProgressBarModule,
    MatSnackBarModule, MatDividerModule, MatTooltipModule
  ],
  template: `
    <div class="video-page">

      <!-- Header -->
      <div class="page-header">
        <h2>🎬 Video Studio</h2>
        <p>Generate downloadable MP4 videos from your scripts — AI voice, scene images & subtitles</p>
      </div>

      <!-- Pipeline overview -->
      <div class="pipeline-bar">
        <div *ngFor="let s of pipelineSteps; let last=last" class="pipe-step">
          <span class="pipe-icon">{{ s.icon }}</span>
          <span class="pipe-label">{{ s.label }}</span>
          <span *ngIf="!last" class="pipe-arr">›</span>
        </div>
      </div>

      <!-- No scripts -->
      <div class="empty-state" *ngIf="!scripts.length">
        <div style="font-size:56px">📹</div>
        <h3>No scripts yet</h3>
        <p>Upload a document and generate content first, then come back here.</p>
      </div>

      <!-- Main layout -->
      <div class="main-layout" *ngIf="scripts.length">

        <!-- ── Left: settings ── -->
        <div class="left-col">
          <mat-card class="settings-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>tune</mat-icon>
              <mat-card-title>Video Settings</mat-card-title>
            </mat-card-header>
            <mat-card-content>

              <!-- Fix 3A: Script selector -->
              <div class="field-group">
                <label>📄 Select Script ({{ scripts.length }} available)</label>
                <div class="script-list">
                  <div *ngFor="let s of scripts"
                       class="script-option"
                       [class.selected]="selectedScriptIndex === s.index"
                       (click)="selectedScriptIndex = s.index">
                    <div class="script-opt-num">{{ s.index }}</div>
                    <div class="script-opt-text">
                      <span class="script-opt-title">{{ s.title }}</span>
                      <span class="script-opt-meta">~{{ s.estimatedDurationMinutes }}min</span>
                    </div>
                    <mat-icon class="script-opt-check"
                              *ngIf="selectedScriptIndex === s.index">
                      check_circle
                    </mat-icon>
                  </div>
                </div>
              </div>

              <mat-divider style="margin:12px 0"></mat-divider>

              <!-- Resolution -->
              <div class="field-group">
                <label>📐 Resolution</label>
                <div class="btn-group">
                  <button *ngFor="let r of resolutions"
                          [class.active]="resolution === r.value"
                          (click)="resolution = r.value"
                          class="opt-btn">
                    {{ r.label }}
                  </button>
                </div>
              </div>

              <!-- Voice -->
              <div class="field-group">
                <label>🎙️ AI Voice</label>
                <select [(ngModel)]="voiceName" class="sel">
                  <option value="en-US-JennyNeural">🇺🇸 Jenny — Friendly Female</option>
                  <option value="en-US-GuyNeural">🇺🇸 Guy — Professional Male</option>
                  <option value="en-US-AriaNeural">🇺🇸 Aria — News Female</option>
                  <option value="en-US-DavisNeural">🇺🇸 Davis — Casual Male</option>
                  <option value="en-GB-SoniaNeural">🇬🇧 Sonia — British Female</option>
                  <option value="en-GB-RyanNeural">🇬🇧 Ryan — British Male</option>
                  <option value="en-IN-NeerjaNeural">🇮🇳 Neerja — Indian Female</option>
                </select>
              </div>

              <!-- Style -->
              <div class="field-group">
                <label>🎨 Image Style</label>
                <select [(ngModel)]="imageStyle" class="sel">
                  <option value="cinematic, professional, high quality">🎬 Cinematic</option>
                  <option value="minimalist, clean, modern">⬜ Minimalist</option>
                  <option value="vibrant, colorful, energetic">🌈 Vibrant</option>
                  <option value="dark, dramatic, moody">🌑 Dark & Dramatic</option>
                  <option value="flat design, illustration">🎨 Illustrated</option>
                  <option value="realistic photography, bokeh">📷 Photorealistic</option>
                </select>
              </div>

              <mat-divider style="margin:12px 0"></mat-divider>

              <!-- Toggles -->
              <div class="toggle-row">
                <span>🖼️ Generate AI Images</span>
                <mat-slide-toggle [(ngModel)]="generateImages" color="accent"></mat-slide-toggle>
              </div>
              <div class="toggle-row">
                <span>📝 Burn Subtitles</span>
                <mat-slide-toggle [(ngModel)]="generateSubtitles" color="accent"></mat-slide-toggle>
              </div>

            </mat-card-content>
            <mat-card-actions style="padding:16px">
              <button mat-flat-button class="gen-btn"
                      (click)="startGeneration()"
                      [disabled]="isGenerating || !selectedScriptIndex">
                <mat-icon>{{ isGenerating ? 'hourglass_empty' : 'movie_creation' }}</mat-icon>
                {{ isGenerating ? 'Generating…' : 'Generate Video' }}
              </button>
            </mat-card-actions>
          </mat-card>

          <!-- Requirements box -->
          <div class="req-box">
            <p class="req-title">⚙️ Requirements</p>
            <div class="req-item">
              <span>FFmpeg</span>
              <a href="https://ffmpeg.org/download.html" target="_blank">Download →</a>
            </div>
            <div class="req-item">
              <span>Azure Speech Key</span>
              <a href="https://portal.azure.com" target="_blank">Portal →</a>
            </div>
            <div class="req-item">
              <span>Image Deployment</span>
              <code>{{ imageDeploymentHint }}</code>
            </div>
          </div>
        </div>

        <!-- ── Right: progress + results ── -->
        <div class="right-col">

          <!-- Active job progress -->
          <mat-card class="progress-card" *ngIf="activeJob">
            <div class="prog-header">
              <div class="prog-title">
                <span class="spin" *ngIf="activeJob.status==='running'">⚡</span>
                {{ activeJob.scriptTitle }}
              </div>
              <span class="prog-pct">{{ activeJob.progressPercent }}%</span>
            </div>

            <!-- Stage pills -->
            <div class="stage-pills">
              <div *ngFor="let s of pipelineSteps" class="s-pill"
                   [class.s-active]="activeJob.stage === s.label"
                   [class.s-done]="isStepDone(activeJob.stage, s.label)">
                {{ s.icon }} {{ s.label }}
              </div>
            </div>

            <mat-progress-bar
              mode="determinate"
              [value]="activeJob.progressPercent"
              [color]="activeJob.status==='completed' ? 'accent' : 'primary'"
              style="margin:10px 0;border-radius:4px">
            </mat-progress-bar>

            <p class="prog-msg">{{ activeJob.message }}</p>
          </mat-card>

          <!-- ── Fix 3B: Completed videos with proper download ── -->
          <div *ngIf="completedJobs.length" class="results-section">
            <h3 class="results-title">
              ✅ Generated Videos ({{ completedJobs.length }})
            </h3>

            <div *ngFor="let job of completedJobs" class="video-card">

              <!-- Thumbnail preview -->
              <div class="v-thumb">
                <img *ngIf="job.thumbnailUrl"
                     [src]="resolveUrl(job.thumbnailUrl)"
                     alt="thumbnail"
                     (error)="hideImage($event)">
                <div *ngIf="!job.thumbnailUrl" class="v-thumb-ph">🎬</div>
                <span class="v-dur" *ngIf="job.durationSeconds">
                  {{ fmtDuration(job.durationSeconds) }}
                </span>
              </div>

              <!-- Info + downloads -->
              <div class="v-body">
                <p class="v-title">{{ job.scriptTitle }}</p>

                <div class="v-meta">
                  <span *ngIf="job.resolution">📐 {{ job.resolution }}</span>
                  <span *ngIf="job.fileSizeBytes">💾 {{ fmtSize(job.fileSizeBytes) }}</span>
                  <span *ngIf="job.durationSeconds">⏱️ {{ fmtDuration(job.durationSeconds) }}</span>
                </div>

                <!-- Download buttons -->
                <div class="v-actions">

                  <!-- MP4 download — Fix 3B: force browser download -->
                  <button class="dl-btn"
                          (click)="downloadVideo(job)"
                          *ngIf="job.videoId || job.downloadUrl">
                    ⬇️ Download MP4
                  </button>

                  <!-- SRT subtitle download -->
                  <button class="sub-btn"
                          (click)="downloadSubtitle(job)"
                          *ngIf="job.subtitleUrl">
                    📝 Download SRT
                  </button>

                </div>
              </div>
            </div>
          </div>

          <!-- Failed jobs -->
          <div *ngIf="failedJobs.length" class="failed-section">
            <h3 style="color:#f87171;font-size:14px;margin:0 0 8px">
              ❌ Failed ({{ failedJobs.length }})
            </h3>
            <div *ngFor="let job of failedJobs" class="failed-card">
              <span class="f-title">{{ job.scriptTitle }}</span>
              <span class="f-err">{{ job.errorMessage }}</span>
            </div>
          </div>

          <!-- Empty right panel -->
          <div class="right-empty"
               *ngIf="!activeJob && !completedJobs.length && !failedJobs.length">
            <div style="font-size:52px">🎬</div>
            <h3>No videos yet</h3>
            <p>Select a script on the left and click <strong>Generate Video</strong>.</p>
            <p class="hint">Generation takes 5–15 minutes depending on script length.</p>
          </div>

        </div>
      </div>
    </div>
  `,
})
export class VideoComponent implements OnInit, OnDestroy {
  result:        ContentGenerationResult | null = null;
  scripts:       YouTubeVideoScript[] = [];
  activeJob:     VideoJob | null = null;
  completedJobs: VideoJob[] = [];
  failedJobs:    VideoJob[] = [];
  isGenerating   = false;

  // Settings — Fix 3A
  selectedScriptIndex = 0;
  resolution          = 1;
  voiceName           = 'en-US-JennyNeural';
  voiceStyle          = 'newscast';
  generateImages      = true;
  generateSubtitles   = true;
  imageStyle          = 'cinematic, professional, high quality';
  imageDeploymentHint = 'MAI-Image-2.5';

  resolutions = [
    { label: '720p',  value: 0 },
    { label: '1080p', value: 1 },
    { label: '4K',    value: 2 },
  ];

  pipelineSteps = [
    { icon: '📋', label: 'Storyboard' },
    { icon: '🖼️', label: 'Images'     },
    { icon: '🎙️', label: 'Voice'      },
    { icon: '📝', label: 'Subtitles'  },
    { icon: '🎬', label: 'Compose'    },
  ];

  private stageOrder = ['Storyboard', 'Images', 'Voice', 'Subtitles', 'Compose'];
  private subs: Subscription[] = [];
  private pollTimer: any;

  constructor(
    private state:   ContentStateService,
    private signalR: SignalRService,
    private api:     ApiService,
    private http:    HttpClient,
    private snack:   MatSnackBar
  ) {}

  ngOnInit(): void {
    this.subs.push(
      this.state.result$.subscribe(r => {
        this.result  = r;
        this.scripts = r?.youTubeScripts ?? [];
        if (this.scripts.length && !this.selectedScriptIndex)
          this.selectedScriptIndex = this.scripts[0].index;
      }),
      this.signalR.videoProgress$.subscribe((u: VideoProgressUpdate) => {
        if (!this.activeJob) return;
        this.activeJob.stage           = this.mapStage(u.stage);
        this.activeJob.message         = u.message;
        this.activeJob.progressPercent = u.progressPercent;
        this.activeJob.status          = 'running';
      }),
      this.signalR.videoComplete$.subscribe((d: any) => this.onComplete(d)),
      this.signalR.videoError$.subscribe((e: string) => {
        if (!this.activeJob) return;
        this.activeJob.status       = 'failed';
        this.activeJob.errorMessage = e;
        this.failedJobs.unshift({ ...this.activeJob });
        this.activeJob    = null;
        this.isGenerating = false;
        clearInterval(this.pollTimer);
        this.snack.open(`❌ ${e}`, 'OK', { duration: 6000 });
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    clearInterval(this.pollTimer);
  }

  async startGeneration(): Promise<void> {
    if (!this.result || this.isGenerating || !this.selectedScriptIndex) return;

    const script = this.scripts.find(s => s.index === this.selectedScriptIndex);
    if (!script) { this.snack.open('Please select a script', 'OK', { duration: 3000 }); return; }

    this.isGenerating = true;

    if (!this.signalR.isConnected)
      try { await this.signalR.connect(); } catch { /* continue */ }

    this.activeJob = {
      videoSessionId:  '',
      scriptTitle:     script.title,
      scriptIndex:     script.index,
      status:          'pending',
      progressPercent: 0,
      stage:           'Storyboard',
      message:         'Starting pipeline...'
    };

    const req = {
      sessionId:         this.result.sessionId,
      scriptIndex:       this.selectedScriptIndex,
      voiceName:         this.voiceName,
      voiceStyle:        this.voiceStyle,
      speechRate:        1.0,
      generateSubtitles: this.generateSubtitles,
      generateImages:    this.generateImages,
      imageStyle:        this.imageStyle,
      outputFormat:      'mp4',
      resolution:        this.resolution
    };

    this.api.startVideoGeneration(req).subscribe({
      next: async (resp) => {
        this.activeJob!.videoSessionId = resp.videoSessionId;
        this.activeJob!.status         = 'running';
        this.activeJob!.message        = 'Pipeline running — generating storyboard...';
        await this.signalR.joinSession(resp.videoSessionId);
        this.startPolling(script.title);
        this.snack.open(`🎬 Started: ${script.title}`, 'OK', { duration: 3000 });
      },
      error: err => {
        this.isGenerating             = false;
        this.activeJob!.status        = 'failed';
        this.activeJob!.errorMessage  = err.error?.message || err.message;
        this.failedJobs.unshift({ ...this.activeJob! });
        this.activeJob = null;
        this.snack.open(`❌ ${err.error?.message || 'Failed to start'}`, 'OK', { duration: 6000 });
      }
    });
  }

  // Fix 3B: Download MP4 using Blob API — forces browser Save dialog
  downloadVideo(job: VideoJob): void {
    const url = job.downloadUrl
      ? this.resolveUrl(job.downloadUrl)
      : `${environment.apiUrl}/video/download/${job.videoId}`;

    this.snack.open('⏳ Preparing download...', '', { duration: 2000 });

    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const a        = document.createElement('a');
        const objectUrl = URL.createObjectURL(blob);
        a.href         = objectUrl;
        a.download     = `${job.scriptTitle.replace(/[^a-z0-9]/gi, '_')}.mp4`;
        a.click();
        URL.revokeObjectURL(objectUrl);
        this.snack.open('✅ Download started!', 'OK', { duration: 3000 });
      },
      error: () => this.snack.open('❌ Download failed', 'OK', { duration: 4000 })
    });
  }

  // Fix 3B: Download SRT subtitle file
  downloadSubtitle(job: VideoJob): void {
    if (!job.subtitleUrl) return;
    const url = this.resolveUrl(job.subtitleUrl);

    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const a        = document.createElement('a');
        const objectUrl = URL.createObjectURL(blob);
        a.href         = objectUrl;
        a.download     = `${job.scriptTitle.replace(/[^a-z0-9]/gi, '_')}.srt`;
        a.click();
        URL.revokeObjectURL(objectUrl);
        this.snack.open('✅ Subtitle downloaded!', '', { duration: 2000 });
      },
      error: () => this.snack.open('❌ Subtitle download failed', 'OK', { duration: 4000 })
    });
  }

  hideImage(event: Event): void {
    const target = event.target as HTMLElement | null;
    if (target?.style) {
      target.style.display = 'none';
    }
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  private startPolling(scriptTitle: string): void {
    this.pollTimer = setInterval(() => {
      this.api.getAllVideos().subscribe(videos => {
        const done = videos.find(v =>
          v.title === scriptTitle &&
          (v.status === 'Completed' || v.status === 'Failed'));
        if (done) { clearInterval(this.pollTimer); this.onComplete(done); }
      });
    }, 7000);
  }

  private onComplete(data: any): void {
    clearInterval(this.pollTimer);
    this.isGenerating = false;

    if (data.status === 'Completed') {
      const job: VideoJob = {
        ...this.activeJob!,
        status:          'completed',
        progressPercent: 100,
        stage:           'Compose',
        message:         'Ready to download!',
        videoId:         data.videoId || data.id,
        durationSeconds: data.durationSeconds,
        fileSizeBytes:   data.fileSizeBytes,
        resolution:      data.resolution,
        downloadUrl:     data.downloadUrl,
        subtitleUrl:     data.subtitleUrl,
        thumbnailUrl:    data.thumbnailUrl
      };
      this.completedJobs.unshift(job);
      this.snack.open(`✅ "${job.scriptTitle}" is ready!`, 'OK', { duration: 6000 });
    } else {
      const job: VideoJob = {
        ...this.activeJob!,
        status:       'failed',
        stage:        'Failed',
        message:      data.errorMessage || 'Failed',
        errorMessage: data.errorMessage
      };
      this.failedJobs.unshift(job);
      this.snack.open(`❌ ${data.errorMessage || 'Video generation failed'}`, 'OK', { duration: 6000 });
    }
    this.activeJob = null;
  }

  isStepDone(current: string, step: string): boolean {
    return this.stageOrder.indexOf(current) > this.stageOrder.indexOf(step) &&
           this.stageOrder.indexOf(step) >= 0;
  }

  private mapStage(s: string): string {
    const m: Record<string, string> = {
      'Storyboard Agent':     'Storyboard', 'GeneratingStoryboard': 'Storyboard',
      'Image Agent':          'Images',     'GeneratingImages':     'Images',
      'Voice Agent':          'Voice',      'GeneratingVoice':      'Voice',
      'Subtitle Agent':       'Subtitles',  'GeneratingSubtitles':  'Subtitles',
      'Video Composer':       'Compose',    'ComposingVideo':        'Compose',
    };
    return m[s] || s;
  }

  resolveUrl(path: string): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    return `${environment.apiUrl.replace('/api', '')}${path}`;
  }

  fmtDuration(s: number): string {
    const m = Math.floor(s / 60);
    const sec = Math.floor(s % 60);
    return `${m}:${sec.toString().padStart(2, '0')}`;
  }

  fmtSize(b: number): string {
    if (!b) return '';
    return b < 1048576 ? `${(b / 1024).toFixed(0)}KB` : `${(b / 1048576).toFixed(1)}MB`;
  }
}
