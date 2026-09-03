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
