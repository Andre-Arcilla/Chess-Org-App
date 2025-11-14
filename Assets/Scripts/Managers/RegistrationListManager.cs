using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class RegistrationListManager : MonoBehaviour
{
    public static RegistrationListManager Instance { get; private set; }

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
    [SerializeField] private Transform mainView;
    [SerializeField] private GameObject pageToShow;
    [SerializeField] private TextMeshProUGUI StudName;
    [SerializeField] private TextMeshProUGUI StudNum;
    [SerializeField] private TextMeshProUGUI StudEmail;
    [SerializeField] private TextMeshProUGUI RegDate;

    // Currently Selected Item
    private RegisterModel currentRegistrant;

    // Cached data and gameobject, StudNum key + data model/gameobject value
    private Dictionary<string, RegisterModel> registrationDataCache = new Dictionary<string, RegisterModel>();
    private Dictionary<string, GameObject> registrationObjectCache = new Dictionary<string, GameObject>();

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
        var dbRegistrations = GenerateDatabase.Instance.database.Table<RegisterModel>().ToList();

        // Check if data is cached, if not, generate it
        foreach (var registration in dbRegistrations)
        {
            if (registrationDataCache.TryGetValue(registration.StudNum, out var cached))
            {
                if (registration.LastModified > cached.LastModified)
                {
                    registrationDataCache[registration.StudNum] = registration;
                    registrationObjectCache[registration.StudNum].GetComponent<RegistrationCard>().SetInformation(registration);
                }
            }
            else
            {
                var newCard = Instantiate(cardPrefab, cardContainer);
                newCard.GetComponent<RegistrationCard>().SetInformation(registration);
                registrationDataCache[registration.StudNum] = registration;
                registrationObjectCache[registration.StudNum] = newCard;
            }
        }

        var dbKeys = new HashSet<string>(dbRegistrations.Select(registration => registration.StudNum));
        var deletedKeys = registrationDataCache.Keys.Where(key => !dbKeys.Contains(key)).ToList();

        // Delete cached data not in database
        foreach (var key in deletedKeys)
        {
            Destroy(registrationObjectCache[key]);
            registrationDataCache.Remove(key);
            registrationObjectCache.Remove(key);
        }
    }

    // Called by ProfileCard, show selected profile and pass information
    public void ShowItem(RegisterModel registration)
    {
        currentRegistrant = new RegisterModel
        {
            RegID = registration.RegID,
            StudName = registration.StudName,
            Email = registration.Email,
            StudNum = registration.StudNum,
            Password = registration.Password
        };

        StudName.text = registration.StudName;
        StudNum.text = registration.StudNum;
        StudEmail.text = registration.Email;
        RegDate.text = registration.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");

        pageToShow.transform.SetParent(mainView);
        pageToShow.transform.SetAsLastSibling();
    }

    public void ApproveRegistration()
    {
        // remove in regTable, add in profileTable
        ProfileModel newEntry = new ProfileModel
        {
            StudName = currentRegistrant.StudName,
            Email = currentRegistrant.Email,
            StudNum = currentRegistrant.StudNum,
            Password = currentRegistrant.Password,
            Rating = 100,
            Role = "Member",
            Date = DateTime.Now,
            LastModified = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        GenerateDatabase.Instance.database.Insert(newEntry);

        GenerateDatabase.Instance.database.Delete(currentRegistrant);

        if (registrationDataCache.ContainsKey(currentRegistrant.StudNum))
        {
            Destroy(registrationObjectCache[currentRegistrant.StudNum]);
            registrationDataCache.Remove(currentRegistrant.StudNum);
            registrationObjectCache.Remove(currentRegistrant.StudNum);
        }

        GenerateList();
    }

    public void RejectRegistration()
    {
        GenerateDatabase.Instance.database.Delete(currentRegistrant);

        if (registrationDataCache.ContainsKey(currentRegistrant.StudNum))
        {
            Destroy(registrationObjectCache[currentRegistrant.StudNum]);
            registrationDataCache.Remove(currentRegistrant.StudNum);
            registrationObjectCache.Remove(currentRegistrant.StudNum);
        }

        GenerateList();
    }
}