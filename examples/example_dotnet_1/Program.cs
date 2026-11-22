using System;
using System.IO;

class Program
{
    static void Main()
    {
        // Read the calculation from files/file.txt
        string filePath = Path.Combine("files", "file.txt");
        string calculation = File.ReadAllText(filePath).Trim();

        // Parse and calculate the expression
        int result = Calculate(calculation);

        Console.WriteLine($"The result of the calculation is: {result}");
    }

    static int Calculate(string expression)
    {
        // Simple parser for expressions like "15*8"
        expression = expression.Replace(" ", "");

        if (expression.Contains("*"))
        {
            var parts = expression.Split('*');
            return int.Parse(parts[0]) * int.Parse(parts[1]);
        }
        if (expression.Contains("/"))
        {
            var parts = expression.Split('/');
            return int.Parse(parts[0]) / int.Parse(parts[1]);
        }
        if (expression.Contains("+"))
        {
            var parts = expression.Split('+');
            return int.Parse(parts[0]) + int.Parse(parts[1]);
        }
        if (expression.Contains("-"))
        {
            var parts = expression.Split('-');
            return int.Parse(parts[0]) - int.Parse(parts[1]);
        }

        return int.Parse(expression);
    }
}
