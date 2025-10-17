using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NavbarManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> navBarButtons = new List<GameObject>();
    [SerializeField] private ThemeManager themeManager;

    public void SelectButton(GameObject senderObj)
    {
        foreach (var buttonObj in navBarButtons)
        {
            var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            var image = buttonObj.GetComponentInChildren<Image>();
            if (text == null || image == null)
            {
                continue;
            }

            if (buttonObj == senderObj)
            {
                text.color = Color.black;
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
            }
            else
            {
                text.color = themeManager._currentTheme.NavBarText;
                image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
            }
        }
    }
}