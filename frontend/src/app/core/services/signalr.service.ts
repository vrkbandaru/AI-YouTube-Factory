import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import { AgentProgressUpdate, VideoProgressUpdate } from '../models/content.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hub!: signalR.HubConnection;

  // Content generation events
  private _agentProgress      = new Subject<AgentProgressUpdate>();
  private _generationComplete = new Subject<{ sessionId: string; summary: any }>();
  private _generationError    = new Subject<string>();

  // Video generation events
  private _videoProgress      = new Subject<VideoProgressUpdate>();
  private _videoComplete      = new Subject<any>();
  private _videoError         = new Subject<string>();

  private _connected = new BehaviorSubject<boolean>(false);

  agentProgress$      = this._agentProgress.asObservable();
  generationComplete$ = this._generationComplete.asObservable();
  generationError$    = this._generationError.asObservable();
  videoProgress$      = this._videoProgress.asObservable();
  videoComplete$      = this._videoComplete.asObservable();
  videoError$         = this._videoError.asObservable();
  connected$          = this._connected.asObservable();

  async connect(): Promise<void> {
    if (this.hub?.state === signalR.HubConnectionState.Connected) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/hubs/content`, {
        transport: signalR.HttpTransportType.None, // auto
        skipNegotiation: false
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Content generation events
    this.hub.on('AgentProgress',       (u: AgentProgressUpdate) => this._agentProgress.next(u));
    this.hub.on('GenerationComplete',  (d: any)                 => this._generationComplete.next(d));
    this.hub.on('GenerationError',     (e: string)              => this._generationError.next(e));

    // Video generation events
    this.hub.on('VideoProgress',           (u: VideoProgressUpdate) => this._videoProgress.next(u));
    this.hub.on('VideoGenerationComplete', (d: any)                 => this._videoComplete.next(d));
    this.hub.on('VideoGenerationError',    (e: string)              => this._videoError.next(e));

    this.hub.onreconnected(() => this._connected.next(true));
    this.hub.onclose(()      => this._connected.next(false));

    await this.hub.start();
    this._connected.next(true);
  }

  async joinSession(sessionId: string): Promise<void> {
    if (this.hub?.state === signalR.HubConnectionState.Connected)
      await this.hub.invoke('JoinSession', sessionId);
  }

  async leaveSession(sessionId: string): Promise<void> {
    if (this.hub?.state === signalR.HubConnectionState.Connected)
      await this.hub.invoke('LeaveSession', sessionId);
  }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this._connected.next(false);
  }

  get isConnected(): boolean {
    return this.hub?.state === signalR.HubConnectionState.Connected;
  }
}
