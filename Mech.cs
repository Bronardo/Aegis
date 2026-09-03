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
