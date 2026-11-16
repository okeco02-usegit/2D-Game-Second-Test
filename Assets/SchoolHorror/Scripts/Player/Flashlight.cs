using UnityEngine;
using UnityEngine.InputSystem;

namespace SchoolHorror
{
    /// <summary>Đèn pin hình nón chiếu theo chuột, hao pin dần; pin yếu thì mờ và ngắn lại.</summary>
    public sealed class Flashlight : MonoBehaviour
    {
        public float Battery { get; private set; } = 1f;
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public Vector2 Origin => (Vector2)transform.position + new Vector2(0f, 0.25f);
        public bool IsOn => Battery > 0f;
        /// <summary>Pin khỏe thì chùm sáng vươn tới bức tường xa nhất; pin yếu thì ngắn dần.</summary>
        public float Range => Mathf.Lerp(GameConfig.FlashlightLowRange, GameConfig.FlashlightRange, Power);
        public float Intensity => Mathf.Lerp(0.45f, 1f, Power) * flicker;
        /// <summary>Độ sáng còn lại ở cuối chùm sáng: pin khỏe vẫn thấy rõ tường xa, pin yếu thì tắt dần về 0.</summary>
        public float FarBrightness => 0.55f * Power;
        public float HalfAngleRad => GameConfig.FlashlightHalfAngleDeg * Mathf.Deg2Rad;

        /// <summary>1 khi pin còn khỏe; tụt dần về 0 khi pin dưới ngưỡng yếu.</summary>
        float Power => Mathf.Clamp01(Battery / GameConfig.BatteryLowThreshold);

        SchoolMap map;
        Camera cam;
        GameManager game;
        float flicker = 1f;

        public void Init(SchoolMap schoolMap, Camera camera, GameManager gameManager)
        {
            map = schoolMap;
            cam = camera;
            game = gameManager;
        }

        void Update()
        {
            if (!game.IsPlaying) return;

            Battery = Mathf.Max(0f, Battery - GameConfig.BatteryDrainPerSec * Time.deltaTime);
            bool low = Battery < GameConfig.BatteryLowThreshold;
            flicker = low ? 0.8f + 0.2f * Mathf.PerlinNoise(Time.time * 12f, 0f) : 1f;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector3 screen = mouse.position.ReadValue();
            screen.z = -cam.transform.position.z;
            Vector2 toMouse = (Vector2)cam.ScreenToWorldPoint(screen) - Origin;
            if (toMouse.sqrMagnitude > 0.01f) AimDirection = toMouse.normalized;
        }

        public void Recharge(float amount) => Battery = Mathf.Min(1f, Battery + amount);

        /// <summary>Điểm p có nằm trong chùm sáng và không bị tường/cửa che không.</summary>
        public bool Illuminates(Vector2 p)
        {
            if (!IsOn) return false;
            Vector2 toPoint = p - Origin;
            if (toPoint.sqrMagnitude > Range * Range) return false;
            if (Vector2.Angle(AimDirection, toPoint) > GameConfig.FlashlightHalfAngleDeg) return false;
            return map.HasLineOfSight(Origin, p);
        }
    }
}
