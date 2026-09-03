using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // Wave progression & pathfinding role: schedules spawns and owns the
    // list of live Mechs. Returns credits / core damage events to the game.
    public class WaveManager
    {
        private readonly GridMap _grid;
        private readonly List<Mech> _mechs = new List<Mech>();
        private readonly Queue<bool> _spawnQueue = new Queue<bool>(); // true = tank

        private int _wave;
        private double _spawnTimer;
        private const double SpawnInterval = 0.85;

        public int Wave => _wave;
        public bool WaveInProgress { get; private set; }
        public IReadOnlyList<Mech> Mechs => _mechs;

        // When the whole wave is cleared (no spawns left, no mechs alive).
        public bool WaveComplete =>
            WaveInProgress && _spawnQueue.Count == 0 && _mechs.Count == 0;

        public WaveManager(GridMap grid)
        {
            _grid = grid;
        }

        public void StartWave()
        {
            if (WaveInProgress) return;

            _wave++;
            WaveInProgress = true;
            _spawnTimer = SpawnInterval; // small delay before the first unit arrives
            _spawnQueue.Clear();

            int scouts = 5 + _wave * 2;
            int tanks = _wave >= 2 ? (_wave - 1) * 2 : 0;

            // Interleave: roughly every third unit from wave 2+ is a tank.
            for (int i = 0; i < scouts + tanks; i++)
            {
                _spawnQueue.Enqueue(tanks > 0 && i % 3 == 2);
            }
        }

        public void Update(double dt, out int creditsEarned, out int coreDamage)
        {
            creditsEarned = 0;
            coreDamage = 0;

            if (WaveInProgress)
            {
                _spawnTimer -= dt;
                if (_spawnTimer <= 0 && _spawnQueue.Count > 0)
                {
                    SpawnMech(_spawnQueue.Dequeue());
                    _spawnTimer = SpawnInterval;
                }
            }

            List<Mech> removed = new List<Mech>();

            foreach (Mech mech in _mechs)
            {
                mech.Update(dt);

                if (mech.IsDead)
                {
                    creditsEarned += mech.Reward;
                    removed.Add(mech);
                }
                else if (mech.ReachedCore)
                {
                    coreDamage += mech.CoreDamage;
                    removed.Add(mech);
                }
            }

            foreach (Mech mech in removed)
            {
                _mechs.Remove(mech);
            }

            if (WaveComplete)
            {
                WaveInProgress = false;
            }
        }

        private void SpawnMech(bool isTank)
        {
            var first = _grid.Waypoints[0];
            double x = _grid.CenterX(first.col < 0 ? 0 : first.col) - GridMap.TileSize;
            double y = _grid.CenterY(first.row);

            if (isTank)
            {
                double hp = 60 + _wave * 22;
                double speed = 42 + _wave * 1.2;
                int reward = 18 + _wave * 2;
                _mechs.Add(new HeavyTankMech(x, y, _grid.Waypoints, hp, speed, reward));
            }
            else
            {
                double hp = 28 + _wave * 9;
                double speed = 78 + _wave * 2.0;
                int reward = 8 + _wave;
                _mechs.Add(new ScoutMech(x, y, _grid.Waypoints, hp, speed, reward));
            }
        }
    }
}
