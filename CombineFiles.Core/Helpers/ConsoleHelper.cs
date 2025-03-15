using System;

namespace CombineFiles.Core.Helpers;

public static class ConsoleHelper
{
    /// <summary>
    /// Scrive un messaggio in console con il colore specificato e poi resetta il colore.
    /// </summary>
    /// <param name="message">Il messaggio da scrivere.</param>
    /// <param name="color">Il colore da usare.</param>
    public static void WriteColored(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}