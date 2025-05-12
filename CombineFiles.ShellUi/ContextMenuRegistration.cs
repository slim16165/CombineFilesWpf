using System;
using Microsoft.Win32;

namespace CombineFiles.ShellUi;

[Obsolete("Al momento non è usato")]
public static class ContextMenuRegistration
{
    private const string MenuName = "*\\shell\\Copia con CombineFiles";
    private const string CommandName = "*\\shell\\Copia con CombineFiles\\command";

    public static void Register(string exePath)
    {
        try
        {
            using (var key = Registry.ClassesRoot.CreateSubKey(MenuName))
            {
                key.SetValue("", "Copia con CombineFiles");
            }

            using (var keyCmd = Registry.ClassesRoot.CreateSubKey(CommandName))
            {
                // %* permette di passare più file come argomenti in certe circostanze
                // Se riscontri problemi con la multi-selezione, valuta comandi COM
                string command = $"\"{exePath}\" \"%1\"";
                keyCmd.SetValue("", command);
            }
        }
        catch (Exception ex)
        {
            // Gestire l'eccezione
            throw;
        }
    }

    public static void Unregister()
    {
        try
        {
            Registry.ClassesRoot.DeleteSubKeyTree(MenuName, false);
        }
        catch
        {
            // Ignora errori se la chiave non esiste
        }
    }
}