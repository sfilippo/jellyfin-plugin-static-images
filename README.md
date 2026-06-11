# Jellyfin Static Images

> Chances are that there were easier ways to do this than this, but my OCD
> decided this would've taken short enough to be worth it. Trying vibe-coding
> via Codex, on Windows lol.

`Jellyfin.Plugin.StaticImages` is a local image provider for Jellyfin. It lets
you override artwork for media items by placing image files in a mounted,
read-only directory, named with the item's IMDb or TMDb provider ID.

The plugin never writes to the image root, changes source files, or copies them
to another location. When no matching file exists, it returns no image and
Jellyfin continues with its other configured image providers.

## Compatibility

This version targets Jellyfin server 10.11 and .NET 9. It builds against
`Jellyfin.Controller` and `Jellyfin.Model` 10.11.11.

## Installation

### Jellyfin Catalog

Add this repository in **Dashboard > Plugins > Repositories**:

```text
https://raw.githubusercontent.com/sfilippo/jellyfin-plugin-static-images/main/manifest.json
```

Then open the plugin catalog, install **Static Images**, and restart Jellyfin.

### Manual Installation

1. Build the plugin:

   ```shell
   dotnet restore
   dotnet build --configuration Release
   ```

2. Create a directory under Jellyfin's plugins directory, for example:

   ```text
   plugins/Static Images/
   ```

3. Copy `Jellyfin.Plugin.StaticImages.dll` from
   `Jellyfin.Plugin.StaticImages/bin/Release/net9.0/` into that directory.
4. Restart Jellyfin.
5. Open **Dashboard > Plugins > Static Images** and configure the image root if
   it differs from `/custom-jellyfin-images`.
6. Refresh metadata for affected library items. Enable image replacement in
   the refresh dialog when replacing an image that Jellyfin has already saved.

## Docker

Mount the source directory read-only:

```yaml
services:
  jellyfin:
    volumes:
      - /home/user/jellyfin-custom-images:/custom-jellyfin-images:ro
```

The default plugin setting already points to `/custom-jellyfin-images`.

## Directory Layout

The folder names correspond exactly to Jellyfin image types:

```text
/custom-jellyfin-images/
├── Primary/
│   ├── tt1375666.jpg
│   └── 27205.png
├── Backdrop/
├── Logo/
└── Thumb/
```

Supported extensions, in lookup order:

```text
.jpg
.jpeg
.png
.webp
```

Only one file is selected for each image type and provider ID. For example:

```text
/custom-jellyfin-images/
└── Primary/
    └── tt1375666.jpg
```

provides the primary image for *Inception*.

## Provider IDs

The lookup order is:

1. IMDb, such as `tt1375666`
2. TMDb, such as `27205`

Fallback is evaluated independently for each image type. For example, an IMDb
poster can be used together with a TMDb logo when those are the matching files
available.

IMDb IDs must have the form `tt` followed by digits. TMDb IDs must contain only
digits. Invalid IDs are ignored so they cannot be interpreted as file paths.

## How It Works

The plugin implements Jellyfin's `ILocalImageProvider`. Jellyfin calls
`GetImages` during image discovery, and the provider returns `LocalImageInfo`
objects whose `FileInfo` comes from Jellyfin's `IDirectoryService`.

This interface was chosen instead of `IRemoteImageProvider` because the images
already exist on the Jellyfin server's filesystem. It allows Jellyfin to use
the original path directly and requires no download endpoint, stream copying,
or write access. The provider uses `IHasOrder` with an order of `-100`, so it
runs before Jellyfin's ordinary local image provider unless an administrator
configures a different provider order.

## Limitations

- Images are discovered during Jellyfin metadata/image refreshes, not by a
  filesystem watcher.
- The plugin supports one image per provider ID and image type. Numbered or
  multiple backdrops are not supported.
- Only `Primary`, `Backdrop`, `Logo`, and `Thumb` are supported.
- Provider IDs must already be present on the Jellyfin item.
- Jellyfin controls provider ordering and whether an existing saved image is
  replaced. Use the metadata refresh options when applying an override to an
  item that already has artwork.

## Development

Requirements:

- .NET 9 SDK
- Network access to restore NuGet packages

Build:

```shell
dotnet build Jellyfin.Plugin.StaticImages.sln --configuration Release
```

The output assembly is:

```text
Jellyfin.Plugin.StaticImages/bin/Release/net9.0/Jellyfin.Plugin.StaticImages.dll
```

## Releases

Releases are published by pushing a four-part version tag:

```shell
git tag v1.0.0.0
git push origin v1.0.0.0
```

The release workflow builds the plugin, uploads the ZIP to GitHub Releases,
calculates its MD5 checksum, and updates `manifest.json` for Jellyfin's catalog.
