using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.Progress;
using static UnityEngine.Rendering.DebugUI.Table;
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
    [SerializeField] private GameObject kingPrefab;
    [SerializeField] private GameObject pawnPrefab;
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject bishopPrefab;
    [SerializeField] private GameObject rookPrefab;
    [SerializeField] private GameObject queenPrefab;
    [SerializeField] private Color black;
    [SerializeField] private Color white;

    [SerializeField] public GameObject focus;
    [SerializeField] private GameObject selectedPiece;
    [SerializeField] private int enPassantIndex = -1;
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

        string fenPos = "rnbqkbnr/4r3/8/8/8/8/4R3/RNBQKBNR w KQkq 0 - 1";

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
        int originIndex = Array.IndexOf(tileObjects, origin);
        int destinationIndex = Array.IndexOf(tileObjects, destination);

        // En passant capture check
        if (destinationIndex == enPassantIndex)
        {
            int capturedPawnIndex = enPassantIndex + ((GetPieceColor(int.Parse(selectedPiece.name)) == Piece.White) ? +8 : -8);

            tileContent[capturedPawnIndex] = 0;
            Transform capTile = tileObjects[capturedPawnIndex].transform.GetChild(0);
            foreach (Transform child in capTile)
                Destroy(child.gameObject);
        }

        enPassantIndex = -1;

        // Check if move allows for an en passant
        if (GetPieceType(int.Parse(selectedPiece.name)) == Piece.Pawn)
        {
            int originRow = originIndex / 8;
            int destinationRow = destinationIndex / 8;

            if (Math.Abs(originRow - destinationRow) == 2)
            {
                enPassantIndex = (originIndex + destinationIndex) / 2;
            }
        }

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
                GetBishopMoves(originGO, pieceGO);
                break;

            case Piece.Knight:
                GetKnightMoves(originGO, pieceGO);
                break;

            case Piece.Queen:
                GetQueenMoves(originGO, pieceGO);
                break;

            case Piece.King:
                GetKingMoves(originGO, pieceGO);
                break;

            case Piece.Pawn:
                GetPawnMoves(originGO, pieceGO);
                break;

            default:
                break;
        }
    }

    // Rook moves
    private void GetRookMoves(GameObject originGO, GameObject pieceGO)
    {
        int piece = GetPieceType(int.Parse(pieceGO.name));
        int color = GetPieceColor(int.Parse(pieceGO.name));

        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        int kingTile = Array.IndexOf(tileContent, (Piece.King | color));

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
    private void GetBishopMoves(GameObject originGO, GameObject pieceGO)
    {
        int piece = GetPieceType(int.Parse(pieceGO.name));
        int color = GetPieceColor(int.Parse(pieceGO.name));

        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        // Search movable tiles
        foreach (int dir in diagonalDir) // Loop through all directions
        {
            int index = originIndex;

            while (true)
            {
                int nextRow = index / 8;
                int nextCol = index % 8;

                // Check if moving off the board
                if (dir == +9 && (nextRow == 7 || nextCol == 7)) break;   // down-right
                if (dir == +7 && (nextRow == 7 || nextCol == 0)) break;   // down-left
                if (dir == -9 && (nextRow == 0 || nextCol == 0)) break;   // up-left
                if (dir == -7 && (nextRow == 0 || nextCol == 7)) break;   // up-right

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

    // Knight moves// Knight moves
    private void GetKnightMoves(GameObject originGO, GameObject pieceGO)
    {
        // No need to get piece type since it's already a knight, but keeping for consistency
        // int piece = GetPieceType(int.Parse(pieceGO.name)); 
        int color = GetPieceColor(int.Parse(pieceGO.name));

        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);
        int originRow = originIndex / 8;
        int originCol = originIndex % 8;

        // Search movable tiles
        foreach (int dir in knightDir) // Loop through all 8 knight moves
        {
            int targetIndex = originIndex + dir;

            // Check if the target index is outside the board bounds (0 to 63)
            if (targetIndex < 0 || targetIndex >= tileContent.Length)
            {
                continue;
            }

            int targetRow = targetIndex / 8;
            int targetCol = targetIndex % 8;

            // Check for wrapping/jumping across the board edges.
            // A knight move changes the column index by 1 or 2, and the row index by 1 or 2.
            // The absolute change in column should not exceed 2, and the absolute change in row should not exceed 2.
            // AND (abs_col_change == 1 AND abs_row_change == 2) OR (abs_col_change == 2 AND abs_row_change == 1)
            int colChange = Math.Abs(targetCol - originCol);
            int rowChange = Math.Abs(targetRow - originRow);

            if ((colChange == 1 && rowChange == 2) || (colChange == 2 && rowChange == 1))
            {
                // Tile is on the board and is a valid L-move
                int targetPiece = tileContent[targetIndex];

                // Check if tile is occupied by a piece of the same color
                if (targetPiece != Piece.None && GetPieceColor(targetPiece) == color)
                {
                    continue; // Skip the move
                }

                // Tile is either empty or contains an enemy piece
                moves.Add(tileObjects[targetIndex]);
            }
        }

        // Highlight moveable tiles
        foreach (GameObject tile in moves)
        {
            tile.GetComponent<Image>().color = Color.blue;
        }
    }

    // Queen moves
    private void GetQueenMoves(GameObject originGO, GameObject pieceGO)
    {
        int piece = GetPieceType(int.Parse(pieceGO.name));
        int color = GetPieceColor(int.Parse(pieceGO.name));

        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        // Search movable tiles
        foreach (int dir in lateralDir.Concat(diagonalDir)) // Loop through all directions
        {
            int index = originIndex;

            while (true)
            {
                int nextRow = index / 8;
                int nextCol = index % 8;

                // Check if moving off the board
                // LATERAL
                if (dir == +1 && nextCol == 7) break;   // right edge
                if (dir == -1 && nextCol == 0) break;   // left edge
                if (dir == +8 && nextRow == 7) break;   // bottom edge
                if (dir == -8 && nextRow == 0) break;   // top edge
                // DIAGONAL
                if (dir == +9 && (nextRow == 7 || nextCol == 7)) break;   // down-right
                if (dir == +7 && (nextRow == 7 || nextCol == 0)) break;   // down-left
                if (dir == -9 && (nextRow == 0 || nextCol == 0)) break;   // up-left
                if (dir == -7 && (nextRow == 0 || nextCol == 7)) break;   // up-right

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

    // king moves
    private void GetKingMoves(GameObject originGO, GameObject pieceGO)
    {
        int piece = GetPieceType(int.Parse(pieceGO.name));
        int color = GetPieceColor(int.Parse(pieceGO.name));

        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        // Search movable tiles
        foreach (int dir in lateralDir.Concat(diagonalDir)) // Loop through all directions
        {
            int index = originIndex;
            int row = index / 8;
            int col = index % 8;

            // Check if moving off the board
            // LATERAL
            if (dir == +1 && col == 7) continue; // right
            if (dir == -1 && col == 0) continue; // left
            if (dir == +8 && row == 7) continue; // down
            if (dir == -8 && row == 0) continue; // up
            // DIAGONAL
            if (dir == +9 && (row == 7 || col == 7)) continue; // down-right
            if (dir == -9 && (row == 0 || col == 0)) continue; // up-left
            if (dir == -7 && (row == 0 || col == 7)) continue; // up-right
            if (dir == +7 && (row == 7 || col == 0)) continue; // down-left

            index += dir;

            // Stop if tile occupied
            if (tileContent[index] != Piece.None)
            {
                if (GetPieceColor(tileContent[index]) != color)
                {
                    moves.Add(tileObjects[index]); // Enemy piece
                }
                continue;
            }

            moves.Add(tileObjects[index]);
        }

        // Highlight moveable tiles
        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

    // pawn moves
    private void GetPawnMoves(GameObject originGO, GameObject pieceGO)
    {
        int piece = GetPieceType(int.Parse(pieceGO.name));
        int color = GetPieceColor(int.Parse(pieceGO.name));

        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        // Search movable tiles
        foreach (int dir in lateralDir) // Loop through all directions
        {
            int index = originIndex;
            int row = index / 8;
            int steps = 1;

            if ((row == 1 && color == Piece.Black) || (row == 6 && color == Piece.White))
            {
                steps = 2;
            }

            // Forward movement
            for (int i = 1; i <= steps; i++)
            {
                int nextRow = index / 8;

                // Check if moving off the board
                if (dir == +1 || dir == -1) continue; // left or right
                if (dir == +8 && nextRow == 7) continue; // down
                if (dir == -8 && nextRow == 0) continue; // up
                if (dir == +8 && color == Piece.White) continue; // down
                if (dir == -8 && color == Piece.Black) continue; // up

                index += dir;

                // Stop if tile occupied
                if (tileContent[index] != Piece.None)
                {
                    break;
                }
                moves.Add(tileObjects[index]);
            }
        }

        // Search for diagonal captures
        foreach (int dir in diagonalDir)
        {
            int index = originIndex;
            int row = index / 8;
            int col = index % 8;

            // DIAGONAL
            if (dir == +9 && (row == 7 || col == 7)) continue; // down-right
            if (dir == -9 && (row == 0 || col == 0)) continue; // up-left
            if (dir == +7 && (row == 7 || col == 0)) continue; // down-left
            if (dir == -7 && (row == 0 || col == 7)) continue; // up-right
            if ((dir == +9 || dir == +7) && color == Piece.White) continue; 
            if ((dir == -9 || dir == -7) && color == Piece.Black) continue; 

            index += dir;

            // Normal capture
            if (tileContent[index] != Piece.None &&
                GetPieceColor(tileContent[index]) != color)
            {
                moves.Add(tileObjects[index]);
                continue;
            }

            // En passant capture
            if (index == enPassantIndex)
            {
                // The pawn to be captured is behind the target square
                int capturedPawnIndex = enPassantIndex + ((color == Piece.White) ? +8 : -8);

                if (GetPieceColor(tileContent[capturedPawnIndex]) != color &&
                    GetPieceType(tileContent[capturedPawnIndex]) == Piece.Pawn)
                {
                    moves.Add(tileObjects[index]);
                }
            }
        }

        // Highlight moveable tiles
        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

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
            case Piece.King: return kingPrefab;
            case Piece.Pawn: return pawnPrefab;
            case Piece.Knight: return knightPrefab;
            case Piece.Bishop: return bishopPrefab;
            case Piece.Rook: return rookPrefab;
            case Piece.Queen: return queenPrefab;
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
