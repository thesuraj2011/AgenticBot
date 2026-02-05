using AgenticBot.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Collections.Concurrent;
using System.Text;

namespace AgenticBot.Services;

/// <summary>
/// Service that manages the AI agent with tool-calling capabilities.
/// Uses Ollama for free local LLM inference.
/// </summary>
public interface IAgentService
{
    Task<(string Response, List<string> ToolsUsed)> ChatAsync(string sessionId, string message);
    void ClearSession(string sessionId);
}

#pragma warning disable SKEXP0070 // Ollama connector is experimental

public class AgentService : IAgentService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ConcurrentDictionary<string, ChatHistory> _sessions = new();
    private readonly ILogger<AgentService> _logger;

    private const string SystemPrompt =
        """
        You are a helpful AI assistant with access to various tools. You can:
        - Get current time and perform date calculations
        - Perform mathematical calculations
        - Get weather information for cities
        - Fetch random facts and jokes
        - Get country information
    - Manage tasks and reminders for users

      When users ask questions that require these capabilities, use the appropriate tools.
        Be conversational, helpful, and friendly. If you use a tool, incorporate the result naturally into your response.
        Always be accurate and if you're unsure about something, say so.
    """;

    public AgentService(Kernel kernel, ILogger<AgentService> logger)
    {
        _kernel = kernel;
        _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        _logger = logger;
    }

    public async Task<(string Response, List<string> ToolsUsed)> ChatAsync(string sessionId, string message)
    {
        var toolsUsed = new List<string>();

        // Get or create chat history for this session
        var history = _sessions.GetOrAdd(sessionId, _ =>
        {
 var newHistory = new ChatHistory();
            newHistory.AddSystemMessage(SystemPrompt);
            return newHistory;
        });

     // Add user message to history
    history.AddUserMessage(message);

        try
        {
         // Configure execution settings for tool calling
      var executionSettings = new OllamaPromptExecutionSettings
       {
       FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

  // Get response from the model (with automatic tool calling)
       var response = await _chatCompletionService.GetChatMessageContentAsync(
                history,
       executionSettings,
     _kernel);

// Extract tool usage information
            if (response.Metadata?.TryGetValue("Usage", out var usage) == true)
   {
           _logger.LogInformation("Token usage: {Usage}", usage);
            }

            // Check for function calls in the response
   foreach (var item in history)
 {
        if (item.Role == AuthorRole.Tool)
     {
          var toolName = item.Metadata?.GetValueOrDefault("ChatCompletionToolCallId")?.ToString();
         if (!string.IsNullOrEmpty(toolName) && !toolsUsed.Contains(toolName))
{
            toolsUsed.Add(toolName);
        }
         }
            }

      var assistantMessage = response.Content ?? "I apologize, but I couldn't generate a response.";

            // Add assistant response to history
            history.AddAssistantMessage(assistantMessage);

            // Limit history size to prevent token overflow
      TrimHistory(history);

  return (assistantMessage, toolsUsed);
   }
        catch (Exception ex)
    {
            _logger.LogError(ex, "Error processing chat message");
     
     var errorMessage = ex.Message.Contains("connection")
     ? "I'm having trouble connecting to my AI backend. Please make sure Ollama is running locally (run 'ollama serve' in terminal)."
  : $"I encountered an error: {ex.Message}";

            history.AddAssistantMessage(errorMessage);
          return (errorMessage, toolsUsed);
        }
    }

    public void ClearSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    private static void TrimHistory(ChatHistory history)
    {
        // Keep system message + last 20 messages
        const int maxMessages = 21;
        while (history.Count > maxMessages)
    {
          // Remove the second message (first after system)
         history.RemoveAt(1);
        }
    }
}

#pragma warning restore SKEXP0070
