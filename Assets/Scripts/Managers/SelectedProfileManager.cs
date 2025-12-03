using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
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
    [SerializeField] private Image gameWinFill;
    [SerializeField] private Image gameDrawFill;

    [Header("Puzzle Stats UI References")]
    [SerializeField] private TextMeshProUGUI totalPuzzles;
    [SerializeField] private TextMeshProUGUI puzzleWinText;
    [SerializeField] private TextMeshProUGUI puzzleLoseText;
    [SerializeField] private Image puzzleWinFill;

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

    [Header("Popup")]
    [SerializeField] private GameObject popupObject;
    [SerializeField] private Transform canvasView;

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
            PuzzlesWin = currentProfile.PuzzlesWin,
            PuzzlesTotal = currentProfile.PuzzlesTotal,
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
        studRatingText.text = "Rating: " + currentProfile.Rating;

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
    }

    public void SaveProfileChanges(GameObject profileWindow)
    {
        var oldStudNum = currentProfile.StudNum;

        //verify studnum if unique
        if (GenerateDatabase.Instance.database.Table<ProfileModel>().Any(profile => profile.StudNum == studNumInput.text) && currentProfile.StudNum != studNumInput.text)
        {
            StartCoroutine(ShowPopup("Student number already in use"));
            return;
        }

        //verify if email is umak
        if (!Regex.IsMatch(studEmailInput.text, @"^[a-z0-9\.]+@umak\.edu\.ph$", RegexOptions.IgnoreCase))
        {
            StartCoroutine(ShowPopup("Please use a UMAK email"));
            return;
        }

        currentProfile.StudName = studNameInput.text;
        currentProfile.StudNum = studNumInput.text;
        currentProfile.Email = studEmailInput.text;
        if (!string.IsNullOrWhiteSpace(studPassInput.text))
            currentProfile.Password = HashScript.Hash(studPassInput.text);
        var newStudNum = currentProfile.StudNum;

        GenerateDatabase.Instance.database.Execute(
            "UPDATE Profiles SET StudNum = ? WHERE StudNum = ?",
                newStudNum,
                oldStudNum
        );

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

    public void SetRoleMember() => SetRole("Member");
    public void SetRoleCoach() => SetRole("Coach");
    public void SetRoleAdmin() => SetRole("Admin");
    public void SetRoleDisabled() => SetRole("Disabled");

    void SetRole(string newRole)
    {
        currentProfile.Role = newRole;
        currentProfile.LastModified = DateTimeOffset.Now.ToUnixTimeSeconds();
        GenerateDatabase.Instance.database.Update(currentProfile);
        UpdateProfileInfoPage();
        ProfileListManager.Instance.GenerateList();
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