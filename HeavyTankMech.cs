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
