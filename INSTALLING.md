- [Setup Development Environment](#setup-development-environment)
- [`config.json` Description](#config-description)
- [Environment Variables](#environment-variables)


# Setup Development Environment

There are a few things that are required to setup a development environment for Xenia
- [Privileged Gateway Intents](#privileged-gateway-intents)
- [.NET 7.0 SDK+Runtime](#installing-net-sdk)
- [MongoDB Server (Preferably in Docker)](#setup-mongodb-server)
- Discord Server to test (optional)
- [First Run](#first-run)
- [Setup Google Cloud Credentials](#setup-google-cloud-credentials)

## Privileged Gateway Intents
On the Discord Developer Portal make sure that `Server Members Intent` and `Message Content Intent` is enabled under the `Bot` section of your application. It should look like the following screenshot

![screenshot](https://res.kate.pet/upload/f8da69ab-1d8b-4b95-9f4c-541b8bee953f/firefox_rhTUUBcWwc.png)

## Installing .NET SDK
If you are using Visual Studio 2022/2019, make sure that `.NET desktop development` is selected under the `Desktop & Mobile` section of the installer. If you're not sure, open up Visual Studio Installer and click on the `Modify` button on your installed version of Visual Studio.

![screen recording](https://res.kate.pet/upload/fa204728-ccf1-4a6e-8f70-abf120eb5c49/setup_18x4FGb9x3.gif)

Once you've installed the .NET SDK you can launch the Visual Studio solution.

On Linux machines, install the runtime and SDK packages for .NET 7.0 package through Microsoft's Repository. See [this guide](https://learn.microsoft.com/en-us/dotnet/core/install/linux).

## Setup MongoDB Server
To setup the MongoDB server with Docker run the following command.

```
docker run -p 27020:27017 --name mongodb-xenia-discord -d -e MONGO_INITDB_ROOT_USERNAME=user -e MONGO_INITDB_ROOT_PASSWORD=password mongo
```

It will set set the username and password to match the following connection URL
```
mongodb://user:password@localhost:27020
```

This should be good enough for local testing.

## First Run

When you first start Xenia, it will definitely not work at all. Make sure that the following items are set in `config.json` which is usually located at `XeniaBot.Core/bin/Debug/data/config.json` relative to the solution directory.

### Setup Google Cloud Credentials

Generate your Google Cloud Credentials with [this guide](https://developers.google.com/workspace/guides/create-credentials). Make sure to export them as a JSON.

Once you've done that, create the `GCSKey_Translate` key in your `config.json` and set the value to the content of the JSON.

**Note** The type of the `GCSKey_Translate` must match the type `GoogleCloudKey` in `XeniaBot.Shared/_Config/ConfigData.cs`

# Config Description
| Key                        | Required           | Default Value | Description |
| -------------------------- | ------------------ | ------------- | ----------- |
| `DiscordToken`             | ✔️                  | `""`          | Discord Token from the Developer Portal |
| `DeveloperMode`            | ❌                 | `false`       | Restrict all command handling to `DeveloperMode_Server`, only works for attribute-based commands |
| `DeveloperMode_Server`     | ❌                 | `0`           | Server Id to restrict command handling when `DeveloperMode` is `true` |
| `UserWhitelistEnable`      | ❌                 | `false`       | Restrict all command handling to users defined in `UserWhitelist`, only works for attribute-based commands |
| `UserWhitelist`            | ❌                 | `[]`          | Array of user snowflakes that can use the bot when `UserWhitelistEnable` is true.
| `Prefix`                   | ✔️                  | `"x."`          | Message prefix for text-based commands |
| `ErrorChannel`             | ✔️                  | `0`           | Text Channel where errors get reported to |
| `ErrorGuild`               | ✔️                  | `0`           | Guild where `ErrorChannel` is in |
| `MongoDBServer`            | ✔️                  | `""`          | MongoDB Connection URL, same thing you'd put into MongoDB Compass |
| `BanSync_AdminServer`      | ✔️                  | `0`           | Server Id for the admin ban sync logs |
| `BanSync_GlobalLogChannel` | ✔️                  | `0`           | Global Log Channel for Ban Sync. Must be in `BanSync_AdminServer` |
| `BanSync_RequestChannel`   | ✔️                  | `0`           | Channel Id for where the Ban Sync requests go into. |
| `WeatherAPI_Key`           | ✔️                  | `""`          | API Key for `/weather` commands. See: [https://weatherapi.com](https://weatherapi.com) |
| `GCSKey_Translate`         | ❌                 | `null`        | See [Setting up Google Cloud Credentials](#setup-google-cloud-credentials) |
| `ESix_Username`            | ✔️                  | `""`          | Username for `/esix` commands. See [this Steam guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2841522348) for instructions. |
| `ESix_ApiKey`              | ✔️                  | `""`          | Api Key for `/esix` commands. See [this Steam guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2841522348) for instructions.
| `Prometheus_Enable`        | ✔️                  | `true`        | Enable Prometheus Exporter |
| `Prometheus_Port`          | ✔️                  | `4823`        | Port for Prometheus Exporter |
| `Prometheus_Url`           | ✔️                  | `"/metrics"`  | Base Url for Prometheus Exporter |
| `Prometheus_Hostname`      | ✔️                  | `"+"`         | Hostname to listen Prometheus Exporter on |
| `Invite_Permissions`       | ✔️                  | `415471496311`| Permissions for the `/invite` command. Use the permissions generated with [this website](https://discordapi.com/permissions.html). |
| `AuthentikToken`           | ❌                 | `""`          | Api Token for `/auth` commands. |
| `AuthentikUrl`             | ❌                 | `""`          | Base Url for Authentik server. |
| `AuthentikEnable`          | ❌                 | `false`       | Enable `/auth` commands. |
| `HasDashboard`             | ❌                 | `false`       | Should the bot say that there is a dashboard in the `/dashboard` command |
| `DashboardLocation`        | ✔️                  | `""`          | Dashboard Url that is used in the `/dashboard` command |
| `DiscordBotList_Token`     | ❌                 | `""`          | Token to use when sending available commands to DiscordBotList.com |
| `OAuth_ClientId`           | ✔️ (Dashboard Only) | `""`          | Discord OAuth Client Id |
| `OAuth_ClientSecret`       | ✔️ (Dashboard Only) | `""`          | Discord OAuth Client Secret |

# Environment Variables
| Key | Type | Description |
| --- | ---- | ----------- |
| `LOG_COLOR` | boolean | Enable colors in log output (default: `true`) |
| `CONFIG_LOCATION` | string | Config file location (default: `./data/config.json` ) |
| `DATA_DIR` | string | Data Directory. (default: `./data/`) |
| `DATA_DIR_FONTCACHE` | string | Font cache for MediaManu commands. (default: `./data/fontcache`) |