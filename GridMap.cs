using System.Collections.Generic;
using SplashKitSDK;

namespace Aegis
{
    // Environment Manager role (RDD): owns the tile grid, the Mech path nodes
    // and spatial occupancy (where turrets are built).
    public class GridMap
    {
        public const int TileSize = 40;
        public const int Cols = 16;
        public const int Rows = 15;
        public const int GridWidth = Cols * TileSize;   // 640
        public const int GridHeight = Rows * TileSize;  // 600

        private readonly bool[,] _isPath;
        private readonly bool[,] _occupied;
        private readonly List<(int col, int row)> _waypoints;

        // The Power Core sits at the final waypoint tile.
        public (int col, int row) CoreTile { get; }

        public IReadOnlyList<(int col, int row)> Waypoints => _waypoints;

        public GridMap()
        {
            _isPath = new bool[Cols, Rows];
            _occupied = new bool[Cols, Rows];
            _waypoints = new List<(int col, int row)>();
            CoreTile = (8, 6);
            BuildPath();
        }

        // Snake-like route entering from off the left edge and ending at the Core.
        private void BuildPath()
        {
            AddPathPoint(-1, 2);   // spawns off the left edge...
            AddPathPoint(4, 2);
            AddPathPoint(4, 7);
            AddPathPoint(11, 7);
            AddPathPoint(11, 2);
            AddPathPoint(14, 2);
            AddPathPoint(14, 11);
            AddPathPoint(1, 11);
            AddPathPoint(1, 6);
            AddPathPoint(7, 6);
            AddPathPoint(CoreTile.col, CoreTile.row);  // ...reaches the Core
        }

        private void AddPathPoint(int col, int row)
        {
            _waypoints.Add((col, row));

            if (_waypoints.Count >= 2)
            {
                (int prevCol, int prevRow) = _waypoints[_waypoints.Count - 2];
                MarkSegment(prevCol, prevRow, col, row);
            }
        }

        // Mark every tile between two axis-aligned waypoints as "path" (non-buildable).
        private void MarkSegment(int c1, int r1, int c2, int r2)
        {
            if (r1 == r2)
            {
                int start = System.Math.Min(c1, c2);
                int end = System.Math.Max(c1, c2);
                for (int c = start; c <= end; c++) SetPath(c, r1);
            }
            else if (c1 == c2)
            {
                int start = System.Math.Min(r1, r2);
                int end = System.Math.Max(r1, r2);
                for (int r = start; r <= end; r++) SetPath(c1, r);
            }
        }

        private void SetPath(int col, int row)
        {
            if (col >= 0 && col < Cols && row >= 0 && row < Rows)
            {
                _isPath[col, row] = true;
            }
        }

        public double CenterX(int col) => col * TileSize + TileSize / 2.0;
        public double CenterY(int row) => row * TileSize + TileSize / 2.0;

        public bool InGrid(int col, int row) => col >= 0 && col < Cols && row >= 0 && row < Rows;

        public bool IsPathTile(int col, int row) => InGrid(col, row) && _isPath[col, row];

        public bool IsOccupied(int col, int row) => InGrid(col, row) && _occupied[col, row];

        public void SetOccupied(int col, int row, bool value)
        {
            if (InGrid(col, row)) _occupied[col, row] = value;
        }

        // A tile can accept a turret if it is a normal build tile with nothing on it.
        public bool IsBuildable(int col, int row) =>
            InGrid(col, row) && !_isPath[col, row] && !_occupied[col, row];

        // Convert a screen (mouse) position into a tile coordinate.
        public (int col, int row)? TileAt(double x, double y)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight) return null;
            return ((int)(x / TileSize), (int)(y / TileSize));
        }

        // ---- Rendering -------------------------------------------------------

        public void Draw(double elapsed)
        {
            DrawTiles();
            DrawWaypointMarkers();
            DrawCore(elapsed);
        }

        private void DrawTiles()
        {
            for (int col = 0; col < Cols; col++)
            {
                for (int row = 0; row < Rows; row++)
                {
                    double x = col * TileSize;
                    double y = row * TileSize;

                    if (_isPath[col, row])
                    {
                        Color path = (col + row) % 2 == 0 ? Theme.TilePathA : Theme.TilePathB;
                        SplashKit.FillRectangle(path, x + 1, y + 1, TileSize - 2, TileSize - 2);
                    }
                    else
                    {
                        SplashKit.FillRectangle(Theme.TileBuild, x + 1, y + 1, TileSize - 2, TileSize - 2);
                    }
                }
            }
        }

        // Subtle markers at path corners so the route reads clearly.
        private void DrawWaypointMarkers()
        {
            foreach ((int col, int row) in _waypoints)
            {
                if (col < 0) continue;                                  // off-grid spawn point
                if (col == CoreTile.col && row == CoreTile.row) continue; // drawn as the Core

                SplashKit.FillCircle(Theme.Waypoint, CenterX(col), CenterY(row), 3);
            }
        }

        private void DrawCore(double elapsed)
        {
            double cx = CenterX(CoreTile.col);
            double cy = CenterY(CoreTile.row);
            double pulse = System.Math.Sin(elapsed * 3) * 3;

            SplashKit.FillCircle(Theme.CoreGlow, cx, cy, 26 + pulse);
            SplashKit.FillCircle(Theme.Core, cx, cy, 17);
            SplashKit.FillCircle(Theme.CoreInner, cx, cy, 8);
        }
    }
}
