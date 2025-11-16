using SQLite4Unity3d;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TournamentList : MonoBehaviour
{
    public static TournamentList Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Card Details")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;

    [Header("Page References")]
    [SerializeField] private GameObject tourButton;
    [SerializeField] private Transform canvasView;
    [SerializeField] private Transform mainView;
    [SerializeField] private GameObject pageToRemove;
    [SerializeField] private GameObject pageToShow;

    [Header("Post References")]
    [SerializeField] private TextMeshProUGUI postTitle;
    [SerializeField] private TextMeshProUGUI postDate;
    [SerializeField] private TextMeshProUGUI postContent;

    [Header("Edit Page References")]
    [SerializeField] private TMP_InputField inputTitle;
    [SerializeField] private TMP_InputField inputText;
    [SerializeField] private TextMeshProUGUI inputDate;

    [Header("Make Page References")]
    [SerializeField] private TMP_InputField newPostTitle;
    [SerializeField] private TMP_InputField newPostText;
    [SerializeField] private TextMeshProUGUI newPostDate;

    // Current Post
    private TournamentModel currentTournament;

    // Cached data and gameobject, StudNum key + data model/gameobject value
    private Dictionary<string, TournamentModel> tourDataCache = new Dictionary<string, TournamentModel>();
    private Dictionary<string, GameObject> tourObjectCache = new Dictionary<string, GameObject>();

    void Start()
    {
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        GenerateList();
    }

    public void GenerateList()
    {
        var dbTournaments = GenerateDatabase.Instance.database.Table<TournamentModel>().ToList();

        // Check if data is cached, if not, generate it
        foreach (var tournament in dbTournaments)
        {
            if (tourDataCache.TryGetValue(tournament.TourID.ToString(), out var cached))
            {
                if (tournament.LastModified > cached.LastModified)
                {
                    tourDataCache[tournament.TourID.ToString()] = tournament;
                    tourObjectCache[tournament.TourID.ToString()].GetComponent<TournamentCard>().SetInformation(tournament);
                }
            }
            else
            {
                var newCard = Instantiate(cardPrefab, cardContainer);
                newCard.GetComponent<TournamentCard>().SetInformation(tournament);
                tourDataCache[tournament.TourID.ToString()] = tournament;
                tourObjectCache[tournament.TourID.ToString()] = newCard;
            }
        }

        var dbKeys = new HashSet<string>(dbTournaments.Select(announcement => announcement.TourID.ToString()));
        var deletedKeys = tourDataCache.Keys.Where(key => !dbKeys.Contains(key)).ToList();

        // Delete cached data not in database
        foreach (var key in deletedKeys)
        {
            Destroy(tourObjectCache[key]);
            tourDataCache.Remove(key);
            tourObjectCache.Remove(key);
        }
    }

    public void ShowItem(TournamentModel tournament)
    {
        currentTournament = new TournamentModel
        {
            TourID = tournament.TourID,
            Author = tournament.Author,
            LastEditor = tournament.LastEditor,
            Title = tournament.Title,
            Date = tournament.Date,
            Text = tournament.Text,
            IsEditing = tournament.IsEditing,
            LastModified = tournament.LastModified
        };

        postTitle.text = currentTournament.Title;
        postDate.text = currentTournament.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");
        postContent.text = currentTournament.Text;

        pageToRemove.transform.SetParent(canvasView);
        pageToRemove.transform.SetAsFirstSibling();
        pageToRemove.SetActive(false);

        pageToShow.transform.SetParent(mainView);
        pageToShow.transform.SetSiblingIndex(1);
        pageToShow.SetActive(true);
    }

    public void EditPost()
    {
        inputTitle.text = currentTournament.Title;
        inputText.text = currentTournament.Text;
        inputDate.text = currentTournament.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");
    }

    public void DeletePost()
    {
        GenerateDatabase.Instance.database.Delete(currentTournament);

        if (tourDataCache.ContainsKey(currentTournament.TourID.ToString()))
        {
            Destroy(tourObjectCache[currentTournament.TourID.ToString()]);
            tourDataCache.Remove(currentTournament.TourID.ToString());
            tourObjectCache.Remove(currentTournament.TourID.ToString());
        }

        GenerateList();
    }

    public void SavePost()
    {
        if (string.IsNullOrEmpty(inputTitle.text) || string.IsNullOrEmpty(inputText.text))
        {
            return;
        }

        currentTournament.Title = inputTitle.text;
        currentTournament.Text = inputText.text;
        // Add current logged in user's StudNum

        currentTournament.LastModified = DateTimeOffset.Now.ToUnixTimeSeconds();
        GenerateDatabase.Instance.database.Update(currentTournament);

        GenerateList();
    }

    public void CreatePost()
    {
        if (string.IsNullOrEmpty(newPostTitle.text) || string.IsNullOrEmpty(newPostText.text))
        {
            return;
        }

        TournamentModel newEntry = new TournamentModel();

        newEntry.Title = newPostTitle.text;
        newEntry.Date = DateTime.Now;
        newEntry.Text = newPostText.text;
        newEntry.Author = "A12346169"; //replace with current user
        newEntry.LastEditor = "A12346169"; //replace with current user
        newEntry.IsEditing = 0;
        newEntry.LastModified = DateTimeOffset.Now.ToUnixTimeSeconds();

        GenerateDatabase.Instance.database.Insert(newEntry);

        GenerateList();
    }

    public void SelectTab()
    {
        var bg = tourButton.GetComponentsInChildren<Image>().FirstOrDefault(image => image.transform != tourButton.transform);
        var text = tourButton.GetComponentInChildren<TextMeshProUGUI>(true);

        bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 0);
        text.color = Color.white;
        AnnouncementList.Instance.DeselectTab();
    }
    
    public void DeselectTab()
    {
        var bg = tourButton.GetComponentsInChildren<Image>().FirstOrDefault(image => image.transform != tourButton.transform);
        var text = tourButton.GetComponentInChildren<TextMeshProUGUI>(true);

        bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 1);
        text.color = Color.black;
    }

    public void ClearInputFields()
    {
        newPostDate.text = DateTime.Now.ToString("MMMM dd, yyyy hh:mm:ss tt");
        newPostTitle.text = "";
        newPostText.text = "";
    }
}
