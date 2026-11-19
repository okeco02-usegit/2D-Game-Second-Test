using System.Collections.Generic;
using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Biến dữ liệu SchoolMap thành các GameObject trong scene: nền bản đồ, cửa, nến.</summary>
    public sealed class WorldBuilder
    {
        public readonly List<CandleLight> Candles = new List<CandleLight>();

        readonly SchoolMap map;
        readonly SpriteLibrary sprites;
        readonly Transform parent;
        readonly System.Random rng;

        public WorldBuilder(SchoolMap map, SpriteLibrary sprites, Transform parent, System.Random rng)
        {
            this.map = map;
            this.sprites = sprites;
            this.parent = parent;
            this.rng = rng;
        }

        public void Build()
        {
            BuildMapSprite();
            OutsideScenery.Build(map, sprites.UnlitMaterial, parent, rng);
            BuildDoors();
            BuildCandles();
        }

        void BuildMapSprite()
        {
            var renderer = NewChild("Map").AddComponent<SpriteRenderer>();
            renderer.sprite = new MapTextureBuilder(map, rng).Build();
            renderer.sharedMaterial = sprites.UnlitMaterial;
            renderer.sortingOrder = 0;
        }

        void BuildDoors()
        {
            foreach (Vector2Int tile in map.DoorTiles)
            {
                var door = NewChild("Door " + tile).AddComponent<Door>();
                door.Init(tile, rng.NextDouble() < 0.6 ? DoorState.Closed : DoorState.Open, sprites);
                map.RegisterDoor(door);
            }
        }

        void BuildCandles()
        {
            foreach (Vector2Int tile in map.CandleTiles)
            {
                Vector2 position = SchoolMap.TileCenter(tile);
                GameObject go = NewChild("Candle " + tile);
                go.transform.position = position;
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites.Candle;
                renderer.sharedMaterial = sprites.UnlitMaterial;
                renderer.sortingOrder = 5;
                Candles.Add(new CandleLight(position, GameConfig.CandleRadius, (float)rng.NextDouble() * 100f));
            }
        }

        GameObject NewChild(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
