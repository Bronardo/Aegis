namespace Aegis
{
    // Abstract base for every object that lives on the battlefield.
    // Holds a position and promises that subclasses can be drawn.
    public abstract class GameObject
    {
        public double X { get; set; }
        public double Y { get; set; }

        public GameObject(double x, double y)
        {
            X = x;
            Y = y;
        }

        public abstract void Draw();
    }
}
