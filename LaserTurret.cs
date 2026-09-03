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
