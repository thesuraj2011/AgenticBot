using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgenticBot.Plugins;

/// <summary>
/// Plugin providing task and reminder management capabilities.
/// Stores tasks in memory (for demo purposes - would use database in production).
/// </summary>
public class TaskManagerPlugin
{
  private static readonly Dictionary<string, List<TaskItem>> _tasks = [];

    [KernelFunction, Description("Creates a new task or reminder")]
    public string CreateTask(
        [Description("The session ID to associate the task with")] string sessionId,
     [Description("The task description")] string description,
     [Description("Optional due date in yyyy-MM-dd format")] string? dueDate = null,
        [Description("Priority: low, medium, or high")] string priority = "medium")
    {
        if (!_tasks.ContainsKey(sessionId))
        {
            _tasks[sessionId] = [];
     }

        DateTime? parsedDueDate = null;
        if (!string.IsNullOrEmpty(dueDate) && DateTime.TryParse(dueDate, out var date))
     {
            parsedDueDate = date;
        }

  var task = new TaskItem
  {
            Id = Guid.NewGuid().ToString()[..8],
Description = description,
      DueDate = parsedDueDate,
            Priority = priority.ToLower(),
      CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

   _tasks[sessionId].Add(task);
        return $"Task created successfully! ID: {task.Id}, Description: '{description}', Priority: {priority}" +
               (parsedDueDate.HasValue ? $", Due: {parsedDueDate:yyyy-MM-dd}" : "");
    }

    [KernelFunction, Description("Lists all tasks for a session")]
    public string ListTasks(
      [Description("The session ID to list tasks for")] string sessionId,
    [Description("Filter: all, pending, or completed")] string filter = "all")
    {
  if (!_tasks.ContainsKey(sessionId) || _tasks[sessionId].Count == 0)
        {
  return "No tasks found for this session.";
        }

        var tasks = _tasks[sessionId];

  tasks = filter.ToLower() switch
        {
  "pending" => tasks.Where(t => !t.IsCompleted).ToList(),
         "completed" => tasks.Where(t => t.IsCompleted).ToList(),
            _ => tasks
 };

        if (tasks.Count == 0)
        {
            return $"No {filter} tasks found.";
     }

        var result = $"Tasks ({filter}):\n";
        foreach (var task in tasks.OrderBy(t => t.DueDate).ThenBy(t => t.Priority))
{
     var status = task.IsCompleted ? "?" : "?";
          var due = task.DueDate.HasValue ? $" | Due: {task.DueDate:yyyy-MM-dd}" : "";
     result += $"{status} [{task.Id}] {task.Description} ({task.Priority}){due}\n";
        }

     return result.TrimEnd();
  }

    [KernelFunction, Description("Marks a task as completed")]
    public string CompleteTask(
        [Description("The session ID")] string sessionId,
  [Description("The task ID to complete")] string taskId)
    {
 if (!_tasks.ContainsKey(sessionId))
        {
            return "No tasks found for this session.";
        }

        var task = _tasks[sessionId].FirstOrDefault(t => t.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));
        if (task == null)
{
         return $"Task with ID '{taskId}' not found.";
  }

    task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;
    return $"Task '{task.Description}' marked as completed!";
    }

    [KernelFunction, Description("Deletes a task")]
    public string DeleteTask(
 [Description("The session ID")] string sessionId,
        [Description("The task ID to delete")] string taskId)
    {
        if (!_tasks.ContainsKey(sessionId))
     {
            return "No tasks found for this session.";
        }

     var task = _tasks[sessionId].FirstOrDefault(t => t.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));
        if (task == null)
        {
      return $"Task with ID '{taskId}' not found.";
        }

        _tasks[sessionId].Remove(task);
        return $"Task '{task.Description}' deleted successfully!";
    }

    private class TaskItem
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "medium";
   public DateTime CreatedAt { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
