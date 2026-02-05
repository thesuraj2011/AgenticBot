using AgenticBot.Models;
using AgenticBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgenticBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IDirectActionService _directActionService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IAgentService agentService,
        IDirectActionService directActionService,
        ILogger<ChatController> logger)
    {
        _agentService = agentService;
        _directActionService = directActionService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the AI chatbot
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message cannot be empty" });
        }

        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

        _logger.LogInformation("Chat request - Session: {SessionId}, Message: {Message}",
          sessionId, request.Message);

        // Try direct action first (no LLM required)
        var (directResponse, toolUsed, nextActions, incidents) = await _directActionService.TryHandleDirectAsync(sessionId, request.Message);

        if (directResponse != null)
        {
            _logger.LogInformation("Handled directly with tool: {Tool}", toolUsed);
            return Ok(new ChatResponse
            {
                Message = directResponse,
                SessionId = sessionId,
                ToolsUsed = toolUsed != null ? [toolUsed] : [],
                NextActions = nextActions,
                Incidents = incidents,
                Timestamp = DateTime.UtcNow
            });
        }

        // Fall back to LLM for complex queries
        var (response, toolsUsed) = await _agentService.ChatAsync(sessionId, request.Message);

        return Ok(new ChatResponse
        {
            Message = response,
            SessionId = sessionId,
            ToolsUsed = toolsUsed,
            NextActions = GetDefaultNextActions(request.Message),
            Incidents = null,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Clear chat history for a session
    /// </summary>
    [HttpDelete("{sessionId}")]
    public IActionResult ClearSession(string sessionId)
    {
        _agentService.ClearSession(sessionId);
        return Ok(new { message = "Session cleared successfully" });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    private static string[] GetDefaultNextActions(string message)
    {
        // Provide contextual next actions based on the message
        var lower = message.ToLowerInvariant();

        if (lower.Contains("weather"))
            return ["Check another city", "Get current time", "Tell me a joke"];
        if (lower.Contains("time") || lower.Contains("date"))
            return ["Check weather", "Calculate something", "Random fact"];
        if (lower.Contains("task"))
            return ["List my tasks", "Create another task", "Check weather"];
        if (lower.Contains("joke") || lower.Contains("fact"))
            return ["Another joke", "Random fact", "Check weather"];

        return ["Check weather", "What time is it?", "Tell me a joke"];
    }
}
