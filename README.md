# Shortcake Discord Bot
Another General-purpose Discord Bot for the masses.

## Setup Development Environment

There are a few things that are required to setup a development environment for Shortcake
- .NET 6.0 SDK+Runtime
- MongoDB Server (Preferably in Docker)
- Discord Server to test

### Installing .NET SDK
If you are using Visual Studio 20XX, make sure that `.NET desktop development` is selected under the `Desktop & Mobile` section of the installer. If you're not sure, open up Visual Studio Installer and click on the `Modify` button on your installed version of Visual Studio.

![screen recording](https://res.kate.pet/upload/fa204728-ccf1-4a6e-8f70-abf120eb5c49/setup_18x4FGb9x3.gif)

Once you've installed the .NET SDK you can launch the Visual Studio solution.

On Linux machines, install the runtime and SDK packages for .NET 6.0 package through Microsoft's Repository. See [this guide](https://learn.microsoft.com/en-us/dotnet/core/install/linux).

### Setup MongoDB Server
To setup the MongoDB server with Docker run the following command.

```
docker run -p 27020:27017 --name mongodb-shortcake -d -e MONGO_INITDB_ROOT_USERNAME=user -e MONGO_INITDB_ROOT_PASSWORD=password mongo
```

It will set set the username and password to match the following connection URL
```
mongodb://user:password@localhost:27020
```

This should be good enough for local testing.

### First Run

When you first start Shortcake, it will definitely not work at all. Make sure that the following items are set in `config.json` which is usually located at `Shortcake.Core/bin/Debug/config.json` relative to the solution directory.


## `config.json` Description
| Key | Description |
| --- | ----------- |
| `DiscordToken` | Discord Token from the Developer Portal |
| `DeveloperMode` | Restrict all command handling to `DeveloperMode_Server`, only works for attribute-based commands |
| `UserWhitelistEnable` | Restrict all command handling to users defined in `UserWhitelist`, only works for attribute-based commands |
| `Prefix` | Message prefix for text-based commands |
| `ErrorChannel` | Text Channel where errors get reported to |
| `ErrorGuild` | Guild where `ErrorChannel` is in |
| `MongoDBServer` | MongoDB Connection URL, same thing you'd put into MongoDB Compass |
