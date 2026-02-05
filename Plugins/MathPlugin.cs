using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgenticBot.Plugins;

/// <summary>
/// Plugin providing mathematical calculations to the agent.
/// </summary>
public class MathPlugin
{
  [KernelFunction, Description("Adds two numbers together")]
    public double Add(
        [Description("First number")] double a,
        [Description("Second number")] double b)
    {
 return a + b;
    }

    [KernelFunction, Description("Subtracts the second number from the first")]
    public double Subtract(
      [Description("First number")] double a,
    [Description("Second number")] double b)
    {
        return a - b;
    }

    [KernelFunction, Description("Multiplies two numbers")]
    public double Multiply(
    [Description("First number")] double a,
        [Description("Second number")] double b)
    {
        return a * b;
    }

    [KernelFunction, Description("Divides the first number by the second")]
    public string Divide(
        [Description("Dividend (number to be divided)")] double a,
        [Description("Divisor (number to divide by)")] double b)
    {
      if (b == 0)
        {
 return "Error: Cannot divide by zero";
        }
     return (a / b).ToString();
    }

    [KernelFunction, Description("Calculates the percentage of a number")]
 public double Percentage(
        [Description("The base number")] double number,
      [Description("The percentage to calculate")] double percent)
    {
        return (number * percent) / 100;
    }

    [KernelFunction, Description("Calculates the square root of a number")]
    public string SquareRoot([Description("The number to find the square root of")] double number)
    {
 if (number < 0)
      {
          return "Error: Cannot calculate square root of a negative number";
        }
        return Math.Sqrt(number).ToString();
    }

    [KernelFunction, Description("Raises a number to a power")]
    public double Power(
      [Description("The base number")] double baseNumber,
   [Description("The exponent")] double exponent)
 {
     return Math.Pow(baseNumber, exponent);
    }
}
