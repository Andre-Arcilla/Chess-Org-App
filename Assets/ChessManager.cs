using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ChessManager : MonoBehaviour
{
    public static ChessManager Instance { get; private set; }

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

    [Header("Chess Pieces")]
    [SerializeField] private GameObject King;
    [SerializeField] private GameObject Pawn;
    [SerializeField] private GameObject Knight;
    [SerializeField] private GameObject Bishop;
    [SerializeField] private GameObject Rook;
    [SerializeField] private GameObject Queen;
    [SerializeField] private Color black;
    [SerializeField] private Color white;

    [SerializeField] private GameObject[] tileObjects;
    [SerializeField] public int[] tileContent;

    [SerializeField] private Transform mainView;
    [SerializeField] public Transform MainView => mainView;
    [SerializeField] public GameObject[] TileObjects => tileObjects;

    private void Start()
    {
        StartBoard();
    }

    private void StartBoard()
    {
        tileContent = new int[tileObjects.Length];

        string fenPos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq 0 - 1";

        ReadFENPos(fenPos);
    }

    // Takes a FEN string and sets up the board
    private void ReadFENPos(string fen)
    {
        Dictionary<char, int> pieceSymbol = new Dictionary<char, int>()
        {
            ['k'] = Piece.King,
            ['p'] = Piece.Pawn, 
            ['n'] = Piece.Knight, 
            ['b'] = Piece.Bishop, 
            ['r'] = Piece.Rook, 
            ['q'] = Piece.Queen
        };

        string fenBoard = fen.Split(' ')[0];
        int tiles = 0;

        foreach (char symbol in fenBoard)
        {
            if (symbol != '/')
            {
                if (char.IsDigit(symbol))
                {
                    tiles += (int)char.GetNumericValue(symbol);
                }
                else
                {
                    int pieceColor = (char.IsUpper(symbol)) ? Piece.White : Piece.Black;
                    int pieceType = pieceSymbol[char.ToLower(symbol)];
                    tileContent[tiles] = pieceType | pieceColor;

                    GameObject prefab = GetPrefabForPiece(pieceType);
                    Transform holder = tileObjects[tiles].transform.GetChild(0);

                    if (prefab != null)
                    {
                        GameObject pieceGO = Instantiate(prefab, holder);
                        Image img = pieceGO.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = (pieceColor == Piece.White) ? white : black;
                        }
                    }

                    tiles++;
                }
            }
        }
    }

    public void MovePiece(GameObject piece, GameObject destination)
    {
        int pieceToMove = tileContent[Array.IndexOf(tileObjects, piece)];

        tileContent[Array.IndexOf(tileObjects, piece)] = 0;
        tileContent[Array.IndexOf(tileObjects, destination)] = 0;
        tileContent[Array.IndexOf(tileObjects, destination)] = pieceToMove;
    }


    // ===== HELPER METHODS =====

    private GameObject GetPrefabForPiece(int pieceType)
    {
        switch (pieceType)
        {
            case Piece.King: return King;
            case Piece.Pawn: return Pawn;
            case Piece.Knight: return Knight;
            case Piece.Bishop: return Bishop;
            case Piece.Rook: return Rook;
            case Piece.Queen: return Queen;
            default: return null;
        }
    }

    private int GetPieceType(int piece)
    {
        return piece & 0b0111; // same as & 7
    }

    private bool IsWhite(int piece)
    {
        return (piece & Piece.White) != 0;
    }

    private bool IsBlack(int piece)
    {
        return (piece & Piece.Black) != 0;
    }

    public void BackButton()
    {
        SceneLoader.Instance.LoadNewScene("AdminScene");
    }
}
