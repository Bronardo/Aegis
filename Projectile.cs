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
