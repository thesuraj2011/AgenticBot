using System.Net.Http.Json;
using System.Text.Json;

namespace AgenticBot.Services;

/// <summary>
/// Service to fetch incident data from external APIs
/// Uses JSONPlaceholder API as a dummy data source
/// Maps external data to incident format
/// </summary>
public interface IExternalIncidentApiService
{
    Task<List<IncidentDto>> GetOpenIncidentsAsync();
    Task<List<IncidentDto>> GetResolvedIncidentsAsync();
    Task<IncidentDto?> GetIncidentDetailsAsync(string incidentId);
    Task<List<IncidentDto>> GetIncidentsByPriorityAsync(string priority);
    Task<IncidentAnalytics> AnalyzeIncidentsAsync();
}

public class ExternalIncidentApiService : IExternalIncidentApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalIncidentApiService> _logger;
    private const string JsonPlaceholderApiUrl = "https://jsonplaceholder.typicode.com";
    private static List<IncidentDto> _cachedIncidents = [];
    private static DateTime _lastCacheTime = DateTime.MinValue;
    private const int CacheDurationMinutes = 5;

    public ExternalIncidentApiService(HttpClient httpClient, ILogger<ExternalIncidentApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get open incidents from external API
    /// </summary>
    public async Task<List<IncidentDto>> GetOpenIncidentsAsync()
    {
        try
        {
            var incidents = await FetchAndMapIncidentsAsync();
            var openIncidents = incidents
                .Where(i => i.Status == "Open")
                .OrderByDescending(i => i.Priority)
                .ThenBy(i => i.CreatedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} open incidents from API", openIncidents.Count);
            return openIncidents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching open incidents from API");
            return [];
        }
    }

    /// <summary>
    /// Get resolved incidents from external API
    /// </summary>
    public async Task<List<IncidentDto>> GetResolvedIncidentsAsync()
    {
        try
        {
            var incidents = await FetchAndMapIncidentsAsync();
            var resolvedIncidents = incidents
                .Where(i => i.Status == "Resolved")
                .OrderByDescending(i => i.ResolvedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} resolved incidents from API", resolvedIncidents.Count);
            return resolvedIncidents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching resolved incidents from API");
            return [];
        }
    }

    /// <summary>
    /// Get specific incident details
    /// </summary>
    public async Task<IncidentDto?> GetIncidentDetailsAsync(string incidentId)
    {
        try
        {
            var incidents = await FetchAndMapIncidentsAsync();
            var incident = incidents.FirstOrDefault(i => i.Id == incidentId);

            if (incident != null)
            {
                _logger.LogInformation("Retrieved incident details for {IncidentId}", incidentId);
            }
            else
            {
                _logger.LogWarning("Incident {IncidentId} not found", incidentId);
            }

            return incident;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching incident details for {IncidentId}", incidentId);
            return null;
        }
    }

    /// <summary>
    /// Get incidents filtered by priority
    /// </summary>
    public async Task<List<IncidentDto>> GetIncidentsByPriorityAsync(string priority)
    {
        try
        {
            var incidents = await FetchAndMapIncidentsAsync();
            var filtered = incidents
                .Where(i => i.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.CreatedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} incidents with priority {Priority}", filtered.Count, priority);
            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering incidents by priority {Priority}", priority);
            return [];
        }
    }

    /// <summary>
    /// Get analytics about incidents
    /// </summary>
    public async Task<IncidentAnalytics> AnalyzeIncidentsAsync()
    {
        try
        {
            var incidents = await FetchAndMapIncidentsAsync();
            
            var openCount = incidents.Count(i => i.Status == "Open");
            var resolvedCount = incidents.Count(i => i.Status == "Resolved");
            var criticalCount = incidents.Count(i => i.Priority == "critical");
            var highCount = incidents.Count(i => i.Priority == "high");

            var avgResolutionTime = incidents
                .Where(i => i.ResolvedAt.HasValue)
                .Average(i => (i.ResolvedAt.Value - i.CreatedAt).TotalHours);

            var topCategory = incidents
                .GroupBy(i => i.Category)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            var analytics = new IncidentAnalytics
            {
                TotalIncidents = incidents.Count,
                OpenCount = openCount,
                ResolvedCount = resolvedCount,
                CriticalPriorityCount = criticalCount,
                HighPriorityCount = highCount,
                AverageResolutionHours = avgResolutionTime,
                TopCategory = topCategory?.Key ?? "N/A",
                TopCategoryCount = topCategory?.Count() ?? 0
            };

            _logger.LogInformation("Generated incident analytics: {TotalCount} total, {OpenCount} open, {ResolvedCount} resolved", 
                analytics.TotalIncidents, analytics.OpenCount, analytics.ResolvedCount);

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing incidents");
            return new IncidentAnalytics();
        }
    }

    /// <summary>
    /// Fetch and map incidents from JSONPlaceholder API
    /// </summary>
    private async Task<List<IncidentDto>> FetchAndMapIncidentsAsync()
    {
        // Check cache first
        if (DateTime.UtcNow - _lastCacheTime < TimeSpan.FromMinutes(CacheDurationMinutes) && _cachedIncidents.Any())
        {
            _logger.LogInformation("Using cached incidents ({Count} items)", _cachedIncidents.Count);
            return _cachedIncidents;
        }

        try
        {
            // Fetch posts from JSONPlaceholder (will act as incidents)
            var posts = await _httpClient.GetFromJsonAsync<List<JsonPlaceholderPost>>($"{JsonPlaceholderApiUrl}/posts?_limit=20");
            
            if (posts == null || !posts.Any())
            {
                _logger.LogWarning("No posts retrieved from JSONPlaceholder API");
                return _cachedIncidents;
            }

            // Fetch users for additional details
            var users = await _httpClient.GetFromJsonAsync<List<JsonPlaceholderUser>>($"{JsonPlaceholderApiUrl}/users");
            
            if (users == null)
            {
                users = new List<JsonPlaceholderUser>();
            }

            // Map to incidents
            var incidents = posts.Select((post, index) => MapToIncident(post, users, index)).ToList();

            _cachedIncidents = incidents;
            _lastCacheTime = DateTime.UtcNow;

            _logger.LogInformation("Fetched and cached {Count} incidents from JSONPlaceholder API", incidents.Count);
            return incidents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching incidents from JSONPlaceholder API");
            return _cachedIncidents; // Return cached if available
        }
    }

    /// <summary>
    /// Map JSONPlaceholder post to incident
    /// </summary>
    private static IncidentDto MapToIncident(JsonPlaceholderPost post, List<JsonPlaceholderUser> users, int index)
    {
        var user = users.FirstOrDefault(u => u.Id == post.UserId);
        var createdDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 30));
        var isResolved = Random.Shared.Next(0, 100) > 60; // 40% chance resolved
        var priority = GetRandomPriority();
        var category = GetRandomCategory();

        // Translate title and description to English
        var translatedTitle = TranslateToEnglish(post.Title);
        var translatedDescription = TranslateToEnglish(post.Body);

        return new IncidentDto
        {
            Id = $"INC{post.Id:D8}",
            Title = translatedTitle,
            Description = translatedDescription,
            Status = isResolved ? "Resolved" : "Open",
            Priority = priority,
            Severity = (Random.Shared.Next(1, 5)).ToString(),
            Category = category,
            AssignedTo = user?.Name ?? "Unassigned",
            CreatedAt = createdDate,
            ResolvedAt = isResolved ? createdDate.AddDays(Random.Shared.Next(1, 7)) : null
        };
    }

    /// <summary>
    /// Translate common Latin text patterns to English
    /// JSONPlaceholder uses Lorem Ipsum, so we map common patterns
    /// </summary>
    private static string TranslateToEnglish(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "No description";

        // Common translations from Lorem Ipsum to English descriptions
        var translations = new Dictionary<string, string>()
        {
            { "lorem ipsum dolor sit amet", "System performance issue reported" },
            { "consectetur adipiscing elit", "Database connectivity problem detected" },
            { "sed do eiusmod tempor incididunt", "Service timeout encountered" },
            { "ut labore et dolore magna aliqua", "Memory usage spike observed" },
            { "ut enim ad minim veniam", "API response time degraded" },
            { "quis nostrud exercitation", "Authentication service down" },
            { "ullamco laboris nisi ut aliquip", "Network latency increased" },
            { "ex ea commodo consequat", "CPU usage at critical level" },
            { "duis aute irure dolor", "Cache invalidation failed" },
            { "in reprehenderit in voluptate", "Database connection timeout" },
            { "velit esse cillum dolore", "Load balancer misconfigured" },
            { "eu fugiat nulla pariatur", "Certificate expiration warning" },
            { "excepteur sint occaecat cupidatat", "Disk space running low" },
            { "non proident sunt in culpa", "Email service unavailable" },
            { "qui officia deserunt mollit anim", "Backup job failed" },
            { "id est laborum", "Configuration sync failed" }
        };

        var lowerText = text.ToLowerInvariant();
        
        // Try exact phrase matches first
        foreach (var kvp in translations)
        {
            if (lowerText.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }

        // If no exact match, generate a meaningful description based on content length
        if (text.Length < 50)
            return "System incident reported - requires investigation";
        else if (text.Length < 100)
            return "Technical issue detected - needs urgent attention";
        else
            return "Critical system problem identified - immediate action required";
    }

    private static string GetRandomPriority()
    {
        var priorities = new[] { "low", "medium", "high", "critical" };
        return priorities[Random.Shared.Next(priorities.Length)];
    }

    private static string GetRandomCategory()
    {
        var categories = new[] { "Network", "Database", "Application", "Server", "Security", "Performance", "Other" };
        return categories[Random.Shared.Next(categories.Length)];
    }
}

/// <summary>
/// DTO for Incident data
/// </summary>
public class IncidentDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "medium";
    public string Severity { get; set; } = "3";
    public string Category { get; set; } = "Other";
    public string AssignedTo { get; set; } = "Unassigned";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// DTO for Incident Analytics
/// </summary>
public class IncidentAnalytics
{
    public int TotalIncidents { get; set; }
    public int OpenCount { get; set; }
    public int ResolvedCount { get; set; }
    public int CriticalPriorityCount { get; set; }
    public int HighPriorityCount { get; set; }
    public double AverageResolutionHours { get; set; }
    public string TopCategory { get; set; } = "N/A";
    public int TopCategoryCount { get; set; }
}

/// <summary>
/// JSONPlaceholder API models
/// </summary>
public class JsonPlaceholderPost
{
    public int UserId { get; set; }
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class JsonPlaceholderUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
