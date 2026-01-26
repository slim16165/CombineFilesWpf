using System;
using System.Collections.Generic;
using System.CommandLine;

namespace CombineFiles.ConsoleApp.Extensions;

/// <summary>
/// Builder per la creazione di Option con un'interfaccia fluida che distingue alias lunghi e corti.
/// </summary>
public class OptionBuilder<T>
{
    private string? _longAlias;
    private string? _shortAlias;
    private string _description = "";
    private T _defaultValue = default!;
    private bool _hasDefaultValue = false;

    /// <summary>
    /// Imposta l'alias lungo. È obbligatorio, ed è formattato automaticamente per iniziare con "--".
    /// </summary>
    public OptionBuilder<T> WithLongAlias(string longAlias)
    {
        _longAlias = FormatLongAlias(longAlias);
        return this;
    }

    /// <summary>
    /// Imposta l'alias corto. Viene formattato automaticamente per iniziare con "-".
    /// </summary>
    public OptionBuilder<T> WithShortAlias(string shortAlias)
    {
        _shortAlias = FormatShortAlias(shortAlias);
        return this;
    }

    /// <summary>
    /// Imposta la descrizione dell'opzione.
    /// </summary>
    public OptionBuilder<T> WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Imposta il valore di default, abilitando il default.
    /// </summary>
    public OptionBuilder<T> WithDefaultValue(T defaultValue)
    {
        _defaultValue = defaultValue;
        _hasDefaultValue = true;
        return this;
    }

    /// <summary>
    /// Costruisce l'istanza di Option combinando gli alias e applicando le impostazioni.
    /// </summary>
    public Option<T> Build()
    {
        var aliases = BuildAliasList();
        // In System.CommandLine 2.0+, il costruttore accetta params string[] per gli alias
        // Usiamo reflection per chiamare il costruttore con params
        Option<T> option;
        var constructor = typeof(Option<T>).GetConstructor(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string[]) },
            null);
        
        if (constructor != null)
        {
            // Costruttore che accetta string[]
            option = (Option<T>)constructor.Invoke(new object[] { aliases.ToArray() });
        }
        else
        {
            // Fallback: crea con il primo alias e aggiungi gli altri tramite Aliases collection
            option = new Option<T>(aliases[0])
            {
                Description = _description
            };
            // Aggiungi gli altri alias
            for (int i = 1; i < aliases.Count; i++)
            {
                // In System.CommandLine 2.0+, gli alias si aggiungono tramite la proprietà Aliases
                var aliasesProperty = typeof(Option<T>).BaseType?.GetProperty("Aliases");
                if (aliasesProperty != null && aliasesProperty.GetValue(option) is System.Collections.Generic.ICollection<string> aliasesCollection)
                {
                    aliasesCollection.Add(aliases[i]);
                }
            }
        }
        
        option.Description = _description;
        
        // Imposta il default value se specificato (System.CommandLine 2.0+ usa DefaultValueFactory)
        if (_hasDefaultValue)
        {
            option.DefaultValueFactory = _ => _defaultValue;
        }
        
        return option;
    }

    /// <summary>
    /// Costruisce la lista degli alias, includendo sia quello lungo che quello corto se specificato.
    /// Se c'è una collisione (alias duplicato), lo short alias non viene aggiunto.
    /// </summary>
    private List<string> BuildAliasList()
    {
        var aliases = new List<string>();

        if (!string.IsNullOrEmpty(_longAlias))
        {
            aliases.Add(_longAlias);
            // Aggiunge anche la versione in minuscolo, se diversa
            var lowerAlias = _longAlias.ToLowerInvariant();
            if (!aliases.Contains(lowerAlias))
                aliases.Add(lowerAlias);
        }
        else
        {
            throw new InvalidOperationException("L'alias lungo è obbligatorio.");
        }

        if (!string.IsNullOrEmpty(_shortAlias))
        {
            if (!aliases.Contains(_shortAlias))
                aliases.Add(_shortAlias);
        }

        return aliases;
    }


    /// <summary>
    /// Formatta l'alias lungo per assicurarsi che inizi con "--".
    /// </summary>
    private static string FormatLongAlias(string alias)
    {
        // Se l'alias inizia già con "-" (sia uno che due), restituisce l'alias così com'è.
        if (alias.StartsWith("-"))
            return alias;
        return "--" + alias;
    }


    /// <summary>
    /// Formatta lo short alias per assicurarsi che inizi con "-".
    /// </summary>
    private static string FormatShortAlias(string alias)
    {
        return alias.StartsWith("-") ? alias : "-" + alias;
    }
}

/// <summary>
/// Metodo helper per iniziare la costruzione dell'opzione in modo fluente.
/// </summary>
public static class OptionBuilder
{
    public static OptionBuilder<T> For<T>(string longAlias)
    {
        return new OptionBuilder<T>().WithLongAlias(longAlias);
    }
}