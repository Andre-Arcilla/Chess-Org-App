using SQLite4Unity3d;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileListManager : MonoBehaviour
{
    public static ProfileListManager Instance { get; private set; }

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
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardContainer;

    [Header("Page References")]
    [SerializeField] private Transform canvasView;
    [SerializeField] private Transform mainView;
    [SerializeField] private GameObject pageToRemove;
    [SerializeField] private GameObject pageToShow;

    // Cached data and gameobject, StudNum key + data model/gameobject value
    private Dictionary<string, ProfileModel> profileDataCache = new Dictionary<string, ProfileModel>();
    private Dictionary<string, GameObject> profileObjectCache = new Dictionary<string, GameObject>();

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
        var dbProfiles = GenerateDatabase.Instance.database.Table<ProfileModel>().ToList();

        // Check if data is cached, if not, generate it
        foreach (var profile in dbProfiles)
        {
            if (profileDataCache.TryGetValue(profile.StudNum, out var cached))
            {
                if (profile.LastModified > cached.LastModified)
                {
                    profileDataCache[profile.StudNum] = profile;
                    profileObjectCache[profile.StudNum].GetComponent<ProfileCard>().SetInformation(profile);
                }
            }
            else
            {
                var newCard = Instantiate(cardPrefab, cardContainer);
                newCard.GetComponent<ProfileCard>().SetInformation(profile);
                profileDataCache[profile.StudNum] = profile;
                profileObjectCache[profile.StudNum] = newCard;
            }
        }

        var dbKeys = new HashSet<string>(dbProfiles.Select(profile => profile.StudNum));
        var deletedKeys = profileDataCache.Keys.Where(key => !dbKeys.Contains(key)).ToList();

        // Delete cached data not in database
        foreach (var key in deletedKeys)
        {
            Destroy(profileObjectCache[key]);
            profileDataCache.Remove(key);
            profileObjectCache.Remove(key);
        }
    }

    // Called by ProfileCard, show selected profile and pass information
    public void ShowItem(ProfileModel profile)
    {
        SelectedProfileManager.Instance.SetCurrentProfile(profile);

        // I have to switch pages here
        pageToRemove.transform.SetParent(canvasView);
        pageToRemove.transform.SetAsFirstSibling();

        pageToShow.transform.SetParent(mainView);
        pageToShow.transform.SetSiblingIndex(1);
    }
}
