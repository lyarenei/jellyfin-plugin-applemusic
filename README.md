<div align=center>
<h1>Apple Music <i>metadata</i> plugin for Jellyfin</h1>
<img alt="Plugin Banner" width=50% height=50% src="https://raw.githubusercontent.com/lyarenei/jellyfin-plugin-itunes/master/image.png?sanitize=true"/>

[![Build](https://github.com/lyarenei/jellyfin-plugin-itunes/actions/workflows/build.yaml/badge.svg)](https://github.com/lyarenei/jellyfin-plugin-itunes/actions/workflows/build.yaml)
[![GPLv3 License](https://img.shields.io/github/license/lyarenei/jellyfin-plugin-itunes.svg)](https://github.com/lyarenei/jellyfin-plugin-itunes)
[![Current Release](https://img.shields.io/github/release/lyarenei/jellyfin-plugin-itunes.svg)](https://github.com/lyarenei/jellyfin-plugin-itunes/releases)

</div>

## About
This plugin scrapes metadata from Apple Music and saves them in Jellyfin database.

Currently supported features:
* Album image and metadata provider
* Artist image and metadata provider

Please note that since the plugin must do a search like a person does (instead of direct ID matching like for example MusicBrainz plugin does),
the results can be a hit or miss.

Because of that, personally, I'd only recommend to use this plugin manually or at most as a backup to more 'reliable' sources,
especially if you have somewhat less known stuff in your library.

This, however, mostly applies only to automatic identification.
Once there are `Apple Music` IDs available in the metadata, the plugin will use them for getting/refreshing metadata.

Note: For localized metadata, the plugin uses metadata **region** instead of language, as Apple Music uses country
codes in the web URLs.

## Installation

As always, the plugin can be installed via repository or manually.

#### Repository

The plugin is available on `https://repo.xkrivo.net/jellyfin/manifest.json`.

Head over to `Repositories` tab in Jellyfin server settings > Plugins (advanced section), and add the repository there.

#### Manual

To install the plugin manually, either compile the plugin yourself or grab a release from the
[releases page](https://github.com/lyarenei/jellyfin-plugin-itunes/releases).

For more details on how to build and install the plugin, check out the [Build section](#build).

## Configuration

This plugin has no configuration (at least right now). Once you install it, it's ready to use.

However, don't forget to enable `Apple Music` sources in library settings.

## Development

There is nothing special to set up. You obviously need the [.Net 8.x SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed on your system.
Then clone the repo, open it in your favorite editor, restore dependencies and you should be good to go.
Debugging requires additional setup - documented in the [jellyfin plugin template](https://github.com/jellyfin/jellyfin-plugin-template#6-set-up-debugging).

### Build

Run `dotnet publish --configuration Release --output bin` command.
After successfully compiling, you'll find few files in the output directory.

To install the plugin, copy the following files into the `${CONFIG_DIR}/plugins/apple-music` directory (create it first):
- Jellyfin.Plugin.ITunes.dll
- AngleSharp.dll
- AngleSharp.XPath.dll

After restarting Jellyfin, the plugin should be recognized by the server and active.

## Licence

The plugin code and packages are distributed under the GPLv3 License. See [LICENSE](./LICENSE) for more information.

Per the licence requirements, the following modifications were made:
- Plugin code restructure/cleanup/refactor
- Use Apple Music for search instead of iTunes JSON API
- Support for fetching metadata instead of only images
