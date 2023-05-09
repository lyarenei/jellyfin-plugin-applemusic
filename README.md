# Apple Music _metadata_ plugin for Jellyfin

<img alt="Plugin Banner" src="https://raw.githubusercontent.com/lyarenei/jellyfin-plugin-itunes/master/image.png?sanitize=true"/>

[![Build](https://github.com/lyarenei/jellyfin-plugin-itunes/actions/workflows/build.yaml/badge.svg)](https://github.com/lyarenei/jellyfin-plugin-itunes/actions/workflows/build.yaml)
[![GPLv3 License](https://img.shields.io/github/license/lyarenei/jellyfin-plugin-itunes.svg)](https://github.com/lyarenei/jellyfin-plugin-itunes)
[![Current Release](https://img.shields.io/github/release/lyarenei/jellyfin-plugin-itunes.svg)](https://github.com/lyarenei/jellyfin-plugin-itunes/releases)

## About
This plugin scrapes metadata from Apple Music and saves them in Jellyfin database.

Currently supported features:
* Album image and metadata provider
* Artist image and metadata provider

## Installation

There are no releases yet, as I wanted to focus on the functionality and evaluate how much would be possible to implement with Apple Music scraping.
Once the plugin will be ready for general use, I'll provide automated releases and a Jellyfin repository to install from.

With that said, you can always compile and install the plugin manually. Check out the [Build section](#build) for more details.

## Configuration

This plugin has no configuration (at least right now). Once you install it, it's ready to use.

However, don't forget to enable `Apple Music` sources in library settings.

## Development

There is nothing special to set up. You obviously need the [.Net 6.x SDK](https://dotnet.microsoft.com/download/dotnet/6.0) installed on your system.
Then clone the repo, open it in your favorite editor, restore dependencies and you should be good to go.
Debugging requires additional setup - documented in the [jellyfin plugin template](https://github.com/jellyfin/jellyfin-plugin-template#6-set-up-debugging).

### Build

Run `dotnet publish --configuration Release --output bin` command.
After successfully compiling, you'll find few files in the output directory.

To install the plugin, copy the following files into the `${CONFIG_DIR}/plugins/itunes` directory (create it first):
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
