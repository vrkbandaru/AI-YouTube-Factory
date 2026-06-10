import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ContentStateService } from '../../core/services/content-state.service';
import { VideoContentPlan, PublishingScheduleItem, ContentGenerationResult } from '../../core/models/content.models';

@Component({
  selector: 'app-planner',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatButtonModule, MatIconModule,
    MatTabsModule, MatTableModule, MatChipsModule, MatSnackBarModule
  ],
  template: `
    <div class="planner-page">
      <div class="page-header">
        <div>
          <h2>📅 Content Strategy & Plan</h2>
          <p>12-week publishing calendar and channel growth strategy</p>
        </div>
      </div>

      <div class="planner-layout" *ngIf="plan">

        <!-- Strategy Overview -->
        <div class="strategy-banner">
          <div class="strategy-icon">🎯</div>
          <div class="strategy-text">
            <h3>Channel Strategy</h3>
            <p>{{ plan.overallStrategy }}</p>
          </div>
        </div>

        <!-- Content Pillars -->
        <div class="pillars-section">
          <h3>🏛️ Content Pillars</h3>
          <div class="pillars-grid">
            <div *ngFor="let pillar of plan.contentPillars; let i = index" class="pillar-card" [class]="'pillar-' + i">
              <div class="pillar-icon">{{ getPillarIcon(i) }}</div>
              <span class="pillar-name">{{ pillar }}</span>
            </div>
          </div>
        </div>

        <!-- Publishing Calendar -->
        <div class="calendar-section">
          <div class="section-header">
            <h3>📆 12-Week Publishing Calendar</h3>
            <button mat-stroked-button (click)="copyCalendar()" class="copy-btn">
              <mat-icon>content_copy</mat-icon> Copy Calendar
            </button>
          </div>

          <div class="calendar-grid">
            <div *ngFor="let item of plan.publishingSchedule" class="calendar-item" [class]="'platform-' + getPlatformClass(item.platform)">
              <div class="cal-week">W{{ item.weekNumber }}</div>
              <div class="cal-day">{{ item.publishDay }}</div>
              <div class="cal-platform">{{ getPlatformIcon(item.platform) }} {{ item.platform }}</div>
              <div class="cal-type" [class]="'type-' + item.contentType?.toLowerCase()?.replace(' ', '-')">
                {{ item.contentType }}
              </div>
              <div class="cal-title">{{ item.title }}</div>
            </div>
          </div>
        </div>

        <!-- Weekly View by Week -->
        <div class="weeks-section">
          <h3>🗓️ Week-by-Week View</h3>
          <div class="weeks-grid">
            <div *ngFor="let week of getWeeks()" class="week-card">
              <div class="week-label">Week {{ week }}</div>
              <div class="week-items">
                <div *ngFor="let item of getItemsForWeek(week)" class="week-item" [class]="'platform-' + getPlatformClass(item.platform)">
                  <span class="wi-day">{{ item.publishDay?.slice(0,3) }}</span>
                  <span class="wi-icon">{{ getPlatformIcon(item.platform) }}</span>
                  <span class="wi-title">{{ item.title }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Series Ideas -->
        <div class="series-section" *ngIf="plan.seriesIdeas?.length">
          <h3>🎬 Series Ideas</h3>
          <div class="series-grid">
            <div *ngFor="let idea of plan.seriesIdeas; let i = index" class="series-card">
              <div class="series-num">{{ i + 1 }}</div>
              <p>{{ idea }}</p>
            </div>
          </div>
        </div>

        <!-- Growth Strategy -->
        <div class="growth-section">
          <div class="section-header">
            <h3>📈 Channel Growth Strategy</h3>
            <button mat-stroked-button (click)="copyGrowth()" class="copy-btn">
              <mat-icon>content_copy</mat-icon> Copy
            </button>
          </div>
          <div class="growth-text">{{ plan.channelGrowthStrategy }}</div>
        </div>

      </div>

      <div class="empty-state" *ngIf="!plan">
        <div class="empty-icon">📅</div>
        <p>No content plan generated yet.</p>
      </div>
    </div>
  `,
  styles: [`
    .planner-page { padding: 24px 40px; background: #0f0f1a; min-height: calc(100vh - 128px); }

    .page-header { margin-bottom: 24px; }
    .page-header h2 { color: #e2e8f0; font-size: 24px; margin: 0 0 4px; }
    .page-header p { color: #64748b; margin: 0; }

    .planner-layout { display: flex; flex-direction: column; gap: 28px; }

    .strategy-banner {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      background: linear-gradient(135deg, rgba(99,102,241,0.1), rgba(139,92,246,0.1));
      border: 1px solid rgba(99,102,241,0.3);
      border-radius: 16px;
      padding: 20px 24px;
    }
    .strategy-icon { font-size: 36px; flex-shrink: 0; }
    .strategy-text h3 { color: #e2e8f0; font-size: 16px; font-weight: 700; margin: 0 0 8px; }
    .strategy-text p { color: #94a3b8; font-size: 14px; line-height: 1.7; margin: 0; }

    .pillars-section h3, .calendar-section h3, .weeks-section h3, .series-section h3, .growth-section h3 {
      color: #e2e8f0; font-size: 18px; margin: 0 0 16px;
    }

    .pillars-grid { display: flex; gap: 12px; flex-wrap: wrap; }
    .pillar-card {
      display: flex; align-items: center; gap: 10px;
      padding: 14px 20px; border-radius: 12px;
      border: 1px solid; font-weight: 600; font-size: 14px;
    }
    .pillar-0 { background: rgba(99,102,241,0.1); border-color: rgba(99,102,241,0.3); color: #818cf8; }
    .pillar-1 { background: rgba(16,185,129,0.1); border-color: rgba(16,185,129,0.3); color: #34d399; }
    .pillar-2 { background: rgba(245,158,11,0.1); border-color: rgba(245,158,11,0.3); color: #fbbf24; }
    .pillar-3 { background: rgba(236,72,153,0.1); border-color: rgba(236,72,153,0.3); color: #f472b6; }
    .pillar-icon { font-size: 20px; }

    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
    .copy-btn { color: #818cf8 !important; border-color: rgba(99,102,241,0.3) !important; }

    .calendar-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 10px; }
    .calendar-item {
      background: #1a1a2e;
      border-radius: 10px;
      padding: 12px 14px;
      border-left: 3px solid;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .platform-youtube { border-color: #ef4444; }
    .platform-linkedin { border-color: #0ea5e9; }
    .platform-twitter, .platform-x { border-color: #1d9bf0; }
    .platform-shorts { border-color: #f59e0b; }

    .cal-week { color: #64748b; font-size: 11px; font-weight: 700; text-transform: uppercase; }
    .cal-day { color: #818cf8; font-size: 13px; font-weight: 600; }
    .cal-platform { color: #94a3b8; font-size: 11px; }
    .cal-type { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; }
    .cal-type.type-long-video { color: #ef4444; }
    .cal-type.type-shorts { color: #f59e0b; }
    .cal-type.type-linkedin { color: #0ea5e9; }
    .cal-type.type-twitter { color: #1d9bf0; }
    .cal-title { color: #e2e8f0; font-size: 12px; font-weight: 500; line-height: 1.3; }

    .weeks-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 12px; }
    .week-card { background: #1a1a2e; border: 1px solid rgba(99,102,241,0.15); border-radius: 12px; overflow: hidden; }
    .week-label { background: rgba(99,102,241,0.15); color: #818cf8; font-size: 13px; font-weight: 700; padding: 8px 14px; }
    .week-items { padding: 8px; display: flex; flex-direction: column; gap: 4px; }
    .week-item { display: flex; align-items: center; gap: 8px; padding: 6px 8px; border-radius: 6px; }
    .platform-youtube.week-item { background: rgba(239,68,68,0.07); }
    .platform-linkedin.week-item { background: rgba(14,165,233,0.07); }
    .platform-shorts.week-item { background: rgba(245,158,11,0.07); }
    .wi-day { color: #64748b; font-size: 11px; font-weight: 600; width: 28px; flex-shrink: 0; }
    .wi-icon { font-size: 14px; flex-shrink: 0; }
    .wi-title { color: #94a3b8; font-size: 11px; flex: 1; line-height: 1.3; }

    .series-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 12px; }
    .series-card {
      background: #1a1a2e; border: 1px solid rgba(139,92,246,0.2);
      border-radius: 12px; padding: 16px;
      display: flex; align-items: flex-start; gap: 12px;
    }
    .series-num {
      width: 32px; height: 32px; background: rgba(139,92,246,0.2);
      color: #a78bfa; border-radius: 8px;
      display: flex; align-items: center; justify-content: center;
      font-weight: 800; font-size: 14px; flex-shrink: 0;
    }
    .series-card p { color: #cbd5e1; font-size: 14px; line-height: 1.5; margin: 0; }

    .growth-text {
      background: #1a1a2e;
      border: 1px solid rgba(16,185,129,0.2);
      border-radius: 12px;
      padding: 20px 24px;
      color: #94a3b8;
      font-size: 14px;
      line-height: 1.8;
      white-space: pre-wrap;
    }

    .empty-state { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 400px; gap: 12px; color: #475569; }
    .empty-icon { font-size: 48px; }

    .getPlatformIcon { font-size: 14px; }
  `]
})
export class PlannerComponent implements OnInit {
  result: ContentGenerationResult | null = null;
  plan: VideoContentPlan | null = null;

  constructor(private state: ContentStateService, private snack: MatSnackBar) {}

  ngOnInit(): void {
    this.state.result$.subscribe(r => {
      this.result = r;
      this.plan = r?.contentPlan ?? null;
    });
  }

  getWeeks(): number[] {
    const weeks = [...new Set(this.plan?.publishingSchedule.map(i => i.weekNumber) || [])];
    return weeks.sort((a, b) => a - b);
  }

  getItemsForWeek(week: number): PublishingScheduleItem[] {
    return this.plan?.publishingSchedule.filter(i => i.weekNumber === week) || [];
  }

  getPlatformClass(platform: string): string {
    return platform?.toLowerCase().replace(/[^a-z]/g, '') || 'other';
  }

  getPlatformIcon(platform: string): string {
    const icons: Record<string, string> = {
      'YouTube': '🎬', 'YouTube Shorts': '⚡', 'LinkedIn': '💼', 'Twitter': '🐦', 'X': '🐦', 'Instagram': '📸'
    };
    return icons[platform] || '📱';
  }

  getPillarIcon(i: number): string {
    return ['🎓', '💡', '🚀', '🔥', '⭐', '🎯'][i] || '📌';
  }

  copyCalendar(): void {
    const rows = this.plan?.publishingSchedule.map(i =>
      `Week ${i.weekNumber} | ${i.publishDay} | ${i.platform} | ${i.contentType} | ${i.title}`
    ).join('\n');
    navigator.clipboard.writeText(`Week | Day | Platform | Type | Title\n${rows}`);
    this.snack.open('✅ Calendar copied!', '', { duration: 1500 });
  }

  copyGrowth(): void {
    navigator.clipboard.writeText(this.plan?.channelGrowthStrategy || '');
    this.snack.open('✅ Growth strategy copied!', '', { duration: 1500 });
  }
}
