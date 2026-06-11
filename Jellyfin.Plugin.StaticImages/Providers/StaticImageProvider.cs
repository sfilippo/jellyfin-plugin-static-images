using System.Text.RegularExpressions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.StaticImages.Providers;

/// <summary>
/// Finds local images named after IMDb or TMDb provider IDs.
/// </summary>
public sealed partial class StaticImageProvider : ILocalImageProvider, IHasOrder
{
    private static readonly string[] _supportedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private static readonly (ImageType Type, string DirectoryName)[] _supportedImageTypes =
    [
        (ImageType.Primary, nameof(ImageType.Primary)),
        (ImageType.Backdrop, nameof(ImageType.Backdrop)),
        (ImageType.Logo, nameof(ImageType.Logo)),
        (ImageType.Thumb, nameof(ImageType.Thumb))
    ];

    private readonly ILogger<StaticImageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticImageProvider"/> class.
    /// </summary>
    /// <param name="logger">The Jellyfin logger.</param>
    public StaticImageProvider(ILogger<StaticImageProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Static Images";

    /// <inheritdoc />
    public int Order => -100;

    /// <inheritdoc />
    public bool Supports(BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetProviderIds(item).Count > 0;
    }

    /// <inheritdoc />
    public IEnumerable<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(directoryService);

        var rootDirectory = Plugin.Instance?.Configuration.ImageRootDirectory;
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            yield break;
        }

        var providerIds = GetProviderIds(item);
        if (providerIds.Count == 0)
        {
            yield break;
        }

        foreach (var (imageType, directoryName) in _supportedImageTypes)
        {
            var image = FindImage(
                rootDirectory,
                directoryName,
                imageType,
                providerIds,
                directoryService);

            if (image is not null)
            {
                yield return image;
            }
        }
    }

    private LocalImageInfo? FindImage(
        string rootDirectory,
        string directoryName,
        ImageType imageType,
        IReadOnlyList<string> providerIds,
        IDirectoryService directoryService)
    {
        foreach (var providerId in providerIds)
        {
            foreach (var extension in _supportedExtensions)
            {
                var path = Path.Combine(rootDirectory, directoryName, providerId + extension);
                var file = directoryService.GetFile(path);
                if (file is null || file.IsDirectory || file.Length <= 0)
                {
                    continue;
                }

                _logger.LogInformation(
                    "Found custom image: {ImageType} / {ProviderId}",
                    imageType,
                    providerId);

                return new LocalImageInfo
                {
                    FileInfo = file,
                    Type = imageType
                };
            }
        }

        _logger.LogDebug(
            "No custom image found: {ImageType} / {ProviderIds}",
            imageType,
            string.Join(", ", providerIds));

        return null;
    }

    private static IReadOnlyList<string> GetProviderIds(BaseItem item)
    {
        var providerIds = new List<string>(2);

        if (TryGetProviderId(item, MetadataProvider.Imdb, out var imdbId)
            && ImdbIdRegex().IsMatch(imdbId))
        {
            providerIds.Add(imdbId);
        }

        if (TryGetProviderId(item, MetadataProvider.Tmdb, out var tmdbId)
            && TmdbIdRegex().IsMatch(tmdbId))
        {
            providerIds.Add(tmdbId);
        }

        return providerIds;
    }

    private static bool TryGetProviderId(
        BaseItem item,
        MetadataProvider provider,
        out string providerId)
    {
        if (item.ProviderIds.TryGetValue(provider.ToString(), out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            providerId = value.Trim();
            return true;
        }

        providerId = string.Empty;
        return false;
    }

    [GeneratedRegex("^tt[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ImdbIdRegex();

    [GeneratedRegex("^[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex TmdbIdRegex();
}
