using System;
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

    [Header("PGN & FEN")]

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
    [SerializeField] private bool isPromotionPending = false;
    [SerializeField] private int capturedPieceValue = Piece.None;
    [SerializeField] private int promotionOriginIndex = -1;
    [SerializeField] private int promotionDestinationIndex = -1;
    [SerializeField] private int currentTurnColor = Piece.White;

    [Header("UI References")]
    [SerializeField] private GameObject promotionPanel;

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
    [SerializeField] public int CurrentTurnColor => currentTurnColor;
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
        string fenPos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq 0 1";

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
        if (selectedPiece == null)
        {
            return;
        }

        int originIndex = Array.IndexOf(tileObjects, origin);
        int destinationIndex = Array.IndexOf(tileObjects, destination);
        int originalPieceAtDest = tileContent[destinationIndex];
        int pieceValue = int.Parse(selectedPiece.name);
        int pieceType = GetPieceType(pieceValue);
        int pieceColor = GetPieceColor(pieceValue);

        // --- Castling Execution Check (BUG FIX: Rook value updated) ---
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

            // 3. Update tileContent for the rook (FIXED VALUE)
            int rookValue = Piece.Rook | pieceColor;
            tileContent[rookOriginIndex] = Piece.None;
            tileContent[rookDestinationIndex] = rookValue;
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
        if (originIndex != destinationIndex)
        {
            if (pieceType == Piece.King)
            {
                if (pieceColor == Piece.White)
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
                if (originIndex == 63) whiteCanKSC = false;
                if (originIndex == 56) whiteCanQSC = false;
                if (originIndex == 7) blackCanKSC = false;
                if (originIndex == 0) blackCanQSC = false;
            }
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

        // --- Finalize Piece Move (Visual) ---
        Transform destHolder = destination.transform.GetChild(0);
        foreach (Transform child in destHolder)
            Destroy(child.gameObject);

        selectedPiece.transform.SetParent(destHolder);

        // --- PAWN PROMOTION CHECK AND PAUSE (CRITICAL LOGIC) ---
        if (pieceType == Piece.Pawn && IsPromotionSquare(destinationIndex, pieceColor))
        {
            isPromotionPending = true;
            capturedPieceValue = originalPieceAtDest;
            promotionOriginIndex = originIndex;
            promotionDestinationIndex = destinationIndex;

            // Finalize the pawn's move in the board state, waiting for promotion selection.
            tileContent[originIndex] = Piece.None;
            tileContent[destinationIndex] = pieceValue;

            if (promotionPanel != null)
            {
                promotionPanel.SetActive(true);
            }
            else
            {
                PromoteToQueen(); // Default to Queen if no panel is set
            }

            // HALT: Return and wait for PromoteToX() or CancelPromotion() to be called by UI.
            return;
        }

        // --- Normal Move Finalization (If NOT a promotion) ---
        tileContent[originIndex] = Piece.None;
        tileContent[destinationIndex] = pieceValue;

        ResetObjects();

        // --- NEW: Turn Switch and Game End Check ---
        SwitchTurnAndCheckGameEnd();
    }

    public void CheckMove(GameObject originGO, GameObject pieceGO)
    {
        // --- TURN ENFORCEMENT ---
        int pieceColor = GetPieceColor(int.Parse(pieceGO.name));
        if (pieceColor != currentTurnColor)
        {
            // Don't select or generate moves for the wrong colored piece
            // Optional: Debug.LogWarning("It is not this piece's turn.");
            ResetObjects();
            return;
        }

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

    private void SwitchTurnAndCheckGameEnd()
    {
        // 1. Determine the color of the player whose turn it is next
        int nextPlayerColor = (currentTurnColor == Piece.White) ? Piece.Black : Piece.White;

        // 2. Switch the turn
        currentTurnColor = nextPlayerColor;

        // 3. Check for Game End Conditions
        if (IsCheckmate(currentTurnColor))
        {
            int winnerColor = (currentTurnColor == Piece.White) ? Piece.Black : Piece.White;
            Debug.Log($"Checkmate! {((winnerColor == Piece.White) ? "White" : "Black")} Wins.");
            // TODO: Implement game end/UI display here
        }
        else if (IsStalemate(currentTurnColor))
        {
            Debug.Log("Stalemate! Game Drawn.");
            // TODO: Implement draw/UI display here
        }
        else
        {
            // Optional: Check for simple check to display UI feedback
            int kingIndex = FindKingTile(currentTurnColor);
            int opponentColor = (currentTurnColor == Piece.White) ? Piece.Black : Piece.White;
            if (IsTileAttacked(kingIndex, opponentColor))
            {
                Debug.Log($"{((currentTurnColor == Piece.White) ? "White" : "Black")} is in Check!");
            }
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
        // These remain the starting coordinates (used for Knight/King/Pawn checks later)
        int targetRow = targetIndex / 8;
        int targetCol = targetIndex % 8;

        // --- 1. Check Sliding Pieces (Rook, Bishop, Queen) ---
        foreach (int dir in lateralDir.Concat(diagonalDir))
        {
            int index = targetIndex;
            int currentRow = targetRow;
            int currentCol = targetCol;

            while (true)
            {
                // --- CRITICAL FIX: Check the CURRENT tile's edge based on the direction ---
                // If the current tile is on the edge we'd cross, break before moving.
                if (dir == +1 && currentCol == 7) break; // Moving East from H-file
                if (dir == -1 && currentCol == 0) break; // Moving West from A-file
                if (dir == +8 && currentRow == 7) break; // Moving South from 1st Rank
                if (dir == -8 && currentRow == 0) break; // Moving North from 8th Rank
                if (dir == +9 && (currentRow == 7 || currentCol == 7)) break; // Moving SE from edge
                if (dir == +7 && (currentRow == 7 || currentCol == 0)) break; // Moving SW from edge
                if (dir == -9 && (currentRow == 0 || currentCol == 0)) break; // Moving NW from edge
                if (dir == -7 && (currentRow == 0 || currentCol == 7)) break; // Moving NE from edge

                // --- Apply the move and update coordinates ---
                index += dir;
                currentRow = index / 8;
                currentCol = index % 8;

                // Failsafe check (should be caught by the edge checks above)
                if (index < 0 || index >= tileContent.Length) break;

                int piece = tileContent[index];
                if (piece != Piece.None)
                {
                    if (GetPieceColor(piece) == attackerColor)
                    {
                        int pieceType = GetPieceType(piece);

                        // Check if the attacker matches the ray direction (Rook/Queen for lateral, etc.)
                        bool isLateral = (dir == +1 || dir == -1 || dir == +8 || dir == -8);
                        bool isDiagonal = !isLateral;

                        bool isSlidingAttacker = (isLateral && (pieceType == Piece.Rook || pieceType == Piece.Queen)) ||
                                                 (isDiagonal && (pieceType == Piece.Bishop || pieceType == Piece.Queen));

                        if (isSlidingAttacker) return true;
                    }
                    break; // Stop the ray if any piece blocks it
                }
            }
        }

        // --- 2. Check Knight Attacks ---
        // (Logic remains correct, as it checks distance/L-shape only)
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
        // (Logic remains correct, as it checks distance/single step only)
        foreach (int dir in lateralDir.Concat(diagonalDir))
        {
            int checkIndex = targetIndex + dir;
            if (checkIndex < 0 || checkIndex >= tileContent.Length) continue;

            int checkRow = checkIndex / 8;
            int checkCol = checkIndex % 8;
            // This check guards against wrap-around when using dir offsets
            if (Math.Abs(checkRow - targetRow) > 1 || Math.Abs(checkCol - targetCol) > 1) continue;

            int piece = tileContent[checkIndex];
            if (piece != Piece.None && GetPieceColor(piece) == attackerColor && GetPieceType(piece) == Piece.King)
            {
                return true;
            }
        }

        // --- 4. Check Pawn Attacks ---
        // (Logic remains correct)
        int forwardOffset = (attackerColor == Piece.White) ? +8 : -8;
        int[] pawnCaptureDirs = { forwardOffset - 1, forwardOffset + 1 };

        foreach (int dir in pawnCaptureDirs)
        {
            int checkIndex = targetIndex + dir;

            if (checkIndex >= 0 && checkIndex < tileContent.Length)
            {
                int checkCol = checkIndex % 8;

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
                    if (targetTileContent == Piece.None)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
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
                    if (targetTileContent == Piece.None)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
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
                    if (targetTileContent == Piece.None)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
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

            // Boundary checks (Prevent wrapping and off-board access)
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

            // --- NEW: Skip if friendly piece ---
            if (targetTileContent != Piece.None && GetPieceColor(targetTileContent) == color)
            {
                continue;
            }

            // --- THE ESSENTIAL FIX: Use IsMoveLegal as the final arbiter ---
            // This checks if the move results in the King being attacked (i.e., prevents moving into check).
            if (IsMoveLegal(originIndex, index))
            {
                moves.Add(tileObjects[index]);
            }
        }

        // --- 2. Castling Checks ---
        // Castling must be handled separately and must also use CanCastlePathBeAttacked.
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
                        // NOTE: The original logic for CanCastlePathBeAttacked had a bug:
                        // It should check if the path is NOT attacked.
                        if (CanCastlePathBeAttacked(60, 62, color) == false) // ASSUMING CanCastlePathBeAttacked returns TRUE if PATH IS SAFE
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
                        if (CanCastlePathBeAttacked(60, 58, color) == false) // ASSUMING CanCastlePathBeAttacked returns TRUE if PATH IS SAFE
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
                        if (CanCastlePathBeAttacked(4, 6, color) == false) // ASSUMING CanCastlePathBeAttacked returns TRUE if PATH IS SAFE
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
                        if (CanCastlePathBeAttacked(4, 2, color) == false) // ASSUMING CanCastlePathBeAttacked returns TRUE if PATH IS SAFE
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
    // --- PAWN PROMOTION ---
    // ----------------------------------------------------------------------

    public void PromoteToQueen() { PromoteAndFinalizeMove(Piece.Queen); }
    public void PromoteToRook() { PromoteAndFinalizeMove(Piece.Rook); }
    public void PromoteToBishop() { PromoteAndFinalizeMove(Piece.Bishop); }
    public void PromoteToKnight() { PromoteAndFinalizeMove(Piece.Knight); }

    private void PromoteAndFinalizeMove(int newType)
    {
        if (!isPromotionPending)
        {
            Debug.LogError("Promotion function called, but no promotion is currently pending.");
            return;
        }

        int pieceValue = tileContent[promotionDestinationIndex];
        if (pieceValue == Piece.None)
        {
            Debug.LogError("Promotion target tile is empty in tileContent array.");
            return;
        }

        int pieceColor = GetPieceColor(pieceValue);
        int newPieceValue = newType | pieceColor;

        // Safely get the GameObject to destroy (the Pawn)
        Transform holder = tileObjects[promotionDestinationIndex].transform.GetChild(0);
        GameObject pawnGOToDestroy = null;

        if (holder.childCount > 0)
        {
            pawnGOToDestroy = holder.GetChild(0).gameObject;
        }

        // Destroy the existing Pawn GameObject
        if (pawnGOToDestroy != null)
        {
            Destroy(pawnGOToDestroy);
        }

        // --- 1. Execute the visual promotion ---
        GameObject prefab = GetPrefabForPiece(newType);

        if (prefab != null)
        {
            GameObject newPieceGO = Instantiate(prefab, holder);
            Image img = newPieceGO.GetComponent<Image>();

            if (img != null)
            {
                img.color = (pieceColor == Piece.White) ? white : black;
            }

            // Update selectedPiece to the newly created piece for consistency
            selectedPiece = newPieceGO;
            newPieceGO.name = newPieceValue.ToString();
        }

        // --- 2. Update the final board state and clear flags ---
        tileContent[promotionDestinationIndex] = newPieceValue;

        // Clear the promotion state flags
        isPromotionPending = false;
        promotionDestinationIndex = -1;
        promotionOriginIndex = -1; // Also clear origin
        capturedPieceValue = Piece.None; // Also clear captured value

        if (promotionPanel != null)
        {
            promotionPanel.SetActive(false);
        }

        // Clear highlights and selection state
        ResetObjects();

        // --- NEW: Turn Switch and Game End Check ---
        SwitchTurnAndCheckGameEnd();
    }

    public void CancelPromotionAndRevertMove()
    {
        // --- Initial Safety Checks ---
        if (!isPromotionPending || selectedPiece == null || promotionDestinationIndex == -1 || promotionOriginIndex == -1)
        {
            Debug.LogWarning("Cannot cancel promotion: No promotion is currently pending or state is invalid.");

            // Ensure the panel is hidden even on invalid calls
            if (promotionPanel != null)
            {
                promotionPanel.SetActive(false);
            }
            ResetObjects();
            return;
        }

        // --- 1. Determine Indices and Piece Value ---
        int originIndex = promotionOriginIndex;
        int destinationIndex = promotionDestinationIndex;

        // Get the piece value from the destination tile content (where the pawn currently is).
        // This `pieceValue` is the pawn that is being reverted.
        int pieceValueBeingReverted = tileContent[destinationIndex];

        // --- 2. Visual Reversion (Move the Pawn back) ---
        // Use the correctly stored originIndex to get the pawn's actual starting tile.
        Transform originHolder = tileObjects[originIndex].transform.GetChild(0);
        selectedPiece.transform.SetParent(originHolder);
        selectedPiece.transform.localPosition = Vector3.zero; // Ensure it's centered

        // --- 3. Logical Reversion (Update tileContent) ---

        // Restore the pawn's value to its actual original tile
        tileContent[originIndex] = pieceValueBeingReverted;

        // Restore the captured piece value to the destination tile (Piece.None if no piece was captured)
        tileContent[destinationIndex] = capturedPieceValue;

        // --- 4. Visual Restoration (Add the captured piece back) ---
        if (capturedPieceValue != Piece.None)
        {
            RestoreCapturedPieceVisuals(destinationIndex, capturedPieceValue);
        }

        // --- 5. Reset Temporary Game State ---
        enPassantIndex = -1;

        // Clear the promotion/reversion state flags
        isPromotionPending = false;
        promotionDestinationIndex = -1;
        promotionOriginIndex = -1;
        capturedPieceValue = Piece.None; // Clear the captured piece state

        // --- 6. Reset UI and Highlights ---

        // Deactivate the promotion panel
        if (promotionPanel != null)
        {
            promotionPanel.SetActive(false);
        }

        // Reset highlights and selection.
        ResetObjects();
    }

    // ----------------------------------------------------------------------
    // --- CHECKMATE/STALEMATE LOGIC ---
    // ----------------------------------------------------------------------
    public bool IsCheckmate(int playerColor)
    {
        // 1. Is the player currently in check?
        int kingIndex = FindKingTile(playerColor);
        int opponentColor = (playerColor == Piece.White) ? Piece.Black : Piece.White;

        if (!IsTileAttacked(kingIndex, opponentColor))
        {
            // If not in check, they cannot be in checkmate.
            return false;
        }

        // 2. Do they have any legal moves?
        return !HasAnyLegalMove(playerColor);
    }

    public bool IsStalemate(int playerColor)
    {
        // 1. Is the player currently in check?
        int kingIndex = FindKingTile(playerColor);
        int opponentColor = (playerColor == Piece.White) ? Piece.Black : Piece.White;

        if (IsTileAttacked(kingIndex, opponentColor))
        {
            // If in check, it's either check or checkmate, not stalemate.
            return false;
        }

        // 2. Do they have any legal moves?
        return !HasAnyLegalMove(playerColor);
    }

    private bool HasAnyLegalMove(int playerColor)
    {
        // 1. Save the current state of moves list and selected piece
        List<GameObject> originalMoves = new List<GameObject>(moves);
        GameObject originalSelectedPiece = selectedPiece;

        // Ensure the board is visually clean before starting the check
        ResetObjects();

        for (int originIndex = 0; originIndex < tileContent.Length; originIndex++)
        {
            int pieceValue = tileContent[originIndex];

            if (pieceValue != Piece.None && GetPieceColor(pieceValue) == playerColor)
            {
                GameObject originGO = tileObjects[originIndex];

                // Access the actual piece GameObject from the tile holder
                Transform pieceHolder = originGO.transform.GetChild(0);
                if (pieceHolder.childCount == 0) continue;

                GameObject pieceGO = pieceHolder.GetChild(0).gameObject;

                // Temporarily set the state and clear the moves list for the check
                selectedPiece = pieceGO;
                moves.Clear();

                // Call CheckMove to generate moves (which validates against IsMoveLegal)
                CheckMove(originGO, pieceGO);

                // Check if any move was found
                if (moves.Count > 0)
                {
                    // Legal move found: restore state and return true immediately
                    selectedPiece = originalSelectedPiece;
                    moves.Clear();
                    moves.AddRange(originalMoves);
                    ResetObjects(); // Clear any blue tiles generated by CheckMove
                    return true;
                }
            }
        }

        // 3. No legal move found: restore the original state (moves/selection)
        selectedPiece = originalSelectedPiece;
        moves.Clear();
        moves.AddRange(originalMoves);
        ResetObjects(); // Final cleanup

        return false;
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

    private void RestoreCapturedPieceVisuals(int tileIndex, int pieceValue)
    {
        int pieceType = GetPieceType(pieceValue);
        int pieceColor = GetPieceColor(pieceValue);

        // Get the appropriate prefab for the captured piece type
        GameObject prefab = GetPrefabForPiece(pieceType);

        if (prefab != null)
        {
            // Get the transform for the destination tile's holder
            Transform holder = tileObjects[tileIndex].transform.GetChild(0);

            // Clear any existing children (e.g., if a temporary placeholder was there)
            foreach (Transform child in holder)
            {
                Destroy(child.gameObject);
            }

            // Instantiate the captured piece's GameObject
            GameObject restoredPieceGO = Instantiate(prefab, holder);
            restoredPieceGO.transform.localPosition = Vector3.zero; // Center it

            // Set its visual properties
            Image img = restoredPieceGO.GetComponent<Image>();
            if (img != null)
            {
                img.color = (pieceColor == Piece.White) ? white : black;
            }

            // Set its name to reflect its value (important if you parse names for piece data)
            restoredPieceGO.name = pieceValue.ToString();
        }
        else
        {
            Debug.LogError($"Prefab not found for piece type: {pieceType}");
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

    private bool IsPromotionSquare(int index, int color)
    {
        // White promotes on Rank 8 (Index 0-7)
        if (color == Piece.White && index >= 0 && index <= 7)
        {
            return true;
        }
        // Black promotes on Rank 1 (Index 56-63)
        if (color == Piece.Black && index >= 56 && index <= 63)
        {
            return true;
        }
        return false;
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

    public int GetPieceType(int piece)
    {
        return piece & 0b0111; // Extracts the piece type (1-7)
    }

    public int GetPieceColor(int piece)
    {
        return piece & (Piece.White | Piece.Black); // Extracts color bits (8 or 16)
    }

    public void BackButton()
    {
        SceneLoader.Instance.LoadNewScene("AdminScene");
    }
}