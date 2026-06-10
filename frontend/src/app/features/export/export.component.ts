import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { ContentStateService } from '../../core/services/content-state.service';
import { ApiService } from '../../core/services/api.service';
import { ContentGenerationResult } from '../../core/models/content.models';

interface ExportOption {
  id: string;
  icon: string;
  title: string;
  description: string;
  format: string;
  color: string;
  downloading: boolean;
}

@Component({
  selector: 'app-export',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatButtonModule, MatIconModule,
    MatProgressBarModule, MatSnackBarModule, MatDividerModule
  ],
  template: `
    <div class="export-page">
      <div class="page-header">
        <div>
          <h2>⬇️ Export Content</h2>
          <p>Download all generated content in your preferred format</p>
        </div>
      </div>

      <!-- Summary Stats -->
      <div class="stats-row" *ngIf="result">
        <div class="stat-pill">🎬 {{ result.youTubeScripts?.length }} YouTube Scripts</div>
        <div class="stat-pill">⚡ {{ result.shortsScripts?.length }} Shorts</div>
        <div class="stat-pill">💼 {{ result.linkedInPosts?.length }} LinkedIn Posts</div>
        <div class="stat-pill">🐦 {{ result.twitterThreads?.length }} Twitter Threads</div>
        <div class="stat-pill">🖼️ {{ result.thumbnailPrompts?.length }} Thumbnail Prompts</div>
        <div class="stat-pill">📅 {{ result.contentPlan?.publishingSchedule?.length }} Schedule Items</div>
      </div>

      <!-- Export Options Grid -->
      <div class="export-grid">
        <div *ngFor="let opt of exportOptions" class="export-card" [style.border-color]="opt.color + '44'">
          <div class="export-icon" [style.background]="opt.color + '22'" [style.border-color]="opt.color + '44'">{{ opt.icon }}</div>
          <div class="export-info">
            <h3>{{ opt.title }}</h3>
            <p>{{ opt.description }}</p>
            <span class="format-badge" [style.color]="opt.color" [style.background]="opt.color + '15'">{{ opt.format }}</span>
          </div>
          <button mat-flat-button class="download-btn" [style.background]="opt.color + '22'" [style.color]="opt.color"
                  (click)="download(opt)" [disabled]="opt.downloading">
            <mat-icon *ngIf="!opt.downloading">download</mat-icon>
            <mat-progress-bar *ngIf="opt.downloading" mode="indeterminate" style="width:60px"></mat-progress-bar>
            {{ opt.downloading ? 'Downloading...' : 'Download' }}
          </button>
        </div>
      </div>

      <mat-divider class="divider"></mat-divider>

      <!-- Quick Copy Section -->
      <div class="quick-copy-section">
        <h3>⚡ Quick Copy</h3>
        <div class="copy-grid">
          <button mat-stroked-button class="copy-item" (click)="copyScriptTitles()">
            <mat-icon>movie</mat-icon> All Video Titles
          </button>
          <button mat-stroked-button class="copy-item" (click)="copyAllHashtags()">
            <mat-icon>tag</mat-icon> All Hashtags
          </button>
          <button mat-stroked-button class="copy-item" (click)="copyAllKeywords()">
            <mat-icon>search</mat-icon> All SEO Keywords
          </button>
          <button mat-stroked-button class="copy-item" (click)="copyAllDallePrompts()">
            <mat-icon>image</mat-icon> All DALL-E Prompts
          </button>
          <button mat-stroked-button class="copy-item" (click)="copyAllTags()">
            <mat-icon>label</mat-icon> All YouTube Tags
          </button>
          <button mat-stroked-button class="copy-item" (click)="copyContentCalendar()">
            <mat-icon>calendar_month</mat-icon> Content Calendar
          </button>
        </div>
      </div>

      <!-- Session Info -->
      <div class="session-info" *ngIf="sessionId">
        <mat-icon>info_outline</mat-icon>
        <span>Session ID: <code>{{ sessionId }}</code> — Save this to retrieve your content later.</span>
      </div>
    </div>
  `,
  styles: [`
    .export-page { padding: 24px 40px; background: #0f0f1a; min-height: calc(100vh - 128px); }

    .page-header { margin-bottom: 20px; }
    .page-header h2 { color: #e2e8f0; font-size: 24px; margin: 0 0 4px; }
    .page-header p { color: #64748b; margin: 0; }

    .stats-row { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 28px; }
    .stat-pill { background: rgba(99,102,241,0.1); border: 1px solid rgba(99,102,241,0.2); color: #818cf8; padding: 6px 14px; border-radius: 20px; font-size: 13px; font-weight: 600; }

    .export-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 16px; margin-bottom: 32px; }

    .export-card {
      background: #1a1a2e;
      border: 1px solid;
      border-radius: 16px;
      padding: 20px;
      display: flex;
      align-items: center;
      gap: 16px;
      transition: all 0.2s;
    }
    .export-card:hover { transform: translateY(-2px); background: #1e1e38; }

    .export-icon {
      width: 52px; height: 52px; border-radius: 12px; border: 1px solid;
      display: flex; align-items: center; justify-content: center;
      font-size: 24px; flex-shrink: 0;
    }

    .export-info { flex: 1; }
    .export-info h3 { color: #e2e8f0; font-size: 15px; font-weight: 700; margin: 0 0 4px; }
    .export-info p { color: #64748b; font-size: 12px; margin: 0 0 8px; line-height: 1.4; }
    .format-badge { font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 6px; }

    .download-btn { border-radius: 10px !important; font-size: 13px !important; font-weight: 600 !important; display: flex !important; align-items: center !important; gap: 4px !important; flex-shrink: 0; }

    .divider { border-color: rgba(99,102,241,0.2) !important; margin: 8px 0 28px !important; }

    .quick-copy-section h3 { color: #e2e8f0; font-size: 18px; margin: 0 0 16px; }
    .copy-grid { display: flex; flex-wrap: wrap; gap: 10px; }
    .copy-item { color: #818cf8 !important; border-color: rgba(99,102,241,0.3) !important; display: flex !important; align-items: center !important; gap: 6px !important; }
    .copy-item mat-icon { font-size: 18px; width: 18px; height: 18px; }

    .session-info {
      margin-top: 28px;
      display: flex;
      align-items: center;
      gap: 8px;
      color: #475569;
      font-size: 13px;
      background: rgba(99,102,241,0.05);
      border: 1px solid rgba(99,102,241,0.15);
      border-radius: 8px;
      padding: 12px 16px;
    }
    .session-info mat-icon { color: #6366f1; font-size: 18px; }
    code { background: rgba(99,102,241,0.1); padding: 2px 6px; border-radius: 4px; color: #818cf8; font-size: 12px; }
  `]
})
export class ExportComponent implements OnInit {
  result: ContentGenerationResult | null = null;
  sessionId: string | null = null;

  exportOptions: ExportOption[] = [
    {
      id: 'markdown',
      icon: '📝',
      title: 'Complete Content Pack',
      description: 'All scripts, shorts, social posts, thumbnails & content plan in one Markdown file',
      format: 'Markdown (.md)',
      color: '#818cf8',
      downloading: false
    },
    {
      id: 'json',
      icon: '📦',
      title: 'Full JSON Export',
      description: 'Complete structured data for all generated content — perfect for developers',
      format: 'JSON (.json)',
      color: '#10b981',
      downloading: false
    },
    {
      id: 'scripts-only',
      icon: '🎬',
      title: 'YouTube Scripts Only',
      description: 'All video scripts with SEO data, chapters and thumbnail prompts',
      format: 'Markdown (.md)',
      color: '#ef4444',
      downloading: false
    },
    {
      id: 'social-only',
      icon: '📱',
      title: 'Social Media Pack',
      description: 'LinkedIn posts and Twitter threads ready to publish',
      format: 'Markdown (.md)',
      color: '#0ea5e9',
      downloading: false
    },
    {
      id: 'shorts-only',
      icon: '⚡',
      title: 'Shorts Scripts Pack',
      description: 'All shorts scripts with hashtags and visual concepts',
      format: 'Markdown (.md)',
      color: '#f59e0b',
      downloading: false
    },
    {
      id: 'thumbnails-only',
      icon: '🖼️',
      title: 'Thumbnail Prompts Pack',
      description: 'DALL-E 3 and Midjourney prompts for all video thumbnails',
      format: 'Text (.txt)',
      color: '#ec4899',
      downloading: false
    },
  ];

  constructor(
    private state: ContentStateService,
    private api: ApiService,
    private snack: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.state.result$.subscribe(r => this.result = r);
    this.state.session$.subscribe(s => this.sessionId = s?.sessionId ?? null);
  }

  download(opt: ExportOption): void {
    if (!this.sessionId) return;
    opt.downloading = true;

    if (opt.id === 'json') {
      this.api.downloadJson(this.sessionId).subscribe({
        next: blob => { this.saveBlob(blob, `content-factory-${this.result?.topic}.json`); opt.downloading = false; },
        error: () => { opt.downloading = false; this.snack.open('❌ Download failed', 'OK', { duration: 3000 }); }
      });
    } else if (opt.id === 'markdown') {
      this.api.downloadMarkdown(this.sessionId).subscribe({
        next: blob => { this.saveBlob(blob, `content-factory-${this.result?.topic}.md`); opt.downloading = false; },
        error: () => { opt.downloading = false; this.snack.open('❌ Download failed', 'OK', { duration: 3000 }); }
      });
    } else {
      // Client-side export for filtered content
      const content = this.buildLocalExport(opt.id);
      const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
      this.saveBlob(blob, `${opt.id}-${this.result?.topic}-${new Date().toISOString().slice(0, 10)}.md`);
      opt.downloading = false;
      this.snack.open(`✅ ${opt.title} downloaded!`, 'OK', { duration: 2000 });
    }
  }

  private buildLocalExport(type: string): string {
    const r = this.result;
    if (!r) return '';

    if (type === 'scripts-only') {
      return r.youTubeScripts.map(s =>
        `# ${s.title}\n**Duration:** ~${s.estimatedDurationMinutes}min\n\n## Hook\n${s.hook}\n\n## Introduction\n${s.introduction}\n\n${s.mainContent.map(sec => `## ${sec.title}\n${sec.content}`).join('\n\n')}\n\n## CTA\n${s.callToAction}\n\n## Outro\n${s.outro}\n\n### SEO\n- Primary Keyword: ${s.seo?.primaryKeyword}\n- Tags: ${s.seo?.tags?.join(', ')}\n\n---\n`
      ).join('\n');
    }
    if (type === 'social-only') {
      const li = r.linkedInPosts.map(p => `## LinkedIn Post ${p.index}: ${p.title}\n${p.content}\n\n${p.hashtags.join(' ')}\n\n---`).join('\n\n');
      const tw = r.twitterThreads.map(t => `## Thread ${t.index}: ${t.topic}\n${t.tweets.map((tw, i) => `${i + 1}/ ${tw}`).join('\n\n')}\n\n${t.hashtags.join(' ')}\n\n---`).join('\n\n');
      return `# LinkedIn Posts\n\n${li}\n\n# Twitter Threads\n\n${tw}`;
    }
    if (type === 'shorts-only') {
      return r.shortsScripts.map(s => `# Short #${s.index}: ${s.title}\n\n🎣 **HOOK:** ${s.hook}\n\n💡 **MAIN:** ${s.mainPoint}\n\n📢 **CTA:** ${s.callToAction}\n\n🎥 **Visual:** ${s.visualConcept}\n\n${s.hashtags.join(' ')}\n\n---`).join('\n\n');
    }
    if (type === 'thumbnails-only') {
      return r.thumbnailPrompts.map((t, i) => `=== Thumbnail ${i + 1}: ${t.videoTitle} ===\nMain Text: ${t.mainText}\nColor Scheme: ${t.colorScheme}\n\nDALL-E 3:\n${t.dallePrompt}\n\nMidjourney:\n${t.midjourneyPrompt}\n`).join('\n---\n\n');
    }
    return '';
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename; a.click();
    URL.revokeObjectURL(url);
    this.snack.open(`✅ ${filename} downloaded!`, 'OK', { duration: 2000 });
  }

  copyScriptTitles(): void {
    const titles = this.result?.youTubeScripts.map((s, i) => `${i + 1}. ${s.title}`).join('\n') || '';
    navigator.clipboard.writeText(titles);
    this.snack.open('✅ Video titles copied!', '', { duration: 1500 });
  }

  copyAllHashtags(): void {
    const tags = new Set<string>();
    this.result?.shortsScripts.forEach(s => s.hashtags.forEach(t => tags.add(t)));
    this.result?.linkedInPosts.forEach(p => p.hashtags.forEach(t => tags.add(t)));
    navigator.clipboard.writeText([...tags].join(' '));
    this.snack.open('✅ All hashtags copied!', '', { duration: 1500 });
  }

  copyAllKeywords(): void {
    const kws = this.result?.youTubeScripts.map(s => s.seo?.primaryKeyword).filter(Boolean).join('\n') || '';
    navigator.clipboard.writeText(kws);
    this.snack.open('✅ Keywords copied!', '', { duration: 1500 });
  }

  copyAllDallePrompts(): void {
    const prompts = this.result?.thumbnailPrompts.map((t, i) => `${i + 1}. ${t.dallePrompt}`).join('\n\n') || '';
    navigator.clipboard.writeText(prompts);
    this.snack.open('✅ DALL-E prompts copied!', '', { duration: 1500 });
  }

  copyAllTags(): void {
    const tags = new Set<string>();
    this.result?.youTubeScripts.forEach(s => s.seo?.tags?.forEach(t => tags.add(t)));
    navigator.clipboard.writeText([...tags].join(', '));
    this.snack.open('✅ YouTube tags copied!', '', { duration: 1500 });
  }

  copyContentCalendar(): void {
    const cal = this.result?.contentPlan?.publishingSchedule
      .map(i => `Week ${i.weekNumber} | ${i.publishDay} | ${i.platform} | ${i.contentType} | ${i.title}`)
      .join('\n') || '';
    navigator.clipboard.writeText(`Week | Day | Platform | Type | Title\n${cal}`);
    this.snack.open('✅ Calendar copied!', '', { duration: 1500 });
  }
}
