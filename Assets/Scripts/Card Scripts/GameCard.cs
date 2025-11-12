using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameCard : MonoBehaviour
{
    [Header("Item ID")]
    [SerializeField] private GameModel game;
    public GameModel _game => game;

    [Header("Card Texts")]
    [SerializeField] private TextMeshProUGUI cardTitle;
    [SerializeField] private TextMeshProUGUI cardSubTitle;

    public void SetInformation(GameModel game)
    {
        this.game = game;
        cardTitle.text = $"Game #{game.GameNum.ToString()}";
        cardSubTitle.text = $"{game.PlayerColor} | {game.Result}";
    }

    public void OnClick()
    {
        SelectedProfileManager.Instance.ShowItem(game);
    }
}