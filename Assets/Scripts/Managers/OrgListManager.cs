using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class OrgListManager : MonoBehaviour
{
    public static OrgListManager Instance { get; private set; }

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
    [SerializeField] private TMP_InputField studNameInput;
    [SerializeField] private TMP_InputField studNumInput;
    [SerializeField] private TMP_InputField newStudNameInput;
    [SerializeField] private TMP_InputField newStudNumInput;

    // Currently Selected Item
    private OrgMemberModel currentOrgMember;

    // Cached data and gameobject, StudNum key + data model/gameobject value
    private Dictionary<string, OrgMemberModel> orgDataCache = new Dictionary<string, OrgMemberModel>();
    private Dictionary<string, GameObject> orgObjectCache = new Dictionary<string, GameObject>();

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
        var dbOrgList = GenerateDatabase.Instance.database.Table<OrgMemberModel>().ToList();

        // Check if data is cached, if not, generate it
        foreach (var orgMember in dbOrgList)
        {
            if (CheckRegistrations(orgMember))
            {
                continue;
            }

            if (orgDataCache.TryGetValue(orgMember.StudNum, out var cached))
            {
                if (orgMember.LastModified > cached.LastModified)
                {
                    orgDataCache[orgMember.StudNum] = orgMember;
                    orgObjectCache[orgMember.StudNum].GetComponent<OrgMemberCard>().SetInformation(orgMember);
                }
            }
            else
            {
                var newCard = Instantiate(cardPrefab, cardContainer);
                newCard.GetComponent<OrgMemberCard>().SetInformation(orgMember);
                orgDataCache[orgMember.StudNum] = orgMember;
                orgObjectCache[orgMember.StudNum] = newCard;
            }
        }

        var dbKeys = new HashSet<string>(dbOrgList.Select(registration => registration.StudNum));
        var deletedKeys = orgDataCache.Keys.Where(key => !dbKeys.Contains(key)).ToList();

        // Delete cached data not in database
        foreach (var key in deletedKeys)
        {
            Destroy(orgObjectCache[key]);
            orgDataCache.Remove(key);
            orgObjectCache.Remove(key);
        }
    }

    // Called by OrgMemberCard, show selected orgmember and pass information
    public void ShowItem(OrgMemberModel orgMember)
    {
        currentOrgMember = new OrgMemberModel
        {
            MmbrID = orgMember.MmbrID,
            StudName = orgMember.StudName,
            StudNum = orgMember.StudNum,
            LastModified = orgMember.LastModified
        };

        studNameInput.text = orgMember.StudName;
        studNumInput.text = orgMember.StudNum;

        pageToShow.transform.SetParent(mainView);
        pageToShow.transform.SetAsLastSibling();
        pageToShow.SetActive(true);
    }

    public void SaveChanges()
    {
        //verify studname/studnum if unique
        if (GenerateDatabase.Instance.database.Table<OrgMemberModel>().Any(profile => profile.StudName == studNameInput.text) && currentOrgMember.StudNum != studNumInput.text)
        {
            Debug.Log("StudName already exists");
            return;
        }

        if (GenerateDatabase.Instance.database.Table<OrgMemberModel>().Any(profile => profile.StudNum == studNumInput.text) && currentOrgMember.StudNum != studNumInput.text)
        {
            Debug.Log("StudNum already exists");
            return;
        }

        currentOrgMember.StudName = studNameInput.text;
        currentOrgMember.StudNum = studNumInput.text;

        currentOrgMember.LastModified = DateTimeOffset.Now.ToUnixTimeSeconds();
        GenerateDatabase.Instance.database.Update(currentOrgMember);

        GenerateList();
    }

    public void DeleteEntry()
    {
        GenerateDatabase.Instance.database.Delete(currentOrgMember);

        if (orgDataCache.ContainsKey(currentOrgMember.StudNum))
        {
            Destroy(orgObjectCache[currentOrgMember.StudNum]);
            orgDataCache.Remove(currentOrgMember.StudNum);
            orgObjectCache.Remove(currentOrgMember.StudNum);
        }

        GenerateList();
    }

    public void AddEntry()
    {
        OrgMemberModel newEntry = new OrgMemberModel
        {
            StudName = newStudNameInput.text,
            StudNum = newStudNumInput.text,
            LastModified = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        GenerateDatabase.Instance.database.Insert(newEntry);

        newStudNameInput.text = "";
        newStudNumInput.text = "";

        GenerateList();
    }

    public bool CheckRegistrations(OrgMemberModel entry)
    {
        var studentExists = GenerateDatabase.Instance.database.Table<RegisterModel>()
            .Where(account => account.StudName.ToUpper() == entry.StudName.ToUpper()).FirstOrDefault();

        var numberExists = GenerateDatabase.Instance.database.Table<RegisterModel>()
            .Where(account => account.StudNum.ToUpper() == entry.StudNum.ToUpper()).FirstOrDefault();

        if (studentExists != null && numberExists != null)
        {
            GenerateDatabase.Instance.database.Execute(
                "INSERT INTO Profiles (StudName, StudNum, Email, Password) VALUES (?, ?, ?, ?)",
                numberExists.StudName, numberExists.StudNum, numberExists.Email, numberExists.Password
            );

            GenerateDatabase.Instance.database.Delete(entry);
            GenerateDatabase.Instance.database.Delete(studentExists);
            return true; // Successfully moved and deleted
        }
        return false; // Not found in registrations, keep in list
    }
}