import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { ContentStateService } from './core/services/content-state.service';
import { UploadComponent } from './features/upload/upload.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { ScriptsComponent } from './features/scripts/scripts.component';
import { ShortsComponent } from './features/shorts/shorts.component';
import { SocialComponent } from './features/social/social.component';
import { ThumbnailsComponent } from './features/thumbnails/thumbnails.component';
import { PlannerComponent } from './features/planner/planner.component';
import { VideoComponent } from './features/video/video.component';
import { ExportComponent } from './features/export/export.component';
import { ContentGenerationResult } from './core/models/content.models';

interface NavTab {
  id: string;
  label: string;
  icon: string;
  requiresResult: boolean;
  badge?: (r: ContentGenerationResult) => string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule, RouterOutlet, MatTabsModule, MatToolbarModule,
    MatIconModule, MatBadgeModule,
    UploadComponent, DashboardComponent, ScriptsComponent, ShortsComponent,
    SocialComponent, ThumbnailsComponent, PlannerComponent,
    VideoComponent, ExportComponent
  ],
  template: `
    <div class="app-shell">
      <!-- Header -->
      <mat-toolbar class="app-toolbar">
        <div class="toolbar-brand">
          <span class="brand-icon">🎬</span>
          <div class="brand-text">
            <span class="brand-title">AI YouTube Content Factory</span>
            <span class="brand-sub">Powered by Azure OpenAI + Semantic Kernel</span>
          </div>
        </div>
        <div class="toolbar-status" *ngIf="session$ | async as session">
          <span class="status-chip" [class]="'status-' + session.status">
            <span class="status-dot"></span>
            {{ session.status === 'running' ? 'Generating...' : session.status }}
          </span>
          <span class="topic-label">{{ session.topic }}</span>
        </div>
      </mat-toolbar>

      <!-- Tab Navigation -->
      <mat-tab-group
        [(selectedIndex)]="selectedTabIndex"
        (selectedIndexChange)="onTabChange($event)"
        class="main-tabs"
        animationDuration="200ms">

        <mat-tab *ngFor="let tab of tabs; let i = index"
                 [disabled]="tab.requiresResult && !result">
          <ng-template mat-tab-label>
            <mat-icon class="tab-icon">{{ tab.icon }}</mat-icon>
            <span>{{ tab.label }}</span>
            <span *ngIf="result && tab.badge && tab.badge(result)"
                  class="tab-badge">{{ tab.badge(result) }}</span>
          </ng-template>
          <div class="tab-content">
            <app-upload     *ngIf="tab.id === 'upload'"></app-upload>
            <app-dashboard  *ngIf="tab.id === 'dashboard'"></app-dashboard>
            <app-scripts    *ngIf="tab.id === 'scripts'    && result"></app-scripts>
            <app-shorts     *ngIf="tab.id === 'shorts'     && result"></app-shorts>
            <app-social     *ngIf="tab.id === 'social'     && result"></app-social>
            <app-thumbnails *ngIf="tab.id === 'thumbnails' && result"></app-thumbnails>
            <app-planner    *ngIf="tab.id === 'planner'    && result"></app-planner>
            <app-video      *ngIf="tab.id === 'video'      && result"></app-video>
            <app-export     *ngIf="tab.id === 'export'     && result"></app-export>
          </div>
        </mat-tab>

      </mat-tab-group>
    </div>
  `,
  styles: [`
    .app-shell { display: flex; flex-direction: column; height: 100vh; background: #0f0f1a; }

    .app-toolbar {
      background: linear-gradient(135deg, #1a0533 0%, #0d1b2a 100%) !important;
      border-bottom: 1px solid rgba(99,102,241,0.3);
      padding: 0 24px; height: 64px;
      display: flex; align-items: center; justify-content: space-between;
      flex-shrink: 0;
    }
    .toolbar-brand { display: flex; align-items: center; gap: 12px; }
    .brand-icon { font-size: 32px; }
    .brand-title { font-size: 18px; font-weight: 700; color: #e2e8f0; display: block; }
    .brand-sub   { font-size: 11px; color: #94a3b8; display: block; }

    .toolbar-status { display: flex; align-items: center; gap: 12px; }
    .status-chip {
      display: flex; align-items: center; gap: 6px;
      padding: 4px 12px; border-radius: 20px;
      font-size: 12px; font-weight: 600; text-transform: capitalize;
    }
    .status-running   { background:rgba(245,158,11,0.15); color:#fbbf24; border:1px solid rgba(245,158,11,0.3); }
    .status-completed { background:rgba(16,185,129,0.15); color:#34d399; border:1px solid rgba(16,185,129,0.3); }
    .status-error     { background:rgba(239,68,68,0.15);  color:#f87171; border:1px solid rgba(239,68,68,0.3);  }
    .status-dot { width:6px; height:6px; border-radius:50%; background:currentColor; }
    .status-running .status-dot { animation: pulse 1.5s infinite; }
    .topic-label { color:#94a3b8; font-size:13px; }

    .main-tabs { flex:1; overflow:hidden; }
    ::ng-deep .main-tabs .mat-mdc-tab-header       { background:#12122a; border-bottom:1px solid rgba(99,102,241,0.2); }
    ::ng-deep .main-tabs .mat-mdc-tab              { color:#94a3b8; min-width:90px; }
    ::ng-deep .main-tabs .mat-mdc-tab.mdc-tab--active { color:#818cf8; }
    ::ng-deep .main-tabs .mdc-tab-indicator__content--underline { border-color:#818cf8; }
    ::ng-deep .main-tabs .mat-mdc-tab-body-wrapper { flex:1; overflow:auto; }

    .tab-icon  { font-size:18px; width:18px; height:18px; margin-right:6px; }
    .tab-badge { margin-left:6px; background:#6366f1; color:white; border-radius:10px; padding:1px 7px; font-size:11px; font-weight:700; }
    .tab-content { height:100%; overflow:auto; }

    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:0.3} }
  `]
})
export class AppComponent implements OnInit {
  selectedTabIndex = 0;
  result: ContentGenerationResult | null = null;

  tabs: NavTab[] = [
    { id: 'upload',     label: 'Upload',        icon: 'cloud_upload',    requiresResult: false },
    { id: 'dashboard',  label: 'Agents',         icon: 'smart_toy',       requiresResult: false },
    { id: 'scripts',    label: 'Scripts',        icon: 'movie',           requiresResult: true,
      badge: r => r.youTubeScripts?.length ? `${r.youTubeScripts.length}` : '' },
    { id: 'shorts',     label: 'Shorts',         icon: 'bolt',            requiresResult: true,
      badge: r => r.shortsScripts?.length ? `${r.shortsScripts.length}` : '' },
    { id: 'social',     label: 'Social',         icon: 'share',           requiresResult: true,
      badge: r => r.linkedInPosts?.length ? `${r.linkedInPosts.length + (r.twitterThreads?.length||0)}` : '' },
    { id: 'thumbnails', label: 'Thumbnails',     icon: 'image',           requiresResult: true },
    { id: 'planner',    label: 'Content Plan',   icon: 'calendar_month',  requiresResult: true },
    { id: 'video',      label: 'Video Studio',   icon: 'videocam',        requiresResult: true },
    { id: 'export',     label: 'Export',         icon: 'download',        requiresResult: true },
  ];

  session$ = this.state.session$;

  constructor(private state: ContentStateService) {}

  ngOnInit(): void {
    this.state.result$.subscribe(r => { this.result = r; });
    this.state.activeTab$.subscribe(tab => {
      const idx = this.tabs.findIndex(t => t.id === tab);
      if (idx >= 0) this.selectedTabIndex = idx;
    });
  }

  onTabChange(index: number): void {
    this.state.setActiveTab(this.tabs[index]?.id || 'upload');
  }
}
