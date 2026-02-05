using AgenticBot.Models;
using AgenticBot.Plugins;
using System.Text.RegularExpressions;

namespace AgenticBot.Services;

/// <summary>
/// Handles direct incident management actions without requiring LLM.
/// Falls back to LLM for complex queries.
/// Now uses external API for real incident data.
/// </summary>
public interface IDirectActionService
{
    Task<(string? Response, string? ToolUsed, string[]? NextActions, List<IncidentResponse>? Incidents)> TryHandleDirectAsync(string sessionId, string message);
}

public class DirectActionService : IDirectActionService
{
    private readonly IExternalIncidentApiService _externalApiService;
    private readonly ILogger<DirectActionService> _logger;

    public DirectActionService(IExternalIncidentApiService externalApiService, ILogger<DirectActionService> logger)
    {
        _externalApiService = externalApiService;
        _logger = logger;
    }

    public async Task<(string? Response, string? ToolUsed, string[]? NextActions, List<IncidentResponse>? Incidents)> TryHandleDirectAsync(string sessionId, string message)
    {
        var lowerMessage = message.ToLowerInvariant().Trim();
        _logger.LogInformation("Processing incident message: {Message}", message);

        // ===== OPEN INCIDENTS =====
        if (lowerMessage.Contains("open incident") || lowerMessage.Contains("show open") || 
            lowerMessage.Contains("list open") || lowerMessage.Contains("get open"))
        {
            try
            {
                var priority = ExtractPriority(message);
                var incidents = await _externalApiService.GetOpenIncidentsAsync();
                
                if (priority != "all")
                {
                    incidents = incidents.Where(i => i.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var responseIncidents = ConvertToResponseFormat(incidents);
                var summaryMessage = $"Found {incidents.Count} open incidents. Click on any to view details.";
                return (summaryMessage, "Incidents", ["View critical incidents", "Show resolved incidents", "Analyze incidents"], responseIncidents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching open incidents");
                return ("Error fetching incidents from API. Please try again.", "Incidents", ["Show open incidents"], null);
            }
        }

        // ===== CRITICAL INCIDENTS =====
        if (lowerMessage.Contains("critical incident") || lowerMessage.Contains("critical") ||
            lowerMessage.Contains("urgent") || lowerMessage.Contains("high priority incident"))
        {
            try
            {
                var incidents = await _externalApiService.GetIncidentsByPriorityAsync("critical");
                var responseIncidents = ConvertToResponseFormat(incidents);
                var summaryMessage = $"Found {incidents.Count} critical incidents. Click on any to view details.";
                return (summaryMessage, "Incidents", ["Assign incident", "View high priority", "Analyze incidents"], responseIncidents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching critical incidents");
                return ("Error fetching critical incidents from API.", "Incidents", ["Show open incidents"], null);
            }
        }

        // ===== HIGH PRIORITY INCIDENTS =====
        if (lowerMessage.Contains("high priority") && lowerMessage.Contains("incident"))
        {
            try
            {
                var incidents = await _externalApiService.GetIncidentsByPriorityAsync("high");
                var responseIncidents = ConvertToResponseFormat(incidents);
                var summaryMessage = $"Found {incidents.Count} high priority incidents. Click on any to view details.";
                return (summaryMessage, "Incidents", ["View critical incidents", "Assign incident", "Update status"], responseIncidents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching high priority incidents");
                return ("Error fetching high priority incidents from API.", "Incidents", ["Show open incidents"], null);
            }
        }

        // ===== RESOLVED INCIDENTS =====
        if (lowerMessage.Contains("resolved incident") || lowerMessage.Contains("closed incident") || 
            lowerMessage.Contains("completed incident"))
        {
            try
            {
                var count = ExtractNumberFromMessage(message, 10);
                var incidents = await _externalApiService.GetResolvedIncidentsAsync();
                incidents = incidents.Take(count).ToList();

                var responseIncidents = ConvertToResponseFormat(incidents);
                var summaryMessage = $"Found {incidents.Count} resolved incidents. Click on any to view details.";
                return (summaryMessage, "Incidents", ["View open incidents", "Analyze incidents", "Get incident count"], responseIncidents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching resolved incidents");
                return ("Error fetching resolved incidents from API.", "Incidents", ["Show open incidents"], null);
            }
        }

        // ===== INCIDENT COUNT =====
        if (lowerMessage.Contains("total incident") || lowerMessage.Contains("incident count") ||
            lowerMessage.Contains("how many incident") || lowerMessage.Contains("incident summary"))
        {
            try
            {
                var analytics = await _externalApiService.AnalyzeIncidentsAsync();
                var result = $"Incident Summary:\n" +
                           $"Total incidents: {analytics.TotalIncidents}\n" +
                           $"Open: {analytics.OpenCount}\n" +
                           $"Resolved: {analytics.ResolvedCount}";
                return (result, "Incidents", ["Show open incidents", "Show resolved incidents", "Analyze incidents"], null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident count");
                return ("Error fetching incident count from API.", "Incidents", ["Show open incidents"], null);
            }
        }

        // ===== INCIDENT DETAILS =====
        if (lowerMessage.Contains("incident") && (lowerMessage.Contains("detail") || 
            lowerMessage.Contains("info") || lowerMessage.Contains("show")))
        {
            var incidentId = ExtractIncidentId(message);
            if (!string.IsNullOrEmpty(incidentId))
            {
                try
                {
                    _logger.LogInformation("Getting incident details for: {IncidentId}", incidentId);
                    var incident = await _externalApiService.GetIncidentDetailsAsync(incidentId);
                    
                    if (incident == null)
                    {
                        return ($"Incident '{incidentId}' not found.", "Incidents", ["Show open incidents"], null);
                    }

                    var result = FormatIncidentDetails(incident);
                    var responseIncidents = new List<IncidentResponse> { ConvertToResponseFormat(incident) };
                    var nextActions = GetIntelligentNextActions(incident);
                    return (result, "Incidents", nextActions, responseIncidents);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching incident details");
                    return ("Error fetching incident details from API.", "Incidents", ["Show open incidents"], null);
                }
            }
        }

        // ===== UPDATE INCIDENT STATUS =====
        if ((lowerMessage.Contains("update") || lowerMessage.Contains("change") || lowerMessage.Contains("set")) &&
            lowerMessage.Contains("incident") && lowerMessage.Contains("status"))
        {
            var incidentId = ExtractIncidentId(message);
            var newStatus = ExtractNewStatus(message);
            
            if (!string.IsNullOrEmpty(incidentId) && !string.IsNullOrEmpty(newStatus))
            {
                try
                {
                    _logger.LogInformation("Updating incident {IncidentId} to status {Status}", incidentId, newStatus);
                    // Note: External API doesn't support updates in read-only mode
                    var result = $"Incident status update request sent for {incidentId} to {newStatus}.\n" +
                               "(Status updates are read-only from external API)";
                    return (result, "Incidents", ["Get incident details", "View updated list", "Analyze incidents"], null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating incident");
                    return ("Error updating incident status.", "Incidents", ["Show open incidents"], null);
                }
            }
            return ("Please provide incident ID and new status (Open, In Progress, On Hold, or Resolved).", "Incidents", ["Show open incidents"], null);
        }

        // ===== RESOLVE INCIDENT =====
        if ((lowerMessage.Contains("resolve") || lowerMessage.Contains("close") || lowerMessage.Contains("mark as resolved")) &&
            lowerMessage.Contains("incident"))
        {
            var incidentId = ExtractIncidentId(message);
            if (!string.IsNullOrEmpty(incidentId))
            {
                try
                {
                    _logger.LogInformation("Resolving incident: {IncidentId}", incidentId);
                    var result = $"Incident {incidentId} resolve request sent.\n" +
                               "(Operations are read-only from external API)";
                    return (result, "Incidents", ["View resolved incidents", "Show open incidents", "Analyze incidents"], null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolving incident");
                    return ("Error resolving incident.", "Incidents", ["Show open incidents"], null);
                }
            }
            return ("Please provide incident ID to resolve. Example: 'Resolve incident INC001234'", "Incidents", ["Show open incidents"], null);
        }

        // ===== ASSIGN INCIDENT =====
        if ((lowerMessage.Contains("assign") || lowerMessage.Contains("assign to")) &&
            lowerMessage.Contains("incident"))
        {
            var incidentId = ExtractIncidentId(message);
            var assignee = ExtractAssignee(message);
            
            if (!string.IsNullOrEmpty(incidentId) && !string.IsNullOrEmpty(assignee))
            {
                try
                {
                    _logger.LogInformation("Assigning incident {IncidentId} to {Assignee}", incidentId, assignee);
                    var result = $"Incident {incidentId} assignment request to {assignee} sent.\n" +
                               "(Assignment operations are read-only from external API)";
                    return (result, "Incidents", ["Get incident details", "Update status", "View open incidents"], null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning incident");
                    return ("Error assigning incident.", "Incidents", ["Show open incidents"], null);
                }
            }
            return ("Please provide incident ID and assignee. Example: 'Assign incident INC001234 to John Doe'", "Incidents", ["Show open incidents"], null);
        }

        // ===== ANALYZE INCIDENTS =====
        if (lowerMessage.Contains("analyze") || lowerMessage.Contains("analysis") ||
            lowerMessage.Contains("incident metrics") || lowerMessage.Contains("incident trend"))
        {
            if (lowerMessage.Contains("incident"))
            {
                try
                {
                    _logger.LogInformation("Analyzing incidents for session: {SessionId}", sessionId);
                    var analytics = await _externalApiService.AnalyzeIncidentsAsync();
                    var result = FormatAnalytics(analytics);
                    return (result, "Incidents", ["View open incidents", "Show resolved incidents", "Create new incident"], null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analyzing incidents");
                    return ("Error analyzing incidents from API.", "Incidents", ["Show open incidents"], null);
                }
            }
        }

        // ===== CREATE INCIDENT =====
        if ((lowerMessage.Contains("create") || lowerMessage.Contains("add") || lowerMessage.Contains("new")) &&
            lowerMessage.Contains("incident"))
        {
            var title = ExtractIncidentTitle(message);

            if (!string.IsNullOrEmpty(title))
            {
                try
                {
                    _logger.LogInformation("Creating new incident: {Title}", title);
                    var result = $"Incident creation request submitted: '{title}'\n" +
                               $"(Incidents can only be viewed from external API in this agent)\n" +
                               "Use 'Show open incidents' to see all current incidents.";
                    return (result, "Incidents", ["View open incidents", "Assign incident", "Analyze incidents"], null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating incident");
                    return ("Error creating incident.", "Incidents", ["Show open incidents"], null);
                }
            }
            return ("Please provide incident title. Example: 'Create incident: Database connection timeout'", "Incidents", ["Show open incidents"], null);
        }

        // No direct match found
        _logger.LogInformation("No direct incident action match for: {Message}", message);
        return (null, null, null, null);
    }

    private static List<IncidentResponse> ConvertToResponseFormat(List<IncidentDto> incidents)
    {
        return incidents.Select(i => new IncidentResponse
        {
            Id = i.Id,
            Title = i.Title,
            Description = i.Description,
            Status = i.Status,
            Priority = i.Priority,
            Category = i.Category,
            AssignedTo = i.AssignedTo
        }).ToList();
    }

    private static IncidentResponse ConvertToResponseFormat(IncidentDto incident)
    {
        return new IncidentResponse
        {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            Status = incident.Status,
            Priority = incident.Priority,
            Category = incident.Category,
            AssignedTo = incident.AssignedTo
        };
    }

    private static string FormatIncidentList(List<IncidentDto> incidents, string title)
    {
        if (!incidents.Any())
        {
            return $"{title}: No incidents found.";
        }

        var result = $"{title}:\n\n";
        foreach (var incident in incidents)
        {
            var priorityIcon = incident.Priority switch
            {
                "critical" => "??",
                "high" => "??",
                "medium" => "??",
                _ => "??"
            };

            var statusIcon = incident.Status switch
            {
                "Open" => "??",
                "In Progress" => "??",
                "On Hold" => "??",
                "Resolved" => "?",
                _ => "?"
            };

            result += $"{statusIcon} {priorityIcon} [{incident.Id}] {incident.Title}\n" +
                     $"   Assigned: {incident.AssignedTo}\n" +
                     $"   Category: {incident.Category}\n\n";
        }

        return result.TrimEnd();
    }

    private static string FormatIncidentDetails(IncidentDto incident)
    {
        return $"Incident Details:\n" +
               $"ID: {incident.Id}\n" +
               $"Title: {incident.Title}\n" +
               $"Description: {incident.Description}\n" +
               $"Status: {incident.Status}\n" +
               $"Priority: {incident.Priority}\n" +
               $"Severity: {incident.Severity}\n" +
               $"Category: {incident.Category}\n" +
               $"Assigned To: {incident.AssignedTo}\n" +
               $"Created: {incident.CreatedAt:yyyy-MM-dd HH:mm}\n" +
               (incident.ResolvedAt.HasValue ? $"Resolved: {incident.ResolvedAt:yyyy-MM-dd HH:mm}" : "Not yet resolved");
    }

    private static string FormatAnalytics(IncidentAnalytics analytics)
    {
        return $"Incident Analytics:\n" +
               $"Total Incidents: {analytics.TotalIncidents}\n" +
               $"Open: {analytics.OpenCount}\n" +
               $"Resolved: {analytics.ResolvedCount}\n" +
               $"Critical Priority: {analytics.CriticalPriorityCount}\n" +
               $"High Priority: {analytics.HighPriorityCount}\n" +
               $"Avg Resolution Time: {analytics.AverageResolutionHours:F1} hours\n" +
               $"Top Category: {analytics.TopCategory} ({analytics.TopCategoryCount} incidents)";
    }

    private static string? ExtractIncidentId(string message)
    {
        var match = Regex.Match(message, @"INC\d+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }

    private static string? ExtractNewStatus(string message)
    {
        var lower = message.ToLowerInvariant();
        if (lower.Contains("resolved") || lower.Contains("close")) return "Resolved";
        if (lower.Contains("in progress") || lower.Contains("in-progress")) return "In Progress";
        if (lower.Contains("on hold") || lower.Contains("on-hold")) return "On Hold";
        if (lower.Contains("open")) return "Open";
        return null;
    }

    private static string? ExtractAssignee(string message)
    {
        var match = Regex.Match(message, @"(?:assign\s+)?to\s+([^,.\n]+?)(?:\s*$|,|\.)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        
        match = Regex.Match(message, @"INC\d+\s+(.+?)(?:\s*$|,|\.)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var afterId = match.Groups[1].Value.Trim();
            if (!afterId.ToLowerInvariant().Contains("to"))
            {
                return afterId;
            }
        }
        
        return null;
    }

    private static string ExtractPriority(String message)
    {
        var lower = message.ToLowerInvariant();
        if (lower.Contains("critical")) return "critical";
        if (lower.Contains("high")) return "high";
        if (lower.Contains("medium")) return "medium";
        if (lower.Contains("low")) return "low";
        return "all";
    }

    private static string? ExtractIncidentTitle(string message)
    {
        var match = Regex.Match(message, @"(?:create|add|new)\s+incident[:\s]+(.+?)(?:\s+(?:priority|category|description)|$)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        return null;
    }

    private static int ExtractNumberFromMessage(string message, int defaultValue)
    {
        var match = Regex.Match(message, @"\d+");
        if (match.Success && int.TryParse(match.Value, out var number))
        {
            return Math.Min(number, 50);
        }
        return defaultValue;
    }

    /// <summary>
    /// Generate intelligent next actions based on incident details
    /// </summary>
    private static string[] GetIntelligentNextActions(IncidentDto incident)
    {
        var actions = new List<string>();

        // Based on status - suggest relevant actions
        if (incident.Status == "Open")
        {
            actions.Add("Assign incident to team member");
            actions.Add("Update status to In Progress");
            
            // If critical, suggest immediate action
            if (incident.Priority == "critical")
            {
                actions.Add("Escalate to management");
            }
        }
        else if (incident.Status == "In Progress")
        {
            actions.Add("Mark as On Hold");
            actions.Add("Mark as Resolved");
            actions.Add("Notify assignee");
        }
        else if (incident.Status == "On Hold")
        {
            actions.Add("Resume incident");
            actions.Add("Change assignment");
        }
        else if (incident.Status == "Resolved")
        {
            actions.Add("View resolution details");
            actions.Add("Find similar resolved incidents");
        }

        // Based on priority - suggest priority-specific actions
        if (incident.Priority == "critical" || incident.Priority == "high")
        {
            if (!actions.Contains("Notify assignee"))
                actions.Add("Notify assignee");
        }

        // Always include option to analyze similar incidents
        if (!actions.Contains("Find similar incidents"))
            actions.Add("Find similar incidents");

        // Always include back option
        actions.Add("Back to incident list");

        return actions.Take(4).ToArray(); // Return top 4 relevant actions
    }
}
