using System;
using System.Collections.Generic;
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

    // ----------------------------------------------------------
    // ❗ ShipInfo : يمثل سفينة واحدة (مكانها، حجمها، اتجاهها)
    // ----------------------------------------------------------
    public class ShipInfo
    {
        public int X { get; }
        public int Y { get; }
        public int Size { get; }
        public bool Horizontal { get; }

        public ShipInfo(int x, int y, int size, bool horizontal)
        {
            X = x;
            Y = y;
            Size = size;
            Horizontal = horizontal;
        }

        // ترجع جميع خلايا السفينة، نستخدمها في اكتشاف تدمير سفينة لاحقاً
        public IEnumerable<(int x, int y)> GetCells()
        {
            for (int i = 0; i < Size; i++)
            {
                int cx = Horizontal ? X + i : X;
                int cy = Horizontal ? Y : Y + i;
                yield return (cx, cy);
            }
        }
    }

    /// <summary>
    /// Represents the 10x10 board and fleet placement logic.
    /// </summary>
    public class Board
    {
        // ----------------------------------------------------------
        // ❗ قائمة السفن الموضوعة في هذا اللوح
        // نحتاجها لكي نكتشف عندما يتم تدمير سفينة بالكامل
        // ----------------------------------------------------------
        private List<ShipInfo> _ships = new List<ShipInfo>();

        public const int GridSize = 10;

        public static readonly int[] FleetTemplate = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        public static readonly int TotalShipCells = FleetTemplate.Sum();

        private readonly CellState[,] _cells = new CellState[GridSize, GridSize];
        private readonly Random _rnd = new Random();

        public int ShipCellsCount { get; private set; }

        public bool IsFleetComplete => ShipCellsCount == TotalShipCells;

        public CellState GetCell(int x, int y) => _cells[x, y];

        public void Clear()
        {
            Array.Clear(_cells, 0, _cells.Length);

            // ❗ إعادة تعيين السفن
            _ships.Clear();

            ShipCellsCount = 0;
        }

        // ----------------------------------------------------------
        // وضع الأسطول عشوائيًا
        // ----------------------------------------------------------
        public void PlaceFleetRandom()
        {
            Clear();

            foreach (int size in FleetTemplate)
            {
                bool placed = false;
                while (!placed)
                {
                    bool horizontal = _rnd.Next(2) == 0;
                    int x = _rnd.Next(GridSize);
                    int y = _rnd.Next(GridSize);

                    if (!TryPlaceShip(x, y, size, horizontal))
                        continue;

                    placed = true;
                }
            }
        }

        // ----------------------------------------------------------
        // محاولة وضع سفينة واحدة
        // ----------------------------------------------------------
        public bool TryPlaceShip(int x, int y, int size, bool horizontal)
        {
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

            // وضع الخلايا
            for (int i = 0; i < size; i++)
            {
                int cx = horizontal ? x + i : x;
                int cy = horizontal ? y : y + i;
                _cells[cx, cy] = CellState.Ship;
            }

            ShipCellsCount += size;

            // ❗ تسجيل السفينة في قائمة السفن
            _ships.Add(new ShipInfo(x, y, size, horizontal));

            return true;
        }

        private bool CanPlaceShip(int x, int y, int size, bool horizontal)
        {
            for (int i = 0; i < size; i++)
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

        // ----------------------------------------------------------
        // Apply enemy shot + detect destroyed ship
        // ----------------------------------------------------------
        public CellState ReceiveShot(
            int x,
            int y,
            out bool isHit,
            out bool hasLost,
            out int destroyedShipSize)
        {
            isHit = false;
            hasLost = false;

            // ❗ القيمة الافتراضية: لم يتم تدمير سفينة
            destroyedShipSize = 0;

            // إصابة سفينة
            if (_cells[x, y] == CellState.Ship)
            {
                _cells[x, y] = CellState.Hit;
                isHit = true;

                ShipCellsCount--;
                if (ShipCellsCount == 0)
                    hasLost = true;

                // ----------------------------------------------------------
                // ❗ إضافة منطق اكتشاف تدمير سفينة
                // ----------------------------------------------------------
                ShipInfo destroyedShip = CheckIfShipDestroyed(x, y);
                if (destroyedShip != null)
                {
                    destroyedShipSize = destroyedShip.Size;
                }
            }
            else if (_cells[x, y] == CellState.Empty)
            {
                _cells[x, y] = CellState.Miss;
            }

            return _cells[x, y];
        }

        // ----------------------------------------------------------
        // تسجيل نتيجة الطلقة على لوحة العدو المحلية
        // ----------------------------------------------------------
        public void MarkShotResult(int x, int y, bool isHit)
        {
            if (isHit)
                _cells[x, y] = CellState.Hit;
            else if (_cells[x, y] == CellState.Empty)
                _cells[x, y] = CellState.Miss;
        }

        // ----------------------------------------------------------
        // ❗ فحص هل الضربة الأخيرة دمّرت سفينة بالكامل؟
        // ----------------------------------------------------------
        public ShipInfo CheckIfShipDestroyed(int hitX, int hitY)
        {
            foreach (var ship in _ships)
            {
                // هل هذه السفينة تحتوي على الخلية المضروبة؟
                foreach (var cell in ship.GetCells())
                {
                    if (cell.x == hitX && cell.y == hitY)
                    {
                        // فحص جميع خلايا السفينة
                        bool destroyed = true;
                        foreach (var c in ship.GetCells())
                        {
                            if (_cells[c.x, c.y] != CellState.Hit)
                            {
                                destroyed = false;
                                break;
                            }
                        }

                        if (destroyed)
                            return ship;
                    }
                }
            }

            return null;
        }
    }
}
