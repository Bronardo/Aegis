# Aegis Protocol: Core Defense — Source Code Listing

**SIT771 Task 7.3D** — a SplashKit tower-defense game in C# (.NET 8).

Complete source for the project (15 C# files). The SplashKit SDK
binding (`lib/SplashKit.cs`) is generated tooling and is not included.
Build and run with `skm dotnet build` / `skm dotnet run`.

---

Program.cs: entry point — opens the 960x640 window and starts the game loop.

```csharp
using SplashKitSDK;

namespace Aegis
{
    public class Program
    {
        public static void Main()
        {
            Window window = new Window("Aegis Protocol: Core Defense", 960, 640);
            AegisGame game = new AegisGame(window);

            // AegisGame drives the main loop (controller role from the RDD design)
            game.Run();

            window.Close();
        }
    }
}
```

---
AegisGame.cs: controller — main loop, game states (Menu/Playing/Paused/GameOver), input routing, economy, turret/wave management, and update/draw coordination.

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    public enum GameState { Menu, Playing, Paused, GameOver }

    // Controller role (RDD): drives the main loop, routes input, owns the
    // subsystems (GridMap, WaveManager, turrets, projectiles, UIOverlay) and
    // coordinates the economy (energy credits) and core health.
    public class AegisGame
    {
        private readonly Window _window;
        private readonly GridMap _grid;
        private readonly SplashKitSDK.Timer _timer;

        private GameState _state = GameState.Menu;
        private double _elapsed;
        private double _dt;
        private bool _fastForward;

        // Economy / defence status
        private int _coreHealth = 20;
        private int _credits = 120;
        private int _waveNumber;
        private double _repairCooldown;

        // Battlefield entities
        private WaveManager? _waves;
        private readonly List<Turret> _turrets = new List<Turret>();
        private readonly List<Projectile> _projectiles = new List<Projectile>();
        private Turret? _selected;
        private TurretType? _buildType;

        public GameState State => _state;
        public int CoreHealth => _coreHealth;
        public int Credits => _credits;
        public int WaveNumber => _waveNumber;
        public bool FastForward => _fastForward;
        public double RepairCooldown => _repairCooldown;
        public Turret? SelectedTurret => _selected;
        public TurretType? BuildType => _buildType;
        public GridMap Grid => _grid;
        public List<Turret> Turrets => _turrets;

        public WaveManager? Waves => _waves;

        public AegisGame(Window window)
        {
            _window = window;
            _grid = new GridMap();
            _timer = SplashKit.CreateTimer("AegisTimer");
            _timer.Start();
        }

        public void Run()
        {
            while (!_window.CloseRequested)
            {
                HandleInput();
                Update();
                Draw();
            }
        }

        // ---- Input -----------------------------------------------------------

        public void HandleInput()
        {
            SplashKit.ProcessEvents();

            if (SplashKit.KeyTyped(KeyCode.EscapeKey))
            {
                if (_state == GameState.Playing)
                {
                    // First clear any selection / build tool, otherwise quit via window close.
                    if (_buildType.HasValue) _buildType = null;
                    else if (_selected != null) _selected = null;
                }
                else if (_state == GameState.Paused)
                {
                    _state = GameState.Playing;
                }
            }

            switch (_state)
            {
                case GameState.Menu:
                    if (SplashKit.KeyTyped(KeyCode.ReturnKey)) StartGame();
                    break;

                case GameState.Playing:
                    HandlePlayingInput();
                    break;

                case GameState.Paused:
                    if (SplashKit.KeyTyped(KeyCode.PKey)) _state = GameState.Playing;
                    break;

                case GameState.GameOver:
                    if (SplashKit.KeyTyped(KeyCode.RKey)) StartGame();
                    break;
            }
        }

        private void HandlePlayingInput()
        {
            if (SplashKit.KeyTyped(KeyCode.PKey))
            {
                _state = GameState.Paused;
                return;
            }
            if (SplashKit.KeyTyped(KeyCode.FKey))
            {
                _fastForward = !_fastForward;
            }

            // Right click cancels a pending build or clears selection.
            if (SplashKit.MouseClicked(MouseButton.RightButton))
            {
                _buildType = null;
                _selected = null;
                return;
            }

            if (SplashKit.MouseClicked(MouseButton.LeftButton))
            {
                Point2D mouse = SplashKit.MousePosition();

                if (mouse.X >= GridMap.GridWidth)
                {
                    UIOverlay.HandleSidebarClick(this, mouse);
                }
                else
                {
                    HandleGridClick(mouse);
                }
            }
        }

        private void HandleGridClick(Point2D mouse)
        {
            (int col, int row)? tile = _grid.TileAt(mouse.X, mouse.Y);
            if (!tile.HasValue) return;

            (int col, int row) = tile.Value;

            Turret? existing = TurretAt(col, row);

            // Build mode: try to place the selected turret on this tile.
            if (_buildType.HasValue)
            {
                TryBuild(_buildType.Value, col, row);
                return;
            }

            // Otherwise: select a turret that is already standing here.
            _selected = existing;
        }

        private Turret? TurretAt(int col, int row)
        {
            foreach (Turret turret in _turrets)
            {
                if (turret.Col == col && turret.Row == row) return turret;
            }
            return null;
        }

        private void TryBuild(TurretType type, int col, int row)
        {
            int cost = Turret.CostOf(type);

            if (!_grid.IsBuildable(col, row)) return;
            if (_credits < cost) return;

            _credits -= cost;
            Turret turret = Turret.Create(type, col, row, _grid);
            _turrets.Add(turret);
            _grid.SetOccupied(col, row, true);
            _selected = turret;
        }

        private void StartGame()
        {
            _coreHealth = 20;
            _credits = 120;
            _waveNumber = 0;
            _elapsed = 0;
            _fastForward = false;
            _repairCooldown = 0;
            _turrets.Clear();
            _projectiles.Clear();
            _selected = null;
            _buildType = null;

            // Rebuild occupancy grid for a fresh game.
            _waves = new WaveManager(_grid);
            _state = GameState.Playing;
        }

        // ---- Actions triggered by the UIOverlay / hotkeys --------------------

        public void SelectBuild(TurretType? type)
        {
            // Clicking the active build button again toggles it off.
            _buildType = (_buildType == type) ? null : type;
            _selected = null;
        }

        public void StartWave()
        {
            if (_state == GameState.Playing && _waves != null && !_waves.WaveInProgress)
            {
                _waves.StartWave();
            }
        }

        public void TogglePause()
        {
            if (_state == GameState.Playing) _state = GameState.Paused;
            else if (_state == GameState.Paused) _state = GameState.Playing;
        }

        public void ToggleFastForward() => _fastForward = !_fastForward;

        public const int RepairCost = 40;
        private const double RepairCooldownTime = 15;
        private const int RepairAmount = 5;

        public bool CanRepair =>
            _state == GameState.Playing && _repairCooldown <= 0 &&
            _credits >= RepairCost && _coreHealth < 20;

        public void EmergencyRepair()
        {
            if (!CanRepair) return;
            _credits -= RepairCost;
            _coreHealth = System.Math.Min(20, _coreHealth + RepairAmount);
            _repairCooldown = RepairCooldownTime;
        }

        public bool CanUpgradeSelected =>
            _selected != null && _selected.CanUpgrade && _credits >= _selected.UpgradeCost;

        public void UpgradeSelected()
        {
            if (_selected == null || !_selected.CanUpgrade) return;
            if (_credits < _selected.UpgradeCost) return;

            _credits -= _selected.UpgradeCost;
            _selected.ApplyUpgrade();
        }

        public void SellSelected()
        {
            if (_selected == null) return;

            _credits += _selected.SellValue;
            _grid.SetOccupied(_selected.Col, _selected.Row, false);
            _turrets.Remove(_selected);
            _selected = null;
        }

        // ---- Update ----------------------------------------------------------

        public void Update()
        {
            if (_state != GameState.Playing) return;

            _dt = _timer.Ticks / 1000.0;
            _timer.Reset();
            _elapsed += _dt;

            if (_repairCooldown > 0) _repairCooldown -= _dt;
            double sdt = _dt * (_fastForward ? 2 : 1);

            if (_waves == null) return;

            _waves.Update(sdt, out int creditsEarned, out int coreDamage);
            _credits += creditsEarned;

            if (coreDamage > 0)
            {
                _coreHealth = System.Math.Max(0, _coreHealth - coreDamage);
                if (_coreHealth <= 0)
                {
                    _state = GameState.GameOver;
                    return;
                }
            }

            List<Mech> mechs = new List<Mech>(_waves.Mechs);

            foreach (Turret turret in _turrets)
            {
                turret.Update(sdt, mechs, _projectiles);
            }

            List<Projectile> deadProjectiles = new List<Projectile>();
            foreach (Projectile projectile in _projectiles)
            {
                projectile.Update(sdt);
                if (projectile.Dead) deadProjectiles.Add(projectile);
            }
            foreach (Projectile projectile in deadProjectiles)
            {
                _projectiles.Remove(projectile);
            }

            // Surviving a whole wave pays a completion bonus.
            if (!_waves.WaveInProgress && _waves.Wave > _waveNumber)
            {
                _waveNumber = _waves.Wave;
                _credits += 30 + _waveNumber * 5;
            }
        }

        // ---- Draw ------------------------------------------------------------

        public void Draw()
        {
            _window.Clear(Theme.Background);

            if (_state == GameState.Menu)
            {
                _grid.Draw(_elapsed);
                UIOverlay.DrawMenu(_window);
            }
            else
            {
                DrawBattle();
                UIOverlay.DrawSidebar(this, _window);

                if (_state == GameState.Paused) UIOverlay.DrawPaused(_window);
                else if (_state == GameState.GameOver) UIOverlay.DrawGameOver(this, _window);
            }

            _window.Refresh(60);
        }

        private void DrawBattle()
        {
            _grid.Draw(_elapsed);

            if (_waves != null)
            {
                foreach (Mech mech in _waves.Mechs) mech.Draw();
            }

            foreach (Turret turret in _turrets)
            {
                if (turret == _selected)
                {
                    SplashKit.FillCircle(Theme.FreezePulse, turret.X, turret.Y, turret.Range);
                    SplashKit.DrawCircle(Theme.CoreInner, turret.X, turret.Y, turret.Range);
                }
                turret.Draw();
            }

            foreach (Projectile projectile in _projectiles)
            {
                projectile.Draw();
            }

            DrawBuildPreview();
        }

        private void DrawBuildPreview()
        {
            if (!_buildType.HasValue) return;

            Point2D mouse = SplashKit.MousePosition();
            (int col, int row)? tile = _grid.TileAt(mouse.X, mouse.Y);
            if (!tile.HasValue) return;

            (int c, int r) = tile.Value;
            double px = c * GridMap.TileSize;
            double py = r * GridMap.TileSize;

            bool ok = _grid.IsBuildable(c, r) && _credits >= Turret.CostOf(_buildType.Value);
            Color tint = ok ? SplashKit.RGBAColor(110, 220, 130, 90)
                            : SplashKit.RGBAColor(235, 90, 90, 90);

            SplashKit.FillRectangle(tint, px, py, GridMap.TileSize, GridMap.TileSize);

            if (ok)
            {
                SplashKit.DrawCircle(Theme.FreezePulse,
                                     _grid.CenterX(c), _grid.CenterY(r),
                                     Turret.RangeOf(_buildType.Value));
            }
        }
    }
}

```

---
GridMap.cs: environment manager — tile grid, snake path waypoints, Power Core position, path/occupancy checks, and coordinate conversion.

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // Environment Manager role (RDD): owns the tile grid, the Mech path nodes
    // and spatial occupancy (where turrets are built).
    public class GridMap
    {
        public const int TileSize = 40;
        public const int Cols = 16;
        public const int Rows = 15;
        public const int GridWidth = Cols * TileSize;   // 640
        public const int GridHeight = Rows * TileSize;  // 600

        private readonly bool[,] _isPath;
        private readonly bool[,] _occupied;
        private readonly List<(int col, int row)> _waypoints;

        // The Power Core sits at the final waypoint tile.
        public (int col, int row) CoreTile { get; }

        public IReadOnlyList<(int col, int row)> Waypoints => _waypoints;

        public GridMap()
        {
            _isPath = new bool[Cols, Rows];
            _occupied = new bool[Cols, Rows];
            _waypoints = new List<(int col, int row)>();
            CoreTile = (8, 6);
            BuildPath();
        }

        // Snake-like route entering from off the left edge and ending at the Core.
        private void BuildPath()
        {
            AddPathPoint(-1, 2);   // spawns off the left edge...
            AddPathPoint(4, 2);
            AddPathPoint(4, 7);
            AddPathPoint(11, 7);
            AddPathPoint(11, 2);
            AddPathPoint(14, 2);
            AddPathPoint(14, 11);
            AddPathPoint(1, 11);
            AddPathPoint(1, 6);
            AddPathPoint(7, 6);
            AddPathPoint(CoreTile.col, CoreTile.row);  // ...reaches the Core
        }

        private void AddPathPoint(int col, int row)
        {
            _waypoints.Add((col, row));

            if (_waypoints.Count >= 2)
            {
                (int prevCol, int prevRow) = _waypoints[_waypoints.Count - 2];
                MarkSegment(prevCol, prevRow, col, row);
            }
        }

        // Mark every tile between two axis-aligned waypoints as "path" (non-buildable).
        private void MarkSegment(int c1, int r1, int c2, int r2)
        {
            if (r1 == r2)
            {
                int start = System.Math.Min(c1, c2);
                int end = System.Math.Max(c1, c2);
                for (int c = start; c <= end; c++) SetPath(c, r1);
            }
            else if (c1 == c2)
            {
                int start = System.Math.Min(r1, r2);
                int end = System.Math.Max(r1, r2);
                for (int r = start; r <= end; r++) SetPath(c1, r);
            }
        }

        private void SetPath(int col, int row)
        {
            if (col >= 0 && col < Cols && row >= 0 && row < Rows)
            {
                _isPath[col, row] = true;
            }
        }

        public double CenterX(int col) => col * TileSize + TileSize / 2.0;
        public double CenterY(int row) => row * TileSize + TileSize / 2.0;

        public bool InGrid(int col, int row) => col >= 0 && col < Cols && row >= 0 && row < Rows;

        public bool IsPathTile(int col, int row) => InGrid(col, row) && _isPath[col, row];

        public bool IsOccupied(int col, int row) => InGrid(col, row) && _occupied[col, row];

        public void SetOccupied(int col, int row, bool value)
        {
            if (InGrid(col, row)) _occupied[col, row] = value;
        }

        // A tile can accept a turret if it is a normal build tile with nothing on it.
        public bool IsBuildable(int col, int row) =>
            InGrid(col, row) && !_isPath[col, row] && !_occupied[col, row];

        // Convert a screen (mouse) position into a tile coordinate.
        public (int col, int row)? TileAt(double x, double y)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight) return null;
            return ((int)(x / TileSize), (int)(y / TileSize));
        }

        // ---- Rendering -------------------------------------------------------

        public void Draw(double elapsed)
        {
            DrawTiles();
            DrawWaypointMarkers();
            DrawCore(elapsed);
        }

        private void DrawTiles()
        {
            for (int col = 0; col < Cols; col++)
            {
                for (int row = 0; row < Rows; row++)
                {
                    double x = col * TileSize;
                    double y = row * TileSize;

                    if (_isPath[col, row])
                    {
                        Color path = (col + row) % 2 == 0 ? Theme.TilePathA : Theme.TilePathB;
                        SplashKit.FillRectangle(path, x + 1, y + 1, TileSize - 2, TileSize - 2);
                    }
                    else
                    {
                        SplashKit.FillRectangle(Theme.TileBuild, x + 1, y + 1, TileSize - 2, TileSize - 2);
                    }
                }
            }
        }

        // Subtle markers at path corners so the route reads clearly.
        private void DrawWaypointMarkers()
        {
            foreach ((int col, int row) in _waypoints)
            {
                if (col < 0) continue;                                  // off-grid spawn point
                if (col == CoreTile.col && row == CoreTile.row) continue; // drawn as the Core

                SplashKit.FillCircle(Theme.Waypoint, CenterX(col), CenterY(row), 3);
            }
        }

        private void DrawCore(double elapsed)
        {
            double cx = CenterX(CoreTile.col);
            double cy = CenterY(CoreTile.row);
            double pulse = System.Math.Sin(elapsed * 3) * 3;

            SplashKit.FillCircle(Theme.CoreGlow, cx, cy, 26 + pulse);
            SplashKit.FillCircle(Theme.Core, cx, cy, 17);
            SplashKit.FillCircle(Theme.CoreInner, cx, cy, 8);
        }
    }
}

```

---
GameObject.cs: abstract base for all battlefield objects (X/Y position plus abstract Draw()).

```csharp
namespace Aegis
{
    // Abstract base for every object that lives on the battlefield.
    // Holds a position and promises that subclasses can be drawn.
    public abstract class GameObject
    {
        public double X { get; set; }
        public double Y { get; set; }

        public GameObject(double x, double y)
        {
            X = x;
            Y = y;
        }

        public abstract void Draw();
    }
}

```

---
Theme.cs: shared static color palette for the tactical/sci-fi look.

```csharp
using SplashKitSDK;

namespace Aegis
{
    // Central palette so every class draws with a consistent tactical / sci-fi look.
    public static class Theme
    {
        // Backgrounds
        public static Color Background    = SplashKit.RGBColor(18, 20, 30);
        public static Color Sidebar       = SplashKit.RGBColor(26, 29, 42);
        public static Color SidebarEdge   = SplashKit.RGBColor(52, 58, 78);
        public static Color TileBuild     = SplashKit.RGBColor(34, 38, 54);
        public static Color TileGrid      = SplashKit.RGBColor(46, 52, 72);
        public static Color TilePathA     = SplashKit.RGBColor(96, 78, 52);
        public static Color TilePathB     = SplashKit.RGBColor(86, 69, 46);
        public static Color Waypoint      = SplashKit.RGBColor(190, 170, 120);

        // Power core
        public static Color CoreGlow      = SplashKit.RGBAColor(90, 200, 255, 70);
        public static Color Core          = SplashKit.RGBColor(45, 190, 255);
        public static Color CoreInner     = SplashKit.RGBColor(220, 245, 255);

        // Mechs (enemies)
        public static Color Scout         = SplashKit.RGBColor(235, 96, 70);
        public static Color ScoutDark     = SplashKit.RGBColor(150, 45, 35);
        public static Color Tank          = SplashKit.RGBColor(110, 130, 120);
        public static Color TankDark      = SplashKit.RGBColor(55, 70, 62);
        public static Color MechTracks    = SplashKit.RGBColor(30, 30, 34);
        public static Color HealthBack    = SplashKit.RGBColor(40, 10, 10);
        public static Color HealthBar     = SplashKit.RGBColor(110, 220, 110);
        public static Color SlowTint      = SplashKit.RGBAColor(110, 200, 255, 90);

        // Turrets / projectiles / effects
        public static Color Gatling       = SplashKit.RGBColor(220, 170, 70);
        public static Color GatlingDark   = SplashKit.RGBColor(120, 90, 35);
        public static Color Laser         = SplashKit.RGBColor(230, 70, 130);
        public static Color LaserBeam     = SplashKit.RGBColor(255, 120, 190);
        public static Color Freeze        = SplashKit.RGBColor(90, 200, 235);
        public static Color FreezePulse   = SplashKit.RGBAColor(120, 215, 250, 120);
        public static Color BasePlatform  = SplashKit.RGBColor(60, 66, 86);
        public static Color Bullet        = SplashKit.RGBColor(255, 230, 150);

        // UI
        public static Color Text          = SplashKit.RGBColor(225, 230, 240);
        public static Color TextDim       = SplashKit.RGBColor(150, 158, 178);
        public static Color Good          = SplashKit.RGBColor(110, 220, 130);
        public static Color Bad           = SplashKit.RGBColor(235, 90, 90);
        public static Color Button        = SplashKit.RGBColor(46, 52, 74);
        public static Color ButtonHover   = SplashKit.RGBColor(62, 70, 100);
        public static Color ButtonActive  = SplashKit.RGBColor(60, 110, 95);
        public static Color ButtonDisabled= SplashKit.RGBColor(36, 39, 52);
    }
}

```

---
Mech.cs: abstract enemy — waypoint movement toward the Core, armor-mitigated damage, slow status, health bar, polymorphic body drawing.

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // Hazard role (RDD): abstract enemy unit that follows the GridMap waypoints
    // toward the Power Core. Concrete subclasses supply their look and stats.
    public abstract class Mech : GameObject
    {
        private readonly IReadOnlyList<(int col, int row)> _waypoints;
        private int _waypointIndex;
        private double _slowTimer;     // seconds of slow remaining

        protected Mech(double x, double y, IReadOnlyList<(int col, int row)> waypoints,
                       double maxHp, double speed, int armor, int reward, int coreDamage)
            : base(x, y)
        {
            _waypoints = waypoints;
            _waypointIndex = 0;
            MaxHp = maxHp;
            Hp = maxHp;
            Speed = speed;
            Armor = armor;
            Reward = reward;
            CoreDamage = coreDamage;
        }

        public double MaxHp { get; }
        public double Hp { get; private set; }
        public double Speed { get; }
        public int Armor { get; }
        public int Reward { get; }
        public int CoreDamage { get; }
        public bool IsDead => Hp <= 0;
        public bool ReachedCore => _waypointIndex >= _waypoints.Count;
        public bool IsSlowed => _slowTimer > 0;

        public virtual double Radius => 14;

        public Circle HitCircle => SplashKit.CircleAt(X, Y, Radius);

        // Freeze turrets call this; refreshes the duration if already chilled.
        public void ApplySlow(double duration)
        {
            if (_slowTimer < duration) _slowTimer = duration;
        }

        // Armor subtracts a flat amount from every hit.
        public void TakeDamage(int amount)
        {
            double dealt = amount - Armor;
            if (dealt < 1) dealt = 1;
            Hp -= dealt;
        }

        public void Move(double dt)
        {
            if (_slowTimer > 0) _slowTimer -= dt;
            if (ReachedCore) return;

            (int col, int row) target = _waypoints[_waypointIndex];
            double tx = target.col * GridMap.TileSize + GridMap.TileSize / 2.0;
            double ty = target.row * GridMap.TileSize + GridMap.TileSize / 2.0;

            double dx = tx - X;
            double dy = ty - Y;
            double dist = System.Math.Sqrt(dx * dx + dy * dy);

            double speed = Speed * (IsSlowed ? 0.45 : 1.0);
            double step = speed * dt;

            if (dist <= step)
            {
                X = tx;
                Y = ty;
                _waypointIndex++;
            }
            else
            {
                X += dx / dist * step;
                Y += dy / dist * step;
            }
        }

        public void Update(double dt) => Move(dt);

        // Subclasses paint their own chassis; this wraps it with the health
        // bar and the freeze tint (common to every mech).
        public override void Draw()
        {
            DrawBody();

            if (IsSlowed)
            {
                SplashKit.FillCircle(Theme.SlowTint, X, Y, Radius + 2);
            }

            DrawHealthBar();
        }

        private void DrawHealthBar()
        {
            const double w = 28;
            double frac = System.Math.Max(0, Hp / MaxHp);
            double bx = X - w / 2.0;
            double by = Y - Radius - 10;

            SplashKit.FillRectangle(Theme.HealthBack, bx, by, w, 4);
            SplashKit.FillRectangle(Theme.HealthBar, bx, by, w * frac, 4);
        }

        protected abstract void DrawBody();
    }
}

```

---
ScoutMech.cs: fast, low-health recon unit (armor 0, core damage 1) with a diamond chassis.

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // Fast, low-health recon unit. Triangular chassis so it reads as "agile".
    public class ScoutMech : Mech
    {
        public ScoutMech(double x, double y, IReadOnlyList<(int col, int row)> waypoints,
                         double hp, double speed, int reward)
            : base(x, y, waypoints, hp, speed, armor: 0, reward, coreDamage: 1)
        {
        }

        public override double Radius => 12;

        protected override void DrawBody()
        {
            // Tracks
            SplashKit.FillRectangle(Theme.MechTracks, X - 11, Y - 9, 22, 5);
            SplashKit.FillRectangle(Theme.MechTracks, X - 11, Y + 4, 22, 5);

            // Sleek diamond hull (symmetric, so it reads well on the winding path)
            Point2D right = SplashKit.PointAt(X + 12, Y);
            Point2D left = SplashKit.PointAt(X - 12, Y);
            Point2D top = SplashKit.PointAt(X, Y - 10);
            Point2D bottom = SplashKit.PointAt(X, Y + 10);

            SplashKit.FillTriangle(Theme.Scout, right.X, right.Y, top.X, top.Y, bottom.X, bottom.Y);
            SplashKit.FillTriangle(Theme.Scout, left.X, left.Y, top.X, top.Y, bottom.X, bottom.Y);
            SplashKit.DrawTriangle(Theme.ScoutDark, right.X, right.Y, top.X, top.Y, bottom.X, bottom.Y);
            SplashKit.DrawTriangle(Theme.ScoutDark, left.X, left.Y, top.X, top.Y, bottom.X, bottom.Y);

            // Sensor core
            SplashKit.FillCircle(Theme.CoreInner, X, Y, 3);
        }
    }
}

```

---
HeavyTankMech.cs: slow, heavily-armoured unit (armor 3, core damage 3) with a chunky tank chassis.

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // Slow, heavily-armoured unit. Chunky rectangular tank chassis.
    public class HeavyTankMech : Mech
    {
        public HeavyTankMech(double x, double y, IReadOnlyList<(int col, int row)> waypoints,
                             double hp, double speed, int reward)
            : base(x, y, waypoints, hp, speed, armor: 3, reward, coreDamage: 3)
        {
        }

        public override double Radius => 16;

        protected override void DrawBody()
        {
            // Wide tread blocks
            SplashKit.FillRectangle(Theme.MechTracks, X - 16, Y - 13, 32, 6);
            SplashKit.FillRectangle(Theme.MechTracks, X - 16, Y + 7, 32, 6);

            // Hull
            SplashKit.FillRectangle(Theme.Tank, X - 14, Y - 10, 28, 20);
            SplashKit.DrawRectangle(Theme.TankDark, X - 14, Y - 10, 28, 20);

            // Armoured turret on top
            SplashKit.FillRectangle(Theme.TankDark, X - 8, Y - 6, 16, 12);
            SplashKit.FillCircle(Theme.ScoutDark, X, Y, 4);

            // Rivets / plating detail
            SplashKit.FillRectangle(Theme.TankDark, X - 14, Y - 2, 28, 2);
        }
    }
}

```

---
WaveManager.cs: wave progression — schedules spawns, owns live Mechs, scales endless waves, reports kill rewards and core damage.

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // Wave progression & pathfinding role: schedules spawns and owns the
    // list of live Mechs. Returns credits / core damage events to the game.
    public class WaveManager
    {
        private readonly GridMap _grid;
        private readonly List<Mech> _mechs = new List<Mech>();
        private readonly Queue<bool> _spawnQueue = new Queue<bool>(); // true = tank

        private int _wave;
        private double _spawnTimer;
        private const double SpawnInterval = 0.85;

        public int Wave => _wave;
        public bool WaveInProgress { get; private set; }
        public IReadOnlyList<Mech> Mechs => _mechs;

        // When the whole wave is cleared (no spawns left, no mechs alive).
        public bool WaveComplete =>
            WaveInProgress && _spawnQueue.Count == 0 && _mechs.Count == 0;

        public WaveManager(GridMap grid)
        {
            _grid = grid;
        }

        public void StartWave()
        {
            if (WaveInProgress) return;

            _wave++;
            WaveInProgress = true;
            _spawnTimer = SpawnInterval; // small delay before the first unit arrives
            _spawnQueue.Clear();

            int scouts = 5 + _wave * 2;
            int tanks = _wave >= 2 ? (_wave - 1) * 2 : 0;

            // Interleave: roughly every third unit from wave 2+ is a tank.
            for (int i = 0; i < scouts + tanks; i++)
            {
                _spawnQueue.Enqueue(tanks > 0 && i % 3 == 2);
            }
        }

        public void Update(double dt, out int creditsEarned, out int coreDamage)
        {
            creditsEarned = 0;
            coreDamage = 0;

            if (WaveInProgress)
            {
                _spawnTimer -= dt;
                if (_spawnTimer <= 0 && _spawnQueue.Count > 0)
                {
                    SpawnMech(_spawnQueue.Dequeue());
                    _spawnTimer = SpawnInterval;
                }
            }

            List<Mech> removed = new List<Mech>();

            foreach (Mech mech in _mechs)
            {
                mech.Update(dt);

                if (mech.IsDead)
                {
                    creditsEarned += mech.Reward;
                    removed.Add(mech);
                }
                else if (mech.ReachedCore)
                {
                    coreDamage += mech.CoreDamage;
                    removed.Add(mech);
                }
            }

            foreach (Mech mech in removed)
            {
                _mechs.Remove(mech);
            }

            if (WaveComplete)
            {
                WaveInProgress = false;
            }
        }

        private void SpawnMech(bool isTank)
        {
            var first = _grid.Waypoints[0];
            double x = _grid.CenterX(first.col < 0 ? 0 : first.col) - GridMap.TileSize;
            double y = _grid.CenterY(first.row);

            if (isTank)
            {
                double hp = 60 + _wave * 22;
                double speed = 42 + _wave * 1.2;
                int reward = 18 + _wave * 2;
                _mechs.Add(new HeavyTankMech(x, y, _grid.Waypoints, hp, speed, reward));
            }
            else
            {
                double hp = 28 + _wave * 9;
                double speed = 78 + _wave * 2.0;
                int reward = 8 + _wave;
                _mechs.Add(new ScoutMech(x, y, _grid.Waypoints, hp, speed, reward));
            }
        }
    }
}

```

---
Turret.cs: abstract defence base — targeting (nearest in range), range/cooldown/damage scaling, upgrade/sell values, turret factory.

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    public enum TurretType { Gatling, Freeze, Laser }

    // Defence role (RDD): abstract base for all turrets. Owns position, range,
    // fire-rate timer, cost and upgrade level; concrete subclasses define how
    // they attack and look.
    public abstract class Turret : GameObject
    {
        protected Turret(int col, int row, double centerX, double centerY)
            : base(centerX, centerY)
        {
            Col = col;
            Row = row;
            Level = 1;
            Cooldown = 0;
            _invested = BaseCost;
        }

        private int _invested;

        public int Col { get; }
        public int Row { get; }
        public int Level { get; private set; }
        public const int MaxLevel = 3;

        public abstract TurretType Type { get; }
        public abstract string Name { get; }
        protected abstract int BaseCost { get; }
        protected abstract double BaseRange { get; }
        protected abstract double BaseDamage { get; }
        protected abstract double BaseFireDelay { get; } // seconds between attacks

        public int Cost => BaseCost;
        public double Range => BaseRange + (Level - 1) * 16;
        public double Damage => BaseDamage + (Level - 1) * BaseDamage * 0.6;
        public double FireDelay => BaseFireDelay * (1 - (Level - 1) * 0.15);
        public bool CanUpgrade => Level < MaxLevel;

        public int UpgradeCost => (int)(BaseCost * 0.8 * Level);
        public int SellValue => (int)(_invested * 0.6);

        protected double Cooldown { get; set; }

        // Called by the game after it has confirmed and charged the player.
        public void ApplyUpgrade()
        {
            if (!CanUpgrade) return;
            _invested += UpgradeCost;
            Level++;
        }

        // Acquire the nearest live Mech within operational radius.
        public Mech? FindTarget(List<Mech> mechs)
        {
            Mech? best = null;
            double bestDist = double.MaxValue;

            foreach (Mech mech in mechs)
            {
                double dx = mech.X - X;
                double dy = mech.Y - Y;
                double dist = System.Math.Sqrt(dx * dx + dy * dy);

                if (dist <= Range && dist < bestDist)
                {
                    bestDist = dist;
                    best = mech;
                }
            }

            return best;
        }

        public virtual void Update(double dt, List<Mech> mechs, List<Projectile> projectiles)
        {
            Cooldown -= dt;

            Mech? target = FindTarget(mechs);
            if (target == null)
            {
                Idle(dt);
                return;
            }

            if (Cooldown <= 0)
            {
                Attack(target, mechs, projectiles);
                Cooldown = FireDelay;
            }
        }

        // All live mechs currently within operational radius (used by AoE turrets).
        protected List<Mech> MechsInRange(List<Mech> mechs)
        {
            List<Mech> inRange = new List<Mech>();
            foreach (Mech mech in mechs)
            {
                double dx = mech.X - X;
                double dy = mech.Y - Y;
                if (dx * dx + dy * dy <= Range * Range) inRange.Add(mech);
            }
            return inRange;
        }

        protected abstract void Attack(Mech target, List<Mech> mechs, List<Projectile> projectiles);
        protected virtual void Idle(double dt) { }

        public override void Draw()
        {
            // Hexagonal base platform
            SplashKit.FillCircle(Theme.BasePlatform, X, Y, 16);
            SplashKit.DrawCircle(Theme.SidebarEdge, X, Y, 16);

            DrawHead();

            // Pips showing upgrade level
            for (int i = 0; i < Level; i++)
            {
                SplashKit.FillCircle(Theme.CoreInner, X - 10 + i * 10, Y + 19, 2.5);
            }
        }

        // Subclasses draw their rotating barrel / crystal / emitter.
        protected abstract void DrawHead();

        // Factory used by the game when placing a turret.
        public static Turret Create(TurretType type, int col, int row, GridMap grid)
        {
            double cx = grid.CenterX(col);
            double cy = grid.CenterY(row);

            return type switch
            {
                TurretType.Gatling => new GatlingTurret(col, row, cx, cy),
                TurretType.Freeze => new FreezeTurret(col, row, cx, cy),
                TurretType.Laser => new LaserTurret(col, row, cx, cy),
                _ => new GatlingTurret(col, row, cx, cy),
            };
        }

        public static int CostOf(TurretType type) => type switch
        {
            TurretType.Gatling => GatlingTurret.StaticCost,
            TurretType.Freeze => FreezeTurret.StaticCost,
            TurretType.Laser => LaserTurret.StaticCost,
            _ => 0,
        };

        public static double RangeOf(TurretType type) => type switch
        {
            TurretType.Gatling => new GatlingTurret(0, 0, 0, 0).BaseRange,
            TurretType.Freeze => new FreezeTurret(0, 0, 0, 0).BaseRange,
            TurretType.Laser => new LaserTurret(0, 0, 0, 0).BaseRange,
            _ => 0,
        };
    }
}

```

---
GatlingTurret.cs: rapid-fire, single-target projectile turret (cost 50).

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // High rate-of-fire, single-target rapid projectile turret.
    public class GatlingTurret : Turret
    {
        public const int StaticCost = 50;

        private double _barrelAngle;

        public GatlingTurret(int col, int row, double x, double y)
            : base(col, row, x, y)
        {
        }

        public override TurretType Type => TurretType.Gatling;
        public override string Name => "Gatling";
        protected override int BaseCost => StaticCost;
        protected override double BaseRange => 110;
        protected override double BaseDamage => 6;
        protected override double BaseFireDelay => 0.35;

        protected override void Attack(Mech target, List<Mech> mechs, List<Projectile> projectiles)
        {
            int dmg = (int)Damage;
            projectiles.Add(new Projectile(X, Y, target, dmg, speed: 420, Theme.Bullet));
        }

        protected override void Idle(double dt)
        {
            _barrelAngle += dt * 1.5; // slowly sweep while idle
        }

        protected override void DrawHead()
        {
            // Rotating barrel block (animated while idle)
            SplashKit.FillRectangle(Theme.GatlingDark, X - 7, Y - 7, 14, 14);

            double len = 14;
            double tipX = X + len;
            double tipY = Y;

            // Twin barrels
            SplashKit.FillRectangle(Theme.Gatling, X - 2, Y - 6, len, 3);
            SplashKit.FillRectangle(Theme.Gatling, X - 2, Y + 3, len, 3);
            SplashKit.FillCircle(Theme.Gatling, tipX, tipY, 5);
        }
    }
}

```

---
FreezeTurret.cs: AoE support turret — periodically slows every Mech in range with a frost pulse (cost 70).

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // Area-of-effect support turret: does no damage itself, but periodically
    // chills every Mech in range, slowing them down for other turrets to pick off.
    public class FreezeTurret : Turret
    {
        public const int StaticCost = 70;

        private double _pulse;   // counts down the visual pulse ring after firing

        public FreezeTurret(int col, int row, double x, double y)
            : base(col, row, x, y)
        {
        }

        public override TurretType Type => TurretType.Freeze;
        public override string Name => "Frost";
        protected override int BaseCost => StaticCost;
        protected override double BaseRange => 105;
        protected override double BaseDamage => 0;
        protected override double BaseFireDelay => 1.6;

        protected override void Attack(Mech target, List<Mech> mechs, List<Projectile> projectiles)
        {
            // AoE frost pulse: slow every mech currently within operational radius.
            foreach (Mech mech in MechsInRange(mechs))
            {
                mech.ApplySlow(2.2);
            }
            _pulse = 0.5;
        }

        public override void Update(double dt, List<Mech> mechs, List<Projectile> projectiles)
        {
            if (_pulse > 0) _pulse -= dt;
            base.Update(dt, mechs, projectiles);
        }

        protected override void DrawHead()
        {
            // Crystal emitter
            Point2D top = SplashKit.PointAt(X, Y - 10);
            Point2D right = SplashKit.PointAt(X + 8, Y);
            Point2D bottom = SplashKit.PointAt(X, Y + 10);
            Point2D left = SplashKit.PointAt(X - 8, Y);

            SplashKit.FillTriangle(Theme.Freeze, top.X, top.Y, right.X, right.Y, bottom.X, bottom.Y);
            SplashKit.FillTriangle(Theme.Freeze, top.X, top.Y, left.X, left.Y, bottom.X, bottom.Y);
            SplashKit.FillCircle(Theme.CoreInner, X, Y, 3);

            // Expanding pulse ring after a frost burst
            if (_pulse > 0)
            {
                double frac = 1 - _pulse / 0.5;
                SplashKit.DrawCircle(Theme.FreezePulse, X, Y, Range * frac);
            }
        }
    }
}

```

---
LaserTurret.cs: long-range beam turret — heavy instant burst damage rendered as a beam (cost 90).

```csharp
using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // Long-range beam turret: applies heavy direct damage in a single burst,
    // rendered as a continuous beam (no projectile).
    public class LaserTurret : Turret
    {
        public const int StaticCost = 90;

        private Mech? _beamTarget;
        private double _beamTimer;

        public LaserTurret(int col, int row, double x, double y)
            : base(col, row, x, y)
        {
        }

        public override TurretType Type => TurretType.Laser;
        public override string Name => "Laser";
        protected override int BaseCost => StaticCost;
        protected override double BaseRange => 150;
        protected override double BaseDamage => 22;
        protected override double BaseFireDelay => 1.1;

        protected override void Attack(Mech target, List<Mech> mechs, List<Projectile> projectiles)
        {
            // Instantaneous beam attack — damage applied directly (no projectile).
            target.TakeDamage((int)Damage);
            _beamTarget = target;
            _beamTimer = 0.12;
        }

        public override void Update(double dt, List<Mech> mechs, List<Projectile> projectiles)
        {
            if (_beamTimer > 0)
            {
                _beamTimer -= dt;
                if (_beamTimer <= 0) _beamTarget = null;
            }

            base.Update(dt, mechs, projectiles);
        }

        protected override void DrawHead()
        {
            // Focusing lens housing
            SplashKit.FillCircle(Theme.Laser, X, Y, 9);
            SplashKit.FillCircle(Theme.CoreInner, X, Y, 3);

            if (_beamTarget != null && !_beamTarget.IsDead)
            {
                // Outer magenta beam with a bright translucent core for a glow.
                SplashKit.DrawLine(Theme.Laser, X, Y, _beamTarget.X, _beamTarget.Y);
                SplashKit.DrawLine(SplashKit.RGBAColor(255, 210, 235, 180),
                                   X, Y, _beamTarget.X, _beamTarget.Y);
            }
        }
    }
}

```

---
Projectile.cs: homing round that travels from a turret to its locked target and applies damage on impact.

```csharp
using SplashKitSDK;

namespace Aegis
{
    // Behavioural entity role: travels from a turret toward a locked target.
    // Trajectory is pre-computed, but we still animate travel time so the
    // player can see rounds flying across the battlefield.
    public class Projectile : GameObject
    {
        private readonly Mech _target;
        private readonly double _speed;
        private readonly int _damage;
        private readonly Color _color;

        public Projectile(double startX, double startY, Mech target,
                          int damage, double speed, Color color)
            : base(startX, startY)
        {
            _target = target;
            _damage = damage;
            _speed = speed;
            _color = color;
        }

        public bool Dead { get; private set; }

        // Returns true on the frame the round strikes its target.
        public bool Update(double dt)
        {
            if (Dead) return false;
            if (_target.IsDead)
            {
                Dead = true;   // target was destroyed by something else first
                return false;
            }

            double dx = _target.X - X;
            double dy = _target.Y - Y;
            double dist = System.Math.Sqrt(dx * dx + dy * dy);
            double step = _speed * dt;

            if (dist <= step + _target.Radius)
            {
                _target.TakeDamage(_damage);
                Dead = true;
                return true;
            }

            X += dx / dist * step;
            Y += dy / dist * step;
            return false;
        }

        public override void Draw()
        {
            SplashKit.FillCircle(_color, X, Y, 4);
            SplashKit.FillCircle(Theme.CoreInner, X, Y, 1.5);
        }
    }
}

```

---
UIOverlay.cs: user interface — sidebar HUD, build/control buttons, turret upgrade/sell panel, and menu/pause/game-over screens with click routing.

```csharp
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

```
