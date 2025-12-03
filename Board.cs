using System;
using System.Linq;

namespace SeaBattle
{
    public enum CellState
    {
        Empty,
        Ship,
        Hit,
        Miss
    }

    /// <summary>
    /// Represents the 10x10 board and fleet placement logic.
    /// </summary>
    public class Board
    {
        public const int GridSize = 10;

        /// <summary>
        /// Standard fleet: 1×4, 2×3, 3×2, 4×1 ship.
        /// </summary>
        public static readonly int[] FleetTemplate = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

        public static readonly int TotalShipCells = FleetTemplate.Sum();

        private readonly CellState[,] _cells = new CellState[GridSize, GridSize];
        private readonly Random _rnd = new Random();

        /// <summary>
        /// Remaining ship cells on this board.
        /// </summary>
        public int ShipCellsCount { get; private set; }

        /// <summary>
        /// True if fleet has been completely placed.
        /// </summary>
        public bool IsFleetComplete => ShipCellsCount == TotalShipCells;

        public CellState GetCell(int x, int y) => _cells[x, y];

        public void Clear()
        {
            Array.Clear(_cells, 0, _cells.Length);
            ShipCellsCount = 0;
        }

        /// <summary>
        /// Places the full standard fleet randomly (no overlaps, no touching).
        /// </summary>
        public void PlaceFleetRandom()
        {
            Clear();           // يصفر المصفوفة و ShipCellsCount = 0

            foreach (int size in FleetTemplate)
            {
                bool placed = false;
                while (!placed)
                {
                    bool horizontal = _rnd.Next(2) == 0;
                    int x = _rnd.Next(GridSize);
                    int y = _rnd.Next(GridSize);

                    // TryPlaceShip يقوم بكل شيء:
                    // - فحص الحدود + التلامس
                    // - وضع السفينة
                    // - زيادة ShipCellsCount بالقيمة الصحيحة
                    if (!TryPlaceShip(x, y, size, horizontal))
                        continue;

                    placed = true;
                }
            }
        }

        /// <summary>
        /// Tries to place a single ship at (x,y) with given length and orientation.
        /// Ensures no overlap, no touching, no out-of-bounds.
        /// </summary>
        public bool TryPlaceShip(int x, int y, int size, bool horizontal)
        {
            // فحص الحدود + التلامس ...
            if (horizontal)
            {
                if (x < 0 || x + size > GridSize || y < 0 || y >= GridSize)
                    return false;
            }
            else
            {
                if (y < 0 || y + size > GridSize || x < 0 || x >= GridSize)
                    return false;
            }

            if (!CanPlaceShip(x, y, size, horizontal))
                return false;

            for (int i = 0; i < size; i++) //???
            {
                int cx = horizontal ? x + i : x;
                int cy = horizontal ? y : y + i;
                _cells[cx, cy] = CellState.Ship;
            }

            ShipCellsCount += size;
            return true;
        }

        /// <summary>
        /// Internal helper: checks that ship cells and neighbors are empty.
        /// </summary>
        private bool CanPlaceShip(int x, int y, int size, bool horizontal)
        {
            for (int i = 0; i < size; i++) //??
            {
                int cx = horizontal ? x + i : x;
                int cy = horizontal ? y : y + i;

                for (int yy = cy - 1; yy <= cy + 1; yy++)
                {
                    for (int xx = cx - 1; xx <= cx + 1; xx++)
                    {
                        if (xx < 0 || yy < 0 || xx >= GridSize || yy >= GridSize)
                            continue;

                        if (_cells[xx, yy] == CellState.Ship)
                            return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Apply enemy shot to this board.
        /// </summary>
        public CellState ReceiveShot(int x, int y, out bool isHit, out bool hasLost)
        {
            isHit = false;
            hasLost = false;

            if (_cells[x, y] == CellState.Ship)
            {
                _cells[x, y] = CellState.Hit;
                isHit = true;
                ShipCellsCount--;
                if (ShipCellsCount == 0)
                {
                    hasLost = true;
                }
            }
            else if (_cells[x, y] == CellState.Empty)
            {
                _cells[x, y] = CellState.Miss;
            }

            return _cells[x, y];
        }

        /// <summary>
        /// Records the result of our shot on the enemy board representation.
        /// </summary>
        public void MarkShotResult(int x, int y, bool isHit)
        {
            if (isHit)
                _cells[x, y] = CellState.Hit;
            else if (_cells[x, y] == CellState.Empty)
                _cells[x, y] = CellState.Miss;
        }
    }
}
