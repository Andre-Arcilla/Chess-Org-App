using System;
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
        {
            yield break;
        }

        if (button == sender)
        {
            text.color = Color.white;
            icon.color = Color.white;
            background.color = new Color(background.color.r, background.color.g, background.color.b, 1f);
        }
        else
        {
            text.color = Color.black;
            icon.color = Color.black;
            background.color = new Color(background.color.r, background.color.g, background.color.b, 0f);
        }

        float targetWidth = (button == sender) ? 1.5f : 1.0f;
        float startWidth = layout.flexibleWidth;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            layout.flexibleWidth = Mathf.Lerp(startWidth, targetWidth, t);
            yield return null;
        }

        layout.flexibleWidth = targetWidth;
    }
}