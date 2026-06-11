# 🎬 AI YouTube Content Factory

> Upload your notes, slides, or docs → Instantly generate 10 YouTube scripts, 20 Shorts, LinkedIn posts, Twitter threads, thumbnail prompts, and a 90-day content calendar — powered by Azure OpenAI + Semantic Kernel.

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Angular 17 Frontend                       │
│  Upload │ Agent Dashboard │ Scripts │ Shorts │ Social │ Export   │
└──────────────────────┬──────────────────────────────────────────┘
                       │ HTTP + SignalR (WebSocket)
┌──────────────────────▼──────────────────────────────────────────┐
│                    .NET 9 Web API                               │
│                                                                  │
│  ┌──────────┐  ┌────────────────────────────────────────────┐   │
│  │ Document │  │           Content Orchestrator              │   │
│  │  Parser  │  │                                            │   │
│  │PDF/PPTX/ │  │  ┌──────────┐  ┌──────────┐  ┌────────┐  │   │
│  │DOCX / MD │  │  │  Script  │  │   SEO    │  │Thumbn- │  │   │
│  └──────────┘  │  │  Agent   │  │  Agent   │  │ail     │  │   │
│                │  └──────────┘  └──────────┘  │ Agent  │  │   │
│                │  ┌──────────┐  ┌──────────┐  └────────┘  │   │
│                │  │ Planner  │  │  Social  │               │   │
│                │  │  Agent   │  │  Agent   │               │   │
│                │  └──────────┘  └──────────┘               │   │
│                └────────────────────────────────────────────┘   │
│                          │ Semantic Kernel                       │
└──────────────────────────┼──────────────────────────────────────┘
                           │
              ┌────────────▼────────────┐
              │    Azure OpenAI GPT-4o  │
              └─────────────────────────┘
```

---

## 📁 Project Structure

```
ai-youtube-factory/
├── backend/
│   ├── src/
│   │   ├── AIYouTubeFactory.API/          # Controllers, Hubs, Middleware
│   │   ├── AIYouTubeFactory.Core/         # Models, Interfaces
│   │   ├── AIYouTubeFactory.Infrastructure/  # Parsers, Orchestrator
│   │   ├── AIYouTubeFactory.Agents/       # 5 AI Agents
│   │   └── AIYouTubeFactory.Tests/        # Unit + Integration tests
│   ├── Dockerfile
│   └── AIYouTubeFactory.sln
├── frontend/
│   ├── src/app/
│   │   ├── core/                          # Models, Services, SignalR
│   │   └── features/                      # 8 feature components
│   ├── Dockerfile
│   └── nginx.conf
├── infra/
│   └── main.bicep                         # Azure Container Apps
├── .github/workflows/
│   └── ci-cd.yml                          # GitHub Actions pipeline
├── docker-compose.yml
└── .env.example
```

---

## ⚡ Quick Start

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Angular CLI](https://angular.io/cli): `npm install -g @angular/cli`
- Azure OpenAI resource with a `gpt-4o` deployment

### 1. Clone & Configure

```bash
git clone https://github.com/yourorg/ai-youtube-factory
cd ai-youtube-factory

# Copy and fill in your Azure keys
cp .env.example .env
```

Edit `.env`:
```env
AZURE_OPENAI_ENDPOINT=https://YOUR-RESOURCE.openai.azure.com/
AZURE_OPENAI_API_KEY=your-key-here
AZURE_OPENAI_DEPLOYMENT=gpt-4o
```

### 2. Run Backend

```bash
cd backend

# Copy your keys to appsettings
# Edit src/AIYouTubeFactory.API/appsettings.Development.json

dotnet restore
dotnet run --project src/AIYouTubeFactory.API

# API runs at: http://localhost:5001
# Swagger UI:  http://localhost:5001/swagger
```

### 3. Run Frontend

```bash
cd frontend
npm install --legacy-peer-deps
npm start

# App runs at: http://localhost:4200
```

### 4. Or Run with Docker

```bash
# Copy .env.example to .env and fill in your Azure keys
cp .env.example .env

docker-compose up --build

# Frontend: http://localhost:4200
# API:      http://localhost:5001
# Swagger:  http://localhost:5001/swagger
```

---

## 🧪 Running Tests

```bash
cd backend

# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific project
dotnet test src/AIYouTubeFactory.Tests/
```

---

## 🔌 API Reference

### Upload Document
```http
POST /api/content/upload
Content-Type: multipart/form-data

file: <your PDF/PPTX/DOCX/MD file>
```
**Response:**
```json
{
  "documentId": "uuid",
  "topic": "Microservices",
  "extractedChars": 15420,
  "sectionCount": 12,
  "preview": "First 500 chars..."
}
```

### Start Generation
```http
POST /api/content/generate
Content-Type: application/json

{
  "documentId": "uuid-from-upload",
  "topic": "Microservices Architecture",
  "youTubeVideoCount": 10,
  "shortsCount": 20,
  "linkedInPostCount": 5,
  "twitterThreadCount": 5,
  "generateThumbnailPrompts": true,
  "generateSEO": true,
  "targetAudience": "software developers",
  "contentStyle": "educational"
}
```
**Response:**
```json
{
  "sessionId": "uuid",
  "message": "Generation started. Connect to SignalR for progress."
}
```

### Connect to SignalR (real-time progress)
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5001/hubs/content')
  .build();

connection.on('AgentProgress', (update) => {
  console.log(update.agentName, update.message, update.progressPercent + '%');
});

connection.on('GenerationComplete', (data) => {
  console.log('Done!', data.summary);
});

await connection.start();
await connection.invoke('JoinSession', sessionId);
```

### Get Results
```http
GET /api/content/results/{sessionId}
```

### Export as Markdown
```http
GET /api/export/{sessionId}/markdown
```

### Export as JSON
```http
GET /api/export/{sessionId}/json
```

---

## 🤖 AI Agents

| Agent | Responsibility | Output |
|-------|---------------|--------|
| **Script Agent** | Generates YouTube & Shorts scripts via GPT-4o | 10 long scripts + 20 shorts |
| **SEO Agent** | Keyword research, optimized titles/descriptions | Tags, chapters, keywords |
| **Thumbnail Agent** | Designs thumbnail concepts | DALL-E 3 + Midjourney prompts |
| **Video Planner Agent** | 12-week content calendar | Publishing schedule + strategy |
| **Social Media Agent** | LinkedIn posts + Twitter threads | Posts ready to publish |

---

## 📄 Supported File Types

| Format | Extension | Notes |
|--------|-----------|-------|
| PDF | `.pdf` | Text extraction via PdfPig |
| PowerPoint | `.pptx`, `.ppt` | Slide-by-slide extraction |
| Word | `.docx`, `.doc` | Full document text |
| Markdown | `.md`, `.markdown` | Raw markdown preserved |
| Plain Text | `.txt` | Direct extraction |

Max file size: **50 MB**

---

## ☁️ Deploy to Azure

### Prerequisites
- Azure CLI installed and logged in
- Resource group created

```bash
# Login
az login

# Create resource group
az group create --name youtube-factory-rg --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group youtube-factory-rg \
  --template-file infra/main.bicep \
  --parameters \
      apiImage=ghcr.io/yourorg/youtube-factory/api:latest \
      frontendImage=ghcr.io/yourorg/youtube-factory/frontend:latest \
      azureOpenAIEndpoint=$AZURE_OPENAI_ENDPOINT \
      azureOpenAIKey=$AZURE_OPENAI_API_KEY
```

### GitHub Actions (Automated)

Set these secrets in your GitHub repository:
```
AZURE_CREDENTIALS         # az ad sp create-for-rbac output
AZURE_RG                  # Resource group name
AZURE_OPENAI_ENDPOINT     # Your OpenAI endpoint
AZURE_OPENAI_API_KEY      # Your OpenAI key
AZURE_OPENAI_DEPLOYMENT   # Deployment name (gpt-4o)
```

Push to `main` → pipeline builds, tests, and deploys automatically.

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 9, ASP.NET Core |
| AI Orchestration | Microsoft Semantic Kernel 1.21 |
| LLM | Azure OpenAI GPT-4o |
| Real-time | SignalR (WebSockets) |
| Search | Azure AI Search |
| Frontend | Angular 17, Angular Material |
| Containerization | Docker, Docker Compose |
| Cloud | Azure Container Apps |
| CI/CD | GitHub Actions |
| IaC | Azure Bicep |

---

## 🔧 Troubleshooting

### SignalR connection fails ("Failed to fetch")
1. Make sure backend is running: `curl http://localhost:5001/health`
2. Use `http://` not `https://` for local dev
3. Check CORS — `Program.cs` uses `SetIsOriginAllowed(_ => true)` in Development
4. Try Long Polling transport if WebSocket is blocked

### PDF extraction returns empty text
- Scanned PDFs (image-only) are not supported — use text-based PDFs
- Try converting with Adobe Acrobat's OCR first

### Azure OpenAI rate limits
- Reduce `youTubeVideoCount` and `shortsCount` in the request
- Add retry logic or increase your TPM quota in Azure Portal

---

## 📝 License

MIT License — see [LICENSE](LICENSE) for details.
