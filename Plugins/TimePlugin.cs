using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgenticBot.Plugins;

/// <summary>
/// Plugin providing time and date functionality to the agent.
/// This demonstrates how agents can use tools to perform actions.
/// </summary>
public class TimePlugin
{
  [KernelFunction, Description("Gets the current date and time in the specified timezone or UTC if not specified")]
    public string GetCurrentTime([Description("The timezone to get time for (e.g., 'Eastern Standard Time', 'Pacific Standard Time'). Defaults to UTC.")] string? timezone = null)
    {
    try
        {
     if (string.IsNullOrEmpty(timezone))
            {
       return $"Current UTC time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
   }

        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
    var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            return $"Current time in {timezone}: {localTime:yyyy-MM-dd HH:mm:ss}";
 }
        catch (TimeZoneNotFoundException)
        {
  return $"Timezone '{timezone}' not found. Current UTC time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        }
 }

    [KernelFunction, Description("Gets the day of the week for today or a specific date")]
    public string GetDayOfWeek([Description("Optional date in yyyy-MM-dd format")] string? date = null)
    {
        if (string.IsNullOrEmpty(date))
        {
            return $"Today is {DateTime.Now.DayOfWeek}";
        }

        if (DateTime.TryParse(date, out var parsedDate))
        {
            return $"{date} is a {parsedDate.DayOfWeek}";
    }

      return "Invalid date format. Please use yyyy-MM-dd format.";
    }

    [KernelFunction, Description("Calculates the number of days between two dates")]
    public string GetDaysBetween(
        [Description("Start date in yyyy-MM-dd format")] string startDate,
        [Description("End date in yyyy-MM-dd format")] string endDate)
  {
        if (!DateTime.TryParse(startDate, out var start))
      {
  return "Invalid start date format. Please use yyyy-MM-dd format.";
        }

        if (!DateTime.TryParse(endDate, out var end))
  {
       return "Invalid end date format. Please use yyyy-MM-dd format.";
   }

        var days = (end - start).Days;
        return $"There are {Math.Abs(days)} days between {startDate} and {endDate}";
    }
}
