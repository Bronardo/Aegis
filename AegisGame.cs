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
