## v1.15 (2nd Nov, 2025)

>[!IMPORTANT]
> I am aware of some issues in Xenia, like Ban Sync records not being visible (ones that were created before 2025).
> This should be fixed in the next update, since it's more of a database issue.
>
> In the "reborn" branch, this is mostly mitigated by using an actual database like PostgreSQL instead of MongoDB.
> To help with this transition to a new DB engine, I will will slowly move features across to the PostgreSQL instead of MongoDB.
>
> This may take a while to do since I have a full-time job and I spend a lot of my spare time working on [Open Fortress](https://openfortress.fun). When it comes to what projects are more important, Xenia has taken a bit of a backseat since it's fairly stable, and only requires some occasional maintenance.

- Replaced custom logging system with [NLog](https://nlog-project.org/) 6.x
- Use GZip Compression with Web Panel (for images, styles, javascript, and fonts)
- Removed e621/e926 commands
- The package `fonts-recommended` is now installed in bot/web panel dockerfile.
  - This should fix non-ASCII characters like Emoji's and Kanji looking like boxes with hexadecimal characters in them.
