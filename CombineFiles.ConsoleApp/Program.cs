using System;
using System.Collections.Generic;

namespace CombineFiles.ConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        // Esempio di parametri (equivalenti a: .\Combine-Files.ps1 -Mode 'extensions' -Extensions '.cs','.xaml' -Recurse)
        var options = new CombineFilesOptions
        {
            Mode = "extensions",
            Extensions = new List<string> { ".cs", ".xaml" },
            Recurse = true,
            EnableLog = true
        };

        // Esegui la logica di combinazione
        CombineFilesApp.Execute(options);

        Console.WriteLine("Fine elaborazione");
        Console.ReadLine();
    }
}
