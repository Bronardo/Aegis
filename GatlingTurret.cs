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
