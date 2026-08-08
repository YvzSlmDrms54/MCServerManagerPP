# MCServerManager++

A Windows desktop app for running a Minecraft Java Edition server — start, stop, back up, and monitor it from one window, with Discord alerts and player management built in.

🔗 **[Website](https://yvzslmdrms54.github.io/MCServerManagerPP/)** · **[Documentation](https://yvzslmdrms54.github.io/MCServerManagerPP/docs.html)** · **[Download](https://yvzslmdrms54.github.io/MCServerManagerPP/download.html)**

![MCServerManager++](docs/assets/icon.png)

## What it does

- **Live console** — watch server output in real time, send commands back without a terminal
- **Automatic server detection** — recognizes Vanilla, PaperMC, Fabric, and Forge on its own
- **Scheduled & on-demand backups** — daily backups plus one automatically before every shutdown
- **Discord webhooks** — server start/stop, player join/leave, backups, crashes, maintenance mode — each with its own channel and optional mention
- **Player management** — OP, whitelist, and ban lists from one panel
- **Maintenance mode** — lock the server down without shutting it off, restores your MOTD and whitelist state when you're done
- **Scheduled tasks** — automatic daily restarts and maintenance windows
- **Crash detection** — a server that dies unexpectedly is logged clearly, both in the console and in Discord
- **Bilingual** — Turkish and English, switchable on first launch
- **System tray** — closing the window doesn't stop the server

## Requirements

| Requirement | Details |
|---|---|
| OS | Windows 10 or later, 64-bit |
| .NET Runtime | [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Java | A runtime matching your Minecraft server version |
| Minecraft server | Your own server files — Vanilla, Paper, Fabric, or Forge |

## Getting started

1. Download the portable build from the [releases page](https://github.com/YvzSlmDrms54/MCServerManagerPP/releases/latest) and extract it anywhere.
2. Run `MCServerManagerPP.exe` once. A `server` folder appears next to it.
3. Copy your Minecraft server files — the `.jar` and an accepted `eula.txt` — into that `server` folder.
4. Press **Start**.

Full setup and feature walkthroughs are in the [documentation](https://yvzslmdrms54.github.io/MCServerManagerPP/docs.html).

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/YvzSlmDrms54/MCServerManagerPP.git
cd MCServerManagerPP
dotnet build
dotnet run
```

## Tech

WPF on .NET 10, C#. No external dependencies beyond the .NET and Windows Forms desktop runtimes (used for the system tray icon).

## License

MIT — see [LICENSE](LICENSE).

## About

Built as part of the [MyBetaSoft](https://github.com/MyBetaSoft) project family.
