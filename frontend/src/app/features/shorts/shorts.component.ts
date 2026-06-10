import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatGridListModule } from '@angular/material/grid-list';
import { ContentStateService } from '../../core/services/content-state.service';
import { ShortsScript, ContentGenerationResult } from '../../core/models/content.models';

@Component({
  selector: 'app-shorts',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatTooltipModule, MatSnackBarModule, MatGridListModule
  ],
  template: `
    <div class="shorts-page">
      <div class="page-header">
        <div>
          <h2>⚡ YouTube Shorts Scripts</h2>
          <p>{{ result?.shortsScripts?.length }} viral short-form scripts — each under 60 seconds</p>
        </div>
        <div class="header-actions">
          <button mat-stroked-button (click)="copyAll()" class="action-btn">
            <mat-icon>content_copy</mat-icon> Copy All
          </button>
        </div>
      </div>

      <div class="shorts-grid">
        <div *ngFor="let short of result?.shortsScripts" class="short-card" [class.selected]="selected?.index === short.index" (click)="select(short)">
          <!-- Phone mockup header -->
          <div class="phone-header">
            <div class="phone-dot"></div>
            <span class="short-num">#{{ short.index }}</span>
            <span class="duration-badge">{{ short.durationSeconds }}s</span>
          </div>

          <div class="short-title">{{ short.title }}</div>

          <!-- Script sections -->
          <div class="script-flow">
            <div class="flow-item hook">
              <span class="flow-label">🎣 HOOK</span>
              <p>{{ short.hook }}</p>
            </div>
            <div class="flow-arrow">↓</div>
            <div class="flow-item main">
              <span class="flow-label">💡 MAIN POINT</span>
              <p>{{ short.mainPoint }}</p>
            </div>
            <div class="flow-arrow">↓</div>
            <div class="flow-item cta">
              <span class="flow-label">📢 CTA</span>
              <p>{{ short.callToAction }}</p>
            </div>
          </div>

          <!-- Visual concept -->
          <div class="visual-concept" *ngIf="short.visualConcept">
            <mat-icon>videocam</mat-icon>
            <span>{{ short.visualConcept }}</span>
          </div>

          <!-- Hashtags -->
          <div class="hashtags">
            <span *ngFor="let tag of short.hashtags" class="hashtag">{{ tag }}</span>
          </div>

          <!-- Actions -->
          <div class="card-actions">
            <button mat-icon-button (click)="copyShort(short, $event)" matTooltip="Copy script">
              <mat-icon>content_copy</mat-icon>
            </button>
            <button mat-icon-button (click)="copyHashtags(short, $event)" matTooltip="Copy hashtags">
              <mat-icon>tag</mat-icon>
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .shorts-page { padding: 24px 40px; background: #0f0f1a; min-height: calc(100vh - 128px); }

    .page-header { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 24px; }
    .page-header h2 { color: #e2e8f0; font-size: 24px; margin: 0 0 4px; }
    .page-header p { color: #64748b; margin: 0; }
    .action-btn { color: #818cf8 !important; border-color: rgba(99,102,241,0.4) !important; }

    .shorts-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 20px;
    }

    .short-card {
      background: #1a1a2e;
      border: 1px solid rgba(99,102,241,0.2);
      border-radius: 16px;
      padding: 16px;
      cursor: pointer;
      transition: all 0.2s;
      display: flex;
      flex-direction: column;
      gap: 12px;
      position: relative;
    }
    .short-card:hover { border-color: #6366f1; transform: translateY(-2px); box-shadow: 0 8px 24px rgba(99,102,241,0.15); }
    .short-card.selected { border-color: #818cf8; background: rgba(99,102,241,0.06); }

    .phone-header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding-bottom: 10px;
      border-bottom: 1px solid rgba(99,102,241,0.15);
    }
    .phone-dot { width: 8px; height: 8px; background: #ef4444; border-radius: 50%; }
    .short-num { color: #64748b; font-size: 12px; font-weight: 600; font-family: monospace; }
    .duration-badge { margin-left: auto; background: rgba(245,158,11,0.15); color: #fbbf24; padding: 2px 8px; border-radius: 8px; font-size: 11px; font-weight: 700; }

    .short-title { color: #e2e8f0; font-size: 14px; font-weight: 700; line-height: 1.4; }

    .script-flow { display: flex; flex-direction: column; gap: 4px; }
    .flow-item { background: rgba(0,0,0,0.25); border-radius: 8px; padding: 8px 10px; border-left: 3px solid; }
    .flow-item.hook { border-color: #f59e0b; }
    .flow-item.main { border-color: #6366f1; }
    .flow-item.cta { border-color: #10b981; }
    .flow-label { font-size: 10px; font-weight: 700; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px; display: block; margin-bottom: 4px; }
    .flow-item p { color: #cbd5e1; font-size: 12px; line-height: 1.5; margin: 0; }
    .flow-arrow { text-align: center; color: #475569; font-size: 16px; line-height: 1; }

    .visual-concept {
      display: flex;
      align-items: flex-start;
      gap: 6px;
      background: rgba(6,182,212,0.08);
      border: 1px solid rgba(6,182,212,0.2);
      border-radius: 8px;
      padding: 8px 10px;
    }
    .visual-concept mat-icon { color: #06b6d4; font-size: 16px; width: 16px; height: 16px; flex-shrink: 0; margin-top: 1px; }
    .visual-concept span { color: #94a3b8; font-size: 12px; line-height: 1.4; }

    .hashtags { display: flex; flex-wrap: wrap; gap: 4px; }
    .hashtag { background: rgba(99,102,241,0.1); color: #818cf8; padding: 2px 8px; border-radius: 6px; font-size: 11px; font-weight: 500; }

    .card-actions { display: flex; gap: 4px; justify-content: flex-end; }
    ::ng-deep .card-actions button { color: #64748b !important; }
    ::ng-deep .card-actions button:hover { color: #818cf8 !important; }
  `]
})
export class ShortsComponent implements OnInit {
  result: ContentGenerationResult | null = null;
  selected: ShortsScript | null = null;

  constructor(private state: ContentStateService, private snack: MatSnackBar) {}

  ngOnInit(): void {
    this.state.result$.subscribe(r => { this.result = r; });
  }

  select(short: ShortsScript): void { this.selected = short; }

  copyShort(short: ShortsScript, e: Event): void {
    e.stopPropagation();
    const text = `🎬 ${short.title}\n\n🎣 HOOK (0-3s):\n${short.hook}\n\n💡 MAIN (4-50s):\n${short.mainPoint}\n\n📢 CTA (last 5s):\n${short.callToAction}\n\n🎥 Visual: ${short.visualConcept}\n\n${short.hashtags.join(' ')}`;
    navigator.clipboard.writeText(text);
    this.snack.open('✅ Short script copied!', '', { duration: 1500 });
  }

  copyHashtags(short: ShortsScript, e: Event): void {
    e.stopPropagation();
    navigator.clipboard.writeText(short.hashtags.join(' '));
    this.snack.open('✅ Hashtags copied!', '', { duration: 1500 });
  }

  copyAll(): void {
    const all = this.result?.shortsScripts.map(s =>
      `--- SHORT #${s.index}: ${s.title} ---\nHOOK: ${s.hook}\nMAIN: ${s.mainPoint}\nCTA: ${s.callToAction}\nHASHTAGS: ${s.hashtags.join(' ')}`
    ).join('\n\n');
    navigator.clipboard.writeText(all || '');
    this.snack.open(`✅ All ${this.result?.shortsScripts.length} shorts copied!`, 'OK', { duration: 2000 });
  }
}
