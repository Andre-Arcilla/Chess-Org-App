using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

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

    [SerializeField] public GameObject focus;
    [SerializeField] private GameObject selectedPiece;
    [SerializeField] private int[] tileContent;
    [SerializeField] private GameObject[] tileObjects;
    [SerializeField] private List<GameObject> moves;

    [SerializeField] private Transform mainView;
    [SerializeField] public GameObject SelectedPiece => selectedPiece;
    [SerializeField] public GameObject[] TileObjects => tileObjects;
    [SerializeField] public List<GameObject> Moves => moves;
    [SerializeField] public Transform MainView => mainView;

    private int[] lateralDir = { +1, -1, +8, -8 };
    private int[] diagonalDir = { +7, -7, +9, -9 };
    private int[] knightDir = { +6, -6, +10, -10, +15, -15, +17, -17 };

    private void Start()
    {
        StartBoard();
    }

    private void StartBoard()
    {
        tileContent = new int[tileObjects.Length];

        string fenPos = "rnbqkbnr/1ppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq 0 - 1";

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

                        pieceGO.name = tileContent[tiles].ToString();
                    }

                    tiles++;
                }
            }
        }
    }

    public void MovePiece(GameObject origin, GameObject destination)
    {
        selectedPiece.transform.SetParent(destination.transform.GetChild(0));

        tileContent[Array.IndexOf(tileObjects, origin)] = 0;

        tileContent[Array.IndexOf(tileObjects, destination)] = int.Parse(selectedPiece.name);

        ResetObjects();
    }

    public void CheckMove(GameObject originGO, GameObject pieceGO)
    {
        //get piece by looking for origin's child
        //check for the possible moves of the piece
        // +7/+9/-7/-9 for diagonals
        // +1/+8/-1/-9 for straights
        // +6/+10/+15/+17/-6/-10/-15/-17 for knights

        ResetObjects();

        selectedPiece = pieceGO;

        int piece = GetPieceType(int.Parse(pieceGO.name));

        switch(piece)
        {
            case Piece.Rook:
                GetRookMoves(originGO, pieceGO);
                break;

            case Piece.Bishop:
                GetRookMoves(originGO, pieceGO);
                break;

            case Piece.Knight:
                GetRookMoves(originGO, pieceGO);
                break;

            case Piece.Queen:
                GetRookMoves(originGO, pieceGO);
                break;

            case Piece.King:
                GetRookMoves(originGO, pieceGO);
                break;

            case Piece.Pawn:
                GetRookMoves(originGO, pieceGO);
                break;

            default:
                break;
        }
    }

    private void GetRookMoves(GameObject originGO, GameObject pieceGO)
    {
        int piece = GetPieceType(int.Parse(pieceGO.name));
        int color = GetPieceColor(int.Parse(pieceGO.name));

        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        // Search movable tiles
        foreach (int dir in lateralDir) // Loop through all directions
        {
            int index = originIndex;

            while (true)
            {
                int nextRow = index / 8;
                int nextCol = index % 8;

                // Check if moving off the board
                if (dir == +1 && nextCol == 7) break;   // right edge
                if (dir == -1 && nextCol == 0) break;   // left edge
                if (dir == +8 && nextRow == 7) break;   // bottom edge
                if (dir == -8 && nextRow == 0) break;   // top edge

                index += dir;

                // Stop if tile occupied
                if (tileContent[index] != Piece.None)
                {
                    if (GetPieceColor(tileContent[index]) != color)
                    {
                        moves.Add(tileObjects[index]); // Enemy piece
                    }
                    break;
                }

                moves.Add(tileObjects[index]);
            }
        }

        // Highlight moveable tiles
        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

    // Bishop moves
    // Knight moves
    // Queen moves
    // king moves
    // pawn moves

    public void ResetObjects()
    {
        selectedPiece = null;
        moves.Clear();

        // Reset tile colors
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int index = row * 8 + col;

                // Alternate color: sum of row + col determines color
                Color tileColor = ((row + col) % 2 == 0)
                    ? new Color32(255, 255, 255, 255)  // gray #848484
                    : new Color32(132, 132, 132, 255); // white #FFFFFF

                tileObjects[index].GetComponent<Image>().color = tileColor;
            }
        }
    }

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

    private int GetPieceColor(int piece)
    {
        return piece & (Piece.White | Piece.Black); // extracts color bits
    }

    public void BackButton()
    {
        SceneLoader.Instance.LoadNewScene("AdminScene");
    }
}
