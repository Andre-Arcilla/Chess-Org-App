using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

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

    [Header("Profile UI References")]
    [SerializeField] private TextMeshProUGUI studNameText;
    [SerializeField] private TextMeshProUGUI studNumText;
    [SerializeField] private TextMeshProUGUI studRatingText;

    [Header("Game Stats UI References")]
    [SerializeField] private TextMeshProUGUI totalGames;
    [SerializeField] private TextMeshProUGUI gameWinText;
    [SerializeField] private TextMeshProUGUI gameDrawText;
    [SerializeField] private TextMeshProUGUI gameLoseText;
    [SerializeField] private LayoutElement gameWinBar;
    [SerializeField] private LayoutElement gameLoseBar;
    [SerializeField] private LayoutElement gameDrawBar;

    [Header("Puzzle Stats UI References")]
    [SerializeField] private TextMeshProUGUI totalPuzzles;
    [SerializeField] private TextMeshProUGUI puzzleWinText;
    [SerializeField] private TextMeshProUGUI puzzleLoseText;
    [SerializeField] private LayoutElement puzzleWinBar;
    [SerializeField] private LayoutElement puzzleLoseBar;

    [Header("Home Page UI References")]
    [SerializeField] private TextMeshProUGUI headerGreeting;
    [SerializeField] private TextMeshProUGUI annTitle;
    [SerializeField] private TextMeshProUGUI annDate;
    [SerializeField] private TextMeshProUGUI annText;
    [SerializeField] private List<GameCard> gameCards;
    [SerializeField] private GameObject noGamesPlaceholder;

    [Header("Profile Edit References")]
    [SerializeField] private TMP_InputField studNameInput;
    [SerializeField] private TMP_InputField studNumInput;
    [SerializeField] private TMP_InputField studEmailInput;
    [SerializeField] private TMP_InputField studPassInput;

    [Header("Game History References")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardContainer;

    // Current Profile
    public ProfileModel currentProfile { private set; get; }

    // Cached data and gameobject, GameNum key + data model/gameobject value
    private Dictionary<int, GameModel> gamesDataCaches = new Dictionary<int, GameModel>();
    private Dictionary<int, GameObject> gamesObjectCaches = new Dictionary<int, GameObject>();

    void Start()
    {
        currentProfile = GenerateDatabase.Instance.currentUser;

        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        if (currentProfile == null)
        {
            SetCurrentProfile(GenerateDatabase.Instance.database.Table<ProfileModel>().FirstOrDefault());
        }
        HomePageSetup();
    }

    public void SetCurrentProfile(ProfileModel currentProfile)
    {
        this.currentProfile = new ProfileModel
        {
            UserID = currentProfile.UserID,
            StudName = currentProfile.StudName,
            Email = currentProfile.Email,
            StudNum = currentProfile.StudNum,
            Password = currentProfile.Password,
            Rating = currentProfile.Rating,
            Puzzles = currentProfile.Puzzles,
            Role = currentProfile.Role,
            Date = currentProfile.Date,
            LastModified = currentProfile.LastModified
        };

        UpdateProfileInfoPage();
    }

    public void UpdateProfileInfoPage()
    {
        studNameText.text = currentProfile.StudName;
        studNumText.text = currentProfile.StudNum;
        studRatingText.text = currentProfile.Rating.ToString();

        GenerateList();
        SetGameStatistics();
        SetPuzzleStatistics();
        HomePageSetup();
    }

    public void GenerateList()
    {
        var dbGames = GenerateDatabase.Instance.database.Table<GameModel>().Where(g => g.StudNum == currentProfile.StudNum).ToList();

        // Check if data is cached, if not, generate it
        foreach (var game in dbGames)
        {
            if (gamesDataCaches.TryGetValue(game.GameNum, out var cached))
            {
                if (game.LastModified > cached.LastModified)
                {
                    gamesDataCaches[game.GameNum] = game;
                    gamesObjectCaches[game.GameNum].GetComponent<GameCard>().SetInformation(game);
                }
            }
            else
            {
                var newCard = Instantiate(cardPrefab, cardContainer);
                newCard.GetComponent<GameCard>().SetInformation(game);
                gamesDataCaches[game.GameNum] = game;
                gamesObjectCaches[game.GameNum] = newCard;
            }
        }

        var dbKeys = new HashSet<int>(dbGames.Select(announcement => announcement.GameNum));
        var deletedKeys = gamesDataCaches.Keys.Where(key => !dbKeys.Contains(key)).ToList();

        // Delete cached data not in database
        foreach (var key in deletedKeys)
        {
            Destroy(gamesObjectCaches[key]);
            gamesDataCaches.Remove(key);
            gamesObjectCaches.Remove(key);
        }
    }

    public void EditProfile()
    {
        studNameInput.text = currentProfile.StudName;
        studNumInput.text = currentProfile.StudNum;
        studEmailInput.text = currentProfile.Email;
        studPassInput.text = currentProfile.Password;
    }

    public void SaveProfileChanges()
    {
        //verify studnum if unique
        if (GenerateDatabase.Instance.database.Table<ProfileModel>().Any(profile => profile.StudNum == studNumInput.text) && currentProfile.StudNum != studNumInput.text)
        {
            Debug.Log("StudNum already exists");
            return;
        }

        //verify if email is umak
        if (!Regex.IsMatch(studEmailInput.text, @"^[a-z0-9\.]+@umak\.edu\.ph$", RegexOptions.IgnoreCase))
        {
            Debug.Log("Please use a umak email");
            return;
        }

        currentProfile.StudName = studNameInput.text;
        currentProfile.StudNum = studNumInput.text;
        currentProfile.Email = studEmailInput.text;
        currentProfile.Password = studPassInput.text;

        currentProfile.LastModified = DateTimeOffset.Now.ToUnixTimeSeconds();
        GenerateDatabase.Instance.database.Update(currentProfile);

        UpdateProfileInfoPage();
        ProfileListManager.Instance.GenerateList();
    }

    private void SetGameStatistics()
    {
        int gameWin = 0;
        int gameLose = 0;
        int gameDraw = 0;

        foreach (var game in gamesDataCaches)
        {
            switch (gamesDataCaches[game.Key].Result)
            {
                case "Win":
                    gameWin++;
                    break;
                case "Lose":
                    gameLose++;
                    break;
                case "Draw":
                    gameDraw++;
                    break;
            }
        }

        int total = gameWin + gameLose + gameDraw;

        if (total > 0)
        {
            gameWinBar.flexibleWidth = (gameWin / (float)total) * 100f;
            gameDrawBar.flexibleWidth = (gameDraw / (float)total) * 100f;
            gameLoseBar.flexibleWidth = (gameLose / (float)total) * 100f;
        }
        else
        {
            // No games yet, set bars to 0
            gameWinBar.flexibleWidth = 33;
            gameDrawBar.flexibleWidth = 33;
            gameLoseBar.flexibleWidth = 33;
        }

        totalGames.text = total.ToString();
        gameWinText.text = gameWin + " Won";
        gameDrawText.text = gameDraw + " Draw";
        gameLoseText.text = gameLose + " Lose";
    }

    private void SetPuzzleStatistics()
    {
        // FAKE PUZZLE COUNT, UPDATE

        int gameWin = 0;
        int gameLose = 0;

        foreach (var game in gamesDataCaches)
        {
            switch (gamesDataCaches[game.Key].Result)
            {
                case "Win":
                    gameWin++;
                    break;
                case "Lose":
                    gameLose++;
                    break;
            }
        }

        int total = gameWin + gameLose;

        if (total > 0)
        {
            puzzleWinBar.flexibleWidth = (gameWin / (float)total) * 100f;
            puzzleLoseBar.flexibleWidth = (gameLose / (float)total) * 100f;
        }
        else
        {
            // No games yet, set bars to 0
            puzzleWinBar.flexibleWidth = 75;
            puzzleLoseBar.flexibleWidth = 25;
        }

        totalPuzzles.text = total.ToString();
        puzzleWinText.text = gameWin + " Won";
        puzzleLoseText.text = gameLose + " Lose";
    }

    private void HomePageSetup()
    {
        AnnouncementModel announcement = GenerateDatabase.Instance.database.Table<AnnouncementModel>().FirstOrDefault();

        if (headerGreeting != null) headerGreeting.text = $"Welcome, {currentProfile.StudName}!";
        if (annTitle != null) annTitle.text = announcement.Title;
        if (annDate != null) annDate.text = announcement.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");
        if (annText != null) annText.text = announcement.Text;

        // --- UPDATED QUERY HERE ---
        // 1. Filter by the current user's StudNum
        // 2. Order by newest
        // 3. Take top 3
        var recentGames = GenerateDatabase.Instance.database.Table<GameModel>()
                            .Where(g => g.StudNum == currentProfile.StudNum) // This filters for the specific user
                            .OrderByDescending(a => a.GameNum)
                            .Take(3)
                            .ToList();

        if (noGamesPlaceholder != null)
        {
            noGamesPlaceholder.SetActive(recentGames.Count == 0);
        }

        for (int i = 0; i < gameCards.Count; i++)
        {
            if (i < recentGames.Count)
            {
                gameCards[i].gameObject.SetActive(true);
                gameCards[i].SetInformation(recentGames[i]);
            }
            else
            {
                gameCards[i].gameObject.SetActive(false);
            }
        }

        // last 3 games
        // game stats
    }

    public void Logout()
    {
        // 1. Clear Global User Reference
        if (GenerateDatabase.Instance != null)
        {
            GenerateDatabase.Instance.currentUser = null;
        }

        // 2. Clear Local User Reference
        currentProfile = null;
    }
}
