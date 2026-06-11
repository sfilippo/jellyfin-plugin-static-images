using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.StaticImages.Configuration;

/// <summary>
/// Static Images plugin configuration.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ImageRootDirectory = "/custom-jellyfin-images";
    }

    /// <summary>
    /// Gets or sets the directory containing image-type subdirectories.
    /// </summary>
    public string ImageRootDirectory { get; set; }
}
