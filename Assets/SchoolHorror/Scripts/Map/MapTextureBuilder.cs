using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Vẽ toàn bộ phần tĩnh của bản đồ (sàn, tường, nội thất) thành một sprite duy nhất.</summary>
    public sealed class MapTextureBuilder
    {
        const int T = GameConfig.PixelsPerTile;
        readonly SchoolMap map;
        readonly System.Random rng;
        readonly PixelBuffer buffer;

        public MapTextureBuilder(SchoolMap map, System.Random rng)
        {
            this.map = map;
            this.rng = rng;
            buffer = new PixelBuffer(map.Width * T, map.Height * T);
        }

        public Sprite Build()
        {
            ForEachTile(PaintBase);
            ForEachTile(PaintShadowsAndLitter);
            PaintDecorAndFurniture();
            return buffer.ToSprite(Vector2.zero);
        }

        void ForEachTile(System.Action<int, int> paint)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    paint(x, y);
        }

        void PaintBase(int x, int y)
        {
            switch (map.Get(x, y))
            {
                case TileType.CorridorFloor:
                case TileType.CorridorProp:
                    TilePainter.CorridorFloor(buffer, x, y, rng);
                    break;
                case TileType.RoomFloor:
                case TileType.Desk:
                case TileType.TeacherDesk:
                    TilePainter.RoomFloor(buffer, x, y, rng);
                    break;
                case TileType.Podium:
                    TilePainter.Podium(buffer, x, y, rng,
                        map.Get(x, y - 1) != TileType.Podium, map.Get(x, y + 1) != TileType.Podium);
                    break;
                case TileType.Door:
                    TilePainter.Threshold(buffer, x, y, rng);
                    break;
                case TileType.BrokenWall:
                    TilePainter.BrokenWall(buffer, x, y, rng);
                    break;
                case TileType.Blackboard:
                    TilePainter.Blackboard(buffer, x, y, rng,
                        map.Get(x, y - 1) != TileType.Blackboard, map.Get(x, y + 1) != TileType.Blackboard);
                    break;
                case TileType.Window:
                    TilePainter.Window(buffer, x, y, rng,
                        map.Get(x - 1, y) != TileType.Window, map.Get(x + 1, y) != TileType.Window);
                    break;
                case TileType.Gate:
                    TilePainter.Gate(buffer, x, y, rng, map.Get(x - 1, y) != TileType.Gate);
                    break;
                default:
                    // Tường có ô trống phía nam thì lộ mặt tường; còn lại là nóc tường.
                    if (map.InBounds(x, y - 1) && !map.IsWallLike(x, y - 1)) TilePainter.WallFace(buffer, x, y, rng);
                    else TilePainter.WallTop(buffer, x, y, rng);
                    break;
            }
        }

        void PaintShadowsAndLitter(int x, int y)
        {
            if (map.IsWallLike(x, y)) return;
            TilePainter.AmbientOcclusion(buffer, x, y,
                map.IsWallLike(x, y + 1), map.IsWallLike(x, y - 1), map.IsWallLike(x - 1, y), map.IsWallLike(x + 1, y));

            TileType type = map.Get(x, y);
            bool plainFloor = type == TileType.CorridorFloor || type == TileType.RoomFloor;
            if (plainFloor && rng.NextDouble() < 0.09) TilePainter.Paper(buffer, x, y, rng);
        }

        void PaintDecorAndFurniture()
        {
            for (int i = 0; i < map.Rooms.Count; i++)
            {
                RectInt room = map.Rooms[i];
                bool frontIsNorth = room.yMin == 4 || room.yMin == 27;
                TilePainter.ClassroomDecor(buffer, room, frontIsNorth, i, rng);
            }
            TilePainter.CorridorDecor(buffer, map, rng);
            ForEachTile(PaintFurniture);
        }

        void PaintFurniture(int x, int y)
        {
            TileType type = map.Get(x, y);
            if (type == TileType.Desk)
            {
                if (map.Get(x - 1, y) == TileType.Desk) return; // đã vẽ cùng nửa trái của bàn đôi
                if (map.Get(x + 1, y) == TileType.Desk) TilePainter.DoubleDesk(buffer, x, y, rng);
                else TilePainter.SingleDesk(buffer, x, y, rng);
            }
            else if (type == TileType.TeacherDesk) TilePainter.TeacherDesk(buffer, x, y);
        }
    }
}
