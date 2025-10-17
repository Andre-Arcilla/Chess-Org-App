
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThemeManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> pages;
    [SerializeField] private List<ColorPaletteSO> themes;
    [SerializeField] private ColorPaletteSO currentTheme;
    public ColorPaletteSO _currentTheme => currentTheme;

    void Start()
    {
        Theme01();
    }

    public void Theme01()
    {
        currentTheme = themes[0];
        ChangeTheme(themes[0]);
    }
    
    public void Theme02()
    {
        currentTheme = themes[1];
        ChangeTheme(themes[1]);
    }

    public void Theme03()
    {
        currentTheme = themes[2];
        ChangeTheme(themes[2]);
    }
    
    public void Theme04()
    {
        currentTheme = themes[3];
        ChangeTheme(themes[3]);
    }

    // add animation to hide/cover screen while doing this =================================================
    private void ChangeTheme(ColorPaletteSO theme)
    {
        string activePage = " ";

        // Enable all pages
        foreach (var page in pages)
        {
            // Remember the current active page
            if (page.gameObject.activeSelf == true)
            {
                activePage = page.name;
            }

            page.SetActive(true);
        }

        // Change background theme
        foreach (var item in GameObject.FindGameObjectsWithTag("Background"))
        {
            var image = item.GetComponent<Image>();
            if (image != null)
            {
                Color curColor = image.color;
                Color newColor = theme.Background;
                image.color = new Color(newColor.r, newColor.g, newColor.b, curColor.a);
            }
        }

        // Change header theme
        foreach (var item in GameObject.FindGameObjectsWithTag("Header"))
        {
            var image = item.GetComponent<Image>();
            if (image != null)
            {
                Color curColor = image.color;
                Color newColor = theme.Header;
                image.color = new Color(newColor.r, newColor.g, newColor.b, curColor.a);
            }
        }

        // Change primary theme
        foreach (var item in GameObject.FindGameObjectsWithTag("Primary"))
        {
            var image = item.GetComponent<Image>();
            if (image != null)
            {
                Color curColor = image.color;
                Color newColor = theme.Primary;
                image.color = new Color(newColor.r, newColor.g, newColor.b, curColor.a);
            }
        }

        // Change secondary theme
        foreach (var item in GameObject.FindGameObjectsWithTag("Secondary"))
        {
            var image = item.GetComponent<Image>();
            if (image != null)
            {
                Color curColor = image.color;
                Color newColor = theme.Secondary;
                image.color = new Color(newColor.r, newColor.g, newColor.b, curColor.a);
            }
        }

        // Change accent theme
        foreach (var item in GameObject.FindGameObjectsWithTag("Accent"))
        {
            var image = item.GetComponent<Image>();
            if (image != null)
            {
                Color curColor = image.color;
                Color newColor = theme.Accent;
                image.color = new Color(newColor.r, newColor.g, newColor.b, curColor.a);
            }
        }

        // Change header text theme
        foreach (var item in GameObject.FindGameObjectsWithTag("HeaderText"))
        {
            var text = item.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                Color curColor = text.color;
                Color newColor = theme.HeaderText;
                text.color = new Color(newColor.r, newColor.g, newColor.b, curColor.a);
            }
        }

        // Change navbar text theme
        foreach (var item in GameObject.FindGameObjectsWithTag("NavBarText"))
        {
            var text = item.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                Color curColor = text.color;
                Color newColor = theme.NavBarText;
                text.color = new Color(newColor.r, newColor.g, newColor.b, curColor.a);
            }
        }

        // Disable pages
        foreach (var page in pages)
        {
            // Skip the current active page
            if (page.name != activePage)
            {
                page.SetActive(false);
            }
        }
    }
}
