# ?? Agentic AI Chatbot

A completely **FREE** agentic AI chatbot built with ASP.NET Core, Semantic Kernel, and Ollama. No paid APIs or services required!

## Features

- **100% Free & Local** - Uses Ollama for local LLM inference
- **Agentic Capabilities** - The AI can use tools to perform real actions:
- ?? **Time & Dates** - Get current time, calculate days between dates
  - ?? **Math** - Perform calculations
  - ??? **Weather** - Get real weather information
  - ?? **Country Info** - Fetch country facts
  - ?? **Task Management** - Create and manage tasks/reminders
  - ?? **Fun** - Random facts and jokes
- **Session Management** - Maintains conversation history per session
- **Modern Web UI** - Beautiful, responsive chat interface
- **REST API** - Can be integrated with other applications

## Prerequisites

1. **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download)
2. **Ollama** - [Download](https://ollama.ai)

## Quick Start

### 1. Install and Start Ollama

```bash
# Download and install Ollama from https://ollama.ai
# Then start the Ollama server:
ollama serve
```

### 2. Pull a Model

```bash
# Recommended model for tool calling:
ollama pull llama3.2

# Alternative models:
ollama pull llama3.1    # Better quality, larger
ollama pull mistral     # Good balance
ollama pull qwen2.5   # Various sizes available
```

### 3. Run the Application

```bash
cd AgenticBot
dotnet run
```

### 4. Open the Chat UI

Navigate to `https://localhost:5001` or `http://localhost:5000` in your browser.

## API Endpoints

### Send a Message
```http
POST /api/chat
Content-Type: application/json

{
    "message": "What's the weather in London?",
    "sessionId": "optional-session-id"
}
```

### Clear Session
```http
DELETE /api/chat/{sessionId}
```

### Health Check
```http
GET /api/chat/health
```

## Configuration

Edit `appsettings.json` to configure:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "llama3.2"
  }
}
```

## Project Structure

```
AgenticBot/
??? Controllers/
?   ??? ChatController.cs    # API endpoints
??? Models/
?   ??? ChatMessage.cs  # Request/response models
??? Plugins/
?   ??? MathPlugin.cs  # Math calculations
?   ??? TaskManagerPlugin.cs  # Task management
?   ??? TimePlugin.cs         # Time/date functions
?   ??? WebSearchPlugin.cs    # Weather, facts, jokes
??? Services/
?   ??? AgentService.cs       # AI agent orchestration
??? wwwroot/
?   ??? index.html            # Web chat UI
??? Program.cs                 # Application setup
??? appsettings.json          # Configuration
```

## Adding Custom Tools

Create a new plugin class with `[KernelFunction]` attributes:

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;

public class MyCustomPlugin
{
    [KernelFunction, Description("Description of what this function does")]
    public string MyFunction([Description("Parameter description")] string input)
    {
        return $"Processed: {input}";
    }
}
```

Register it in `Program.cs`:
```csharp
kernel.Plugins.AddFromType<MyCustomPlugin>("MyPlugin");
```

## Tech Stack

- **ASP.NET Core 9** - Web framework
- **Semantic Kernel** - AI orchestration
- **Ollama** - Local LLM inference (FREE!)
- **Vanilla JS** - Frontend (no framework dependencies)

## Troubleshooting

### "Connection refused" error
- Make sure Ollama is running: `ollama serve`
- Check if the model is pulled: `ollama list`

### Slow responses
- Try a smaller model like `llama3.2` (3B parameters)
- Ensure you have enough RAM (8GB+ recommended)

### Tool calling not working
- Some models have better tool support than others
- `llama3.2`, `llama3.1`, and `qwen2.5` are recommended

## License

MIT License - Feel free to use and modify!
