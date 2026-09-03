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
