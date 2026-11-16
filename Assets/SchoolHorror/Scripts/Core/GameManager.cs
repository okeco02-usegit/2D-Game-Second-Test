using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SchoolHorror
{
    public enum GameState { Menu, Playing, Won, Lost }

    /// <summary>
    /// Điểm khởi đầu của game: dựng toàn bộ thế giới khi vào scene, nối các hệ thống với nhau
    /// và quản lý trạng thái thắng / thua / chơi lại.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public GameState State { get; private set; } = GameState.Menu;
        public bool IsPlaying => State == GameState.Playing;

        Camera cam;
        PlayerController player;
        LightingSystem lighting;
        HudController hud;
        AudioManager sound;

        void Awake()
        {
            var rng = new System.Random();
            var sprites = new SpriteLibrary();
            SchoolMap map = new SchoolLayoutBuilder(rng).Build();

            sound = CreateChild("Audio").AddComponent<AudioManager>();
            var world = new WorldBuilder(map, sprites, CreateChild("World").transform, rng);
            world.Build();
            cam = SetupCamera();

            GameObject playerObject = CreateSpriteObject("Player", sprites.Player, sprites.UnlitMaterial, 10, map.PlayerStart);
            player = playerObject.AddComponent<PlayerController>();
            var flashlight = playerObject.AddComponent<Flashlight>();
            var blink = playerObject.AddComponent<BlinkSystem>();
            var interaction = playerObject.AddComponent<PlayerInteraction>();

            lighting = CreateChild("Lighting").AddComponent<LightingSystem>();
            GameObject angelObject = CreateSpriteObject("Angel", sprites.Angel, sprites.UnlitMaterial, 11, map.AngelStart);
            var angel = angelObject.AddComponent<AngelController>();

            flashlight.Init(map, cam, this, sound);
            blink.Init(this, sound);
            player.Init(map, this, sound, flashlight, playerObject.GetComponent<SpriteRenderer>(), sprites);
            interaction.Init(map, this, sound, player, angel);
            lighting.Init(map, cam, flashlight, world.Candles, sprites.UnlitMaterial);
            angel.Init(map, this, sound, player, blink, lighting);

            new ItemSpawner(map, sprites, CreateChild("Items").transform, rng).Spawn(player, sound);

            hud = CreateChild("HUD").AddComponent<HudController>();
            hud.Init(player, blink, interaction, sprites.JumpscareFace);
            hud.gameObject.SetActive(false);

            var menu = CreateChild("MainMenu").AddComponent<MainMenu>();
            menu.Init(this, sound, sprites);
        }

        public void StartGame()
        {
            if (State != GameState.Menu) return;
            State = GameState.Playing;
            hud.gameObject.SetActive(true);
            hud.OnGameStart();
        }

        void Update()
        {
            if (State != GameState.Won && State != GameState.Lost) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void LateUpdate()
        {
            // Thứ tự quan trọng: camera bám theo người chơi trước, rồi mới tính mặt nạ ánh sáng.
            Vector2 p = player.Position;
            cam.transform.position = new Vector3(p.x, p.y + 0.4f, cam.transform.position.z);
            lighting.Render();
            if (hud.gameObject.activeSelf) hud.Refresh();
        }

        public void Win()
        {
            if (!IsPlaying) return;
            State = GameState.Won;
            sound.SetDanger(0f);
            sound.SetScrapeVolume(0f);
            sound.PlayWin();
            hud.ShowWin();
        }

        public void Lose()
        {
            if (!IsPlaying) return;
            State = GameState.Lost;
            sound.PlayJumpscare();
            hud.ShowJumpscare();
        }

        Camera SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                go.transform.position = new Vector3(0f, 0f, -10f);
            }
            camera.orthographic = true;
            camera.orthographicSize = GameConfig.CameraOrthoSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            return camera;
        }

        GameObject CreateChild(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go;
        }

        GameObject CreateSpriteObject(string name, Sprite sprite, Material material, int sortingOrder, Vector2Int tile)
        {
            GameObject go = CreateChild(name);
            go.transform.position = SchoolMap.TileCenter(tile);
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sharedMaterial = material;
            spriteRenderer.sortingOrder = sortingOrder;
            return go;
        }
    }
}
