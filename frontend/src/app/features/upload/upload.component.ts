import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSliderModule } from '@angular/material/slider';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { ApiService } from '../../core/services/api.service';
import { SignalRService } from '../../core/services/signalr.service';
import { ContentStateService } from '../../core/services/content-state.service';
import { UploadResponse } from '../../core/models/content.models';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSliderModule, MatSelectModule, MatSlideToggleModule,
    MatProgressBarModule, MatChipsModule, MatSnackBarModule, MatDividerModule
  ],
  template: `
    <div class="upload-page">
      <!-- Hero -->
      <div class="hero">
        <div class="hero-content">
          <h1>🎬 AI YouTube Content Factory</h1>
          <p>Upload your notes, slides, or documents. Instantly generate scripts, shorts, LinkedIn posts, thumbnails, and a full content strategy.</p>
          <div class="hero-stats">
            <div class="stat"><span class="stat-num">10+</span><span class="stat-label">YouTube Scripts</span></div>
            <div class="stat"><span class="stat-num">20+</span><span class="stat-label">Shorts</span></div>
            <div class="stat"><span class="stat-num">5+</span><span class="stat-label">LinkedIn Posts</span></div>
            <div class="stat"><span class="stat-num">5+</span><span class="stat-label">Twitter Threads</span></div>
          </div>
        </div>
      </div>

      <div class="upload-layout">
        <!-- Left: Drop Zone -->
        <mat-card class="upload-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>upload_file</mat-icon>
            <mat-card-title>Upload Document</mat-card-title>
            <mat-card-subtitle>PDF, PPTX, DOCX, Markdown, TXT</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>

            <!-- Drop Zone -->
            <div class="drop-zone"
                 [class.drag-over]="isDragging"
                 [class.has-file]="selectedFile"
                 [class.uploading]="uploading"
                 (dragover)="onDragOver($event)"
                 (dragleave)="onDragLeave()"
                 (drop)="onDrop($event)"
                 (click)="fileInput.click()">

              <input #fileInput type="file" hidden
                     accept=".pdf,.pptx,.ppt,.docx,.doc,.md,.markdown,.txt"
                     (change)="onFileSelected($event)">

              <div *ngIf="!selectedFile && !uploading" class="drop-placeholder">
                <div class="drop-icon">📁</div>
                <p class="drop-title">Drop your file here</p>
                <p class="drop-sub">or click to browse</p>
                <div class="supported-types">
                  <span class="type-chip" *ngFor="let t of supportedTypes">{{ t }}</span>
                </div>
              </div>

              <div *ngIf="selectedFile && !uploading && !uploadedDoc" class="file-selected">
                <div class="file-icon">{{ getFileIcon(selectedFile.name) }}</div>
                <p class="file-name">{{ selectedFile.name }}</p>
                <p class="file-size">{{ formatSize(selectedFile.size) }}</p>
                <button mat-flat-button color="primary" (click)="uploadFile($event)">
                  <mat-icon>cloud_upload</mat-icon> Upload & Analyze
                </button>
              </div>

              <div *ngIf="uploading" class="uploading-state">
                <mat-progress-bar mode="indeterminate" color="accent"></mat-progress-bar>
                <p>Parsing document...</p>
              </div>

              <div *ngIf="uploadedDoc" class="upload-success">
                <div class="success-icon">✅</div>
                <p class="success-title">{{ uploadedDoc.fileName }}</p>
                <div class="doc-stats">
                  <span>📝 {{ uploadedDoc.extractedChars | number }} chars</span>
                  <span>📑 {{ uploadedDoc.sectionCount }} sections</span>
                  <span>🏷️ {{ uploadedDoc.topic }}</span>
                </div>
                <div class="preview-box">
                  <p class="preview-label">Preview:</p>
                  <p class="preview-text">{{ uploadedDoc.preview }}...</p>
                </div>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Right: Configuration -->
        <mat-card class="config-card" *ngIf="uploadedDoc">
          <mat-card-header>
            <mat-icon mat-card-avatar>tune</mat-icon>
            <mat-card-title>Generation Settings</mat-card-title>
            <mat-card-subtitle>Customize your content output</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="configForm" class="config-form">

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Topic / Subject</mat-label>
                <input matInput formControlName="topic" placeholder="e.g., Microservices Architecture">
                <mat-icon matSuffix>label</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Target Audience</mat-label>
                <input matInput formControlName="targetAudience" placeholder="e.g., software developers">
                <mat-icon matSuffix>people</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Content Style</mat-label>
                <mat-select formControlName="contentStyle">
                  <mat-option value="educational">📚 Educational</mat-option>
                  <mat-option value="entertaining">🎭 Entertaining</mat-option>
                  <mat-option value="inspirational">✨ Inspirational</mat-option>
                  <mat-option value="tutorial">🛠️ Tutorial / How-to</mat-option>
                  <mat-option value="news">📰 News / Updates</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-divider></mat-divider>
              <p class="section-label">📊 Content Quantities</p>

              <div class="slider-row">
                <label>YouTube Videos: <strong>{{ configForm.get('youTubeVideoCount')?.value }}</strong></label>
                <mat-slider min="1" max="20" step="1" discrete>
                  <input matSliderThumb formControlName="youTubeVideoCount">
                </mat-slider>
              </div>

              <div class="slider-row">
                <label>Shorts: <strong>{{ configForm.get('shortsCount')?.value }}</strong></label>
                <mat-slider min="1" max="30" step="1" discrete>
                  <input matSliderThumb formControlName="shortsCount">
                </mat-slider>
              </div>

              <div class="slider-row">
                <label>LinkedIn Posts: <strong>{{ configForm.get('linkedInPostCount')?.value }}</strong></label>
                <mat-slider min="1" max="15" step="1" discrete>
                  <input matSliderThumb formControlName="linkedInPostCount">
                </mat-slider>
              </div>

              <div class="slider-row">
                <label>Twitter Threads: <strong>{{ configForm.get('twitterThreadCount')?.value }}</strong></label>
                <mat-slider min="1" max="10" step="1" discrete>
                  <input matSliderThumb formControlName="twitterThreadCount">
                </mat-slider>
              </div>

              <mat-divider></mat-divider>
              <p class="section-label">⚙️ Options</p>

              <div class="toggle-row">
                <span>Generate Thumbnail Prompts</span>
                <mat-slide-toggle formControlName="generateThumbnailPrompts" color="accent"></mat-slide-toggle>
              </div>
              <div class="toggle-row">
                <span>SEO Optimization</span>
                <mat-slide-toggle formControlName="generateSEO" color="accent"></mat-slide-toggle>
              </div>

            </form>
          </mat-card-content>
          <mat-card-actions>
            <button mat-flat-button color="accent" class="generate-btn"
                    (click)="startGeneration()" [disabled]="generating">
              <mat-icon>rocket_launch</mat-icon>
              {{ generating ? 'Starting...' : '🚀 Generate Content Factory' }}
            </button>
          </mat-card-actions>
        </mat-card>

        <!-- Placeholder when no doc uploaded -->
        <mat-card class="config-card placeholder-card" *ngIf="!uploadedDoc">
          <div class="placeholder-content">
            <div class="placeholder-icon">⚙️</div>
            <p>Upload a document to configure generation settings</p>
          </div>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .upload-page { min-height: calc(100vh - 128px); background: #0f0f1a; padding: 0; }

    .hero {
      background: linear-gradient(135deg, #1e0040 0%, #0d1b2a 50%, #001a3a 100%);
      padding: 48px 40px 36px;
      border-bottom: 1px solid rgba(99,102,241,0.2);
    }
    .hero h1 { font-size: 32px; font-weight: 800; color: #e2e8f0; margin: 0 0 12px; }
    .hero p { color: #94a3b8; font-size: 16px; max-width: 600px; margin: 0 0 24px; }

    .hero-stats { display: flex; gap: 32px; flex-wrap: wrap; }
    .stat { display: flex; flex-direction: column; align-items: center; }
    .stat-num { font-size: 28px; font-weight: 800; color: #818cf8; }
    .stat-label { font-size: 12px; color: #64748b; text-transform: uppercase; letter-spacing: 1px; }

    .upload-layout {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 24px;
      padding: 24px 40px;
      max-width: 1200px;
    }

    mat-card { background: #1a1a2e !important; color: #e2e8f0 !important; border: 1px solid rgba(99,102,241,0.2) !important; }
    ::ng-deep mat-card-title { color: #e2e8f0 !important; }
    ::ng-deep mat-card-subtitle { color: #94a3b8 !important; }

    .drop-zone {
      border: 2px dashed rgba(99,102,241,0.4);
      border-radius: 16px;
      padding: 40px;
      text-align: center;
      cursor: pointer;
      transition: all 0.2s;
      min-height: 280px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .drop-zone:hover, .drop-zone.drag-over {
      border-color: #818cf8;
      background: rgba(99,102,241,0.05);
    }
    .drop-zone.has-file { border-color: #6366f1; }

    .drop-placeholder { display: flex; flex-direction: column; align-items: center; gap: 8px; }
    .drop-icon { font-size: 48px; }
    .drop-title { font-size: 18px; color: #e2e8f0; margin: 0; font-weight: 600; }
    .drop-sub { color: #64748b; margin: 0; }
    .supported-types { display: flex; gap: 8px; flex-wrap: wrap; justify-content: center; margin-top: 12px; }
    .type-chip {
      background: rgba(99,102,241,0.15); color: #818cf8;
      padding: 3px 10px; border-radius: 12px; font-size: 12px; font-weight: 600;
    }

    .file-selected { display: flex; flex-direction: column; align-items: center; gap: 8px; }
    .file-icon { font-size: 48px; }
    .file-name { color: #e2e8f0; font-weight: 600; margin: 0; }
    .file-size { color: #94a3b8; font-size: 13px; margin: 0; }

    .uploading-state { width: 100%; display: flex; flex-direction: column; gap: 16px; align-items: center; }
    .uploading-state p { color: #94a3b8; }

    .upload-success { display: flex; flex-direction: column; align-items: center; gap: 10px; width: 100%; }
    .success-icon { font-size: 40px; }
    .success-title { color: #34d399; font-weight: 700; font-size: 16px; margin: 0; }
    .doc-stats { display: flex; gap: 16px; flex-wrap: wrap; justify-content: center; }
    .doc-stats span { color: #818cf8; font-size: 13px; background: rgba(99,102,241,0.1); padding: 4px 10px; border-radius: 8px; }
    .preview-box { background: rgba(0,0,0,0.3); border-radius: 8px; padding: 12px; width: 100%; text-align: left; }
    .preview-label { color: #64748b; font-size: 11px; text-transform: uppercase; margin: 0 0 6px; }
    .preview-text { color: #94a3b8; font-size: 13px; margin: 0; line-height: 1.5; }

    .config-form { display: flex; flex-direction: column; gap: 12px; }
    ::ng-deep .config-form .mat-mdc-form-field { width: 100%; }
    ::ng-deep .config-form .mat-mdc-text-field-wrapper { background: rgba(255,255,255,0.04) !important; }
    ::ng-deep .config-form .mat-mdc-floating-label { color: #94a3b8 !important; }
    ::ng-deep .config-form input.mat-mdc-input-element { color: #e2e8f0 !important; }
    ::ng-deep .mat-mdc-select-value-text { color: #e2e8f0 !important; }

    .section-label { color: #94a3b8; font-size: 13px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin: 4px 0; }

    .slider-row { display: flex; flex-direction: column; gap: 4px; }
    .slider-row label { color: #94a3b8; font-size: 13px; }
    .slider-row label strong { color: #818cf8; }

    .toggle-row { display: flex; justify-content: space-between; align-items: center; color: #94a3b8; font-size: 14px; padding: 4px 0; }

    .generate-btn { width: 100% !important; height: 52px !important; font-size: 16px !important; font-weight: 700 !important; border-radius: 12px !important; background: linear-gradient(135deg, #6366f1, #8b5cf6) !important; }

    .placeholder-card { display: flex; align-items: center; justify-content: center; min-height: 300px; }
    .placeholder-content { text-align: center; color: #475569; }
    .placeholder-icon { font-size: 48px; margin-bottom: 12px; }

    .full-width { width: 100%; }
  `]
})
export class UploadComponent implements OnInit {
  selectedFile: File | null = null;
  uploadedDoc: UploadResponse | null = null;
  uploading = false;
  generating = false;
  isDragging = false;

  supportedTypes = ['PDF', 'PPTX', 'DOCX', 'Markdown', 'TXT'];

  configForm!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
    private signalR: SignalRService,
    private state: ContentStateService,
    private snack: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.configForm = this.fb.group({
      topic: ['', Validators.required],
      targetAudience: ['software developers and tech enthusiasts'],
      contentStyle: ['educational'],
      youTubeVideoCount: [10],
      shortsCount: [20],
      linkedInPostCount: [5],
      twitterThreadCount: [5],
      generateThumbnailPrompts: [true],
      generateSEO: [true]
    });

    this.state.uploadedDoc$.subscribe(doc => {
      this.uploadedDoc = doc;
      if (doc) this.configForm.patchValue({ topic: doc.topic });
    });
  }

  onDragOver(e: DragEvent): void { e.preventDefault(); this.isDragging = true; }
  onDragLeave(): void { this.isDragging = false; }
  onDrop(e: DragEvent): void {
    e.preventDefault(); this.isDragging = false;
    const file = e.dataTransfer?.files[0];
    if (file) this.setFile(file);
  }
  onFileSelected(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file) this.setFile(file);
  }
  setFile(file: File): void { this.selectedFile = file; }

  uploadFile(e: Event): void {
    e.stopPropagation();
    if (!this.selectedFile) return;
    this.uploading = true;
    this.api.uploadDocument(this.selectedFile).subscribe({
      next: doc => {
        this.uploading = false;
        this.state.setUploadedDoc(doc);
        this.snack.open(`✅ Document parsed: ${doc.extractedChars.toLocaleString()} characters extracted`, 'OK', { duration: 4000 });
      },
      error: err => {
        this.uploading = false;
        this.snack.open(`❌ Upload failed: ${err.error?.message || err.message}`, 'OK', { duration: 5000 });
      }
    });
  }

  async startGeneration(): Promise<void> {
    if (!this.uploadedDoc || this.configForm.invalid) return;
    this.generating = true;

    try {
      await this.signalR.connect();
    } catch { /* will try anyway */ }

    const req = { documentId: this.uploadedDoc.documentId, ...this.configForm.value };

    this.api.startGeneration(req).subscribe({
      next: async resp => {
        this.generating = false;
        await this.signalR.joinSession(resp.sessionId);
        this.state.startSession(resp.sessionId, req.documentId, req.topic);

        // Listen for completion
        this.signalR.generationComplete$.subscribe(data => {
          this.api.getResults(data.sessionId).subscribe(result => this.state.setResult(result));
        });
        this.signalR.agentProgress$.subscribe(update => this.state.addAgentUpdate(update));
        this.signalR.generationError$.subscribe(err => {
          this.state.setError(err);
          this.snack.open(`❌ Generation error: ${err}`, 'OK', { duration: 6000 });
        });
      },
      error: err => {
        this.generating = false;
        this.snack.open(`❌ Failed to start: ${err.error?.message || err.message}`, 'OK', { duration: 5000 });
      }
    });
  }

  getFileIcon(name: string): string {
    const ext = name.split('.').pop()?.toLowerCase();
    const icons: Record<string, string> = { pdf: '📕', pptx: '📊', ppt: '📊', docx: '📝', doc: '📝', md: '📋', markdown: '📋', txt: '📄' };
    return icons[ext || ''] || '📁';
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
