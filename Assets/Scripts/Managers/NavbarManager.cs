using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NavbarManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> navBarButtons = new List<GameObject>();
    [SerializeField] private ThemeManager themeManager;

    private Dictionary<GameObject, float> baseFontSizes = new Dictionary<GameObject, float>();

    public void SelectButton(GameObject senderObj)
    {
        StopAllCoroutines();

        foreach (var buttonObj in navBarButtons)
        {
            StartCoroutine(ButtonLerp(buttonObj, senderObj));
        }
    }

    private IEnumerator ButtonLerp(GameObject button, GameObject sender)
    {
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        var background = button.GetComponentsInChildren<Image>(true).FirstOrDefault(t => t.name == "Button Wrapper");
        var layout = button.GetComponentInParent<LayoutElement>();
        var icon = button.GetComponentsInChildren<Image>(true).FirstOrDefault(t => t.name == "Button Icon");

        if (text == null || background == null || layout == null || icon == null)
            yield break;

        // record base font size if not stored yet
        if (!baseFontSizes.ContainsKey(button))
            baseFontSizes[button] = text.fontSize;

        float baseFontSize = baseFontSizes[button];
        bool isSelected = (button == sender);

        // --- target values ---
        float duration = 0.25f;
        float elapsed = 0f;

        float startWidth = layout.flexibleWidth;
        float targetWidth = isSelected ? 1.5f : 1.0f;

        float startFontSize = text.fontSize;
        float targetFontSize = isSelected ? baseFontSize + 5f : baseFontSize;

        Color startTextColor = text.color;
        Color targetTextColor = isSelected ? Color.white : Color.black;

        Color startIconColor = icon.color;
        Color targetIconColor = isSelected ? Color.white : Color.black;

        Color startBgColor = background.color;
        Color targetBgColor = new Color(startBgColor.r, startBgColor.g, startBgColor.b, isSelected ? 1f : 0f);

        text.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;

        // --- lerp loop ---
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            layout.flexibleWidth = Mathf.Lerp(startWidth, targetWidth, t);
            text.fontSize = Mathf.Lerp(startFontSize, targetFontSize, t);
            text.color = Color.Lerp(startTextColor, targetTextColor, t);
            icon.color = Color.Lerp(startIconColor, targetIconColor, t);
            background.color = Color.Lerp(startBgColor, targetBgColor, t);

            yield return null;
        }

        layout.flexibleWidth = targetWidth;
        text.fontSize = targetFontSize;
        text.color = targetTextColor;
        icon.color = targetIconColor;
        background.color = targetBgColor;
    }
}
