using SQLite4Unity3d;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnnouncementList : MonoBehaviour
{
    [SerializeField] private GenerateDatabase dbManager;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject parentList;
    [SerializeField] private GameObject announcementPost;
    [SerializeField] private Transform mainView;
    [SerializeField] private Transform canvas;
    private List<AnnouncementModel> announcementsList;

    private SQLiteConnection database;

    void Start()
    {
        database = dbManager.ConnectDB();

        announcementsList = database.Table<AnnouncementModel>().ToList();
        Debug.Log($"{announcementsList[0].Author} s");

        foreach (var item in announcementsList)
        {
            var newCard = Instantiate(cardPrefab, cardContainer);
            newCard.GetComponent<AnnouncementCard>().SetInfo(item, parentList, announcementPost, mainView, canvas);
        }
    }
}
