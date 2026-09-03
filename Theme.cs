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
