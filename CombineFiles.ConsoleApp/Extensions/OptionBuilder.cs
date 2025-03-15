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
        Option<T> option = _hasDefaultValue
            ? new Option<T>(aliases.ToArray(), () => _defaultValue, _description)
            : new Option<T>(aliases.ToArray(), _description);
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