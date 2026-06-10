import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import {
  ContentGenerationResult,
  AgentProgressUpdate,
  GenerationSession,
  UploadResponse
} from '../models/content.models';

@Injectable({ providedIn: 'root' })
export class ContentStateService {
  private _uploadedDoc = new BehaviorSubject<UploadResponse | null>(null);
  private _session = new BehaviorSubject<GenerationSession | null>(null);
  private _result = new BehaviorSubject<ContentGenerationResult | null>(null);
  private _activeTab = new BehaviorSubject<string>('upload');

  uploadedDoc$ = this._uploadedDoc.asObservable();
  session$ = this._session.asObservable();
  result$ = this._result.asObservable();
  activeTab$ = this._activeTab.asObservable();

  setUploadedDoc(doc: UploadResponse): void {
    this._uploadedDoc.next(doc);
  }

  startSession(sessionId: string, documentId: string, topic: string): void {
    this._session.next({
      sessionId,
      documentId,
      topic,
      status: 'running',
      agentUpdates: [],
      startedAt: new Date()
    });
    this._activeTab.next('dashboard');
  }

  addAgentUpdate(update: AgentProgressUpdate): void {
    const session = this._session.getValue();
    if (!session) return;
    const updates = [...session.agentUpdates, update];
    this._session.next({ ...session, agentUpdates: updates });
  }

  setResult(result: ContentGenerationResult): void {
    this._result.next(result);
    const session = this._session.getValue();
    if (session) {
      this._session.next({ ...session, status: 'completed', result });
    }
  }

  setError(msg: string): void {
    const session = this._session.getValue();
    if (session) this._session.next({ ...session, status: 'error' });
  }

  setActiveTab(tab: string): void {
    this._activeTab.next(tab);
  }

  get uploadedDoc(): UploadResponse | null { return this._uploadedDoc.getValue(); }
  get session(): GenerationSession | null { return this._session.getValue(); }
  get result(): ContentGenerationResult | null { return this._result.getValue(); }
}
