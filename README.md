# Jellyfin iTunes Plugin

<p align="center">
<img alt="Plugin Banner" src="https://raw.githubusercontent.com/Shadowghost/jellyfin-plugin-itunes/master/image.png?sanitize=true"/>
</p>

[![🏗️ Build Plugin](https://github.com/Shadowghost/jellyfin-plugin-itunes/actions/workflows/build.yaml/badge.svg)](https://github.com/Shadowghost/jellyfin-plugin-itunes/actions/workflows/build.yaml)
[![GPLv3 License](https://img.shields.io/github/license/Shadowghost/jellyfin-plugin-itunes.svg)](https://github.com/Shadowghost/jellyfin-plugin-itunes)
[![Current Release](https://img.shields.io/github/release/Shadowghost/jellyfin-plugin-itunes.svg)](https://github.com/Shadowghost/jellyfin-plugin-itunes/releases)

## About
Fetches Metadata from iTunes

Currently supported:
* Album Images
* Artist Images

## Installation

[See the official documentation for install instructions](https://jellyfin.org/docs/general/server/plugins/index.html#installing).

## Build

1. To build this plugin you will need [.Net 6.x](https://dotnet.microsoft.com/download/dotnet/6.0).

2. Build plugin with following command
  ```
  dotnet publish --configuration Release --output bin
  ```

3. Place the dll-file in the `plugins/itunes` folder (you might need to create the folders) of your JF install

## Releasing

To release the plugin we recommend [JPRM](https://github.com/oddstr13/jellyfin-plugin-repository-manager) that will build and package the plugin.
For additional context and for how to add the packaged plugin zip to a plugin manifest see the [JPRM documentation](https://github.com/oddstr13/jellyfin-plugin-repository-manager) for more info.

## Contributing

We welcome all contributions and pull requests! If you have a larger feature in mind please open an issue so we can discuss the implementation before you start.
In general refer to our [contributing guidelines](https://github.com/jellyfin/.github/blob/master/CONTRIBUTING.md) for further information.

## Licence

This plugins code and packages are distributed under the GPLv3 License. See [LICENSE](./LICENSE) for more information.
