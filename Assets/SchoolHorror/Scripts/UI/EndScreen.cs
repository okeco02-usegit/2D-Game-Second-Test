using UnityEngine;
using UnityEngine.UI;

namespace SchoolHorror
{
    /// <summary>Màn hình kết thúc: jumpscare khi bị bắt, hoặc thông báo thoát thành công.</summary>
    public sealed class EndScreen : MonoBehaviour
    {
        const float ScareDuration = 1.3f;

        Image background;
        Image face;
        Text title;
        Text subtitle;
        bool scaring;
        float elapsed;

        public void Init(Sprite jumpscareFace)
        {
            var rect = gameObject.AddComponent<RectTransform>();
            UiFactory.Stretch(rect);

            background = UiFactory.CreateImage(transform, "Background", Color.black);
            UiFactory.Stretch(background.rectTransform);

            face = UiFactory.CreateImage(transform, "Jumpscare Face", Color.white);
            face.sprite = jumpscareFace;
            face.preserveAspect = true;
            UiFactory.Stretch(face.rectTransform);

            title = UiFactory.CreateText(transform, "Title", 64, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1000f, 90f));
            subtitle = UiFactory.CreateText(transform, "Subtitle", 26, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.85f));
            UiFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1000f, 40f));

            gameObject.SetActive(false);
        }

        public void ShowJumpscare()
        {
            gameObject.SetActive(true);
            scaring = true;
            elapsed = 0f;
            face.enabled = true;
            title.enabled = false;
            subtitle.enabled = false;
        }

        public void ShowWin()
        {
            gameObject.SetActive(true);
            face.enabled = false;
            ShowMessage("BẠN ĐÃ THOÁT!", new Color(0.55f, 0.9f, 0.65f), new Color(0f, 0f, 0f, 0.88f));
        }

        void ShowMessage(string text, Color titleColor, Color backgroundColor)
        {
            background.color = backgroundColor;
            title.enabled = true;
            title.text = text;
            title.color = titleColor;
            subtitle.enabled = true;
            subtitle.text = "Nhấn [R] để chơi lại";
        }

        void Update()
        {
            if (!scaring) return;
            elapsed += Time.unscaledDeltaTime;

            // Khuôn mặt lao vào màn hình, rung giật trên nền đỏ nhấp nháy
            float zoom = Mathf.Lerp(0.6f, 1.9f, Mathf.Clamp01(elapsed / 0.25f));
            face.rectTransform.localScale = new Vector3(zoom, zoom, 1f);
            face.rectTransform.anchoredPosition = Random.insideUnitCircle * 26f;
            background.color = Mathf.Sin(elapsed * 60f) > 0f ? new Color(0.5f, 0f, 0f) : Color.black;

            if (elapsed < ScareDuration) return;
            scaring = false;
            face.enabled = false;
            ShowMessage("BẠN ĐÃ BỊ BẮT", new Color(0.85f, 0.15f, 0.15f), Color.black);
        }
    }
}
