using System.Linq;
using TMPro;
using UnityEngine;

public class TournamentCard : MonoBehaviour
{
    [SerializeField] private TournamentModel tournament;
    [SerializeField] private TextMeshProUGUI tourTitle;
    [SerializeField] private TextMeshProUGUI tourDate;
    [SerializeField] private GameObject parentList;
    [SerializeField] private GameObject tournamentPost;
    [SerializeField] private Transform mainView;
    [SerializeField] private Transform canvas;

    public void SetInfo(TournamentModel tournamentInfo, GameObject parentList, GameObject tournamentPost, Transform mainView, Transform canvas)
    {
        if (tournament != null)
        {
            return;
        }

        this.parentList = parentList;
        this.mainView = mainView;
        this.canvas = canvas;
        this.tournamentPost = tournamentPost;
        tournament = tournamentInfo;
        tourTitle.text = tournament.Title;
        tourDate.text = tournament.Date;
    }

    public void OpenTournamentPost()
    {
        TextMeshProUGUI title = tournamentPost.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Post Title");
        TextMeshProUGUI date = tournamentPost.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Post Date");
        TextMeshProUGUI content = tournamentPost.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Post Content");

        title.text = tournament.Title;
        date.text = tournament.Date;
        content.text = tournament.Text;

        tournamentPost.transform.SetParent(mainView);
        tournamentPost.transform.SetSiblingIndex(1);

        parentList.transform.SetParent(canvas);
        parentList.transform.SetAsFirstSibling();
    }
}