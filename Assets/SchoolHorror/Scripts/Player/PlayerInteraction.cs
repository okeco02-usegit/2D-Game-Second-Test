using UnityEngine;
using UnityEngine.InputSystem;

namespace SchoolHorror
{
    /// <summary>Tương tác bằng phím: [E] đóng/mở cửa và mở cổng, [F] khóa/mở khóa cửa (mất 0.5 giây mới xong).</summary>
    public sealed class PlayerInteraction : MonoBehaviour
    {
        const float GateRange = 1.7f;

        /// <summary>Dòng gợi ý hiện trên HUD cho vật đang đứng gần.</summary>
        public string Prompt { get; private set; } = "";

        SchoolMap map;
        GameManager game;
        AudioManager sound;
        PlayerController player;
        AngelController angel;
        Door lockingDoor;   // cửa đang được khóa / mở khóa dở
        float lockElapsed;

        public void Init(SchoolMap schoolMap, GameManager gameManager, AudioManager audioManager, PlayerController playerController, AngelController angelController)
        {
            map = schoolMap;
            game = gameManager;
            sound = audioManager;
            player = playerController;
            angel = angelController;
        }

        void Update()
        {
            Prompt = "";
            if (!game.IsPlaying) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool pressE = keyboard.eKey.wasPressedThisFrame;
            bool pressF = keyboard.fKey.wasPressedThisFrame;

            if (lockingDoor != null)
            {
                ContinueLocking();
                return;
            }

            if (Vector2.Distance(player.Position, map.GateCenter) < GateRange)
            {
                HandleGate(pressE);
                return;
            }

            Door door = FindNearestDoor();
            if (door != null) HandleDoor(door, pressE, pressF);
        }

        void HandleGate(bool pressE)
        {
            bool hasAllKeys = player.Keys >= GameConfig.KeyCount;
            Prompt = hasAllKeys
                ? "[E] Open Exit Gate"
                : "Gate is locked - need " + GameConfig.KeyCount + " keys (" + player.Keys + "/" + GameConfig.KeyCount + ")";
            if (!pressE) return;
            if (hasAllKeys) game.Win();
            else sound.PlayRattle();
        }

        void HandleDoor(Door door, bool pressE, bool pressF)
        {
            switch (door.State)
            {
                case DoorState.Open: Prompt = "[E] Close door"; break;
                case DoorState.Closed: Prompt = "[E] Open door     [F] Lock door"; break;
                default: Prompt = "[F] Unlock door"; break;
            }

            if (pressE)
            {
                if (door.IsLocked) sound.PlayRattle();
                else if (!door.IsOpen || !IsDoorwayOccupied(door))
                {
                    door.Toggle();
                    sound.PlayCreak();
                }
            }
            else if (pressF && !door.IsOpen)
            {
                lockingDoor = door;
                lockElapsed = 0f;
            }
        }

        /// <summary>Đang vặn khóa: phải đứng cạnh cửa đủ thời gian. Bỏ đi hoặc cửa bị phá thì hủy.</summary>
        void ContinueLocking()
        {
            bool inRange = (SchoolMap.TileCenter(lockingDoor.Tile) - player.Position).sqrMagnitude
                < GameConfig.InteractRange * GameConfig.InteractRange;
            if (lockingDoor.IsOpen || !inRange)
            {
                lockingDoor = null;
                return;
            }

            Prompt = lockingDoor.IsLocked ? "Unlocking door..." : "Locking door...";
            lockElapsed += Time.deltaTime;
            if (lockElapsed < GameConfig.LockSeconds) return;

            lockingDoor.ToggleLock();
            sound.PlayClick();
            lockingDoor = null;
        }

        bool IsDoorwayOccupied(Door door) => player.OverlapsTile(door.Tile) || angel.Tile == door.Tile;

        Door FindNearestDoor()
        {
            Door nearest = null;
            float best = GameConfig.InteractRange * GameConfig.InteractRange;
            foreach (Door door in map.Doors)
            {
                float d2 = (SchoolMap.TileCenter(door.Tile) - player.Position).sqrMagnitude;
                if (d2 < best) { best = d2; nearest = door; }
            }
            return nearest;
        }
    }
}
