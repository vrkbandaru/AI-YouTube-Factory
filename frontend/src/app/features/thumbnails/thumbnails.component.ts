import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { ContentStateService } from '../../core/services/content-state.service';
import { ThumbnailPrompt, ContentGenerationResult } from '../../core/models/content.models';

@Component({
  selector: 'app-thumbnails',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatButtonModule, MatIconModule,
    MatTooltipModule, MatSnackBarModule, MatTabsModule, MatChipsModule
  ],
  template: `
    <div class="thumbnails-page">
      <div class="page-header">
        <div>
          <h2>🖼️ Thumbnail Prompts</h2>
          <p>{{ result?.thumbnailPrompts?.length }} AI-ready prompts for DALL-E 3 & Midjourney</p>
        </div>
        <div class="header-actions">
          <button mat-stroked-button (click)="copyAllDalle()" class="action-btn dalle-btn">
            🎨 Copy All DALL-E Prompts
          </button>
          <button mat-stroked-button (click)="copyAllMj()" class="action-btn mj-btn">
            ✨ Copy All MJ Prompts
          </button>
        </div>
      </div>

      <div class="thumbnails-layout">
        <!-- Thumbnail Grid -->
        <div class="thumb-grid">
          <div *ngFor="let thumb of result?.thumbnailPrompts; let i = index"
               class="thumb-card"
               [class.selected]="selected?.videoTitle === thumb.videoTitle"
               (click)="select(thumb)">

            <!-- Thumbnail Preview Mockup -->
            <div class="thumb-preview" [style.background]="getGradient(i)">
              <div class="thumb-channel-logo">▶</div>
              <div class="thumb-main-text">{{ thumb.mainText }}</div>
              <div class="thumb-watermark">16:9</div>
              <div class="thumb-expression" *ngIf="thumb.faceExpression && thumb.faceExpression !== 'none'">
                {{ getFaceEmoji(thumb.faceExpression) }}
              </div>
            </div>

            <div class="thumb-meta">
              <p class="thumb-title">{{ thumb.videoTitle }}</p>
              <div class="thumb-tags">
                <span class="color-tag">🎨 {{ thumb.colorScheme }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Prompt Detail -->
        <div class="prompt-detail" *ngIf="selected">
          <div class="detail-title">
            <h3>{{ selected.videoTitle }}</h3>
          </div>

          <div class="big-preview" [style.background]="getGradient(selectedIndex)">
            <div class="big-channel-logo">▶</div>
            <div class="big-main-text">{{ selected.mainText }}</div>
            <div class="big-expression" *ngIf="selected.faceExpression && selected.faceExpression !== 'none'">
              {{ getFaceEmoji(selected.faceExpression) }}
            </div>
          </div>

          <div class="prompt-sections">

            <div class="prompt-block">
              <div class="prompt-header">
                <span class="prompt-platform dalle">🎨 DALL-E 3 Prompt</span>
                <button mat-icon-button (click)="copy(selected.dallePrompt, 'DALL-E')" matTooltip="Copy">
                  <mat-icon>content_copy</mat-icon>
                </button>
              </div>
              <div class="prompt-text" (click)="copy(selected.dallePrompt, 'DALL-E')">{{ selected.dallePrompt }}</div>
            </div>

            <div class="prompt-block">
              <div class="prompt-header">
                <span class="prompt-platform mj">✨ Midjourney Prompt</span>
                <button mat-icon-button (click)="copy(selected.midjourneyPrompt, 'Midjourney')" matTooltip="Copy">
                  <mat-icon>content_copy</mat-icon>
                </button>
              </div>
              <div class="prompt-text" (click)="copy(selected.midjourneyPrompt, 'Midjourney')">{{ selected.midjourneyPrompt }}</div>
            </div>

            <div class="meta-blocks">
              <div class="meta-block">
                <span class="meta-label">Main Text Overlay</span>
                <span class="meta-value big-text">{{ selected.mainText }}</span>
              </div>
              <div class="meta-block">
                <span class="meta-label">Color Scheme</span>
                <span class="meta-value">{{ selected.colorScheme }}</span>
              </div>
              <div class="meta-block">
                <span class="meta-label">Face Expression</span>
                <span class="meta-value">{{ selected.faceExpression }} {{ getFaceEmoji(selected.faceExpression) }}</span>
              </div>
              <div class="meta-block">
                <span class="meta-label">Background Concept</span>
                <span class="meta-value">{{ selected.backgroundDescription }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .thumbnails-page { padding: 24px 40px; background: #0f0f1a; min-height: calc(100vh - 128px); }

    .page-header { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 24px; flex-wrap: wrap; gap: 12px; }
    .page-header h2 { color: #e2e8f0; font-size: 24px; margin: 0 0 4px; }
    .page-header p { color: #64748b; margin: 0; }
    .header-actions { display: flex; gap: 10px; flex-wrap: wrap; }
    .action-btn { font-size: 13px !important; }
    .dalle-btn { color: #f59e0b !important; border-color: rgba(245,158,11,0.4) !important; }
    .mj-btn { color: #8b5cf6 !important; border-color: rgba(139,92,246,0.4) !important; }

    .thumbnails-layout { display: grid; grid-template-columns: 340px 1fr; gap: 24px; }

    .thumb-grid { display: flex; flex-direction: column; gap: 10px; overflow-y: auto; max-height: calc(100vh - 220px); padding-right: 4px; }

    .thumb-card { background: #1a1a2e; border: 1px solid rgba(99,102,241,0.2); border-radius: 12px; overflow: hidden; cursor: pointer; transition: all 0.2s; }
    .thumb-card:hover, .thumb-card.selected { border-color: #6366f1; }

    .thumb-preview {
      width: 100%;
      aspect-ratio: 16/9;
      display: flex;
      align-items: center;
      justify-content: center;
      position: relative;
      overflow: hidden;
    }
    .thumb-channel-logo {
      position: absolute;
      top: 8px; left: 8px;
      background: rgba(0,0,0,0.5);
      color: white;
      padding: 2px 6px;
      border-radius: 4px;
      font-size: 10px;
    }
    .thumb-main-text {
      color: white;
      font-size: 15px;
      font-weight: 900;
      text-transform: uppercase;
      text-shadow: 2px 2px 8px rgba(0,0,0,0.8);
      padding: 8px;
      text-align: center;
      letter-spacing: 1px;
      line-height: 1.2;
      z-index: 1;
    }
    .thumb-watermark {
      position: absolute;
      bottom: 4px; right: 6px;
      color: rgba(255,255,255,0.3);
      font-size: 9px;
    }
    .thumb-expression {
      position: absolute;
      right: 8px; top: 50%;
      transform: translateY(-50%);
      font-size: 28px;
    }
    .thumb-meta { padding: 10px 12px; }
    .thumb-title { color: #e2e8f0; font-size: 12px; font-weight: 600; margin: 0 0 4px; line-height: 1.4; }
    .color-tag { color: #94a3b8; font-size: 11px; }

    /* Detail Panel */
    .prompt-detail { overflow-y: auto; max-height: calc(100vh - 220px); }
    .detail-title { margin-bottom: 16px; }
    .detail-title h3 { color: #e2e8f0; font-size: 18px; margin: 0; }

    .big-preview {
      width: 100%;
      aspect-ratio: 16/9;
      display: flex;
      align-items: center;
      justify-content: center;
      position: relative;
      border-radius: 12px;
      overflow: hidden;
      margin-bottom: 20px;
    }
    .big-channel-logo { position: absolute; top: 12px; left: 12px; background: rgba(0,0,0,0.6); color: white; padding: 4px 10px; border-radius: 6px; font-size: 14px; }
    .big-main-text { color: white; font-size: 32px; font-weight: 900; text-transform: uppercase; text-shadow: 3px 3px 12px rgba(0,0,0,0.9); padding: 16px; text-align: center; letter-spacing: 2px; line-height: 1.2; }
    .big-expression { position: absolute; right: 16px; top: 50%; transform: translateY(-50%); font-size: 60px; }

    .prompt-sections { display: flex; flex-direction: column; gap: 16px; }

    .prompt-block { background: #1a1a2e; border: 1px solid rgba(99,102,241,0.2); border-radius: 12px; overflow: hidden; }
    .prompt-header { display: flex; align-items: center; justify-content: space-between; padding: 10px 14px; border-bottom: 1px solid rgba(99,102,241,0.15); }
    .prompt-platform { font-size: 13px; font-weight: 700; }
    .dalle { color: #f59e0b; }
    .mj { color: #8b5cf6; }
    ::ng-deep .prompt-header button { color: #64748b !important; }
    .prompt-text { padding: 14px; color: #94a3b8; font-size: 13px; line-height: 1.6; cursor: pointer; transition: background 0.15s; }
    .prompt-text:hover { background: rgba(99,102,241,0.05); color: #cbd5e1; }

    .meta-blocks { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
    .meta-block { background: #1a1a2e; border: 1px solid rgba(99,102,241,0.15); border-radius: 10px; padding: 12px 14px; display: flex; flex-direction: column; gap: 4px; }
    .meta-label { color: #64748b; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; }
    .meta-value { color: #e2e8f0; font-size: 13px; }
    .meta-value.big-text { font-size: 18px; font-weight: 900; color: #818cf8; text-transform: uppercase; }
  `]
})
export class ThumbnailsComponent implements OnInit {
  result: ContentGenerationResult | null = null;
  selected: ThumbnailPrompt | null = null;
  selectedIndex = 0;

  private gradients = [
    'linear-gradient(135deg, #1e3a8a, #7c3aed)',
    'linear-gradient(135deg, #7f1d1d, #c2410c)',
    'linear-gradient(135deg, #064e3b, #0369a1)',
    'linear-gradient(135deg, #581c87, #be185d)',
    'linear-gradient(135deg, #1c1917, #292524)',
    'linear-gradient(135deg, #0f172a, #1e3a8a)',
    'linear-gradient(135deg, #14532d, #166534)',
    'linear-gradient(135deg, #7c2d12, #9a3412)',
  ];

  constructor(private state: ContentStateService, private snack: MatSnackBar) {}

  ngOnInit(): void {
    this.state.result$.subscribe(r => {
      this.result = r;
      if (r?.thumbnailPrompts?.length) { this.selected = r.thumbnailPrompts[0]; this.selectedIndex = 0; }
    });
  }

  select(thumb: ThumbnailPrompt): void {
    this.selected = thumb;
    this.selectedIndex = this.result?.thumbnailPrompts.indexOf(thumb) ?? 0;
  }

  getGradient(i: number): string { return this.gradients[i % this.gradients.length]; }

  getFaceEmoji(expr: string): string {
    const map: Record<string, string> = { shocked: '😱', excited: '🤩', curious: '🤔', serious: '😤', happy: '😄', none: '' };
    return map[expr?.toLowerCase()] ?? '😊';
  }

  copy(text: string, platform: string): void {
    navigator.clipboard.writeText(text);
    this.snack.open(`✅ ${platform} prompt copied!`, '', { duration: 1500 });
  }

  copyAllDalle(): void {
    const all = this.result?.thumbnailPrompts.map((t, i) => `Video ${i + 1}: ${t.videoTitle}\n${t.dallePrompt}`).join('\n\n---\n\n');
    navigator.clipboard.writeText(all || '');
    this.snack.open('✅ All DALL-E prompts copied!', 'OK', { duration: 2000 });
  }

  copyAllMj(): void {
    const all = this.result?.thumbnailPrompts.map((t, i) => `${t.midjourneyPrompt}`).join('\n');
    navigator.clipboard.writeText(all || '');
    this.snack.open('✅ All Midjourney prompts copied!', 'OK', { duration: 2000 });
  }
}
