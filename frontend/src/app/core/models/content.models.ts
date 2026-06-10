export interface UploadResponse {
  documentId: string;
  fileName: string;
  topic: string;
  extractedChars: number;
  sectionCount: number;
  preview: string;
}

export interface ContentGenerationRequest {
  documentId: string;
  topic: string;
  youTubeVideoCount: number;
  shortsCount: number;
  linkedInPostCount: number;
  twitterThreadCount: number;
  generateThumbnailPrompts: boolean;
  generateSEO: boolean;
  targetAudience: string;
  contentStyle: string;
}

export interface ContentGenerationResult {
  sessionId: string;
  topic: string;
  youTubeScripts: YouTubeVideoScript[];
  shortsScripts: ShortsScript[];
  linkedInPosts: LinkedInPost[];
  twitterThreads: TwitterThread[];
  thumbnailPrompts: ThumbnailPrompt[];
  contentPlan: VideoContentPlan;
  generatedAt: string;
}

export interface YouTubeVideoScript {
  index: number;
  title: string;
  description: string;
  hook: string;
  introduction: string;
  mainContent: ScriptSection[];
  callToAction: string;
  outro: string;
  estimatedDurationMinutes: number;
  seo: SEOData;
  thumbnailPrompt: ThumbnailPrompt;
}

export interface ScriptSection {
  title: string;
  content: string;
  durationSeconds: number;
  visualNote: string;
}

export interface ShortsScript {
  index: number;
  title: string;
  hook: string;
  mainPoint: string;
  callToAction: string;
  durationSeconds: number;
  hashtags: string[];
  visualConcept: string;
}

export interface LinkedInPost {
  index: number;
  title: string;
  content: string;
  hashtags: string[];
  callToAction: string;
  postType: string;
}

export interface TwitterThread {
  index: number;
  topic: string;
  tweets: string[];
  hashtags: string[];
}

export interface ThumbnailPrompt {
  videoTitle: string;
  mainText: string;
  backgroundDescription: string;
  colorScheme: string;
  faceExpression: string;
  dallePrompt: string;
  midjourneyPrompt: string;
}

export interface SEOData {
  primaryKeyword: string;
  secondaryKeywords: string[];
  tags: string[];
  optimizedTitle: string;
  optimizedDescription: string;
  chapters: string[];
}

export interface VideoContentPlan {
  overallStrategy: string;
  contentPillars: string[];
  publishingSchedule: PublishingScheduleItem[];
  seriesIdeas: string[];
  channelGrowthStrategy: string;
}

export interface PublishingScheduleItem {
  weekNumber: number;
  contentType: string;
  title: string;
  platform: string;
  publishDay: string;
}

export interface AgentProgressUpdate {
  agentName: string;
  status: 'running' | 'completed' | 'error';
  message: string;
  progressPercent: number;
  data?: any;
}

export interface GenerationSession {
  sessionId: string;
  documentId: string;
  topic: string;
  status: 'pending' | 'running' | 'completed' | 'error';
  agentUpdates: AgentProgressUpdate[];
  result?: ContentGenerationResult;
  startedAt: Date;
}

// ── Video Generation Models ───────────────────────────────────────────────────
export interface VideoGenerationRequest {
  sessionId:               string;
  scriptIndex:             number;
  voiceName:               string;
  voiceStyle:              string;
  speechRate:              number;
  generateSubtitles:       boolean;
  generateImages:          boolean;
  imageStyle:              string;
  outputFormat:            string;
  resolution:              number; // 0=720p 1=1080p 2=4K
}

export interface VideoGenerationResponse {
  videoSessionId: string;
  message:        string;
  scriptTitle:    string;
}

export interface GeneratedVideo {
  id:              string;
  title:           string;
  status:          VideoStatus;
  durationSeconds: number;
  fileSizeBytes:   number;
  resolution:      string;
  sceneCount:      number;
  hasSubtitles:    boolean;
  createdAt:       string;
  errorMessage?:   string;
  downloadUrl?:    string;
  subtitleUrl?:    string;
  thumbnailUrl?:   string;
}

export type VideoStatus =
  | 'Pending'
  | 'GeneratingStoryboard'
  | 'GeneratingImages'
  | 'GeneratingVoice'
  | 'GeneratingSubtitles'
  | 'ComposingVideo'
  | 'Completed'
  | 'Failed';

export interface VideoProgressUpdate {
  stage:           string;
  message:         string;
  progressPercent: number;
  status:          VideoStatus;
  previewImagePath?: string;
}
