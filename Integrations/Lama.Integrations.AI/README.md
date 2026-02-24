# Lama.Integrations.AI

AI integration client library for Lama CRM. This library provides interfaces and implementations for AI services used throughout the application.

## Architecture

This is a **pure API client library** - it contains:
- ✅ AI service interfaces (ITextAiService, etc.)
- ✅ Different implementations (Ollama, OpenAI, Claude, etc.)
- ❌ **NO business logic** (business logic lives in `Lama.Application`)

## Current Implementation: Ollama

Ollama is a free, local AI solution that runs large language models on your computer.

### Why Ollama?

- ✅ **FREE** - No API key required
- ✅ **Privacy** - Data stays on your machine
- ✅ **No rate limits** - Use as much as you want
- ✅ **Fast development** - Instant testing
- ✅ **Fallback support** - Works offline with simple heuristics

### Installation

1. **Download Ollama**
   ```bash
   # macOS
   brew install ollama

   # Or download from: https://ollama.com/download
   ```

2. **Start Ollama**
   ```bash
   ollama serve
   ```

3. **Pull a model** (first time only)
   ```bash
   # Pull Llama 3 (recommended, ~4.7GB)
   ollama pull llama3

   # Or try other models:
   ollama pull mistral      # ~4.1GB, faster
   ollama pull codellama    # ~3.8GB, better for code
   ollama pull llama3:70b   # ~40GB, most accurate (requires good hardware)
   ```

4. **Verify it's running**
   ```bash
   curl http://localhost:11434/api/tags
   ```

### Configuration

Edit `appsettings.json`:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3",           // Change to your preferred model
    "MaxTokens": 500,
    "Temperature": 0.7,          // 0.0-1.0 (lower = more deterministic)
    "TimeoutSeconds": 60
  }
}
```

### Available Models

| Model | Size | Use Case |
|-------|------|----------|
| `llama3` | 4.7GB | General purpose (recommended) |
| `llama3:70b` | 40GB | Most accurate (needs 48GB+ RAM) |
| `mistral` | 4.1GB | Faster, good quality |
| `codellama` | 3.8GB | Code generation/analysis |
| `phi` | 1.6GB | Lightweight, fast |

See all models: https://ollama.com/library

### Usage in Application

The business logic uses `ITextAiService` from this library:

```csharp
// In Application layer (business logic)
public class SummarizeActivityCommandHandler
{
    private readonly ITextAiService _textAiService; // Injected from this library

    public async Task<ActivitySummaryDto> Handle(...)
    {
        var summary = await _textAiService.SummarizeAsync(subject, body);
        // ... business logic continues
    }
}
```

### Fallback Behavior

If Ollama is not running, the service automatically falls back to simple heuristics:
- Extracts first sentence
- Takes first 200 characters
- Logs warning: `"Using fallback summarization (Ollama unavailable)"`

This ensures the application works even when Ollama is offline.

## Adding Other AI Providers

Want to add OpenAI, Claude, or Azure? Create a new implementation:

```csharp
// Example: OpenAI implementation
public class OpenAiTextAiService : ITextAiService
{
    public async Task<string> SummarizeAsync(string? subject, string? body, ...)
    {
        // Call OpenAI API
    }
}
```

Then update `DependencyInjection.cs`:
```csharp
services.AddScoped<ITextAiService, OpenAiTextAiService>();
```

## Project Structure

```
Integrations/Lama.Integrations.AI/
├── Configuration/
│   └── OllamaSettings.cs          # Ollama configuration
├── Interfaces/
│   └── ITextAiService.cs          # AI service interface
└── Services/
    ├── LocalTextAiService.cs      # Simple heuristic (no AI)
    └── OllamaTextAiService.cs     # Ollama implementation (current)
```

## Testing

```bash
# Start Ollama
ollama serve

# Run the API
dotnet run --project Lama.Api

# Test the summarization endpoint
curl -X POST http://localhost:5000/crm/ai/activities/{activityId}/summarize
```

## Troubleshooting

**Ollama not responding:**
```bash
# Check if Ollama is running
ollama list

# Restart Ollama
pkill ollama && ollama serve
```

**Model not found:**
```bash
# Pull the model first
ollama pull llama3
```

**Slow responses:**
- Use a smaller model (e.g., `mistral` or `phi`)
- Reduce `MaxTokens` in settings
- Consider using a GPU-enabled machine

## Next Steps

- Add more AI providers (OpenAI, Claude, Azure)
- Implement streaming responses
- Add caching for repeated queries
- Create factory pattern to switch between providers