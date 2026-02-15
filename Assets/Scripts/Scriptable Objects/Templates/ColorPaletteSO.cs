using UnityEngine;

[CreateAssetMenu(fileName = "Palette", menuName = "Color Palette")]
public class ColorPaletteSO : ScriptableObject
{
    public Color Background = Color.white;
    public Color Primary = Color.white;
    public Color Secondary = Color.white;
    public Color Accent = Color.white;
}