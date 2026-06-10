import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatBadgeModule } from '@angular/material/badge';
import { ContentStateService } from '../../core/services/content-state.service';
import { LinkedInPost, TwitterThread, ContentGenerationResult } from '../../core/models/content.models';

@Component({
  selector: 'app-social',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatButtonModule, MatIconModule,
    MatTabsModule, MatChipsModule, MatTooltipModule, MatSnackBarModule, MatBadgeModule
  ],
  template: `
    <div class="social-page">
      <div class="page-header">
        <div>
          <h2>📱 Social Media Content</h2>
          <p>{{ result?.linkedInPosts?.length }} LinkedIn posts • {{ result?.twitterThreads?.length }} Twitter threads</p>
        </div>
      </div>

      <mat-tab-group class="social-tabs">

        <!-- LinkedIn Tab -->
        <mat-tab>
          <ng-template mat-tab-label>
            <span class="tab-icon">💼</span> LinkedIn
            <span class="tab-count">{{ result?.linkedInPosts?.length }}</span>
          </ng-template>

          <div class="posts-layout">
            <!-- Post List -->
            <div class="post-list">
              <div *ngFor="let post of result?.linkedInPosts"
                   class="post-list-item"
                   [class.selected]="selectedLinkedIn?.index === post.index"
                   (click)="selectLinkedIn(post)">
                <div class="post-type-badge" [class]="'type-' + post.postType">{{ getTypeIcon(post.postType) }} {{ post.postType }}</div>
                <p class="post-preview">{{ post.content | slice:0:100 }}...</p>
              </div>
            </div>

            <!-- Post Detail -->
            <div class="post-detail" *ngIf="selectedLinkedIn">
              <div class="linkedin-card">
                <!-- LinkedIn profile mock -->
                <div class="li-profile">
                  <div class="li-avatar">AI</div>
                  <div>
                    <p class="li-name">Your Name • 1st</p>
                    <p class="li-title">Content Creator | Tech Educator</p>
                    <p class="li-time">Just now • 🌐</p>
                  </div>
                  <div class="li-copy-btn">
                    <button mat-flat-button (click)="copyLinkedIn(selectedLinkedIn)">
                      <mat-icon>content_copy</mat-icon> Copy Post
                    </button>
                  </div>
                </div>

                <!-- Post content -->
                <div class="li-content">
                  <pre class="li-text">{{ selectedLinkedIn.content }}</pre>
                  <div class="li-hashtags">
                    <span *ngFor="let tag of selectedLinkedIn.hashtags" class="li-tag">{{ tag }}</span>
                  </div>
                  <div class="li-cta" *ngIf="selectedLinkedIn.callToAction">
                    <mat-icon>chat_bubble_outline</mat-icon>
                    <em>{{ selectedLinkedIn.callToAction }}</em>
                  </div>
                </div>

                <!-- LinkedIn reactions mock -->
                <div class="li-actions">
                  <span>👍 Like</span>
                  <span>💬 Comment</span>
                  <span>🔁 Repost</span>
                  <span>✉️ Send</span>
                </div>
              </div>
            </div>
          </div>
        </mat-tab>

        <!-- Twitter/X Tab -->
        <mat-tab>
          <ng-template mat-tab-label>
            <span class="tab-icon">🐦</span> Twitter / X
            <span class="tab-count">{{ result?.twitterThreads?.length }}</span>
          </ng-template>

          <div class="threads-layout">
            <!-- Thread list -->
            <div class="thread-list">
              <div *ngFor="let thread of result?.twitterThreads"
                   class="thread-list-item"
                   [class.selected]="selectedThread?.index === thread.index"
                   (click)="selectThread(thread)">
                <div class="thread-num">#{{ thread.index }}</div>
                <div>
                  <p class="thread-topic">{{ thread.topic }}</p>
                  <span class="tweet-count">{{ thread.tweets?.length }} tweets</span>
                </div>
              </div>
            </div>

            <!-- Thread detail -->
            <div class="thread-detail" *ngIf="selectedThread">
              <div class="thread-header">
                <h3>{{ selectedThread.topic }}</h3>
                <button mat-flat-button (click)="copyThread(selectedThread)">
                  <mat-icon>content_copy</mat-icon> Copy Thread
                </button>
              </div>

              <div class="tweets-list">
                <div *ngFor="let tweet of selectedThread.tweets; let i = index; let last = last"
                     class="tweet-card">
                  <div class="tweet-header">
                    <div class="tweet-avatar">AI</div>
                    <div class="tweet-meta">
                      <span class="tweet-name">Your Name</span>
                      <span class="tweet-handle">&#64;yourhandle · now</span>
                    </div>
                    <span class="tweet-num">{{ i + 1 }}/{{ selectedThread.tweets.length }}</span>
                  </div>
                  <div class="tweet-body">{{ tweet }}</div>
                  <div class="tweet-chars" [class.near-limit]="tweet.length > 240">
                    {{ tweet.length }}/280
                  </div>
                  <div class="tweet-connector" *ngIf="!last"></div>
                </div>
              </div>

              <div class="thread-hashtags">
                <span *ngFor="let tag of selectedThread.hashtags" class="tw-tag">{{ tag }}</span>
              </div>
            </div>
          </div>
        </mat-tab>

      </mat-tab-group>
    </div>
  `,
  styles: [`
    .social-page { padding: 24px 40px; background: #0f0f1a; min-height: calc(100vh - 128px); }

    .page-header { margin-bottom: 20px; }
    .page-header h2 { color: #e2e8f0; font-size: 24px; margin: 0 0 4px; }
    .page-header p { color: #64748b; margin: 0; }

    ::ng-deep .social-tabs .mat-mdc-tab-header { background: #12122a; border-bottom: 1px solid rgba(99,102,241,0.2); border-radius: 12px 12px 0 0; }
    ::ng-deep .social-tabs .mat-mdc-tab { color: #94a3b8; }
    ::ng-deep .social-tabs .mat-mdc-tab.mdc-tab--active { color: #818cf8; }
    ::ng-deep .social-tabs .mdc-tab-indicator__content--underline { border-color: #818cf8; }

    .tab-icon { margin-right: 6px; }
    .tab-count { margin-left: 8px; background: #6366f1; color: white; border-radius: 10px; padding: 1px 7px; font-size: 11px; font-weight: 700; }

    /* LinkedIn Layout */
    .posts-layout, .threads-layout {
      display: grid;
      grid-template-columns: 280px 1fr;
      gap: 20px;
      padding: 20px 0;
      height: calc(100vh - 280px);
    }

    .post-list, .thread-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
      overflow-y: auto;
      padding-right: 4px;
    }

    .post-list-item {
      background: #1a1a2e;
      border: 1px solid rgba(99,102,241,0.15);
      border-radius: 10px;
      padding: 12px;
      cursor: pointer;
      transition: all 0.15s;
    }
    .post-list-item:hover, .post-list-item.selected { border-color: #6366f1; background: rgba(99,102,241,0.07); }
    .post-type-badge { font-size: 11px; font-weight: 700; text-transform: uppercase; margin-bottom: 6px; }
    .type-story { color: #f59e0b; }
    .type-list { color: #10b981; }
    .type-framework { color: #6366f1; }
    .type-opinion { color: #ec4899; }
    .type-howto { color: #06b6d4; }
    .post-preview { color: #94a3b8; font-size: 12px; line-height: 1.5; margin: 0; }

    /* LinkedIn Card */
    .linkedin-card {
      background: #1e293b;
      border-radius: 16px;
      overflow: hidden;
      border: 1px solid rgba(99,102,241,0.2);
      height: fit-content;
    }
    .li-profile {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 16px 20px;
      border-bottom: 1px solid rgba(255,255,255,0.05);
    }
    .li-avatar {
      width: 48px; height: 48px;
      background: linear-gradient(135deg, #6366f1, #8b5cf6);
      border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      color: white; font-weight: 800; font-size: 14px; flex-shrink: 0;
    }
    .li-name { color: #e2e8f0; font-size: 14px; font-weight: 700; margin: 0; }
    .li-title { color: #94a3b8; font-size: 12px; margin: 2px 0 0; }
    .li-time { color: #64748b; font-size: 11px; margin: 2px 0 0; }
    .li-copy-btn { margin-left: auto; }
    ::ng-deep .li-copy-btn button { background: rgba(99,102,241,0.15) !important; color: #818cf8 !important; font-size: 12px !important; }

    .li-content { padding: 16px 20px; }
    .li-text {
      color: #cbd5e1;
      font-size: 14px;
      line-height: 1.7;
      white-space: pre-wrap;
      font-family: inherit;
      margin: 0 0 12px;
    }
    .li-hashtags { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 12px; }
    .li-tag { color: #0ea5e9; font-size: 13px; cursor: pointer; }
    .li-cta { display: flex; align-items: flex-start; gap: 8px; color: #64748b; font-size: 13px; }
    .li-cta mat-icon { font-size: 16px; width: 16px; height: 16px; flex-shrink: 0; }

    .li-actions { display: flex; gap: 24px; padding: 12px 20px; border-top: 1px solid rgba(255,255,255,0.05); color: #64748b; font-size: 13px; font-weight: 600; }
    .li-actions span { cursor: pointer; }
    .li-actions span:hover { color: #94a3b8; }

    /* Twitter */
    .thread-list-item {
      background: #1a1a2e;
      border: 1px solid rgba(99,102,241,0.15);
      border-radius: 10px;
      padding: 12px;
      cursor: pointer;
      transition: all 0.15s;
      display: flex;
      gap: 10px;
      align-items: flex-start;
    }
    .thread-list-item:hover, .thread-list-item.selected { border-color: #6366f1; }
    .thread-num { color: #818cf8; font-weight: 700; font-size: 16px; flex-shrink: 0; }
    .thread-topic { color: #e2e8f0; font-size: 13px; font-weight: 600; margin: 0 0 4px; }
    .tweet-count { color: #64748b; font-size: 11px; }

    .thread-detail { overflow-y: auto; }
    .thread-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
    .thread-header h3 { color: #e2e8f0; font-size: 18px; margin: 0; }
    ::ng-deep .thread-header button { background: rgba(99,102,241,0.15) !important; color: #818cf8 !important; }

    .tweets-list { display: flex; flex-direction: column; gap: 0; }
    .tweet-card {
      background: #1a1a2e;
      border: 1px solid rgba(99,102,241,0.15);
      border-radius: 12px;
      padding: 16px;
      margin-bottom: 4px;
      position: relative;
    }
    .tweet-header { display: flex; align-items: center; gap: 10px; margin-bottom: 10px; }
    .tweet-avatar {
      width: 40px; height: 40px;
      background: linear-gradient(135deg, #1d9bf0, #6366f1);
      border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      color: white; font-weight: 800; font-size: 12px; flex-shrink: 0;
    }
    .tweet-name { color: #e2e8f0; font-size: 14px; font-weight: 700; display: block; }
    .tweet-handle { color: #64748b; font-size: 12px; display: block; }
    .tweet-num { margin-left: auto; color: #818cf8; font-size: 12px; font-weight: 700; background: rgba(99,102,241,0.1); padding: 2px 8px; border-radius: 8px; }
    .tweet-body { color: #cbd5e1; font-size: 15px; line-height: 1.6; margin-bottom: 8px; }
    .tweet-chars { color: #64748b; font-size: 11px; text-align: right; }
    .tweet-chars.near-limit { color: #f59e0b; }
    .tweet-connector { position: absolute; left: 36px; bottom: -4px; width: 2px; height: 8px; background: rgba(99,102,241,0.3); }

    .thread-hashtags { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 12px; }
    .tw-tag { background: rgba(29,155,240,0.1); color: #1d9bf0; padding: 3px 10px; border-radius: 8px; font-size: 12px; font-weight: 500; }
  `]
})
export class SocialComponent implements OnInit {
  result: ContentGenerationResult | null = null;
  selectedLinkedIn: LinkedInPost | null = null;
  selectedThread: TwitterThread | null = null;

  constructor(private state: ContentStateService, private snack: MatSnackBar) {}

  ngOnInit(): void {
    this.state.result$.subscribe(r => {
      this.result = r;
      if (r?.linkedInPosts?.length) this.selectedLinkedIn = r.linkedInPosts[0];
      if (r?.twitterThreads?.length) this.selectedThread = r.twitterThreads[0];
    });
  }

  selectLinkedIn(post: LinkedInPost): void { this.selectedLinkedIn = post; }
  selectThread(thread: TwitterThread): void { this.selectedThread = thread; }

  getTypeIcon(type: string): string {
    const icons: Record<string, string> = { story: '📖', list: '📋', framework: '🔷', opinion: '💭', howto: '🛠️' };
    return icons[type] || '📝';
  }

  copyLinkedIn(post: LinkedInPost): void {
    const text = `${post.content}\n\n${post.hashtags.join(' ')}`;
    navigator.clipboard.writeText(text);
    this.snack.open('✅ LinkedIn post copied!', '', { duration: 1500 });
  }

  copyThread(thread: TwitterThread): void {
    const text = thread.tweets.map((t, i) => `${i + 1}/ ${t}`).join('\n\n') + '\n\n' + thread.hashtags.join(' ');
    navigator.clipboard.writeText(text);
    this.snack.open(`✅ Thread (${thread.tweets.length} tweets) copied!`, '', { duration: 1500 });
  }
}
