using SplashKitSDK;

namespace Aegis
{
    public class Program
    {
        public static void Main()
        {
            Window window = new Window("Aegis Protocol: Core Defense", 960, 640);
            AegisGame game = new AegisGame(window);

            // AegisGame drives the main loop (controller role from the RDD design)
            game.Run();

            window.Close();
        }
    }
}

