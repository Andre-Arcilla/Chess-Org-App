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
            // Prevent destruction across scene loads if this is a singleton manager
            // transform.SetParent(null); 
            // DontDestroyOnLoad(gameObject);
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

    [Header("Board State")]
    [SerializeField] public GameObject focus;
    [SerializeField] private GameObject selectedPiece;
    [SerializeField] private int enPassantIndex = -1;

    // Castling Rights (K=Kingside, Q=Queenside)
    [SerializeField] private bool whiteCanKSC = true;
    [SerializeField] private bool whiteCanQSC = true;
    [SerializeField] private bool blackCanKSC = true;
    [SerializeField] private bool blackCanQSC = true;

    // tileContent stores the Piece integer value (Type | Color)
    [SerializeField] private int[] tileContent;
    [SerializeField] private GameObject[] tileObjects;
    [SerializeField] private List<GameObject> moves;

    [SerializeField] private Transform mainView;
    [SerializeField] public GameObject SelectedPiece => selectedPiece;
    [SerializeField] public GameObject[] TileObjects => tileObjects;
    [SerializeField] public List<GameObject> Moves => moves;
    [SerializeField] public Transform MainView => mainView;

    // Direction Vectors (based on array index 0-63)
    private int[] lateralDir = { +1, -1, +8, -8 }; // Right, Left, Down, Up
    private int[] diagonalDir = { +7, -7, +9, -9 }; // Down-Left, Up-Right, Down-Right, Up-Left
    private int[] knightDir = { +6, -6, +10, -10, +15, -15, +17, -17 };

    private void Start()
    {
        StartBoard();
    }

    private void StartBoard()
    {
        tileContent = new int[tileObjects.Length];

        // Example starting position:
        // King at e1/e8, Rook at h1/h8, some space on other files
        string fenPos = "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq 0 1";

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

        string[] fenParts = fen.Split(' ');
        string fenBoard = fenParts[0];
        string castlingRights = fenParts[2];
        int tiles = 0;

        // --- 1. Piece Placement ---
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

        // --- 2. Castling Rights ---
        whiteCanKSC = castlingRights.Contains('K');
        whiteCanQSC = castlingRights.Contains('Q');
        blackCanKSC = castlingRights.Contains('k');
        blackCanQSC = castlingRights.Contains('q');
    }

    public void MovePiece(GameObject origin, GameObject destination)
    {
        int originIndex = Array.IndexOf(tileObjects, origin);
        int destinationIndex = Array.IndexOf(tileObjects, destination);
        int pieceValue = int.Parse(selectedPiece.name);
        int pieceType = GetPieceType(pieceValue);

        // --- Castling Execution Check ---
        if (pieceType == Piece.King && Math.Abs(originIndex - destinationIndex) == 2)
        {
            // Calculate rook movement indices
            int rookOriginIndex = (destinationIndex > originIndex) ? (originIndex + 3) : (originIndex - 4);
            int rookDestinationIndex = (destinationIndex > originIndex) ? (destinationIndex - 1) : (destinationIndex + 1);

            // 1. Get the rook GameObject
            Transform rookOriginHolder = tileObjects[rookOriginIndex].transform.GetChild(0);
            GameObject rookGO = null;
            if (rookOriginHolder.childCount > 0)
            {
                rookGO = rookOriginHolder.GetChild(0).gameObject;
            }

            // 2. Move the rook GO
            if (rookGO != null)
            {
                rookGO.transform.SetParent(tileObjects[rookDestinationIndex].transform.GetChild(0));
            }

            // 3. Update tileContent for the rook
            tileContent[rookOriginIndex] = Piece.None;
            tileContent[rookDestinationIndex] = pieceValue; // Rook has the same color/value as the king for now
        }

        // --- En passant capture check ---
        if (destinationIndex == enPassantIndex && pieceType == Piece.Pawn)
        {
            // Determine the location of the captured pawn (one row back from en passant square)
            int capturedPawnIndex = enPassantIndex + ((GetPieceColor(pieceValue) == Piece.White) ? +8 : -8);

            tileContent[capturedPawnIndex] = Piece.None;
            Transform capTile = tileObjects[capturedPawnIndex].transform.GetChild(0);
            foreach (Transform child in capTile)
                Destroy(child.gameObject);
        }

        enPassantIndex = -1;

        // --- Castling Rights Revocation ---
        if (pieceType == Piece.King)
        {
            if (GetPieceColor(pieceValue) == Piece.White)
            {
                whiteCanKSC = false;
                whiteCanQSC = false;
            }
            else
            {
                blackCanKSC = false;
                blackCanQSC = false;
            }
        }
        else if (pieceType == Piece.Rook)
        {
            if (originIndex == 63) whiteCanKSC = false; // H1 (White KS Rook)
            if (originIndex == 56) whiteCanQSC = false; // A1 (White QS Rook)
            if (originIndex == 7) blackCanKSC = false;  // H8 (Black KS Rook)
            if (originIndex == 0) blackCanQSC = false;  // A8 (Black QS Rook)
        }

        // --- Check for new en passant opportunity ---
        if (pieceType == Piece.Pawn)
        {
            int originRow = originIndex / 8;
            int destinationRow = destinationIndex / 8;

            if (Math.Abs(originRow - destinationRow) == 2)
            {
                enPassantIndex = (originIndex + destinationIndex) / 2;
            }
        }

        // --- Finalize Piece Move (King/Regular) ---
        Transform destHolder = destination.transform.GetChild(0);
        foreach (Transform child in destHolder)
            Destroy(child.gameObject);

        selectedPiece.transform.SetParent(destHolder);

        // Update tileContent for the King/Piece
        tileContent[originIndex] = Piece.None;
        tileContent[destinationIndex] = pieceValue;

        ResetObjects();
    }

    public void CheckMove(GameObject originGO, GameObject pieceGO)
    {
        ResetObjects();
        selectedPiece = pieceGO;
        int pieceType = GetPieceType(int.Parse(pieceGO.name));

        switch (pieceType)
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

    // ----------------------------------------------------------------------
    // --- KING SAFETY IMPLEMENTATION ---
    // ----------------------------------------------------------------------

    // Core validation: Simulates a move and checks if the King is attacked afterwards (i.e., checks for pins/self-check)
    private bool IsMoveLegal(int originIndex, int destinationIndex)
    {
        int piece = tileContent[originIndex];
        int pieceType = GetPieceType(piece);
        int pieceColor = GetPieceColor(piece);
        int opponentColor = (pieceColor == Piece.White) ? Piece.Black : Piece.White;

        // Store original board state to revert later
        int originalPieceAtDest = tileContent[destinationIndex];
        int originalPieceAtOrigin = tileContent[originIndex];

        // --- Simulate the move ---
        tileContent[destinationIndex] = originalPieceAtOrigin;
        tileContent[originIndex] = Piece.None;

        // Find the King's new position
        int kingTileIndex = (pieceType == Piece.King) ? destinationIndex : FindKingTile(pieceColor);

        // --- Check if the king is now attacked ---
        bool isKingInCheck = IsTileAttacked(kingTileIndex, opponentColor);

        // --- Undo the move (revert board state) ---
        tileContent[originIndex] = originalPieceAtOrigin;
        tileContent[destinationIndex] = originalPieceAtDest;

        return !isKingInCheck;
    }

    // Castling Safety: Checks if any square the King moves through or lands on is attacked.
    private bool CanCastlePathBeAttacked(int startTile, int endTile, int color)
    {
        int opponentColor = (color == Piece.White) ? Piece.Black : Piece.White;

        // Squares to check: Current square (start), square 1 (middle), square 2 (end)
        int[] squaresToCheck;

        if (endTile > startTile) // Kingside (e->g)
        {
            // Check e1/e8, f1/f8, g1/g8
            squaresToCheck = new int[] { startTile, startTile + 1, startTile + 2 };
        }
        else // Queenside (e->c)
        {
            // Check e1/e8, d1/d8, c1/c8
            squaresToCheck = new int[] { startTile, startTile - 1, startTile - 2 };
        }

        foreach (int tileIndex in squaresToCheck)
        {
            if (IsTileAttacked(tileIndex, opponentColor))
            {
                return false; // Path is attacked
            }
        }
        return true; // Path is safe
    }


    // Checks if a tile is attacked by the attackerColor using ray-tracing
    public bool IsTileAttacked(int targetIndex, int attackerColor)
    {
        int targetRow = targetIndex / 8;
        int targetCol = targetIndex % 8;

        // --- 1. Check Sliding Pieces (Rook, Bishop, Queen) ---
        foreach (int dir in lateralDir.Concat(diagonalDir))
        {
            int index = targetIndex;
            int nextRow = targetRow;
            int nextCol = targetCol;

            while (true)
            {
                // Basic edge checks
                if (dir == +1 && nextCol == 7) break;
                if (dir == -1 && nextCol == 0) break;
                if (dir == +8 && nextRow == 7) break;
                if (dir == -8 && nextRow == 0) break;
                if (dir == +9 && (nextRow == 7 || nextCol == 7)) break;
                if (dir == +7 && (nextRow == 7 || nextCol == 0)) break;
                if (dir == -9 && (nextRow == 0 || nextCol == 0)) break;
                if (dir == -7 && (nextRow == 0 || nextCol == 7)) break;

                index += dir;
                nextRow = index / 8;
                nextCol = index % 8;

                if (index < 0 || index >= tileContent.Length) break;

                int piece = tileContent[index];
                if (piece != Piece.None)
                {
                    if (GetPieceColor(piece) == attackerColor)
                    {
                        int pieceType = GetPieceType(piece);

                        bool isSlidingAttacker = (Array.IndexOf(lateralDir, dir) != -1 && (pieceType == Piece.Rook || pieceType == Piece.Queen)) ||
                                                 (Array.IndexOf(diagonalDir, dir) != -1 && (pieceType == Piece.Bishop || pieceType == Piece.Queen));

                        if (isSlidingAttacker) return true;
                    }
                    break; // Stop the ray if any piece blocks it
                }
            }
        }

        // --- 2. Check Knight Attacks ---
        foreach (int dir in knightDir)
        {
            int checkIndex = targetIndex + dir;
            if (checkIndex < 0 || checkIndex >= tileContent.Length) continue;

            int checkRow = checkIndex / 8;
            int checkCol = checkIndex % 8;
            int colChange = Math.Abs(checkCol - targetCol);
            int rowChange = Math.Abs(checkRow - targetRow);

            if ((colChange == 1 && rowChange == 2) || (colChange == 2 && rowChange == 1))
            {
                int piece = tileContent[checkIndex];
                if (piece != Piece.None && GetPieceColor(piece) == attackerColor && GetPieceType(piece) == Piece.Knight)
                {
                    return true;
                }
            }
        }

        // --- 3. Check King Attack (1 square away) ---
        foreach (int dir in lateralDir.Concat(diagonalDir))
        {
            int checkIndex = targetIndex + dir;
            if (checkIndex < 0 || checkIndex >= tileContent.Length) continue;

            int checkRow = checkIndex / 8;
            int checkCol = checkIndex % 8;
            if (Math.Abs(checkRow - targetRow) > 1 || Math.Abs(checkCol - targetCol) > 1) continue;

            int piece = tileContent[checkIndex];
            if (piece != Piece.None && GetPieceColor(piece) == attackerColor && GetPieceType(piece) == Piece.King)
            {
                return true;
            }
        }

        // --- 4. Check Pawn Attacks ---
        // Direction from target back to the attacker's row
        int forwardOffset = (attackerColor == Piece.White) ? +8 : -8;
        int[] pawnCaptureDirs = { forwardOffset - 1, forwardOffset + 1 }; // Diagonal positions from target

        foreach (int dir in pawnCaptureDirs)
        {
            int checkIndex = targetIndex + dir;

            if (checkIndex >= 0 && checkIndex < tileContent.Length)
            {
                int checkCol = checkIndex % 8;

                // Ensure the tile is indeed diagonal
                if (Math.Abs(checkCol - targetCol) == 1)
                {
                    int piece = tileContent[checkIndex];

                    if (piece != Piece.None &&
                        GetPieceColor(piece) == attackerColor &&
                        GetPieceType(piece) == Piece.Pawn)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // ----------------------------------------------------------------------
    // --- MOVEMENT GENERATION ---
    // ----------------------------------------------------------------------

    private void GetRookMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        foreach (int dir in lateralDir)
        {
            int index = originIndex;

            while (true)
            {
                int nextRow = index / 8;
                int nextCol = index % 8;

                if (dir == +1 && nextCol == 7) break;
                if (dir == -1 && nextCol == 0) break;
                if (dir == +8 && nextRow == 7) break;
                if (dir == -8 && nextRow == 0) break;

                index += dir;
                if (index < 0 || index >= tileContent.Length) break;

                int targetTileContent = tileContent[index];

                if (!IsMoveLegal(originIndex, index))
                {
                    break;
                }

                if (targetTileContent != Piece.None)
                {
                    if (GetPieceColor(targetTileContent) != color)
                    {
                        moves.Add(tileObjects[index]);
                    }
                    break;
                }
                moves.Add(tileObjects[index]);
            }
        }
        foreach (GameObject tile in moves)
        {
            tile.GetComponent<Image>().color = Color.blue;
        }
    }

    private void GetBishopMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        foreach (int dir in diagonalDir)
        {
            int index = originIndex;

            while (true)
            {
                int nextRow = index / 8;
                int nextCol = index % 8;

                if (dir == +9 && (nextRow == 7 || nextCol == 7)) break;
                if (dir == +7 && (nextRow == 7 || nextCol == 0)) break;
                if (dir == -9 && (nextRow == 0 || nextCol == 0)) break;
                if (dir == -7 && (nextRow == 0 || nextCol == 7)) break;

                index += dir;
                if (index < 0 || index >= tileContent.Length) break;

                int targetTileContent = tileContent[index];

                if (!IsMoveLegal(originIndex, index))
                {
                    break;
                }

                if (targetTileContent != Piece.None)
                {
                    if (GetPieceColor(targetTileContent) != color)
                    {
                        moves.Add(tileObjects[index]);
                    }
                    break;
                }

                moves.Add(tileObjects[index]);
            }
        }
        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

    private void GetKnightMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);
        int originRow = originIndex / 8;
        int originCol = originIndex % 8;

        foreach (int dir in knightDir)
        {
            int targetIndex = originIndex + dir;

            if (targetIndex < 0 || targetIndex >= tileContent.Length) continue;

            int targetRow = targetIndex / 8;
            int targetCol = targetIndex % 8;

            int colChange = Math.Abs(targetCol - originCol);
            int rowChange = Math.Abs(targetRow - originRow);

            if ((colChange == 1 && rowChange == 2) || (colChange == 2 && rowChange == 1))
            {
                int targetPiece = tileContent[targetIndex];

                if (targetPiece != Piece.None && GetPieceColor(targetPiece) == color)
                {
                    continue;
                }

                if (IsMoveLegal(originIndex, targetIndex))
                {
                    moves.Add(tileObjects[targetIndex]);
                }
            }
        }
        foreach (GameObject tile in moves)
        {
            tile.GetComponent<Image>().color = Color.blue;
        }
    }

    private void GetQueenMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        foreach (int dir in lateralDir.Concat(diagonalDir))
        {
            int index = originIndex;

            while (true)
            {
                int nextRow = index / 8;
                int nextCol = index % 8;

                if (dir == +1 && nextCol == 7) break;
                if (dir == -1 && nextCol == 0) break;
                if (dir == +8 && nextRow == 7) break;
                if (dir == -8 && nextRow == 0) break;
                if (dir == +9 && (nextRow == 7 || nextCol == 7)) break;
                if (dir == +7 && (nextRow == 7 || nextCol == 0)) break;
                if (dir == -9 && (nextRow == 0 || nextCol == 0)) break;
                if (dir == -7 && (nextRow == 0 || nextCol == 7)) break;

                index += dir;
                if (index < 0 || index >= tileContent.Length) break;

                int targetTileContent = tileContent[index];

                if (!IsMoveLegal(originIndex, index))
                {
                    break;
                }

                if (targetTileContent != Piece.None)
                {
                    if (GetPieceColor(targetTileContent) != color)
                    {
                        moves.Add(tileObjects[index]);
                    }
                    break;
                }

                moves.Add(tileObjects[index]);
            }
        }
        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

    private void GetKingMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        int opponentColor = (color == Piece.White) ? Piece.Black : Piece.White;
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        // --- 1. Single Step King Moves ---
        foreach (int dir in lateralDir.Concat(diagonalDir))
        {
            int index = originIndex;
            int row = index / 8;
            int col = index % 8;

            if (dir == +1 && col == 7) continue;
            if (dir == -1 && col == 0) continue;
            if (dir == +8 && row == 7) continue;
            if (dir == -8 && row == 0) continue;
            if (dir == +9 && (row == 7 || col == 7)) continue;
            if (dir == -9 && (row == 0 || col == 0)) continue;
            if (dir == -7 && (row == 0 || col == 7)) continue;
            if (dir == +7 && (row == 7 || col == 0)) continue;

            index += dir;
            if (index < 0 || index >= tileContent.Length) continue;

            int targetTileContent = tileContent[index];

            // Direct Attack Check: King cannot move into check
            if (IsTileAttacked(index, opponentColor))
            {
                continue;
            }

            if (targetTileContent != Piece.None)
            {
                if (GetPieceColor(targetTileContent) != color)
                {
                    moves.Add(tileObjects[index]);
                }
                continue;
            }
            moves.Add(tileObjects[index]);
        }

        // --- 2. Castling Checks ---
        bool isCurrentlyInCheck = IsTileAttacked(originIndex, opponentColor);

        if (!isCurrentlyInCheck)
        {
            // White King on E1 (index 60)
            if (color == Piece.White && originIndex == 60)
            {
                // Kingside Castling (0-0)
                if (whiteCanKSC && tileContent[61] == Piece.None && tileContent[62] == Piece.None)
                {
                    int rookPiece = tileContent[63];
                    if (GetPieceType(rookPiece) == Piece.Rook && GetPieceColor(rookPiece) == Piece.White)
                    {
                        if (CanCastlePathBeAttacked(60, 62, color))
                        {
                            moves.Add(tileObjects[62]); // Target G1
                        }
                    }
                }
                // Queenside Castling (0-0-0)
                if (whiteCanQSC && tileContent[59] == Piece.None && tileContent[58] == Piece.None && tileContent[57] == Piece.None)
                {
                    int rookPiece = tileContent[56];
                    if (GetPieceType(rookPiece) == Piece.Rook && GetPieceColor(rookPiece) == Piece.White)
                    {
                        if (CanCastlePathBeAttacked(60, 58, color))
                        {
                            moves.Add(tileObjects[58]); // Target C1
                        }
                    }
                }
            }
            // Black King on E8 (index 4)
            else if (color == Piece.Black && originIndex == 4)
            {
                // Kingside Castling (0-0)
                if (blackCanKSC && tileContent[5] == Piece.None && tileContent[6] == Piece.None)
                {
                    int rookPiece = tileContent[7];
                    if (GetPieceType(rookPiece) == Piece.Rook && GetPieceColor(rookPiece) == Piece.Black)
                    {
                        if (CanCastlePathBeAttacked(4, 6, color))
                        {
                            moves.Add(tileObjects[6]); // Target G8
                        }
                    }
                }
                // Queenside Castling (0-0-0)
                if (blackCanQSC && tileContent[3] == Piece.None && tileContent[2] == Piece.None && tileContent[1] == Piece.None)
                {
                    int rookPiece = tileContent[0];
                    if (GetPieceType(rookPiece) == Piece.Rook && GetPieceColor(rookPiece) == Piece.Black)
                    {
                        if (CanCastlePathBeAttacked(4, 2, color))
                        {
                            moves.Add(tileObjects[2]); // Target C8
                        }
                    }
                }
            }
        }

        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

    private void GetPawnMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);
        int forwardDir = (color == Piece.White) ? -8 : +8;
        int startRow = (color == Piece.White) ? 6 : 1;

        // --- Forward Movement ---
        for (int step = 1; step <= 2; step++)
        {
            if (step == 2 && (originIndex / 8 != startRow || tileContent[originIndex + forwardDir] != Piece.None))
                continue;

            int targetIndex = originIndex + (forwardDir * step);

            if (targetIndex < 0 || targetIndex >= tileContent.Length) break;

            if (tileContent[targetIndex] != Piece.None) break;

            if (IsMoveLegal(originIndex, targetIndex))
            {
                moves.Add(tileObjects[targetIndex]);
            }
            else
            {
                if (step == 1) break;
            }
        }

        // --- Diagonal Captures & En Passant ---
        int[] captureDirs = { forwardDir - 1, forwardDir + 1 };

        foreach (int dir in captureDirs)
        {
            int targetIndex = originIndex + dir;

            if (targetIndex < 0 || targetIndex >= tileContent.Length) continue;

            if (Math.Abs((targetIndex % 8) - (originIndex % 8)) != 1) continue;

            int targetTileContent = tileContent[targetIndex];

            bool isNormalCapture = (targetTileContent != Piece.None && GetPieceColor(targetTileContent) != color);

            bool isEnPassant = false;
            if (targetIndex == enPassantIndex)
            {
                // Calculate the index of the captured pawn (one row back from the target square)
                int capturedPawnIndex = enPassantIndex + ((color == Piece.White) ? +8 : -8);

                // Confirm the captured pawn is actually there and is an enemy pawn
                int capturedPiece = tileContent[capturedPawnIndex];

                if (GetPieceType(capturedPiece) == Piece.Pawn && GetPieceColor(capturedPiece) != color)
                {
                    isEnPassant = true;
                }
            }

            if (isNormalCapture || isEnPassant)
            {
                if (IsMoveLegal(originIndex, targetIndex))
                {
                    moves.Add(tileObjects[targetIndex]);
                }
            }
        }

        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

    // ----------------------------------------------------------------------
    // --- UTILITIES ---
    // ----------------------------------------------------------------------

    public void ResetObjects()
    {
        selectedPiece = null;
        moves.Clear();

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int index = row * 8 + col;

                Color tileColor = ((row + col) % 2 == 0)
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(132, 132, 132, 255);

                tileObjects[index].GetComponent<Image>().color = tileColor;
            }
        }
    }

    public int FindKingTile(int color)
    {
        for (int i = 0; i < tileContent.Length; i++)
        {
            if (GetPieceType(tileContent[i]) == Piece.King && GetPieceColor(tileContent[i]) == color)
            {
                return i;
            }
        }
        return -1;
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
        return piece & 0b0111; // Extracts the piece type (1-7)
    }

    private int GetPieceColor(int piece)
    {
        return piece & (Piece.White | Piece.Black); // Extracts color bits (8 or 16)
    }

    public void BackButton()
    {
        SceneLoader.Instance.LoadNewScene("AdminScene");
    }
}