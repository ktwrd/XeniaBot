# Skid Discord Bot
Another General-purpose Discord Bot for the masses.

- [Setup Development Environment](#setup-development-environment)
- [`config.json` Description](#config-description)
- [Module Demos](#module-demos)

## Setup Development Environment

There are a few things that are required to setup a development environment for Skid
- [Privileged Gateway Intents](#privileged-gateway-intents)
- [.NET 6.0 SDK+Runtime](#installing-net-sdk)
- [MongoDB Server (Preferably in Docker)](#setup-mongodb-server)
- Discord Server to test (optional)
- [First Run](#first-run)

### Privileged Gateway Intents
On the Discord Developer Portal make sure that `Server Members Intent` and `Message Content Intent` is enabled under the `Bot` section of your application. It should look like the following screenshot

![screenshot](https://res.kate.pet/upload/f8da69ab-1d8b-4b95-9f4c-541b8bee953f/firefox_rhTUUBcWwc.png)

### Installing .NET SDK
If you are using Visual Studio 20XX, make sure that `.NET desktop development` is selected under the `Desktop & Mobile` section of the installer. If you're not sure, open up Visual Studio Installer and click on the `Modify` button on your installed version of Visual Studio.

![screen recording](https://res.kate.pet/upload/fa204728-ccf1-4a6e-8f70-abf120eb5c49/setup_18x4FGb9x3.gif)

Once you've installed the .NET SDK you can launch the Visual Studio solution.

On Linux machines, install the runtime and SDK packages for .NET 6.0 package through Microsoft's Repository. See [this guide](https://learn.microsoft.com/en-us/dotnet/core/install/linux).

### Setup MongoDB Server
To setup the MongoDB server with Docker run the following command.

```
docker run -p 27020:27017 --name mongodb-skid -d -e MONGO_INITDB_ROOT_USERNAME=user -e MONGO_INITDB_ROOT_PASSWORD=password mongo
```

It will set set the username and password to match the following connection URL
```
mongodb://user:password@localhost:27020
```

This should be good enough for local testing.

### First Run

When you first start Skid, it will definitely not work at all. Make sure that the following items are set in `config.json` which is usually located at `SkidBot.Core/bin/Debug/config.json` relative to the solution directory.

## Config Description
| Key | Required | Default Value | Description |
| --- | -------- | ------------- | ----------- |
| `DiscordToken` | ✔️ | `""` | Discord Token from the Developer Portal |
| `DeveloperMode` | ❌ | `false` | Restrict all command handling to `DeveloperMode_Server`, only works for attribute-based commands |
| `UserWhitelistEnable` | ❌ | `false` | Restrict all command handling to users defined in `UserWhitelist`, only works for attribute-based commands |
| `UserWhitelist` | ❌ | `[]` | Array of user snowflakes that can use the bot when `UserWhitelistEnable` is true.
| `Prefix` | ✔️ | `sk.` | Message prefix for text-based commands |
| `ErrorChannel` | ✔️ | `0` | Text Channel where errors get reported to |
| `ErrorGuild` | ✔️ | `0` | Guild where `ErrorChannel` is in |
| `MongoDBServer` | ✔️ | `""` | MongoDB Connection URL, same thing you'd put into MongoDB Compass |

# Module Demos
## Confession Module
![screen recording](https://res.kate.pet/upload/03bcb777-911d-4774-9454-523b3b238267/DiscordCanary_S5Wm6jtwOd.gif)
## Random Animal
![screen recording](https://res.kate.pet/upload/fd22bbc7-2ec1-4f71-9b28-bf23c0aafdca/DiscordCanary_y05soKK3fv.gif)
