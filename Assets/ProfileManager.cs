using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
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
    [SerializeField] private Image gameWinFill;
    [SerializeField] private Image gameDrawFill;

    [Header("Puzzle Stats UI References")]
    [SerializeField] private TextMeshProUGUI totalPuzzles;
    [SerializeField] private TextMeshProUGUI puzzleWinText;
    [SerializeField] private TextMeshProUGUI puzzleLoseText;
    [SerializeField] private Image puzzleWinFill;

    [Header("Home Page UI References")]
    [SerializeField] private TextMeshProUGUI headerGreeting;
    [SerializeField] private TextMeshProUGUI annTitle;
    [SerializeField] private TextMeshProUGUI annDate;
    [SerializeField] private TextMeshProUGUI annText;
    [SerializeField] private List<GameCard> gameCards;
    [SerializeField] private GameObject noGamesPlaceholder;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Image winFill;
    [SerializeField] private Image drawFill;

    [Header("Profile Edit References")]
    [SerializeField] private TMP_InputField origPassInput;
    [SerializeField] private TMP_InputField newPassInput;
    [SerializeField] private TMP_InputField newPassConfirmInput;

    [Header("Game History References")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardContainer;

    [Header("Popup")]
    [SerializeField] private GameObject popupObject;
    [SerializeField] private Transform canvasView;

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
            PuzzlesWin = currentProfile.PuzzlesWin,
            PuzzlesTotal = currentProfile.PuzzlesTotal,
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
        studRatingText.text = "Rating: " + currentProfile.Rating;

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
        origPassInput.text = "";
        newPassInput.text = "";
        newPassConfirmInput.text = "";
    }

    public void SaveProfileChanges(GameObject profileWindow)
    {
        if (origPassInput.text != currentProfile.Password)
        {
            StartCoroutine(ShowPopup("Incorrect original password"));
            return;
        }

        if (!string.IsNullOrEmpty(newPassInput.text))
        {
            if (newPassInput.text != newPassConfirmInput.text)
            {
                StartCoroutine(ShowPopup("Incorrect new password confirmation"));
                return;
            }
            currentProfile.Password = newPassInput.text;
        }

        profileWindow.transform.SetParent(canvasView);
        profileWindow.transform.SetAsFirstSibling();
        profileWindow.SetActive(false);

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
            gameWinFill.fillAmount = (gameWin / (float)total);
            gameDrawFill.fillAmount = (gameWin / (float)total) + (gameDraw / (float)total);
        }
        else
        {
            gameWinFill.fillAmount = 1f / 3f;
            gameDrawFill.fillAmount = 2f / 3f;
        }

        totalGames.text = "Games Played: " + total;
        gameWinText.text = gameWin + " Won";
        gameDrawText.text = gameDraw + " Draw";
        gameLoseText.text = gameLose + " Lose";
    }

    private void SetPuzzleStatistics()
    {
        var total = currentProfile.PuzzlesTotal;
        var wins = currentProfile.PuzzlesWin;
        var loses = total - wins;

        if (total > 0)
        {
            puzzleWinFill.fillAmount = (wins / (float)total);
        }
        else
        {
            puzzleWinFill.fillAmount = 1f / 2f;
        }

        totalPuzzles.text = "Puzzles Played: " + total;
        puzzleWinText.text = wins + " Won";
        puzzleLoseText.text = loses + " Lose";
    }

    private void HomePageSetup()
    {
        AnnouncementModel announcement = GenerateDatabase.Instance.database.Table<AnnouncementModel>().FirstOrDefault();

        if (headerGreeting != null) headerGreeting.text = $"Welcome, {currentProfile.StudName}!";
        if (annTitle != null) annTitle.text = announcement.Title;
        if (annDate != null) annDate.text = announcement.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");
        if (annText != null) annText.text = announcement.Text;

        // --- 1. Recent Games (Limit 3) ---
        var recentGames = GenerateDatabase.Instance.database.Table<GameModel>().Where(g => g.StudNum == currentProfile.StudNum).OrderByDescending(a => a.GameNum).Take(3).ToList();

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

        // --- 2. Calculate Total Stats (FIXED) ---

        var allUserGames = GenerateDatabase.Instance.database.Table<GameModel>().Where(g => g.StudNum == currentProfile.StudNum).ToList();

        int totalGames = allUserGames.Count;
        int totalWins = allUserGames.Count(g => g.Result.Equals("Win", StringComparison.OrdinalIgnoreCase));
        int totalLoses = allUserGames.Count(g => g.Result.Equals("Lose", StringComparison.OrdinalIgnoreCase));
        int totalDraws = allUserGames.Count(g => g.Result.Equals("Draw", StringComparison.OrdinalIgnoreCase));

        float percWins = ((float)totalWins / totalGames);
        float percDraws = ((float)totalDraws / totalGames);

        winFill.fillAmount = percWins;
        drawFill.fillAmount = percWins + percDraws;
        statsText.text = $"{totalWins} Wins\n{totalDraws} Draws\n{totalLoses} Loses";
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

    private IEnumerator ShowPopup(string message)
    {
        var group = popupObject.GetComponent<CanvasGroup>();
        var text = popupObject.GetComponentInChildren<TextMeshProUGUI>();

        text.text = message;

        // Fade in
        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        group.alpha = 1f;

        // Wait 3 seconds
        yield return new WaitForSeconds(3f);

        // Fade out
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        yield return new WaitForSeconds(5f);

        group.alpha = 0f;
    }
}