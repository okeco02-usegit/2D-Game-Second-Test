using UnityEngine;

namespace SchoolHorror
{
    /// <summary>
    /// Dựng sơ đồ trường bỏ hoang: 2 hành lang chính + 2 lối hẹp phía sau các dãy phòng + 3 hành lang dọc,
    /// tạo thành mạng lưới nhiều ngã rẽ. Có 12 phòng học; mỗi phòng có 2 cửa và cửa sổ nhìn ra hành lang,
    /// tường sau có một mảng sập nên không phòng nào khóa kín được hoàn toàn.
    /// </summary>
    public sealed class SchoolLayoutBuilder
    {
        const int Width = 61;
        const int Height = 39;
        const int RoomWidth = 11;
        const int RoomHeight = 7;
        static readonly int[] RoomColumns = { 5, 17, 33, 45 };
        static readonly int[] DeskColumns = { 3, 4, 6, 7, 9 };
        static readonly int[] WindowColumns = { 3, 4, 6, 7 };

        readonly System.Random rng;
        SchoolMap map;

        public SchoolLayoutBuilder(System.Random rng) => this.rng = rng;

        public SchoolMap Build()
        {
            map = new SchoolMap(Width, Height);

            Fill(1, 1, 59, 2, TileType.CorridorFloor);   // lối hẹp sau dãy phòng dưới
            Fill(1, 12, 59, 3, TileType.CorridorFloor);  // hành lang chính A
            Fill(1, 24, 59, 2, TileType.CorridorFloor);  // lối hẹp giữa hai dãy phòng
            Fill(1, 35, 59, 3, TileType.CorridorFloor);  // hành lang chính B (có cổng)
            Fill(1, 1, 3, 37, TileType.CorridorFloor);   // hành lang dọc tây
            Fill(29, 1, 3, 37, TileType.CorridorFloor);  // hành lang dọc giữa
            Fill(57, 1, 3, 37, TileType.CorridorFloor);  // hành lang dọc đông

            int roomIndex = 0;
            foreach (int x0 in RoomColumns)
            {
                BuildRoom(x0, 4, true, roomIndex++);   // dãy dưới: cửa mở ra hành lang A (phía bắc)
                BuildRoom(x0, 16, false, roomIndex++); // dãy giữa: cửa mở ra hành lang A (phía nam)
                BuildRoom(x0, 27, true, roomIndex++);  // dãy trên: cửa mở ra hành lang B (phía bắc)
            }

            map.Set(29, 38, TileType.Gate);
            map.Set(30, 38, TileType.Gate);
            map.GateCenter = new Vector2(30f, 38.5f);

            map.PlayerStart = new Vector2Int(2, 13);
            map.AngelStart = new Vector2Int(58, 36);

            AddCorridorProps();
            AddOuterWindows();
            AddCorridorCandles();
            return map;
        }

        void AddOuterWindows()
        {
            // Tường ngoài hành lang trên cùng (y = 38): chỉ đặt 3 cửa sổ đôi (2 mở, 1 đóng)
            // - (2..3, 38): open_window góc trên trái (có ánh trăng rọi)
            // - (25..26, 38): closed_window giữa trái
            // - (47..48, 38): open_window bên phải
            int[] topCols = { 2, 3, 25, 26, 47, 48 };
            foreach (int x in topCols) map.Set(x, 38, TileType.Window);

            // Tường ngoài hành lang trái (x = 0) và phải (x = 60): 3 cửa sổ mỗi bên
            int[] sideRows = { 8, 20, 32 };
            foreach (int y in sideRows)
            {
                map.Set(0, y, TileType.Window);
                map.Set(Width - 1, y, TileType.Window);
            }
        }

        void AddCorridorProps()
        {
            // Tủ locker & máy bán nước dọc tường bắc hành lang B (y = 37)
            int[] singleB = { 12, 22, 45 };
            foreach (int x in singleB) map.Set(x, 37, TileType.CorridorProp);

            int[] doubleB = { 5, 8, 18, 35, 39, 50, 54 };
            foreach (int x in doubleB)
            {
                map.Set(x, 37, TileType.CorridorProp);
                map.Set(x + 1, 37, TileType.CorridorProp);
            }

            // Tủ locker xám dọc tường hành lang A (y = 14)
            foreach (int x0 in RoomColumns) map.Set(x0, 14, TileType.CorridorProp);

            // Tủ locker mở dọc hành lang giữa (y = 25)
            map.Set(27, 25, TileType.CorridorProp);
            map.Set(55, 25, TileType.CorridorProp);
        }

        /// <summary>Phòng học: bảng đen + bục giảng ở đầu phía tây, bàn học sinh quay về bảng.</summary>
        void BuildRoom(int x0, int y0, bool frontIsNorth, int index)
        {
            Fill(x0, y0, RoomWidth, RoomHeight, TileType.RoomFloor);
            int frontWall = frontIsNorth ? y0 + RoomHeight : y0 - 1;
            int backWall = frontIsNorth ? y0 - 1 : y0 + RoomHeight;
            int backRow = frontIsNorth ? y0 : y0 + RoomHeight - 1;

            for (int y = y0 + 1; y <= y0 + 5; y++) map.Set(x0, y, TileType.Podium);
            for (int y = y0 + 2; y <= y0 + 4; y++) map.Set(x0 - 1, y, TileType.Blackboard);
            map.Set(x0 + 1, y0 + 3, TileType.TeacherDesk);

            for (int row = 1; row <= 5; row += 2)
                foreach (int col in DeskColumns)
                    map.Set(x0 + col, y0 + row, TileType.Desk);

            // Hai cửa (đầu lớp và cuối lớp) cùng nằm trên tường hành lang, giữa là cửa sổ
            AddDoor(x0 + 1, frontWall);
            AddDoor(x0 + 9, frontWall);
            foreach (int col in WindowColumns) map.Set(x0 + col, frontWall, TileType.Window);

            // Mảng tường sau bị sập: lối vòng mà không khóa nào chặn được
            map.Set(x0 + rng.Next(2, 9), backWall, TileType.BrokenWall);

            map.Rooms.Add(new RectInt(x0, y0, RoomWidth, RoomHeight));
            if (index % 3 == 0) map.CandleTiles.Add(new Vector2Int(x0 + 10, backRow));
        }

        void AddDoor(int x, int y)
        {
            map.Set(x, y, TileType.Door);
            map.DoorTiles.Add(new Vector2Int(x, y));
        }

        void AddCorridorCandles()
        {
            map.CandleTiles.Add(new Vector2Int(22, 12));
            map.CandleTiles.Add(new Vector2Int(40, 14));
            map.CandleTiles.Add(new Vector2Int(1, 25));
            map.CandleTiles.Add(new Vector2Int(59, 20));
            map.CandleTiles.Add(new Vector2Int(26, 37));
            map.CandleTiles.Add(new Vector2Int(34, 37));
        }

        void Fill(int x0, int y0, int w, int h, TileType type)
        {
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++)
                    map.Set(x, y, type);
        }
    }
}
