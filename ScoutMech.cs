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
