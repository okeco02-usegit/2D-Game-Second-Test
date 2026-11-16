using UnityEngine;
using UnityEngine.UI;

namespace SchoolHorror
{
    /// <summary>Một thanh trạng thái có nhãn (pin, mỏi mắt, thể lực), tùy chọn có vạch mốc.</summary>
    public sealed class UiBar
    {
        const float LabelWidth = 96f;
        const float BarWidth = 200f;
        const float BarHeight = 14f;

        readonly Image fill;

        public UiBar(Transform parent, string label, float topOffset, params float[] tickMarks)
        {
            var topLeft = new Vector2(0f, 1f);

            Text text = UiFactory.CreateText(parent, label + " Label", 15, TextAnchor.MiddleLeft, new Color(0.85f, 0.82f, 0.9f));
            text.text = label;
            UiFactory.Place(text.rectTransform, topLeft, new Vector2(20f, -topOffset), new Vector2(LabelWidth, 20f));

            Image background = UiFactory.CreateImage(parent, label + " Bar", new Color(0.05f, 0.04f, 0.09f, 0.85f));
            UiFactory.Place(background.rectTransform, topLeft, new Vector2(20f + LabelWidth, -topOffset - 3f), new Vector2(BarWidth, BarHeight));

            fill = UiFactory.CreateImage(background.transform, "Fill", Color.white);
            UiFactory.Stretch(fill.rectTransform);

            foreach (float mark in tickMarks)
            {
                Image tick = UiFactory.CreateImage(background.transform, "Tick", new Color(0f, 0f, 0f, 0.8f));
                tick.rectTransform.anchorMin = new Vector2(mark, 0f);
                tick.rectTransform.anchorMax = new Vector2(mark, 1f);
                tick.rectTransform.sizeDelta = new Vector2(2f, 0f);
            }
        }

        public void Set(float value, Color color)
        {
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.color = color;
        }
    }
}
