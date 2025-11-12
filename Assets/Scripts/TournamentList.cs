using SQLite4Unity3d;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TournamentList : MonoBehaviour
{
    [SerializeField] private GenerateDatabase dbManager;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject parentList;
    [SerializeField] private GameObject tournamentPost;
    [SerializeField] private Transform mainView;
    [SerializeField] private Transform canvas;
    private List<TournamentModel> tournamentList;

    private SQLiteConnection database;

    void Start()
    {
        database = dbManager.ConnectDB();

        tournamentList = database.Table<TournamentModel>().ToList();

        foreach (var item in tournamentList)
        {
            var newCard = Instantiate(cardPrefab, cardContainer);
            newCard.GetComponent<TournamentCard>().SetInfo(item, parentList, tournamentPost, mainView, canvas);
        }
    }
}
