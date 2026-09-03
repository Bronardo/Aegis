# Aegis Protocol: Core Defense

[![Badge](https://img.shields.io/badge/GitHub-Bronardo%2FAegis-blue)](https://github.com/Bronardo/Aegis)

A tower-defense game built with **SplashKit** and **C# / .NET 8** for SIT771 Task 7.3D.
Rogue mech units pour along a winding route toward your Power Core. Build and upgrade
defensive turrets to stop them before the core is breached.

## How to run

This project was scaffolded with the SplashKit manager (`skm`), which wires up the
native SplashKit libraries. From this folder:

```bash
skm dotnet build
skm dotnet run
```

`skm` ensures the native SplashKit DLLs are on the library path; you can also open the
folder in VS Code and press **F5** (a launch configuration is provided).

## How to play

| Action | Control |
| --- | --- |
| Start / restart | `Enter` (menu), `R` (game over) |
| Select a turret to build | Click the build buttons in the sidebar (Gatling 50 / Frost 70 / Laser 90) |
| Place the selected turret | Left-click a build tile (dark tiles, not the dirt path) |
| Inspect a turret | Left-click a placed turret |
| Upgrade / Sell | Use the panel that appears when a turret is selected |
| Cancel build / deselect | Right-click or `Esc` |
| Send the next wave | Click **Start Wave** |
| Pause / resume | `P` |
| Fast-forward (2x) | `F` |
| Emergency Repair | Sidebar button (costs credits, restores core) |

- You start with **120 energy credits** and **20 core integrity**.
- Kill mechs for credits and earn a bonus each cleared wave; waves grow endlessly.
- A mech that reaches the core deals damage (1 for scouts, 3 for heavy tanks).
- Turrets cap at **level 3**; selling refunds roughly **60%** of the invested credits.

## Architecture

The code is split by responsibility (following the RDD design):

- `Program.cs` — entry point: opens the window and starts the game.
- `AegisGame.cs` — controller / game loop; routes input, owns the economy and state
  (`Menu`, `Playing`, `Paused`, `GameOver`), and runs a delta-time timer with 2x
  fast-forward support.
- `GridMap.cs` — environment manager: tile grid, snake path waypoints, the Power Core,
  occupancy and screen<->tile conversion.
- `GameObject.cs` — abstract base for battlefield objects.
- `Mech.cs`, `ScoutMech.cs`, `HeavyTankMech.cs` — enemy hierarchy (polymorphic `Draw`,
  waypoint movement, armor-mitigated damage, slow status).
- `WaveManager.cs` — spawns and scales endless waves.
- `Turret.cs`, `GatlingTurret.cs`, `FreezeTurret.cs`, `LaserTurret.cs` — turret hierarchy
  (nearest-target acquisition, upgrades, selling; projectile / AoE-slow / beam attacks).
- `Projectile.cs` — rounds fired by gatling turrets.
- `UIOverlay.cs` — sidebar HUD, build/control buttons, turret panel and overlay screens.
- `Theme.cs` — shared color palette.

All graphics are rendered with SplashKit primitive shapes and text — no image or
audio assets are required. Or to be impleted in 7.4H?
