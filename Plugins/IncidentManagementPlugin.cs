using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgenticBot.Plugins;

/// <summary>
/// Plugin providing incident management capabilities for ServiceNow-like operations.
/// Handles open incidents, resolved incidents, and incident actions.
/// Stores incidents in memory (for demo purposes - would use database in production).
/// </summary>
public class IncidentManagementPlugin
{
    private static readonly Dictionary<string, List<IncidentItem>> _incidents = [];

    [KernelFunction, Description("Lists all open incidents")]
    public string ListOpenIncidents(
        [Description("The session ID")] string sessionId,
        [Description("Filter by priority: all, critical, high, medium, or low")] string priority = "all")
    {
        if (!_incidents.ContainsKey(sessionId) || _incidents[sessionId].Count == 0)
        {
            return "No incidents found for this session.";
        }

        var incidents = _incidents[sessionId]
            .Where(i => i.Status == "Open")
            .ToList();

        if (priority.ToLower() != "all")
        {
            incidents = incidents.Where(i => i.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (incidents.Count == 0)
        {
            return $"No open incidents found{(priority.ToLower() != "all" ? $" with {priority} priority" : "")}.";
        }

        var result = $"Open Incidents ({incidents.Count}):\n";
        foreach (var incident in incidents.OrderBy(i => i.CreatedAt).ThenBy(i => i.Priority))
        {
            result += FormatIncidentLine(incident);
        }

        return result.TrimEnd();
    }

    [KernelFunction, Description("Lists recently resolved incidents")]
    public string ListResolvedIncidents(
        [Description("The session ID")] string sessionId,
        [Description("Number of recent incidents to show")] int count = 10)
    {
        if (!_incidents.ContainsKey(sessionId) || _incidents[sessionId].Count == 0)
        {
            return "No incidents found for this session.";
        }

        var incidents = _incidents[sessionId]
            .Where(i => i.Status == "Resolved")
            .OrderByDescending(i => i.ResolvedAt)
            .Take(count)
            .ToList();

        if (incidents.Count == 0)
        {
            return "No resolved incidents found.";
        }

        var result = $"Recently Resolved Incidents ({incidents.Count}):\n";
        foreach (var incident in incidents)
        {
            result += FormatIncidentLine(incident);
        }

        return result.TrimEnd();
    }

    [KernelFunction, Description("Gets total count of open incidents")]
    public string GetOpenIncidentCount(
        [Description("The session ID")] string sessionId)
    {
        if (!_incidents.ContainsKey(sessionId))
        {
            return "Total open incidents: 0";
        }

        var openCount = _incidents[sessionId].Count(i => i.Status == "Open");
        var resolvedCount = _incidents[sessionId].Count(i => i.Status == "Resolved");
        var totalCount = _incidents[sessionId].Count;

        return $"Total incidents: {totalCount}\n" +
               $"Open: {openCount}\n" +
               $"Resolved: {resolvedCount}";
    }

    [KernelFunction, Description("Gets details of a specific incident")]
    public string GetIncidentDetails(
        [Description("The session ID")] string sessionId,
        [Description("The incident ID (e.g., INC001234)")] string incidentId)
    {
        if (!_incidents.ContainsKey(sessionId))
        {
            return $"Incident '{incidentId}' not found.";
        }

        var incident = _incidents[sessionId]
            .FirstOrDefault(i => i.Id.Equals(incidentId, StringComparison.OrdinalIgnoreCase));

        if (incident == null)
        {
            return $"Incident '{incidentId}' not found.";
        }

        return $"Incident Details:\n" +
               $"ID: {incident.Id}\n" +
               $"Title: {incident.Title}\n" +
               $"Description: {incident.Description}\n" +
               $"Status: {incident.Status}\n" +
               $"Priority: {incident.Priority}\n" +
               $"Severity: {incident.Severity}\n" +
               $"Assigned To: {incident.AssignedTo}\n" +
               $"Created: {incident.CreatedAt:yyyy-MM-dd HH:mm}\n" +
               $"Category: {incident.Category}" +
               (incident.ResolvedAt.HasValue ? $"\nResolved: {incident.ResolvedAt:yyyy-MM-dd HH:mm}" : "");
    }

    [KernelFunction, Description("Updates incident status")]
    public string UpdateIncidentStatus(
        [Description("The session ID")] string sessionId,
        [Description("The incident ID")] string incidentId,
        [Description("New status: Open, In Progress, On Hold, or Resolved")] string newStatus)
    {
        if (!_incidents.ContainsKey(sessionId))
        {
            return $"Incident '{incidentId}' not found.";
        }

        var incident = _incidents[sessionId]
            .FirstOrDefault(i => i.Id.Equals(incidentId, StringComparison.OrdinalIgnoreCase));

        if (incident == null)
        {
            return $"Incident '{incidentId}' not found.";
        }

        incident.Status = newStatus;
        if (newStatus.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
        {
            incident.ResolvedAt = DateTime.UtcNow;
        }

        return $"Incident '{incident.Id}' status updated to '{newStatus}'.";
    }

    [KernelFunction, Description("Assigns an incident to a person")]
    public string AssignIncident(
        [Description("The session ID")] string sessionId,
        [Description("The incident ID")] string incidentId,
        [Description("The name or email of the person to assign to")] string assignedTo)
    {
        if (!_incidents.ContainsKey(sessionId))
        {
            return $"Incident '{incidentId}' not found.";
        }

        var incident = _incidents[sessionId]
            .FirstOrDefault(i => i.Id.Equals(incidentId, StringComparison.OrdinalIgnoreCase));

        if (incident == null)
        {
            return $"Incident '{incidentId}' not found.";
        }

        incident.AssignedTo = assignedTo;
        return $"Incident '{incident.Id}' assigned to {assignedTo}.";
    }

    [KernelFunction, Description("Analyzes incident metrics and trends")]
    public string AnalyzeIncidents(
        [Description("The session ID")] string sessionId)
    {
        if (!_incidents.ContainsKey(sessionId) || _incidents[sessionId].Count == 0)
        {
            return "No incidents to analyze.";
        }

        var allIncidents = _incidents[sessionId];
        var openIncidents = allIncidents.Where(i => i.Status == "Open").ToList();
        var resolvedIncidents = allIncidents.Where(i => i.Status == "Resolved").ToList();

        var criticalCount = allIncidents.Count(i => i.Priority == "critical");
        var highCount = allIncidents.Count(i => i.Priority == "high");

        var avgResolutionTime = resolvedIncidents.Count > 0
            ? resolvedIncidents.Average(i => (i.ResolvedAt.HasValue ? (i.ResolvedAt.Value - i.CreatedAt).TotalHours : 0))
            : 0;

        var topCategory = allIncidents
            .GroupBy(i => i.Category)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        return $"Incident Analysis:\n" +
               $"Total Incidents: {allIncidents.Count}\n" +
               $"Open: {openIncidents.Count}\n" +
               $"Resolved: {resolvedIncidents.Count}\n" +
               $"Critical Priority: {criticalCount}\n" +
               $"High Priority: {highCount}\n" +
               $"Avg Resolution Time: {avgResolutionTime:F1} hours\n" +
               $"Top Category: {topCategory?.Key ?? "N/A"} ({topCategory?.Count() ?? 0} incidents)";
    }

    [KernelFunction, Description("Creates a new incident")]
    public string CreateIncident(
        [Description("The session ID")] string sessionId,
        [Description("Incident title")] string title,
        [Description("Incident description")] string description,
        [Description("Priority: critical, high, medium, or low")] string priority = "medium",
        [Description("Severity: 1, 2, 3, or 4")] string severity = "3",
        [Description("Category (e.g., Network, Database, Application)")] string category = "Other")
    {
        if (!_incidents.ContainsKey(sessionId))
        {
            _incidents[sessionId] = [];
        }

        var incident = new IncidentItem
        {
            Id = $"INC{DateTime.UtcNow.Ticks:D12}",
            Title = title,
            Description = description,
            Status = "Open",
            Priority = priority.ToLower(),
            Severity = severity,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            AssignedTo = "Unassigned"
        };

        _incidents[sessionId].Add(incident);
        return $"Incident created successfully! ID: {incident.Id}, Title: '{title}', Priority: {priority}";
    }

    private static string FormatIncidentLine(IncidentItem incident)
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

        return $"{statusIcon} {priorityIcon} [{incident.Id}] {incident.Title} (Assigned: {incident.AssignedTo})\n";
    }

    private class IncidentItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public string Priority { get; set; } = "medium";
        public string Severity { get; set; } = "3";
        public string Category { get; set; } = "Other";
        public string AssignedTo { get; set; } = "Unassigned";
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
