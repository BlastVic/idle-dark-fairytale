using UnityEngine;
using UnityEngine.UI;

/// <summary>Fits a bound equipment title on one line without changing its source name.</summary>
[RequireComponent(typeof(Text))]
public sealed class EquipmentSingleLineName : MonoBehaviour
{
    public int maximumSize = 20, minimumSize = 16;
    string source, rendered;
    float previousWidth = -1;
    Text label;
    public void SetTitle(string title) { source = title; Refresh(); }
    void LateUpdate()
    {
        if (!label) label = GetComponent<Text>();
        if (source == null || label.text != rendered) source = label.text;
        if (label.text != rendered || Mathf.Abs(label.rectTransform.rect.width - previousWidth) > .1f) Refresh();
    }
    void Refresh()
    {
        if (!label) label = GetComponent<Text>();
        previousWidth = label.rectTransform.rect.width;
        label.resizeTextForBestFit = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.text = (source ?? "").Replace('\n', ' ').Replace('\r', ' ');
        label.fontSize = maximumSize;
        while (label.fontSize > minimumSize && label.preferredWidth > previousWidth) label.fontSize--;
        if (previousWidth > 0 && label.preferredWidth > previousWidth) {
            string full = label.text;
            int length = full.Length;
            do {
                length--;
                if (length > 0 && char.IsHighSurrogate(full[length - 1])) length--;
                label.text = full.Substring(0, length).TrimEnd() + "…";
            } while (length > 0 && label.preferredWidth > previousWidth);
        }
        rendered = label.text;
    }
}
