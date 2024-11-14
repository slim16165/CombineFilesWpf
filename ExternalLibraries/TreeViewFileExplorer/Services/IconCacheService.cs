using System;
using System.Collections.Concurrent;
using System.Windows.Media;
using TreeViewFileExplorer.Enums;

namespace TreeViewFileExplorer.Services;

/// <summary>
/// Provides caching functionality for file system icons.
/// </summary>
public class IconCacheService : IDisposable
{
    private readonly ConcurrentDictionary<string, WeakReference<ImageSource>> _iconCache;
    private bool _disposed;

    public IconCacheService()
    {
        _iconCache = new ConcurrentDictionary<string, WeakReference<ImageSource>>();
    }

    /// <summary>
    /// Gets an icon from the cache or returns null if not found.
    /// </summary>
    public ImageSource GetIcon(string path, ItemType type, IconSize size, ItemState state)
    {
        var cacheKey = GenerateCacheKey(path, type, size, state);
        
        if (_iconCache.TryGetValue(cacheKey, out var weakRef))
        {
            if (weakRef.TryGetTarget(out var icon))
            {
                //Logger.Trace($"Icon cache hit for {cacheKey}");
                return icon;
            }
            else
            {
                // Remove dead reference
                _iconCache.TryRemove(cacheKey, out _);
            }
        }

        return null;
    }

    /// <summary>
    /// Adds an icon to the cache.
    /// </summary>
    public void AddIcon(string path, ItemType type, IconSize size, ItemState state, ImageSource icon)
    {
        var cacheKey = GenerateCacheKey(path, type, size, state);
        _iconCache.TryAdd(cacheKey, new WeakReference<ImageSource>(icon));
    }

    /// <summary>
    /// Clears the icon cache.
    /// </summary>
    public void ClearCache()
    {
        _iconCache.Clear();
    }

    private static string GenerateCacheKey(string path, ItemType type, IconSize size, ItemState state)
    {
        return $"{type}_{size}_{state}_{path.ToLowerInvariant()}";
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ClearCache();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
