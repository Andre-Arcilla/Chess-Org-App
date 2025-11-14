using System.Linq;
using TMPro;
using UnityEngine;

public class TournamentCard : MonoBehaviour
{
    [SerializeField] private TournamentModel tournament;
    [SerializeField] private TextMeshProUGUI tourTitle;
    [SerializeField] private TextMeshProUGUI tourDate;

    public void SetInformation(TournamentModel tournamentInfo)
    {
        tournament = tournamentInfo;
        tourTitle.text = tournament.Title;
        tourDate.text = tournament.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");
    }

    public void OnClick()
    {
        TournamentList.Instance.ShowItem(tournament);
    }
}