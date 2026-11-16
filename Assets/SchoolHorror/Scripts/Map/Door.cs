using UnityEngine;

namespace SchoolHorror
{
    public enum DoorState { Open, Closed, Locked }

    /// <summary>Một cánh cửa lớp: Mở / Đóng / Khóa.</summary>
    public sealed class Door : MonoBehaviour
    {
        public Vector2Int Tile { get; private set; }
        public DoorState State { get; private set; }
        public bool IsOpen => State == DoorState.Open;
        public bool IsLocked => State == DoorState.Locked;

        SpriteRenderer spriteRenderer;
        SpriteLibrary sprites;

        public void Init(Vector2Int tile, DoorState initialState, SpriteLibrary spriteLibrary)
        {
            Tile = tile;
            sprites = spriteLibrary;
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sharedMaterial = sprites.UnlitMaterial;
            spriteRenderer.sortingOrder = 6;
            transform.position = new Vector3(tile.x, tile.y, 0f);
            SetState(initialState);
        }

        /// <summary>Đóng/mở. Cửa đang khóa thì không đổi.</summary>
        public void Toggle()
        {
            if (IsLocked) return;
            SetState(IsOpen ? DoorState.Closed : DoorState.Open);
        }

        /// <summary>Khóa cửa đang đóng, hoặc mở khóa cửa đang khóa.</summary>
        public void ToggleLock()
        {
            if (State == DoorState.Closed) SetState(DoorState.Locked);
            else if (State == DoorState.Locked) SetState(DoorState.Closed);
        }

        /// <summary>Tượng phá cửa: bung khóa và mở toang.</summary>
        public void BreakOpen() => SetState(DoorState.Open);

        void SetState(DoorState state)
        {
            State = state;
            spriteRenderer.sprite = state == DoorState.Open ? sprites.DoorOpen
                : state == DoorState.Closed ? sprites.DoorClosed
                : sprites.DoorLocked;
        }
    }
}
