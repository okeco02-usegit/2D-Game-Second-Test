namespace SchoolHorror
{
    /// <summary>Tất cả thông số cân bằng game nằm ở một chỗ để dễ chỉnh.</summary>
    public static class GameConfig
    {
        public const int PixelsPerTile = 16;
        public const float CameraOrthoSize = 5.625f; // 180px chiều cao / 16px mỗi ô / 2

        // Người chơi
        public const float WalkSpeed = 2.9f;
        public const float RunSpeed = 4.8f;
        public const float PlayerRadius = 0.28f;
        public const float StaminaDrainPerSec = 0.28f;
        public const float StaminaRegenPerSec = 0.16f;
        public const float StaminaMinToRun = 0.2f;

        // Đèn pin
        public const float FlashlightRange = 40f;    // đủ dài để chạm bức tường xa nhất trong tầm nhìn
        public const float FlashlightLowRange = 5f;  // tầm chiếu khi pin sắp cạn
        public const float VisionRange = 40f;
        public const float FlashlightHalfAngleDeg = 27f;
        public const float BatteryDrainPerSec = 1f / 140f;
        public const float BatteryLowThreshold = 0.3f;
        public const float BatteryPickupAmount = 0.45f;
        public const float PlayerGlowRadius = 1.6f;

        // Nến
        public const float CandleRadius = 3.2f;

        // Chớp mắt
        public const float FatigueMax = 12f;
        public const float FatigueQuickLimit = 4f;
        public const float FatigueMediumLimit = 8f;
        public const float BlinkQuick = 0.5f;
        public const float BlinkMedium = 1f;
        public const float BlinkLong = 2f;

        // Tượng Angel
        public const float AngelBaseSpeed = 2.15f;
        public const float AngelSpeedPerKey = 0.46f;
        public const float AngelCatchDistance = 0.55f;
        public const float AngelGraceSeconds = 4f;
        public const float ClosedDoorBreakSeconds = 1.5f;
        public const float DoorLockCheckSeconds = 0.5f; // thời gian Angel thử vặn nắm đấm cửa đang khóa
        public const float LockSeconds = 0.5f; // thời gian khóa / mở khóa một cánh cửa

        // Vật phẩm & tương tác
        public const int KeyCount = 5;
        public const int BatteryCount = 7;
        public const float InteractRange = 1.4f;
        public const float PickupRange = 0.6f;
    }
}
