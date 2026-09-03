using System;
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // User interface role (RDD): draws the sidebar HUD, build/control buttons
    // and the selected-turret modal, and routes sidebar clicks back to the game.
    // Presentation only — combat logic stays in the other classes.
    public static class UIOverlay
    {
        private const double SideX = GridMap.GridWidth;     // 640
        private const double SideW = 320;
        private const double BtnH = 34;
        private const double BtnW = 272;
        private const double BtnX = SideX + (SideW - BtnW) / 2;

        // Sidebar button rectangles (laid out top to bottom)
        private static readonly (double y, string id)[] Layout =
        {
            (152, "gatling"),
            (192, "freeze"),
            (232, "laser"),
            (282, "start"),
            (322, "pause"),
            (362, "fast"),
            (402, "repair"),
        };

        // Selected turret panel buttons
        private const double UpgY = 470;
        private const double SellY = 510;

        // ---- Click routing ---------------------------------------------------

        public static void HandleSidebarClick(AegisGame game, Point2D mouse)
        {
            if (mouse.X < SideX) return;

            foreach ((double y, string id) in Layout)
            {
                if (Hit(mouse, y))
                {
                    Activate(game, id);
                    return;
                }
            }

            if (game.SelectedTurret != null)
            {
                if (Hit(mouse, UpgY)) game.UpgradeSelected();
                else if (Hit(mouse, SellY)) game.SellSelected();
            }
        }

        private static void Activate(AegisGame game, string id)
        {
            switch (id)
            {
                case "gatling": game.SelectBuild(TurretType.Gatling); break;
                case "freeze": game.SelectBuild(TurretType.Freeze); break;
                case "laser": game.SelectBuild(TurretType.Laser); break;
                case "start": game.StartWave(); break;
                case "pause": game.TogglePause(); break;
                case "fast": game.ToggleFastForward(); break;
                case "repair": game.EmergencyRepair(); break;
            }
        }

        private static bool Hit(Point2D m, double y) =>
            m.X >= BtnX && m.X <= BtnX + BtnW && m.Y >= y && m.Y <= y + BtnH;

        // ---- Sidebar rendering -----------------------------------------------

        public static void DrawSidebar(AegisGame game, Window window)
        {
            SplashKit.FillRectangle(Theme.Sidebar, SideX, 0, SideW, 640);
            SplashKit.DrawLine(Theme.SidebarEdge, SideX, 0, SideX, 640);

            SplashKit.FillRectangle(Theme.ButtonActive, SideX, 0, SideW, 40);
            SplashKit.DrawText("AEGIS PROTOCOL", Theme.CoreInner, "Arial", 18, BtnX, 11);

            // Core integrity
            SplashKit.DrawText("CORE INTEGRITY", Theme.TextDim, "Arial", 13, BtnX, 52);
            SplashKit.FillRectangle(Theme.HealthBack, BtnX, 72, BtnW, 18);
            double frac = Math.Max(0, game.CoreHealth / 20.0);
            SplashKit.FillRectangle(Theme.Core, BtnX, 72, BtnW * frac, 18);
            SplashKit.DrawText($"{game.CoreHealth} / 20", Theme.Text, "Arial", 14, BtnX + 8, 74);

            // Energy credits
            SplashKit.DrawText("ENERGY CREDITS", Theme.TextDim, "Arial", 13, BtnX, 100);
            SplashKit.DrawText($"{game.Credits}", Theme.Good, "Arial", 22, BtnX + 120, 100);

            // Wave status
            bool waveActive = game.Waves != null && game.Waves.WaveInProgress;
            string waveText = waveActive ? $"WAVE {game.WaveNumber} - INCOMING"
                                         : game.WaveNumber == 0 ? "READY TO DEPLOY"
                                                                : $"WAVE {game.WaveNumber} CLEARED";
            SplashKit.DrawText(waveText, waveActive ? Theme.Bad : Theme.TextDim,
                               "Arial", 13, BtnX, 126);

            DrawBuildButton(152, "GATLING TURRET", GatlingTurret.StaticCost, game, TurretType.Gatling);
            DrawBuildButton(192, "FROST TURRET", FreezeTurret.StaticCost, game, TurretType.Freeze);
            DrawBuildButton(232, "LASER TURRET", LaserTurret.StaticCost, game, TurretType.Laser);

            DrawCtrlButton(282, waveActive ? "WAVE IN PROGRESS" : "START WAVE", enabled: !waveActive);
            DrawCtrlButton(322, game.State == GameState.Paused ? "RESUME [P]" : "PAUSE [P]", enabled: true);
            DrawCtrlButton(362, game.FastForward ? "FAST-FWD: ON [F]" : "FAST-FWD: OFF [F]", enabled: true);

            string repairLabel = game.RepairCooldown > 0
                ? $"REPAIR ({(int)Math.Ceiling(game.RepairCooldown)}s)"
                : $"EMERGENCY REPAIR [{AegisGame.RepairCost}]";
            DrawCtrlButton(402, repairLabel, enabled: game.CanRepair);

            DrawTurretPanel(game);
            DrawHints();
        }

        private static void DrawBuildButton(double y, string label, int cost,
                                            AegisGame game, TurretType type)
        {
            bool selected = game.BuildType == type;
            bool affordable = game.Credits >= cost;
            Color bg = selected ? Theme.ButtonActive : affordable ? Theme.Button : Theme.ButtonDisabled;

            Button(y, bg);
            SplashKit.DrawText(label, Theme.Text, "Arial", 13, BtnX + 10, y + 9);
            SplashKit.DrawText($"{cost}", affordable ? Theme.Good : Theme.Bad,
                               "Arial", 14, BtnX + BtnW - 38, y + 8);
        }

        private static void DrawCtrlButton(double y, string label, bool enabled)
        {
            Button(y, enabled ? Theme.Button : Theme.ButtonDisabled);
            SplashKit.DrawText(label, enabled ? Theme.Text : Theme.TextDim,
                               "Arial", 13, BtnX + 10, y + 9);
        }

        private static void Button(double y, Color bg)
        {
            SplashKit.FillRectangle(bg, BtnX, y, BtnW, BtnH);
            SplashKit.DrawRectangle(Theme.SidebarEdge, BtnX, y, BtnW, BtnH);
        }

        private static void DrawHints()
        {
            SplashKit.DrawText("L-Click: build / select", Theme.TextDim, "Arial", 12, BtnX, 592);
            SplashKit.DrawText("R-Click/Esc: cancel   P pause  F 2x", Theme.TextDim,
                               "Arial", 12, BtnX, 612);
        }

        // Modal-style panel for inspecting / managing a selected turret.
        private static void DrawTurretPanel(AegisGame game)
        {
            double py = 448;
            SplashKit.FillRectangle(Theme.SidebarEdge, BtnX - 4, py - 4, BtnW + 8, 132);

            Turret? t = game.SelectedTurret;
            if (t == null)
            {
                SplashKit.DrawText("Select a turret to", Theme.TextDim, "Arial", 13, BtnX + 8, py + 8);
                SplashKit.DrawText("inspect or upgrade.", Theme.TextDim, "Arial", 13, BtnX + 8, py + 28);
                return;
            }

            SplashKit.DrawText($"{t.Name.ToUpper()}  Lv {t.Level}", Theme.CoreInner,
                               "Arial", 15, BtnX + 8, py + 4);
            SplashKit.DrawText($"DMG {t.Damage:0}   RNG {t.Range:0}", Theme.Text,
                               "Arial", 13, BtnX + 8, py + 26);

            if (t.CanUpgrade)
            {
                Button(UpgY, game.CanUpgradeSelected ? Theme.ButtonActive : Theme.ButtonDisabled);
                SplashKit.DrawText($"UPGRADE  [{t.UpgradeCost}]",
                                   game.CanUpgradeSelected ? Theme.Text : Theme.TextDim,
                                   "Arial", 13, BtnX + 10, UpgY + 9);
            }
            else
            {
                Button(UpgY, Theme.ButtonDisabled);
                SplashKit.DrawText("MAX LEVEL", Theme.TextDim, "Arial", 13, BtnX + 10, UpgY + 9);
            }

            Button(SellY, Theme.Button);
            SplashKit.DrawText($"SELL  [{t.SellValue}]", Theme.Bad, "Arial", 13,
                               BtnX + 10, SellY + 9);
        }

        // ---- Full-screen overlays --------------------------------------------

        public static void DrawMenu(Window window)
        {
            SplashKit.FillRectangle(SplashKit.RGBAColor(10, 12, 20, 210), 0, 0, 960, 640);

            SplashKit.DrawText("AEGIS PROTOCOL", Theme.CoreInner, "Arial", 46, 250, 220);
            SplashKit.DrawText("CORE DEFENSE", Theme.Text, "Arial", 30, 330, 275);
            SplashKit.DrawText("Defend the Power Core against the rogue Mech waves.",
                               Theme.TextDim, "Arial", 16, 250, 330);
            SplashKit.DrawText("Press ENTER to begin", Theme.Good, "Arial", 20, 250, 390);
        }

        public static void DrawPaused(Window window)
        {
            SplashKit.FillRectangle(SplashKit.RGBAColor(10, 12, 20, 170), 0, 0, 960, 640);
            SplashKit.DrawText("PAUSED", Theme.CoreInner, "Arial", 44, 400, 290);
            SplashKit.DrawText("Press P to resume", Theme.Text, "Arial", 18, 360, 360);
        }

        public static void DrawGameOver(AegisGame game, Window window)
        {
            SplashKit.FillRectangle(SplashKit.RGBAColor(20, 8, 8, 210), 0, 0, 960, 640);
            SplashKit.DrawText("CORE BREACHED", Theme.Bad, "Arial", 46, 290, 240);
            SplashKit.DrawText($"You held out through {game.WaveNumber} wave(s).",
                               Theme.Text, "Arial", 20, 320, 310);
            SplashKit.DrawText("Press R to redeploy", Theme.Good, "Arial", 20, 345, 370);
        }
    }
}
