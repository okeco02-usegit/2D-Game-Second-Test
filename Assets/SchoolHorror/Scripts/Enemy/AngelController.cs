using System.Collections.Generic;
using UnityEngine;

namespace SchoolHorror
{
    /// <summary>
    /// Tượng Thiên Thần: hóa đá khi bị nhìn thấy trong vùng sáng lúc mắt người chơi đang mở,
    /// còn lại thì bám theo người chơi bằng A*. Gặp cửa đóng thì đứng đập cho tới khi phá được;
    /// gặp cửa khóa thì không phá nổi, phải vòng lối khác (ví dụ mảng tường sập sau phòng).
    /// </summary>
    public sealed class AngelController : MonoBehaviour
    {
        const float RepathInterval = 0.35f;
        const float BangInterval = 0.65f;
        static readonly Vector2 BodyOffset = new Vector2(0f, 0.3f);

        public bool IsFrozen { get; private set; }
        public Vector2 Position => transform.position;
        public Vector2Int Tile => SchoolMap.TileAt(Position);

        enum DoorAction { None, Knocking, JigglingLocked }

        SchoolMap map;
        GameManager game;
        AudioManager sound;
        PlayerController player;
        BlinkSystem blink;
        LightingSystem lighting;
        Pathfinder pathfinder;
        readonly List<Vector2Int> path = new List<Vector2Int>();
        readonly HashSet<Door> knownBlockedDoors = new HashSet<Door>();
        float repathTimer;
        float graceTimer = GameConfig.AngelGraceSeconds;
        Door currentDoorTarget;
        DoorAction doorAction = DoorAction.None;
        float doorActionElapsed;

        public void Init(SchoolMap schoolMap, GameManager gameManager, AudioManager audioManager,
            PlayerController playerController, BlinkSystem blinkSystem, LightingSystem lightingSystem)
        {
            map = schoolMap;
            game = gameManager;
            sound = audioManager;
            player = playerController;
            blink = blinkSystem;
            lighting = lightingSystem;
            pathfinder = new Pathfinder(map);
        }

        void Update()
        {
            if (!game.IsPlaying)
            {
                sound.SetScrapeVolume(0f);
                return;
            }

            float distance = Vector2.Distance(Position, player.Position);
            sound.SetDanger(Mathf.Clamp01(1f - distance / 10f));

            IsFrozen = IsObserved();
            graceTimer -= Time.deltaTime;
            float speed = GameConfig.AngelBaseSpeed + player.Keys * GameConfig.AngelSpeedPerKey;
            bool moved = !IsFrozen && graceTimer <= 0f && Chase(distance);
            float proximity = Mathf.Clamp01(1f - distance / 18f);
            float stepVol = moved ? proximity * (0.25f + 0.75f * proximity) : 0f;
            sound.SetScrapeVolume(stepVol, speed / GameConfig.AngelBaseSpeed);

            if (!IsFrozen && Vector2.Distance(Position, player.Position) < GameConfig.AngelCatchDistance)
                game.Lose();
        }

        /// <summary>Bị nhìn thấy = mắt mở + không bị che khuất + đang đứng trong vùng sáng (đèn pin hoặc nến).</summary>
        bool IsObserved()
        {
            if (blink.EyesClosed) return false;
            Vector2 body = Position + BodyOffset;
            if (!map.HasLineOfSight(player.Flashlight.Origin, body)) return false;
            return lighting.IsIlluminated(body);
        }

        /// <summary>Tiến một bước về phía người chơi. Trả về true nếu tượng thực sự di chuyển.</summary>
        bool Chase(float distance)
        {
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f || path.Count == 0)
            {
                repathTimer = RepathInterval;
                if (!pathfinder.FindPath(Tile, SchoolMap.TileAt(player.Position), path, knownBlockedDoors)) return false;
            }

            Vector2 target = player.Position; // cùng ô hoặc ô cuối: lao thẳng vào người chơi
            if (path.Count > 0)
            {
                Door door = map.GetDoor(path[0]);
                if (door != null && !door.IsOpen)
                {
                    InteractWithDoor(door, distance);
                    return false;
                }
                if (door != null && door.IsOpen && currentDoorTarget == door)
                {
                    currentDoorTarget = null;
                    doorAction = DoorAction.None;
                }
                if (path.Count > 1) target = SchoolMap.TileCenter(path[0]);
            }

            float speed = GameConfig.AngelBaseSpeed + player.Keys * GameConfig.AngelSpeedPerKey;
            Vector2 next = Vector2.MoveTowards(Position, target, speed * Time.deltaTime);
            transform.position = next;
            if (path.Count > 0 && (next - SchoolMap.TileCenter(path[0])).sqrMagnitude < 0.0004f) path.RemoveAt(0);
            return true;
        }

        void InteractWithDoor(Door door, float distance)
        {
            float volume = Mathf.Clamp01(1.1f - distance / 16f);

            // 1. Cửa đang KHÓA: thử mở trong 0.5s với tiếng vặn nắm đấm, không mở được thì đổi hướng
            if (door.IsLocked)
            {
                if (currentDoorTarget != door || doorAction != DoorAction.JigglingLocked)
                {
                    currentDoorTarget = door;
                    doorAction = DoorAction.JigglingLocked;
                    doorActionElapsed = 0f;
                    sound.PlayDoorHandleJiggle(volume);
                }

                doorActionElapsed += Time.deltaTime;
                if (doorActionElapsed >= GameConfig.DoorLockCheckSeconds)
                {
                    knownBlockedDoors.Add(door);
                    currentDoorTarget = null;
                    doorAction = DoorAction.None;
                    path.Clear();
                    repathTimer = 0f; // ép tính toán đường vòng khác ngay
                }
                return;
            }

            // 2. Cửa đang ĐÓNG (không khóa): cố mở cửa, gõ cửa trong thời gian mở rồi bung cửa kêu kẹt
            if (!door.IsOpen)
            {
                if (knownBlockedDoors.Contains(door)) knownBlockedDoors.Remove(door);

                if (currentDoorTarget != door || doorAction != DoorAction.Knocking)
                {
                    currentDoorTarget = door;
                    doorAction = DoorAction.Knocking;
                    doorActionElapsed = 0f;
                    sound.PlayDoorKnock(volume);
                }

                doorActionElapsed += Time.deltaTime;
                if (doorActionElapsed >= GameConfig.ClosedDoorBreakSeconds)
                {
                    door.BreakOpen();
                    sound.PlayDoorSqueak(volume);
                    currentDoorTarget = null;
                    doorAction = DoorAction.None;
                }
            }
        }
    }
}
