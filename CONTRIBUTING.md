If you would like to Contribute to Xenia Bot, there are some rough things to go through first.

- [Reporting Issues](#reporting-issues)
- [Project Layout](#project-layout)
- [Programming Style](#programming-style)

## Reporting Issues
We love bug reports, but when creating bug reports please show steps to reproduce it (ideally with a screen recording) or describe what went wrong to cause the issue.

Make sure that you search for your issue before creating a new one to make sure there are no duplicate issues.

If you are encountering an issue with our codebase, please state all possible things that could affect it, for example;
- OS/Distro
- CPU Architecture
- SDK Version
- Storage (on the drive/partition where XeniaBot is located)
- Memory Capacity
- Any known workarounds

If you intend on making a pull request, please refrain from;
- Creating a massive, monolithic, PR
- Creating PR's for style changes
- Committing code that you did not create
- Modifying the license
- Breaking other parts of the codebase.

## Project Layout
Xenia Bot a pawful of major parts
- Core
    * The actual bot
    * Anything that handles commands or storing data (that isn't a config) is done through here.
- Data
    * Data Structures and Controllers that interact with MongoDB
- Discord Cache
    * Used with the Server Logging feature to cache messages/profiles
- Shared
    * Shared helpers/controllers between the Bot and the Dashboard
- Web Panel
    * The actual web-based dashboard.
    * This should only send data if it's calling something that is called from the Bot that does the exact same action

For example, if you want to create a feature where it greets new members when they join a server you would do the following
- Create the config/database models in `XeniaBot.Data/Models`, with the model name of something like `GuildGreeterConfigModel`
- Create a controller in `XeniaBot.Data/Controllers` using the [Config Controller Generator](https://ktwrd.github.io/xenia-discord-configgen.html)
- Implement ability to modify the config in the dashboard
    - A new view should be created for this at `XeniaBot.WebPanel/Views/Server/Details/Settings`
    - Create a method in the ServerController class to handle this new view.
    - Try to use 100% SSR (only exception is data tables and reactive fluff)
- If you wish to add the ability to change/set the config via the Bot;
    - Use slash commands
    - Create a module with a descriptive name, like `GreeterConfigModule` in `XeniaBot.Core/Modules/`

## Programming Style
There are some main things to keep in mind;
- Use [Allman Style](https://en.wikipedia.org/wiki/Indentation_style#Allman_style) braces
- Keep your lines short and precise
- Use `string.Join("\n", ...)` instead of doing `my\n thing\n is\n cool`

Pretty much, just follow the dotnet programming style ([see](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md)).