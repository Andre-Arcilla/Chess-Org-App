using UnityEngine;

[CreateAssetMenu(fileName = "Palette", menuName = "Color Palette")]
public class ColorPaletteSO : ScriptableObject
{
    public Color Background = Color.white;
    public Color Primary = Color.white;
    public Color Secondary = Color.white;
    public Color Accent = Color.white;
    public Color Header = Color.white;
    public Color NavBarText = Color.white;
    public Color HeaderText = Color.white;
}