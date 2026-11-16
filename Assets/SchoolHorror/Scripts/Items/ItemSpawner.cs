using System.Collections.Generic;
using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Rải chìa khóa (mỗi chìa một phòng khác nhau) và pin (trong phòng lẫn hành lang) ngẫu nhiên mỗi ván.</summary>
    public sealed class ItemSpawner
    {
        readonly SchoolMap map;
        readonly SpriteLibrary sprites;
        readonly Transform parent;
        readonly System.Random rng;
        readonly HashSet<Vector2Int> usedTiles = new HashSet<Vector2Int>();

        public ItemSpawner(SchoolMap map, SpriteLibrary sprites, Transform parent, System.Random rng)
        {
            this.map = map;
            this.sprites = sprites;
            this.parent = parent;
            this.rng = rng;
            foreach (Vector2Int candle in map.CandleTiles) usedTiles.Add(candle);
        }

        public void Spawn(PlayerController player, AudioManager sound)
        {
            var rooms = new List<RectInt>(map.Rooms);
            Shuffle(rooms);

            for (int i = 0; i < GameConfig.KeyCount; i++)
                Create<KeyPickup>("Key", sprites.Key, RandomTile(rooms[i % rooms.Count], TileType.RoomFloor), player, sound);

            var wholeMap = new RectInt(0, 0, map.Width, map.Height);
            for (int i = 0; i < GameConfig.BatteryCount; i++)
            {
                bool inRoom = i % 2 == 0;
                Vector2Int tile = inRoom
                    ? RandomTile(rooms[rng.Next(rooms.Count)], TileType.RoomFloor)
                    : RandomTile(wholeMap, TileType.CorridorFloor);
                Create<BatteryPickup>("Battery", sprites.Battery, tile, player, sound);
            }
        }

        void Create<T>(string name, Sprite sprite, Vector2Int tile, PlayerController player, AudioManager sound) where T : Pickup
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = SchoolMap.TileCenter(tile);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = sprites.UnlitMaterial;
            renderer.sortingOrder = 5;
            go.AddComponent<T>().Init(player, sound);
        }

        Vector2Int RandomTile(RectInt area, TileType type)
        {
            while (true)
            {
                var tile = new Vector2Int(rng.Next(area.xMin, area.xMax), rng.Next(area.yMin, area.yMax));
                if (map.Get(tile.x, tile.y) == type && tile != map.PlayerStart && usedTiles.Add(tile)) return tile;
            }
        }

        void Shuffle(List<RectInt> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                RectInt tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
