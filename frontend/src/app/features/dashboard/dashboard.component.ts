import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { ContentStateService } from '../../core/services/content-state.service';
import { GenerationSession, AgentProgressUpdate, ContentGenerationResult } from '../../core/models/content.models';

interface AgentCard {
  name: string;
  icon: string;
  description: string;
  color: string;
  update?: AgentProgressUpdate;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatProgressBarModule, MatIconModule, MatButtonModule, MatChipsModule],
  template: `
    <div class="dashboard">

      <!-- No session yet -->
      <div class="empty-state" *ngIf="!session">
        <div class="empty-icon">🤖</div>
        <h2>AI Agents Ready</h2>
        <p>Upload a document and start generation to see agents working in real-time.</p>
      </div>

      <!-- Active or completed session -->
      <div *ngIf="session" class="session-view">

        <!-- Session Header -->
        <div class="session-header">
          <div class="session-info">
            <h2>{{ session.topic }}</h2>
            <span class="session-id">Session: {{ session.sessionId }}</span>
          </div>
          <div class="session-status" [class]="'status-' + session.status">
            <span class="status-dot"></span>
            {{ session.status === 'running' ? 'Agents Working...' : session.status === 'completed' ? '✅ Complete!' : session.status }}
          </div>
        </div>

        <!-- Overall Progress -->
        <div class="overall-progress" *ngIf="session.status === 'running'">
          <mat-progress-bar mode="indeterminate" color="accent"></mat-progress-bar>
        </div>

        <!-- Agent Cards Grid -->
        <div class="agents-grid">
          <div *ngFor="let agent of agentCards" class="agent-card" [class.active]="isActive(agent)" [class.completed]="isCompleted(agent)">
            <div class="agent-icon" [style.background]="agent.color + '22'" [style.border-color]="agent.color + '44'">
              {{ agent.icon }}
            </div>
            <div class="agent-info">
              <h3 class="agent-name">{{ agent.name }}</h3>
              <p class="agent-desc">{{ agent.description }}</p>
              <div class="agent-status" *ngIf="getUpdate(agent.name) as update">
                <mat-progress-bar
                  [mode]="update.status === 'running' ? 'determinate' : 'determinate'"
                  [value]="update.progressPercent"
                  [color]="update.status === 'completed' ? 'accent' : 'primary'">
                </mat-progress-bar>
                <p class="agent-message">{{ update.message }}</p>
              </div>
              <div class="agent-idle" *ngIf="!getUpdate(agent.name) && session.status === 'running'">
                <p class="idle-text">⏳ Waiting...</p>
              </div>
            </div>
            <div class="agent-badge" *ngIf="isCompleted(agent)">✅</div>
            <div class="agent-badge running" *ngIf="isActive(agent)">⚡</div>
          </div>
        </div>

        <!-- Live Log -->
        <mat-card class="log-card" *ngIf="session.agentUpdates.length > 0">
          <mat-card-header>
            <mat-icon mat-card-avatar>terminal</mat-icon>
            <mat-card-title>Agent Activity Log</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="log-container">
              <div *ngFor="let update of session.agentUpdates | slice:-20" class="log-entry" [class]="'log-' + update.status">
                <span class="log-time">{{ getTime() }}</span>
                <span class="log-agent">[{{ update.agentName }}]</span>
                <span class="log-msg">{{ update.message }}</span>
                <span class="log-pct" *ngIf="update.progressPercent">{{ update.progressPercent }}%</span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Results Summary (when complete) -->
        <div class="results-summary" *ngIf="result">
          <h3>🎉 Generation Complete!</h3>
          <div class="summary-grid">
            <div class="summary-card" (click)="goTo('scripts')">
              <span class="summary-num">{{ result.youTubeScripts?.length || 0 }}</span>
              <span class="summary-label">YouTube Scripts</span>
              <span class="summary-icon">🎬</span>
            </div>
            <div class="summary-card" (click)="goTo('shorts')">
              <span class="summary-num">{{ result.shortsScripts?.length || 0 }}</span>
              <span class="summary-label">Shorts</span>
              <span class="summary-icon">⚡</span>
            </div>
            <div class="summary-card" (click)="goTo('social')">
              <span class="summary-num">{{ result.linkedInPosts?.length || 0 }}</span>
              <span class="summary-label">LinkedIn Posts</span>
              <span class="summary-icon">💼</span>
            </div>
            <div class="summary-card" (click)="goTo('social')">
              <span class="summary-num">{{ result.twitterThreads?.length || 0 }}</span>
              <span class="summary-label">Twitter Threads</span>
              <span class="summary-icon">🐦</span>
            </div>
            <div class="summary-card" (click)="goTo('thumbnails')">
              <span class="summary-num">{{ result.thumbnailPrompts?.length || 0 }}</span>
              <span class="summary-label">Thumbnails</span>
              <span class="summary-icon">🖼️</span>
            </div>
            <div class="summary-card" (click)="goTo('export')">
              <span class="summary-num">⬇️</span>
              <span class="summary-label">Export All</span>
              <span class="summary-icon">📦</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard { padding: 24px 40px; background: #0f0f1a; min-height: calc(100vh - 128px); }

    .empty-state { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 400px; gap: 16px; }
    .empty-icon { font-size: 64px; }
    .empty-state h2 { color: #e2e8f0; font-size: 24px; margin: 0; }
    .empty-state p { color: #64748b; text-align: center; max-width: 400px; }

    .session-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; }
    .session-info h2 { color: #e2e8f0; font-size: 24px; margin: 0 0 4px; }
    .session-id { color: #475569; font-size: 12px; font-family: monospace; }

    .session-status {
      display: flex; align-items: center; gap: 8px;
      padding: 8px 16px; border-radius: 20px; font-weight: 600;
    }
    .status-running { background: rgba(245,158,11,0.1); color: #fbbf24; border: 1px solid rgba(245,158,11,0.3); }
    .status-completed { background: rgba(16,185,129,0.1); color: #34d399; border: 1px solid rgba(16,185,129,0.3); }
    .status-error { background: rgba(239,68,68,0.1); color: #f87171; border: 1px solid rgba(239,68,68,0.3); }
    .status-dot { width: 8px; height: 8px; border-radius: 50%; background: currentColor; animation: pulse 1.5s infinite; }
    .status-completed .status-dot { animation: none; }

    .overall-progress { margin-bottom: 24px; border-radius: 4px; overflow: hidden; }

    .agents-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 16px; margin-bottom: 24px; }

    .agent-card {
      background: #1a1a2e; border: 1px solid rgba(99,102,241,0.2);
      border-radius: 16px; padding: 20px;
      display: flex; align-items: flex-start; gap: 16px;
      position: relative; transition: all 0.3s;
    }
    .agent-card.active { border-color: #6366f1; box-shadow: 0 0 20px rgba(99,102,241,0.2); }
    .agent-card.completed { border-color: rgba(16,185,129,0.3); }

    .agent-icon { width: 52px; height: 52px; border-radius: 12px; border: 1px solid; display: flex; align-items: center; justify-content: center; font-size: 24px; flex-shrink: 0; }
    .agent-info { flex: 1; }
    .agent-name { color: #e2e8f0; font-size: 15px; font-weight: 700; margin: 0 0 4px; }
    .agent-desc { color: #64748b; font-size: 12px; margin: 0 0 10px; }
    .agent-message { color: #94a3b8; font-size: 12px; margin: 6px 0 0; }
    .idle-text { color: #475569; font-size: 12px; margin: 0; }

    .agent-badge {
      position: absolute; top: 12px; right: 12px;
      font-size: 16px;
    }

    .log-card { background: #0a0a1a !important; border: 1px solid rgba(99,102,241,0.2) !important; margin-bottom: 24px; }
    ::ng-deep .log-card mat-card-title { color: #e2e8f0 !important; }
    .log-container { max-height: 200px; overflow-y: auto; font-family: monospace; font-size: 12px; }
    .log-entry { display: flex; gap: 10px; padding: 4px 0; border-bottom: 1px solid rgba(255,255,255,0.03); }
    .log-time { color: #475569; flex-shrink: 0; }
    .log-agent { color: #818cf8; font-weight: 600; flex-shrink: 0; }
    .log-msg { color: #94a3b8; flex: 1; }
    .log-pct { color: #34d399; flex-shrink: 0; }
    .log-completed .log-msg { color: #34d399; }
    .log-error .log-msg { color: #f87171; }

    .results-summary h3 { color: #e2e8f0; font-size: 20px; margin: 0 0 16px; }
    .summary-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 12px; }
    .summary-card {
      background: linear-gradient(135deg, rgba(99,102,241,0.1), rgba(139,92,246,0.1));
      border: 1px solid rgba(99,102,241,0.3);
      border-radius: 12px; padding: 20px;
      display: flex; flex-direction: column; align-items: center; gap: 6px;
      cursor: pointer; transition: all 0.2s;
    }
    .summary-card:hover { transform: translateY(-2px); border-color: #818cf8; }
    .summary-num { font-size: 32px; font-weight: 800; color: #818cf8; }
    .summary-label { color: #94a3b8; font-size: 12px; text-align: center; }
    .summary-icon { font-size: 20px; }

    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.3; } }
  `]
})
export class DashboardComponent implements OnInit {
  session: GenerationSession | null = null;
  result: ContentGenerationResult | null = null;

  agentCards: AgentCard[] = [
    { name: 'Script Agent', icon: '📝', description: 'Writes YouTube & Shorts scripts using GPT-4o', color: '#6366f1' },
    { name: 'SEO Agent', icon: '🔍', description: 'Researches keywords & optimizes titles/descriptions', color: '#8b5cf6' },
    { name: 'Thumbnail Agent', icon: '🖼️', description: 'Designs thumbnail concepts & AI prompts', color: '#ec4899' },
    { name: 'Video Planner Agent', icon: '📅', description: 'Creates 90-day content calendar & strategy', color: '#f59e0b' },
    { name: 'Social Media Agent', icon: '📱', description: 'Writes LinkedIn posts & Twitter threads', color: '#10b981' },
    { name: 'Orchestrator', icon: '🤖', description: 'Coordinates all agents & manages workflow', color: '#06b6d4' },
  ];

  constructor(private state: ContentStateService) {}

  ngOnInit(): void {
    this.state.session$.subscribe(s => this.session = s);
    this.state.result$.subscribe(r => this.result = r);
  }

  getUpdate(agentName: string): AgentProgressUpdate | undefined {
    return this.session?.agentUpdates.filter(u => u.agentName === agentName).pop();
  }

  isActive(agent: AgentCard): boolean {
    return this.getUpdate(agent.name)?.status === 'running';
  }

  isCompleted(agent: AgentCard): boolean {
    return this.getUpdate(agent.name)?.status === 'completed';
  }

  getTime(): string {
    return new Date().toLocaleTimeString('en', { hour12: false });
  }

  goTo(tab: string): void {
    this.state.setActiveTab(tab);
  }
}
