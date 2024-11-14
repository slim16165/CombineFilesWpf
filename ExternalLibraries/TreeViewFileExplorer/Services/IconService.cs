using System;
using System.Collections.Concurrent;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TreeViewFileExplorer.Enums;
using TreeViewFileExplorer.Managers;

namespace TreeViewFileExplorer.Services
{
    /// <summary>
    /// Service for retrieving icons as ImageSource objects with caching.
    /// </summary>
    public class IconService : IIconService
    {
        private readonly ShellManager _shellManager;
        private readonly ConcurrentDictionary<string, ImageSource> _iconCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="IconService"/> class.
        /// </summary>
        /// <param name="shellManager">An instance of <see cref="ShellManager"/>.</param>
        public IconService(ShellManager shellManager)
        {
            _shellManager = shellManager;
            _iconCache = new ConcurrentDictionary<string, ImageSource>();
        }

        /// <inheritdoc/>
        public ImageSource GetIcon(string path, ItemType type, IconSize size, ItemState state)
        {
            string cacheKey = $"{type}_{System.IO.Path.GetExtension(path).ToLower()}_{size}_{state}";

            if (_iconCache.TryGetValue(cacheKey, out ImageSource cachedIcon))
            {
                return cachedIcon;
            }

            try
            {
                using (var icon = ShellManager.GetIcon(path, type, size, state))
                {
                    int width = size == IconSize.Small ? 16 : 32;
                    int height = size == IconSize.Small ? 16 : 32;

                    ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(width, height));

                    _iconCache.TryAdd(cacheKey, imageSource);
                    return imageSource;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
