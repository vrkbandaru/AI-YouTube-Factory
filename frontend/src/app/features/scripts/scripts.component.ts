import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTabsModule } from '@angular/material/tabs';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ContentStateService } from '../../core/services/content-state.service';
import { YouTubeVideoScript, ContentGenerationResult } from '../../core/models/content.models';

@Component({
  selector: 'app-scripts',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatExpansionModule, MatTabsModule, MatBadgeModule, MatTooltipModule, MatSnackBarModule
  ],
  template: `
    <div class="scripts-page">
      <div class="page-header">
        <div>
          <h2>📹 YouTube Video Scripts</h2>
          <p>{{ result?.youTubeScripts?.length }} complete scripts ready to record</p>
        </div>
        <button mat-flat-button (click)="copyAll()" class="copy-all-btn">
          <mat-icon>content_copy</mat-icon> Copy All Scripts
        </button>
      </div>

      <div class="scripts-layout">
        <!-- Script List -->
        <div class="script-list">
          <div *ngFor="let script of result?.youTubeScripts"
               class="script-item"
               [class.selected]="selectedScript?.index === script.index"
               (click)="selectScript(script)">
            <div class="script-num">{{ script.index }}</div>
            <div class="script-meta">
              <p class="script-title">{{ script.title }}</p>
              <div class="script-tags">
                <span class="tag duration">🕐 {{ script.estimatedDurationMinutes }}min</span>
                <span class="tag keyword" *ngIf="script.seo?.primaryKeyword">🔑 {{ script.seo.primaryKeyword }}</span>
              </div>
            </div>
            <mat-icon class="chevron">chevron_right</mat-icon>
          </div>
        </div>

        <!-- Script Detail -->
        <div class="script-detail" *ngIf="selectedScript">
          <div class="detail-header">
            <div>
              <h3>{{ selectedScript.title }}</h3>
              <span class="detail-meta">~{{ selectedScript.estimatedDurationMinutes }} minutes • {{ selectedScript.mainContent?.length }} sections</span>
            </div>
            <button mat-icon-button (click)="copyScript(selectedScript)" matTooltip="Copy full script">
              <mat-icon>content_copy</mat-icon>
            </button>
          </div>

          <mat-tab-group class="detail-tabs">

            <!-- Full Script -->
            <mat-tab label="📜 Full Script">
              <div class="script-content">
                <div class="script-section hook">
                  <div class="section-label">🎣 HOOK (0:00 – 0:30)</div>
                  <p>{{ selectedScript.hook }}</p>
                </div>
                <div class="script-section intro">
                  <div class="section-label">📖 INTRODUCTION</div>
                  <p>{{ selectedScript.introduction }}</p>
                </div>
                <div *ngFor="let section of selectedScript.mainContent; let i = index" class="script-section main">
                  <div class="section-label">🔹 {{ section.title?.toUpperCase() }} (~{{ section.durationSeconds }}s)</div>
                  <p>{{ section.content }}</p>
                  <div class="visual-note" *ngIf="section.visualNote">
                    🎥 <em>{{ section.visualNote }}</em>
                  </div>
                </div>
                <div class="script-section cta">
                  <div class="section-label">📢 CALL TO ACTION</div>
                  <p>{{ selectedScript.callToAction }}</p>
                </div>
                <div class="script-section outro">
                  <div class="section-label">👋 OUTRO</div>
                  <p>{{ selectedScript.outro }}</p>
                </div>
              </div>
            </mat-tab>

            <!-- Description & SEO -->
            <mat-tab label="🔍 SEO">
              <div class="seo-content" *ngIf="selectedScript.seo">
                <div class="seo-block">
                  <h4>Optimized Title</h4>
                  <div class="seo-value copyable" (click)="copyText(selectedScript.seo.optimizedTitle)">
                    {{ selectedScript.seo.optimizedTitle || selectedScript.title }}
                  </div>
                </div>
                <div class="seo-block">
                  <h4>Description</h4>
                  <div class="seo-value copyable description-box" (click)="copyText(selectedScript.seo.optimizedDescription)">
                    {{ selectedScript.seo.optimizedDescription || selectedScript.description }}
                  </div>
                </div>
                <div class="seo-block">
                  <h4>Primary Keyword</h4>
                  <div class="keyword-chip primary">{{ selectedScript.seo.primaryKeyword }}</div>
                </div>
                <div class="seo-block">
                  <h4>Secondary Keywords</h4>
                  <div class="chips-row">
                    <span *ngFor="let kw of selectedScript.seo.secondaryKeywords" class="keyword-chip">{{ kw }}</span>
                  </div>
                </div>
                <div class="seo-block">
                  <h4>Tags ({{ selectedScript.seo.tags?.length }})</h4>
                  <div class="chips-row">
                    <span *ngFor="let tag of selectedScript.seo.tags" class="tag-chip">{{ tag }}</span>
                  </div>
                </div>
                <div class="seo-block" *ngIf="selectedScript.seo.chapters?.length">
                  <h4>Chapter Timestamps</h4>
                  <div class="chapters-list">
                    <div *ngFor="let ch of selectedScript.seo.chapters" class="chapter-item">{{ ch }}</div>
                  </div>
                </div>
              </div>
            </mat-tab>

            <!-- Thumbnail -->
            <mat-tab label="🖼️ Thumbnail">
              <div class="thumbnail-content" *ngIf="selectedScript.thumbnailPrompt || getThumbnail(selectedScript) as thumb">
                <div class="thumb-block">
                  <h4>Main Text Overlay</h4>
                  <div class="thumb-text">{{ (selectedScript.thumbnailPrompt || thumb)?.mainText }}</div>
                </div>
                <div class="thumb-block">
                  <h4>Color Scheme</h4>
                  <div class="seo-value">{{ (selectedScript.thumbnailPrompt || thumb)?.colorScheme }}</div>
                </div>
                <div class="thumb-block">
                  <h4>DALL-E 3 Prompt</h4>
                  <div class="seo-value copyable prompt-box" (click)="copyText((selectedScript.thumbnailPrompt || thumb)?.dallePrompt || '')">
                    {{ (selectedScript.thumbnailPrompt || thumb)?.dallePrompt }}
                  </div>
                </div>
                <div class="thumb-block">
                  <h4>Midjourney Prompt</h4>
                  <div class="seo-value copyable prompt-box" (click)="copyText((selectedScript.thumbnailPrompt || thumb)?.midjourneyPrompt || '')">
                    {{ (selectedScript.thumbnailPrompt || thumb)?.midjourneyPrompt }}
                  </div>
                </div>
              </div>
            </mat-tab>

          </mat-tab-group>
        </div>

        <!-- No selection placeholder -->
        <div class="no-selection" *ngIf="!selectedScript">
          <div class="no-sel-icon">👈</div>
          <p>Select a script to view details</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .scripts-page { padding: 24px 40px; background: #0f0f1a; min-height: calc(100vh - 128px); }

    .page-header { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 24px; }
    .page-header h2 { color: #e2e8f0; font-size: 24px; margin: 0 0 4px; }
    .page-header p { color: #64748b; margin: 0; }
    .copy-all-btn { background: rgba(99,102,241,0.15) !important; color: #818cf8 !important; border: 1px solid rgba(99,102,241,0.3) !important; }

    .scripts-layout { display: grid; grid-template-columns: 360px 1fr; gap: 20px; }

    .script-list { display: flex; flex-direction: column; gap: 8px; max-height: calc(100vh - 220px); overflow-y: auto; }
    .script-item {
      background: #1a1a2e; border: 1px solid rgba(99,102,241,0.15);
      border-radius: 12px; padding: 14px 16px;
      display: flex; align-items: center; gap: 12px;
      cursor: pointer; transition: all 0.15s;
    }
    .script-item:hover, .script-item.selected {
      border-color: #6366f1; background: rgba(99,102,241,0.08);
    }
    .script-num {
      width: 28px; height: 28px; border-radius: 8px;
      background: rgba(99,102,241,0.2); color: #818cf8;
      display: flex; align-items: center; justify-content: center;
      font-size: 13px; font-weight: 700; flex-shrink: 0;
    }
    .script-meta { flex: 1; min-width: 0; }
    .script-title { color: #e2e8f0; font-size: 13px; font-weight: 600; margin: 0 0 4px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .script-tags { display: flex; gap: 6px; flex-wrap: wrap; }
    .tag { font-size: 11px; padding: 2px 8px; border-radius: 6px; }
    .tag.duration { background: rgba(99,102,241,0.1); color: #818cf8; }
    .tag.keyword { background: rgba(16,185,129,0.1); color: #34d399; }
    .chevron { color: #475569; flex-shrink: 0; }

    .script-detail { background: #1a1a2e; border: 1px solid rgba(99,102,241,0.2); border-radius: 16px; overflow: hidden; max-height: calc(100vh - 220px); overflow-y: auto; }
    .detail-header { padding: 20px 24px; border-bottom: 1px solid rgba(99,102,241,0.15); display: flex; justify-content: space-between; align-items: flex-start; }
    .detail-header h3 { color: #e2e8f0; font-size: 18px; margin: 0 0 4px; }
    .detail-meta { color: #64748b; font-size: 12px; }
    ::ng-deep .detail-header button { color: #818cf8 !important; }

    ::ng-deep .detail-tabs .mat-mdc-tab-header { background: #12122a; }
    ::ng-deep .detail-tabs .mat-mdc-tab { color: #94a3b8; }
    ::ng-deep .detail-tabs .mat-mdc-tab.mdc-tab--active { color: #818cf8; }
    ::ng-deep .detail-tabs .mdc-tab-indicator__content--underline { border-color: #818cf8; }

    .script-content { padding: 20px 24px; display: flex; flex-direction: column; gap: 20px; }
    .script-section { background: rgba(0,0,0,0.2); border-radius: 10px; padding: 16px; border-left: 3px solid; }
    .script-section.hook { border-color: #f59e0b; }
    .script-section.intro { border-color: #6366f1; }
    .script-section.main { border-color: #8b5cf6; }
    .script-section.cta { border-color: #10b981; }
    .script-section.outro { border-color: #64748b; }
    .section-label { font-size: 11px; font-weight: 700; color: #64748b; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 8px; }
    .script-section p { color: #cbd5e1; font-size: 14px; line-height: 1.7; margin: 0; white-space: pre-wrap; }
    .visual-note { margin-top: 10px; color: #64748b; font-size: 12px; font-style: italic; }

    .seo-content, .thumbnail-content { padding: 20px 24px; display: flex; flex-direction: column; gap: 16px; }
    .seo-block h4 { color: #94a3b8; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; margin: 0 0 8px; }
    .seo-value { color: #e2e8f0; background: rgba(0,0,0,0.3); border-radius: 8px; padding: 10px 14px; font-size: 14px; line-height: 1.6; }
    .seo-value.copyable { cursor: pointer; transition: background 0.15s; }
    .seo-value.copyable:hover { background: rgba(99,102,241,0.1); }
    .description-box, .prompt-box { white-space: pre-wrap; font-size: 13px; }
    .chips-row { display: flex; flex-wrap: wrap; gap: 6px; }
    .keyword-chip { background: rgba(99,102,241,0.15); color: #818cf8; padding: 4px 10px; border-radius: 8px; font-size: 12px; }
    .keyword-chip.primary { background: rgba(99,102,241,0.3); font-weight: 700; }
    .tag-chip { background: rgba(16,185,129,0.1); color: #34d399; padding: 3px 8px; border-radius: 6px; font-size: 11px; }
    .chapters-list { display: flex; flex-direction: column; gap: 4px; }
    .chapter-item { color: #94a3b8; font-size: 13px; font-family: monospace; background: rgba(0,0,0,0.2); padding: 4px 10px; border-radius: 4px; }
    .thumb-block h4 { color: #94a3b8; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; margin: 0 0 8px; }
    .thumb-text { font-size: 28px; font-weight: 900; color: #e2e8f0; text-transform: uppercase; letter-spacing: 2px; background: rgba(0,0,0,0.4); padding: 16px; border-radius: 8px; }

    .no-selection { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 400px; gap: 12px; color: #475569; }
    .no-sel-icon { font-size: 48px; }
  `]
})
export class ScriptsComponent implements OnInit {
  result: ContentGenerationResult | null = null;
  selectedScript: YouTubeVideoScript | null = null;

  constructor(private state: ContentStateService, private snack: MatSnackBar) {}

  ngOnInit(): void {
    this.state.result$.subscribe(r => {
      this.result = r;
      if (r?.youTubeScripts?.length) this.selectedScript = r.youTubeScripts[0];
    });
  }

  selectScript(script: YouTubeVideoScript): void { this.selectedScript = script; }

  getThumbnail(script: YouTubeVideoScript) {
    return this.result?.thumbnailPrompts?.find(t => t.videoTitle === script.title);
  }

  copyText(text: string): void {
    navigator.clipboard.writeText(text);
    this.snack.open('✅ Copied!', '', { duration: 1500 });
  }

  copyScript(script: YouTubeVideoScript): void {
    const text = `# ${script.title}\n\n## Hook\n${script.hook}\n\n## Introduction\n${script.introduction}\n\n${script.mainContent.map(s => `## ${s.title}\n${s.content}`).join('\n\n')}\n\n## Call to Action\n${script.callToAction}\n\n## Outro\n${script.outro}`;
    this.copyText(text);
  }

  copyAll(): void {
    const all = this.result?.youTubeScripts.map(s =>
      `# Video ${s.index}: ${s.title}\n\n${s.hook}\n\n${s.introduction}\n\n${s.mainContent.map(sec => `## ${sec.title}\n${sec.content}`).join('\n\n')}\n\n${s.callToAction}\n\n---`
    ).join('\n\n');
    this.copyText(all || '');
    this.snack.open(`✅ All ${this.result?.youTubeScripts.length} scripts copied!`, 'OK', { duration: 2000 });
  }
}
