using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedProfileManager : MonoBehaviour
{
    public static SelectedProfileManager Instance { get; private set; }

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
    [SerializeField] private ToggleGroup profRole;

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

    [Header("Profile Edit References")]
    [SerializeField] private TMP_InputField studNameInput;
    [SerializeField] private TMP_InputField studNumInput;
    [SerializeField] private TMP_InputField studEmailInput;
    [SerializeField] private TMP_InputField studPassInput;

    [Header("Game History References")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardContainer;

    [Header("Page References")]
    [SerializeField] private Transform hiddenContainer;

    // Current Profile
    private ProfileModel currentProfile;

    // Cached data and gameobject, StudNum key + (GameNum key + data model/gameobject value) value
    private Dictionary<string, Dictionary<int, GameModel>> allProfileGameCaches = new Dictionary<string, Dictionary<int, GameModel>>();
    private Dictionary<string, Dictionary<int, GameObject>> allProfileObjectCaches = new Dictionary<string, Dictionary<int, GameObject>>();

    void Start()
    {
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }
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

    private void UpdateProfileInfoPage()
    {
        studNameText.text = currentProfile.StudName;
        studNumText.text = currentProfile.StudNum;
        studRatingText.text = currentProfile.Rating.ToString();

        var toggles = profRole.GetComponentsInChildren<Toggle>(true);

        foreach (var t in toggles)
        {
            if (t.name == currentProfile.Role)
            {
                t.SetIsOnWithoutNotify(true);
            }
            else
            {
                // Found a different role -> Force it OFF
                t.SetIsOnWithoutNotify(false);
            }
        }

        if (!allProfileGameCaches.ContainsKey(currentProfile.StudNum))
        {
            allProfileGameCaches[currentProfile.StudNum] = new Dictionary<int, GameModel>();
        }

        if (!allProfileObjectCaches.ContainsKey(currentProfile.StudNum))
        {
            allProfileObjectCaches[currentProfile.StudNum] = new Dictionary<int, GameObject>();
        }

        GenerateList();
        SetGameStatistics();
        SetPuzzleStatistics();
    }

    public void GenerateList()
    {
        var dbGames = GenerateDatabase.Instance.database.Table<GameModel>().Where(g => g.StudNum == currentProfile.StudNum).ToList();

        var gameDataCache = allProfileGameCaches[currentProfile.StudNum];
        var gameObjectCache = allProfileObjectCaches[currentProfile.StudNum];

        foreach (var profile in allProfileObjectCaches)
        {
            if (profile.Key != currentProfile.StudNum)
            {
                foreach (var obj in profile.Value)
                {
                    obj.Value.transform.SetParent(hiddenContainer);
                    obj.Value.SetActive(false);
                }
            }
            else if (profile.Key == currentProfile.StudNum)
            {
                foreach (var obj in profile.Value)
                {
                    if (obj.Value.transform.parent != cardContainer)
                    {
                        obj.Value.transform.SetParent(cardContainer);
                        obj.Value.SetActive(true);
                    }
                }
            }
        }

        // Check if data is cached, if not, generate it
        foreach (var game in dbGames)
        {
            if (gameDataCache.TryGetValue(game.GameNum, out var cached))
            {
                if (game.LastModified > cached.LastModified)
                {
                    gameDataCache[game.GameNum] = game;
                    gameObjectCache[game.GameNum].GetComponent<GameCard>().SetInformation(game);
                }
            }
            else
            {
                var newCard = Instantiate(cardPrefab, cardContainer);
                newCard.GetComponent<GameCard>().SetInformation(game);
                gameDataCache[game.GameNum] = game;
                gameObjectCache[game.GameNum] = newCard;
            }
        }

        var dbKeys = new HashSet<int>(dbGames.Select(game => game.GameNum));
        var deletedKeys = gameDataCache.Keys.Where(key => !dbKeys.Contains(key)).ToList();

        // Delete cached data not in database
        foreach (var key in deletedKeys)
        {
            Destroy(gameObjectCache[key]);
            gameDataCache.Remove(key);
            gameObjectCache.Remove(key);
        }
    }

    // Called by GameCard, show selected game and pass information
    public void ShowItem(GameModel game)
    {
        // get PGN of selected game
        string stringPGN = game.PGN;
        bool isViewOnly = ProfileManager.Instance.currentProfile.StudNum != game.StudNum;

        // hold string to pass
        StaticDataString.stringToPass = stringPGN;
        StaticDataString.isViewOnly = isViewOnly;
        StaticDataString.game = game;

        // load new scene
        SceneLoader.Instance.LoadNewScene("AnalysisScene");
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
        var oldStudNum = currentProfile.StudNum;

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
        var newStudNum = currentProfile.StudNum;

        GenerateDatabase.Instance.database.Execute(
            "UPDATE Profiles SET StudNum = ? WHERE StudNum = ?",
                newStudNum,
                oldStudNum
            );

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

        if (allProfileGameCaches.TryGetValue(currentProfile.StudNum, out var gamesDataCaches))
        {
            foreach (var game in gamesDataCaches.Values)
            {
                switch (game.Result)
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

        if (allProfileGameCaches.TryGetValue(currentProfile.StudNum, out var gamesDataCaches))
        {
            foreach (var game in gamesDataCaches.Values)
            {
                switch (game.Result)
                {
                    case "Win":
                        gameWin++;
                        break;
                    case "Lose":
                        gameLose++;
                        break;
                }
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

    public void SetRoleMember() => SetRole("Member");
    public void SetRoleCoach() => SetRole("Coach");
    public void SetRoleAdmin() => SetRole("Admin");
    public void SetRoleDisabled() => SetRole("Disabled");

    void SetRole(string newRole)
    {
        currentProfile.Role = newRole;
        currentProfile.LastModified = DateTimeOffset.Now.ToUnixTimeSeconds();
        GenerateDatabase.Instance.database.Update(currentProfile);
        ProfileListManager.Instance.GenerateList();
    }
}