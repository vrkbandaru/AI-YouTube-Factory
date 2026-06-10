import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  UploadResponse,
  ContentGenerationRequest,
  ContentGenerationResult,
  VideoGenerationRequest,
  VideoGenerationResponse,
  GeneratedVideo
} from '../models/content.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ── Content ────────────────────────────────────────────────────────────────
  uploadDocument(file: File): Observable<UploadResponse> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    return this.http.post<UploadResponse>(`${this.base}/content/upload`, fd);
  }

  startGeneration(req: ContentGenerationRequest): Observable<{ sessionId: string; message: string }> {
    return this.http.post<{ sessionId: string; message: string }>(`${this.base}/content/generate`, req);
  }

  getResults(sessionId: string): Observable<ContentGenerationResult> {
    return this.http.get<ContentGenerationResult>(`${this.base}/content/results/${sessionId}`);
  }

  // ── Export ─────────────────────────────────────────────────────────────────
  getSummary(sessionId: string): Observable<any> {
    return this.http.get(`${this.base}/export/${sessionId}/summary`);
  }

  downloadMarkdown(sessionId: string): Observable<Blob> {
    return this.http.get(`${this.base}/export/${sessionId}/markdown`, { responseType: 'blob' });
  }

  downloadJson(sessionId: string): Observable<Blob> {
    return this.http.get(`${this.base}/export/${sessionId}/json`, { responseType: 'blob' });
  }

  // ── Video ──────────────────────────────────────────────────────────────────
  startVideoGeneration(req: VideoGenerationRequest): Observable<VideoGenerationResponse> {
    return this.http.post<VideoGenerationResponse>(`${this.base}/video/generate`, req);
  }

  getVideo(videoId: string): Observable<GeneratedVideo> {
    return this.http.get<GeneratedVideo>(`${this.base}/video/${videoId}`);
  }

  getAllVideos(): Observable<GeneratedVideo[]> {
    return this.http.get<GeneratedVideo[]>(`${this.base}/video`);
  }

  deleteVideo(videoId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/video/${videoId}`);
  }

  getVideoDownloadUrl(videoId: string): string {
    return `${this.base}/video/download/${videoId}`;
  }

  getSubtitleUrl(videoId: string): string {
    return `${this.base}/video/subtitle/${videoId}`;
  }

  getThumbnailUrl(videoId: string): string {
    return `${this.base}/video/thumbnail/${videoId}`;
  }

  // ── Health ─────────────────────────────────────────────────────────────────
  healthCheck(): Observable<any> {
    return this.http.get(`${this.base}/content/health`);
  }
}
