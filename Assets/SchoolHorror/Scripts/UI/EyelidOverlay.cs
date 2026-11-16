using UnityEngine;
using UnityEngine.UI;

namespace SchoolHorror
{
    /// <summary>Hai mí mắt đen khép từ trên xuống và từ dưới lên khi chớp mắt.</summary>
    public sealed class EyelidOverlay
    {
        readonly RectTransform topLid;
        readonly RectTransform bottomLid;

        public EyelidOverlay(Transform parent)
        {
            topLid = CreateLid(parent, "Top Eyelid");
            bottomLid = CreateLid(parent, "Bottom Eyelid");
            SetClosedAmount(0f);
        }

        static RectTransform CreateLid(Transform parent, string name)
        {
            Image lid = UiFactory.CreateImage(parent, name, Color.black);
            UiFactory.Stretch(lid.rectTransform);
            return lid.rectTransform;
        }

        /// <summary>0 = mở hẳn, 1 = nhắm kín (hai mí chồng lên nhau một chút để không hở khe sáng).</summary>
        public void SetClosedAmount(float amount)
        {
            float cover = Mathf.Clamp01(amount) * 0.52f;
            topLid.anchorMin = new Vector2(0f, 1f - cover);
            bottomLid.anchorMax = new Vector2(1f, cover);
        }
    }
}
