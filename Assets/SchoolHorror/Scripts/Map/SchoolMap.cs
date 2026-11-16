using System.Collections.Generic;
using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Dữ liệu lưới của ngôi trường + các truy vấn va chạm / tầm nhìn.</summary>
    public sealed class SchoolMap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly List<RectInt> Rooms = new List<RectInt>();
        public readonly List<Vector2Int> DoorTiles = new List<Vector2Int>();
        public readonly List<Vector2Int> CandleTiles = new List<Vector2Int>();
        public readonly List<Door> Doors = new List<Door>();
        public Vector2 GateCenter;
        public Vector2Int PlayerStart;
        public Vector2Int AngelStart;

        readonly TileType[,] tiles;
        readonly Door[,] doors;

        public SchoolMap(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new TileType[width, height]; // mặc định toàn Wall
            doors = new Door[width, height];
        }

        public static Vector2 TileCenter(Vector2Int t) => new Vector2(t.x + 0.5f, t.y + 0.5f);
        public static Vector2Int TileAt(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public TileType Get(int x, int y) => InBounds(x, y) ? tiles[x, y] : TileType.Wall;
        public void Set(int x, int y, TileType type) => tiles[x, y] = type;
        public Door GetDoor(int x, int y) => InBounds(x, y) ? doors[x, y] : null;
        public Door GetDoor(Vector2Int t) => GetDoor(t.x, t.y);

        public void RegisterDoor(Door door)
        {
            doors[door.Tile.x, door.Tile.y] = door;
            Doors.Add(door);
        }

        /// <summary>Tường, bảng đen, cổng, cửa sổ: phần kiến trúc cố định, không đi xuyên qua được.</summary>
        public bool IsWallLike(int x, int y)
        {
            TileType t = Get(x, y);
            return t == TileType.Wall || t == TileType.Blackboard || t == TileType.Gate || t == TileType.Window;
        }

        bool IsFurniture(int x, int y)
        {
            TileType t = Get(x, y);
            return t == TileType.Desk || t == TileType.TeacherDesk;
        }

        bool IsClosedDoor(int x, int y)
        {
            Door d = GetDoor(x, y);
            return d != null && !d.IsOpen;
        }

        /// <summary>Chặn đường đi của người chơi.</summary>
        public bool IsSolid(int x, int y) => IsWallLike(x, y) || IsFurniture(x, y) || IsClosedDoor(x, y);

        /// <summary>Chặn ánh sáng và tầm nhìn (bàn ghế thấp và kính cửa sổ thì không chặn).</summary>
        public bool IsOpaque(int x, int y) => (IsWallLike(x, y) && Get(x, y) != TileType.Window) || IsClosedDoor(x, y);

        /// <summary>Ô không phải tường/bàn ghế. Cửa có qua được hay không do Pathfinder quyết định.</summary>
        public bool IsWalkableForAngel(int x, int y) => !IsWallLike(x, y) && !IsFurniture(x, y);

        /// <summary>Bắn tia trên lưới (DDA). Trả về khoảng cách tới ô chắn sáng đầu tiên, tối đa maxDist.</summary>
        public float Raycast(Vector2 origin, Vector2 dir, float maxDist)
        {
            int x = Mathf.FloorToInt(origin.x);
            int y = Mathf.FloorToInt(origin.y);
            int stepX = dir.x > 0 ? 1 : -1;
            int stepY = dir.y > 0 ? 1 : -1;
            float deltaX = dir.x == 0 ? float.PositiveInfinity : Mathf.Abs(1f / dir.x);
            float deltaY = dir.y == 0 ? float.PositiveInfinity : Mathf.Abs(1f / dir.y);
            float nextX = dir.x == 0 ? float.PositiveInfinity : (dir.x > 0 ? x + 1 - origin.x : origin.x - x) * deltaX;
            float nextY = dir.y == 0 ? float.PositiveInfinity : (dir.y > 0 ? y + 1 - origin.y : origin.y - y) * deltaY;

            while (true)
            {
                float t;
                if (nextX < nextY) { t = nextX; nextX += deltaX; x += stepX; }
                else { t = nextY; nextY += deltaY; y += stepY; }
                if (t >= maxDist) return maxDist;
                if (IsOpaque(x, y)) return t;
            }
        }

        public bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float dist = delta.magnitude;
            if (dist < 0.001f) return true;
            return Raycast(from, delta / dist, dist) >= dist - 0.001f;
        }
    }
}
