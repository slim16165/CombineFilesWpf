using System;
using System.Collections.Generic;
using System.IO;

namespace CombineFiles.Core
{
    public static class FileProcessor
    {
        /// <summary>
        /// Metodo per ottenere la lista di file validi, su cui operare.
        /// Puoi estenderlo con filtri, regex, estensioni, etc.
        /// </summary>
        public static List<string> GetFilesToProcess(string[] selectedFiles)
        {
            // Qui potresti replicare la logica del tuo script PowerShell
            // che filtra in base a estensione, dimensione, excludePaths, ecc.
            // Per semplicità, in questo esempio li ritorniamo "as is".
            // Eventuali recursion su directory o filtri vanno aggiunti qui.

            var validFiles = new List<string>();

            foreach (var file in selectedFiles)
            {
                if (File.Exists(file))
                {
                    // Applica filtri personalizzati, se necessario
                    validFiles.Add(file);
                }
            }

            return validFiles;
        }

        /// <summary>
        /// Esempio di metodo che unisce i contenuti di una lista di file.
        /// </summary>
        public static string CombineContents(List<string> files)
        {
            // Qui puoi decidere come unire i file
            // Nel PowerShell si aggiungeva "### NomeFile ###" prima del contenuto, ecc.
            // Qui puoi anche prevedere la conversione CSV->Tabella, JSON->Formattato, etc.

            using (var writer = new StringWriter())
            {
                foreach (var file in files)
                {
                    writer.WriteLine($"### {Path.GetFileName(file)} ###");
                    try
                    {
                        string content = File.ReadAllText(file);
                        writer.WriteLine(content);
                        writer.WriteLine(); // Riga vuota di separazione
                    }
                    catch (Exception ex)
                    {
                        // Gestione eccezioni di lettura
                        writer.WriteLine($"[ERRORE: impossibile leggere {file} - {ex.Message}]");
                    }
                }

                return writer.ToString();
            }
        }

        /// <summary>
        /// Esempio di metodo che formatta un contenuto CSV come tabella (semplificato).
        /// </summary>
        public static string ConvertCsvToTable(string csvContent)
        {
            // Esempio puramente dimostrativo
            var lines = csvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new StringWriter();

            foreach (var line in lines)
            {
                var columns = line.Split(',');
                result.WriteLine(string.Join(" | ", columns));
            }

            return result.ToString();
        }

        public static string PrettyPrintJson(string content)
        {
            throw new NotImplementedException();
        }
    }
}
