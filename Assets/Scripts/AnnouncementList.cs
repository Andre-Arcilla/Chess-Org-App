using SQLite4Unity3d;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public class AnnouncementList : MonoBehaviour
{
    public static AnnouncementList Instance { get; private set; }

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
    [SerializeField] private GameObject annButton;
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
    private AnnouncementModel currentAnnouncement;

    // Cached data and gameobject, StudNum key + data model/gameobject value
    private Dictionary<string, AnnouncementModel> annDataCache = new Dictionary<string, AnnouncementModel>();
    private Dictionary<string, GameObject> annObjectCache = new Dictionary<string, GameObject>();

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
        var dbAnnouncements = GenerateDatabase.Instance.database.Table<AnnouncementModel>().ToList();

        // Check if data is cached, if not, generate it
        foreach (var announcement in dbAnnouncements)
        {
            if (annDataCache.TryGetValue(announcement.AnnID.ToString(), out var cached))
            {
                if (announcement.LastModified > cached.LastModified)
                {
                    annDataCache[announcement.AnnID.ToString()] = announcement;
                    annObjectCache[announcement.AnnID.ToString()].GetComponent<AnnouncementCard>().SetInformation(announcement);
                }
            }
            else
            {
                var newCard = Instantiate(cardPrefab, cardContainer);
                newCard.GetComponent<AnnouncementCard>().SetInformation(announcement);
                annDataCache[announcement.AnnID.ToString()] = announcement;
                annObjectCache[announcement.AnnID.ToString()] = newCard;
            }
        }

        var dbKeys = new HashSet<string>(dbAnnouncements.Select(announcement => announcement.AnnID.ToString()));
        var deletedKeys = annDataCache.Keys.Where(key => !dbKeys.Contains(key)).ToList();

        // Delete cached data not in database
        foreach (var key in deletedKeys)
        {
            Destroy(annObjectCache[key]);
            annDataCache.Remove(key);
            annObjectCache.Remove(key);
        }
    }

    public void ShowItem(AnnouncementModel announcement)
    {
        currentAnnouncement = new AnnouncementModel
        {
            AnnID = announcement.AnnID,
            Author = announcement.Author,
            LastEditor = announcement.LastEditor,
            Title = announcement.Title,
            Date = announcement.Date,
            Text = announcement.Text,
            IsEditing = announcement.IsEditing,
            LastModified = announcement.LastModified
        };

        postTitle.text = currentAnnouncement.Title;
        postDate.text = currentAnnouncement.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");
        postContent.text = currentAnnouncement.Text;

        pageToRemove.transform.SetParent(canvasView);
        pageToRemove.transform.SetAsFirstSibling();

        pageToShow.transform.SetParent(mainView);
        pageToShow.transform.SetSiblingIndex(1);
    }

    public void EditPost()
    {
        inputTitle.text = currentAnnouncement.Title;
        inputText.text = currentAnnouncement.Text;
        inputDate.text = currentAnnouncement.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");
    }

    public void DeletePost()
    {
        GenerateDatabase.Instance.database.Delete(currentAnnouncement);

        if (annDataCache.ContainsKey(currentAnnouncement.AnnID.ToString()))
        {
            Destroy(annObjectCache[currentAnnouncement.AnnID.ToString()]);
            annDataCache.Remove(currentAnnouncement.AnnID.ToString());
            annObjectCache.Remove(currentAnnouncement.AnnID.ToString());
        }

        GenerateList();
    }

    public void SavePost()
    {
        if (string.IsNullOrEmpty(inputTitle.text) ||  string.IsNullOrEmpty(inputText.text))
        {
            return;
        }

        currentAnnouncement.Title = inputTitle.text;
        currentAnnouncement.Text = inputText.text;
        // Add current logged in user's StudNum

        currentAnnouncement.LastModified = DateTimeOffset.Now.ToUnixTimeSeconds();
        GenerateDatabase.Instance.database.Update(currentAnnouncement);

        postTitle.text = currentAnnouncement.Title;
        postDate.text = currentAnnouncement.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");
        postContent.text = currentAnnouncement.Text;

        GenerateList();
    }

    public void CreatePost()
    {
        if (string.IsNullOrEmpty(newPostTitle.text) || string.IsNullOrEmpty(newPostText.text))
        {
            return;
        }

        AnnouncementModel newEntry = new AnnouncementModel();

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
        var bg = annButton.GetComponentsInChildren<Image>().FirstOrDefault(image => image.transform != annButton.transform);
        var text = annButton.GetComponentInChildren<TextMeshProUGUI>(true);

        bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 0);
        text.color = Color.white;
        TournamentList.Instance.DeselectTab();
    }

    public void DeselectTab()
    {
        var bg = annButton.GetComponentsInChildren<Image>().FirstOrDefault(image => image.transform != annButton.transform);
        var text = annButton.GetComponentInChildren<TextMeshProUGUI>(true);

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