using UnityEngine;

public class ColorSelection : MonoBehaviour
{
    [SerializeField] RectTransform[] colors;

    public void SelectColor(RectTransform selection)
    {
        foreach (var color in colors)
        {
            // anchoredPosition is usually what you want in UI
            Vector2 pos = color.anchoredPosition;

            if (color == selection) 
                pos.x = 0;   // selected one
            else 
                pos.x = 40;   // not selected

            color.anchoredPosition = pos;
        }
    }
}
