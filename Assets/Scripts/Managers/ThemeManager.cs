
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

    public void Theme05()
    {
        currentTheme = themes[4];
        ChangeTheme(themes[4]);
    }
    
    public void Theme06()
    {
        currentTheme = themes[5];
        ChangeTheme(themes[5]);
    }

    private void ChangeTheme(ColorPaletteSO theme)
    {
        // 1. Find ALL Images in the scene, passing 'true' to include Inactive ones
        Image[] allImages = FindObjectsOfType<Image>(true);

        // 2. Loop through them once and apply colors based on their tag
        foreach (Image image in allImages)
        {
            // Skip if the image component is somehow null (rare but safe)
            if (image == null) continue;

            if (image.gameObject.CompareTag("Background"))
            {
                ApplyColor(image, theme.Background);
            }
            else if (image.gameObject.CompareTag("Primary"))
            {
                ApplyColor(image, theme.Primary);
            }
            else if (image.gameObject.CompareTag("Secondary"))
            {
                ApplyColor(image, theme.Secondary);
            }
            else if (image.gameObject.CompareTag("Accent"))
            {
                ApplyColor(image, theme.Accent);
            }
        }
    }

    // Helper function to keep code clean and preserve Alpha transparency
    private void ApplyColor(Image img, Color themeColor)
    {
        img.color = new Color(themeColor.r, themeColor.g, themeColor.b, img.color.a);
    }
}
