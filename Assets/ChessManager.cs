using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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

    [Header("Toggle Settings")]
    [SerializeField] private bool canRewriteHistory = false;
    [SerializeField] private bool enableSideSelection = true;

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
    [SerializeField] public GameState CurrentState = GameState.NotStarted;
    [SerializeField] public GameObject focus;
    [SerializeField] public GameObject selectedPiece;
    [SerializeField] private int enPassantIndex = -1;
    [SerializeField] public bool isPromotionPending = false;
    [SerializeField] private int capturedPieceValue = Piece.None;
    [SerializeField] private int promotionOriginIndex = -1;
    [SerializeField] private int promotionDestinationIndex = -1;
    [SerializeField] public int currentTurnColor = Piece.White;
    [SerializeField] private int halfMoveClock = 0;
    public int PlayerSide { get; private set; } = -1;

    [Header("Board Highlights")]
    [SerializeField] private Color lastMoveColor = Color.yellow;
    private Color[] defaultTileColors;
    [SerializeField] private List<int> moveDestinationsHistory = new List<int>();
    [SerializeField] private List<int> moveOriginsHistory = new List<int>();
    private int currentLastMoveIndex = -1;
    private int currentLastMoveOriginIndex = -1;

    [Header("PGN & FEN History")]
    private List<string> positionHistory = new List<string>();
    private int currentHistoryIndex = -1;
    [SerializeField] private bool isReviewing = false;

    [Header("Piece Pooling & State")]
    [SerializeField] private List<GameObject> piecePool = new List<GameObject>();
    [SerializeField] private List<int> piecesCapturedInCurrentState = new List<int>();

    [Header("UI References")]
    [SerializeField] private GameObject promotionPanel;
    [SerializeField] private GameObject sideSelectionPanel;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI winConText;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject whitePanel;
    [SerializeField] public TextMeshProUGUI whiteName;
    [SerializeField] private GameObject blackPanel;
    [SerializeField] public TextMeshProUGUI blackName;
    [SerializeField] public GameObject gameBoard;
    [SerializeField] private Transform pgnContent;
    [SerializeField] private GameObject pgnMoveButtonPrefab;
    [SerializeField] public ScrollRect pgnScrollRect;
    [SerializeField] private Scrollbar pgnScrollBar;
    [SerializeField] public Transform pgnButtonContainer;
    [SerializeField] private GameObject selectedPGNButton;

    [Header("Captured Pieces UI (Trays)")]
    [SerializeField] private Transform whitePawnDeadContainer;
    [SerializeField] private Transform whiteRookDeadContainer;
    [SerializeField] private Transform whiteKnightDeadContainer;
    [SerializeField] private Transform whiteBishopDeadContainer;
    [SerializeField] private Transform whiteQueenDeadContainer;
    [SerializeField] private Transform whiteKingDeadContainer;

    [SerializeField] private Transform blackPawnDeadContainer;
    [SerializeField] private Transform blackRookDeadContainer;
    [SerializeField] private Transform blackKnightDeadContainer;
    [SerializeField] private Transform blackBishopDeadContainer;
    [SerializeField] private Transform blackQueenDeadContainer;
    [SerializeField] private Transform blackKingDeadContainer;

    [SerializeField] private int fullMoveNumber = 1;
    [SerializeField] private List<string> pgnHistoryList = new List<string>();
    [SerializeField] public List<string> PgnHistoryList => pgnHistoryList;

    [SerializeField] private bool whiteCanKSC = true;
    [SerializeField] private bool whiteCanQSC = true;
    [SerializeField] private bool blackCanKSC = true;
    [SerializeField] private bool blackCanQSC = true;

    // tileContent stores the Piece integer value (Type | Color)
    [SerializeField] public int[] tileContent;
    [SerializeField] public GameObject[] tileObjects;
    [SerializeField] public List<GameObject> moves;

    [SerializeField] private Transform mainView;
    [SerializeField] public Transform MainView => mainView;

    // Direction Vectors (based on array index 0-63)
    private int[] lateralDir = { +1, -1, +8, -8 };
    private int[] diagonalDir = { +7, -7, +9, -9 };
    private int[] knightDir = { +6, -6, +10, -10, +15, -15, +17, -17 };

    private void Start()
    {
        // Cache the default colors of the board tiles
        if (tileObjects != null)
        {
            defaultTileColors = new Color[tileObjects.Length];
            for (int i = 0; i < tileObjects.Length; i++)
            {
                Image img = tileObjects[i].GetComponent<Image>();
                if (img != null) defaultTileColors[i] = img.color;
            }
        }

        StartBoard();
    }

    private void StartBoard()
    {
        string defaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq 0 1";
        StartGameFromFEN(defaultFen);
    }

    public void StartGameFromFEN(string fen)
    {
        CurrentState = GameState.NotStarted;

        // 1. Reset History Lists & Flags
        positionHistory.Clear();
        pgnHistoryList.Clear();

        // Reset move history (Destination & Origin)
        moveDestinationsHistory.Clear();
        moveOriginsHistory.Clear();

        currentLastMoveIndex = -1;
        currentLastMoveOriginIndex = -1;

        currentHistoryIndex = -1;
        isReviewing = false;

        // 2. Clear PGN UI (Buttons)
        if (pgnContent != null)
        {
            foreach (Transform child in pgnContent)
            {
                Destroy(child.gameObject);
            }
        }

        // 3. Clear Board Visuals
        ClearAllPieceVisuals();

        // 4. Initialize Logic
        tileContent = new int[tileObjects.Length];

        // 5. Load the FEN (Calls RecordPosition)
        ReadFENPos(fen);

        // 6. Draw the pieces
        RedrawPiecesFromTileContent();

        // NEW: Ensure board colors are reset
        HighlightLastMove();

        if (enableSideSelection && sideSelectionPanel != null)
        {
            sideSelectionPanel.SetActive(true);
        }
        else
        {
            PlayerSide = -1;
            whiteName.text = "Player 1";
            blackName.text = "Player 2";

            FlipBoard(0);
        }
    }

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
        string activeColor = fenParts[1];
        string castlingRights = fenParts[2];
        string enPassantSquare = fenParts[3];
        string halfMoveClockStr = fenParts.Length > 4 ? fenParts[4] : "0";
        string fullMoveNumStr = fenParts.Length > 5 ? fenParts[5] : "1";

        // Clear board logic first
        Array.Clear(tileContent, 0, tileContent.Length);

        int tiles = 0;

        // --- 1. Piece Placement (LOGIC ONLY) ---
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

                    // Only update the integer array, DO NOT Instantiate here
                    tileContent[tiles] = pieceType | pieceColor;

                    tiles++;
                }
            }
        }

        // --- 2. Active Color ---
        currentTurnColor = (activeColor.ToLower() == "w") ? Piece.White : Piece.Black;

        // --- 3. Castling Rights ---
        whiteCanKSC = castlingRights.Contains('K');
        whiteCanQSC = castlingRights.Contains('Q');
        blackCanKSC = castlingRights.Contains('k');
        blackCanQSC = castlingRights.Contains('q');

        // --- 4. En Passant Target Square ---
        enPassantIndex = -1;
        if (enPassantSquare.Length == 2 && enPassantSquare != "-")
        {
            int file = enPassantSquare[0] - 'a';
            int rank = 8 - (enPassantSquare[1] - '0');
            enPassantIndex = rank * 8 + file;
        }

        // --- 5. Halfmove Clock ---
        if (int.TryParse(halfMoveClockStr, out int hmc))
        {
            halfMoveClock = hmc;
        }

        // --- 6. Fullmove Number (NEW) ---
        if (int.TryParse(fullMoveNumStr, out int fmn))
        {
            fullMoveNumber = fmn;
        }

        // 7. Record Initial Position (Only if not reviewing)
        if (!isReviewing) RecordPosition();
    }

    public void MovePiece(GameObject origin, GameObject destination)
    {
        if (selectedPiece == null) return;

        if (CurrentState == GameState.NotStarted)
            CurrentState = GameState.InProgress;

        isReviewing = false;

        // --- History & Logic (Same as before) ---
        if (currentHistoryIndex < positionHistory.Count - 1)
        {
            ClearFutureHistory();
        }

        int originIndex = Array.IndexOf(tileObjects, origin);
        int destinationIndex = Array.IndexOf(tileObjects, destination);
        int originalPieceAtDest = tileContent[destinationIndex];
        int pieceValue = int.Parse(selectedPiece.name);
        int pieceType = GetPieceType(pieceValue);
        int pieceColor = GetPieceColor(pieceValue);

        // --- PGN Pre-calculation ---
        bool isCastling = (pieceType == Piece.King && Math.Abs(originIndex - destinationIndex) == 2);
        bool isEnPassant = (destinationIndex == enPassantIndex && pieceType == Piece.Pawn);
        bool isCapture = (originalPieceAtDest != Piece.None) || isEnPassant;

        // --- Special Move Execution (Castling/EnPassant) ---
        if (isCastling)
        {
            int rookOrigin = (destinationIndex > originIndex) ? (originIndex + 3) : (originIndex - 4);
            int rookDest = (destinationIndex > originIndex) ? (destinationIndex - 1) : (destinationIndex + 1);

            // Move Rook Visuals
            Transform rookHolder = tileObjects[rookOrigin].transform.GetChild(0);
            if (rookHolder.childCount > 0)
            {
                GameObject rook = rookHolder.GetChild(0).gameObject;
                rook.transform.SetParent(tileObjects[rookDest].transform.GetChild(0));
                rook.transform.localPosition = Vector3.zero;
            }
            // Update Logic
            tileContent[rookDest] = (Piece.Rook | pieceColor);
            tileContent[rookOrigin] = Piece.None;
        }

        if (isEnPassant)
        {
            int capturedIndex = enPassantIndex + ((pieceColor == Piece.White) ? 8 : -8);
            Transform capTile = tileObjects[capturedIndex].transform.GetChild(0);
            if (capTile.childCount > 0)
            {
                HandleCapturedVisuals(capTile.GetChild(0).gameObject, (Piece.Pawn | ((pieceColor == Piece.White) ? Piece.Black : Piece.White)));
            }
            tileContent[capturedIndex] = Piece.None;
        }

        enPassantIndex = -1;

        // --- Castling Rights & En Passant Setup (Same as before) ---
        if (pieceType == Piece.Pawn && Math.Abs((originIndex / 8) - (destinationIndex / 8)) == 2)
        {
            enPassantIndex = (originIndex + destinationIndex) / 2;
        }

        // --- Visual Move ---
        Transform destHolder = destination.transform.GetChild(0);
        if (destHolder.childCount > 0)
        {
            HandleCapturedVisuals(destHolder.GetChild(0).gameObject, originalPieceAtDest);
        }
        selectedPiece.transform.SetParent(destHolder);
        selectedPiece.transform.localPosition = Vector3.zero;

        // --- Promotion Check ---
        if (pieceType == Piece.Pawn && IsPromotionSquare(destinationIndex, pieceColor))
        {
            isPromotionPending = true;
            capturedPieceValue = originalPieceAtDest;
            promotionOriginIndex = originIndex;
            promotionDestinationIndex = destinationIndex;
            tileContent[originIndex] = Piece.None;
            tileContent[destinationIndex] = pieceValue;
            if (promotionPanel != null) promotionPanel.SetActive(true);
            else PromoteToQueen();
            return;
        }

        // --- Finalize State ---
        tileContent[originIndex] = Piece.None;
        tileContent[destinationIndex] = pieceValue;

        if (pieceType == Piece.Pawn || isCapture) halfMoveClock = 0;
        else halfMoveClock++;

        UpdatePGN(originIndex, destinationIndex, pieceValue, isCapture, isCastling);

        if (selectedPGNButton != null)
            selectedPGNButton.GetComponent<Image>().enabled = false;

        if (pgnButtonContainer.childCount > 0)
        {
            selectedPGNButton = pgnButtonContainer.GetChild(pgnButtonContainer.childCount - 1).gameObject;
            selectedPGNButton.GetComponent<Image>().enabled = true;
        }

        currentTurnColor = (currentTurnColor == Piece.White) ? Piece.Black : Piece.White;

        // Update the highlight index
        currentLastMoveIndex = destinationIndex;
        currentLastMoveOriginIndex = originIndex;

        RecordPosition();

        if (!isReviewing)
        {
            ClearAllPieceVisuals();
            RedrawPiecesFromTileContent();
        }

        // 1. ResetObjects: Clears blue moves, Applies Yellow Highlight
        ResetObjects();

        // 2. CheckGameEnd: Checks for mate and Applies Red King Highlight (on top of yellow if needed)
        CheckGameEnd();
    }

    public void CheckMove(GameObject originGO, GameObject pieceGO)
    {
        // Check if we are currently looking at a past state
        bool isLookingAtPast = currentHistoryIndex < positionHistory.Count - 1;

        if (isLookingAtPast && !canRewriteHistory)
        {
            // If in "View Only" mode, clear selection and do nothing
            ResetObjects();
            return;
        }

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

    private void CheckGameEnd()
    {
        // --- 1. VISUALS: Highlight King if in Check ---
        // We do this BEFORE checking for checkmate so the King turns red in both cases.
        int kingIndex = FindKingTile(currentTurnColor);
        int opponentColor = (currentTurnColor == Piece.White) ? Piece.Black : Piece.White;
        bool inCheck = IsTileAttacked(kingIndex, opponentColor);

        if (inCheck)
        {
            GameObject kingTile = tileObjects[kingIndex];
            Image tileImg = kingTile.GetComponent<Image>();
            if (tileImg != null)
            {
                tileImg.color = Color.red;
            }
        }

        // --- 2. LOGIC: Check Game Over States ---
        if (inCheck && IsCheckmate(currentTurnColor))
        {
            int winnerColor = (currentTurnColor == Piece.White) ? Piece.Black : Piece.White;

            int playerPieceColor = (PlayerSide == 0) ? Piece.White : Piece.Black;

            CurrentState = (winnerColor == Piece.White) ? GameState.WhiteWin : GameState.BlackWin;

            string winnerMsg = winnerColor == playerPieceColor ? "You Win!" : "Opponent Wins!";

            StartCoroutine(ShowGameOverUI(winnerMsg, "by Checkmate."));
        }
        else if (!inCheck && IsStalemate(currentTurnColor))
        {
            CurrentState = GameState.Draw;
            StartCoroutine(ShowGameOverUI("It's a draw!", "by Stalemate."));
        }
        else if (IsInsufficientMaterial())
        {
            CurrentState = GameState.Draw;
            StartCoroutine(ShowGameOverUI("It's a draw!", "by Insufficient Material."));
        }
        else if (IsThreefoldRepetition())
        {
            CurrentState = GameState.Draw;
            StartCoroutine(ShowGameOverUI("It's a draw!", "by Threefold Repetition."));
        }
        else if (halfMoveClock >= 100)
        {
            CurrentState = GameState.Draw;
            StartCoroutine(ShowGameOverUI("It's a draw!", "by 50-Move Rule."));
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
                return true; // Path IS attacked
            }
        }
        return false; // Path is NOT attacked
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

    public void GetRookMoves(GameObject originGO, GameObject pieceGO)
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

    public void GetBishopMoves(GameObject originGO, GameObject pieceGO)
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

    public void GetKnightMoves(GameObject originGO, GameObject pieceGO)
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

    public void GetQueenMoves(GameObject originGO, GameObject pieceGO)
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

    public void GetKingMoves(GameObject originGO, GameObject pieceGO)
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
                        // ADD MOVE ONLY IF PATH IS NOT ATTACKED (returns FALSE)
                        if (!CanCastlePathBeAttacked(60, 62, color)) // <<< FIX 2: Correctly checking for safe path
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
                        // ADD MOVE ONLY IF PATH IS NOT ATTACKED (returns FALSE)
                        if (!CanCastlePathBeAttacked(60, 58, color)) // <<< FIX 2: Correctly checking for safe path
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
                        // ADD MOVE ONLY IF PATH IS NOT ATTACKED (returns FALSE)
                        if (!CanCastlePathBeAttacked(4, 6, color)) // <<< FIX 2: Correctly checking for safe path
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
                        // ADD MOVE ONLY IF PATH IS NOT ATTACKED (returns FALSE)
                        if (!CanCastlePathBeAttacked(4, 2, color)) // <<< FIX 2: Correctly checking for safe path
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

    public void GetPawnMoves(GameObject originGO, GameObject pieceGO)
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

    public void PromoteToQueen()
    {
        PromoteAndFinalizeMove(Piece.Queen);
    }

    public void PromoteToRook()
    {
        PromoteAndFinalizeMove(Piece.Rook);
    }

    public void PromoteToBishop()
    {
        PromoteAndFinalizeMove(Piece.Bishop);
    }

    public void PromoteToKnight()
    {
        PromoteAndFinalizeMove(Piece.Knight);
    }

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

        // Check if the move that caused promotion was a capture
        bool isCapture = (capturedPieceValue != Piece.None);

        // --- 1. Pool the existing Pawn ---
        // We find the pawn object on the tile and Pool it.
        // This prevents it from being treated as "Dead" or appearing in the captured UI.
        Transform holder = tileObjects[promotionDestinationIndex].transform.GetChild(0);
        if (holder.childCount > 0)
        {
            GameObject pawnGO = holder.GetChild(0).gameObject;
            PoolPiece(pawnGO);
        }

        // --- 2. Update the final board state ---
        tileContent[promotionDestinationIndex] = newPieceValue;

        // --- 3. PGN UPDATE ---
        UpdatePGN(promotionOriginIndex, promotionDestinationIndex, newPieceValue, isCapture, false, true, newType);

        // --- 4. Update Highlights & Turn ---
        currentLastMoveIndex = promotionDestinationIndex;
        currentLastMoveOriginIndex = promotionOriginIndex;

        currentTurnColor = (currentTurnColor == Piece.White) ? Piece.Black : Piece.White;
        halfMoveClock = 0;

        // --- 5. Record History & Cleanup ---
        RecordPosition();

        isPromotionPending = false;
        promotionDestinationIndex = -1;
        promotionOriginIndex = -1;
        capturedPieceValue = Piece.None;

        if (promotionPanel != null) promotionPanel.SetActive(false);

        // --- 6. Visual Refresh ---
        if (selectedPGNButton != null)
            selectedPGNButton.GetComponent<Image>().enabled = false;

        if (pgnButtonContainer.childCount > 0)
        {
            selectedPGNButton = pgnButtonContainer.GetChild(pgnButtonContainer.childCount - 1).gameObject;
            selectedPGNButton.GetComponent<Image>().enabled = true;
        }

        ClearAllPieceVisuals(); // This cleans the board
        RedrawPiecesFromTileContent(); // This puts the new Queen/Rook on the board
        ResetObjects();
        CheckGameEnd();
    }

    public void CancelPromotionAndRevertMove()
    {
        // --- Initial Safety Checks ---
        if (!isPromotionPending || selectedPiece == null || promotionDestinationIndex == -1 || promotionOriginIndex == -1)
        {
            if (promotionPanel != null) promotionPanel.SetActive(false);
            ResetObjects();
            return;
        }

        // --- 1. Determine Indices and Piece Value ---
        int originIndex = promotionOriginIndex;
        int destinationIndex = promotionDestinationIndex;
        int pieceValueBeingReverted = tileContent[destinationIndex];

        // --- 2. Visual Reversion (Move the Pawn back) ---
        Transform originHolder = tileObjects[originIndex].transform.GetChild(0);
        selectedPiece.transform.SetParent(originHolder);
        selectedPiece.transform.localPosition = Vector3.zero;

        // --- 3. Logical Reversion (Update tileContent) ---
        tileContent[originIndex] = pieceValueBeingReverted;
        tileContent[destinationIndex] = capturedPieceValue;

        // --- 4. Visual Restoration & GRAVEYARD FIX ---
        if (capturedPieceValue != Piece.None)
        {
            // A. Put the piece back on the board
            RestoreCapturedPieceVisuals(promotionDestinationIndex, capturedPieceValue);

            // B. REMOVE the "Ghost" piece from the UI Graveyard
            int capturedColor = GetPieceColor(capturedPieceValue);
            int capturedType = GetPieceType(capturedPieceValue);

            // Use the helper to find the specific tray
            Transform graveyard = GetDeadPieceContainer(capturedType, capturedColor);

            // We assume the last piece added to THIS specific tray is the one to remove
            if (graveyard != null && graveyard.childCount > 0)
            {
                Destroy(graveyard.GetChild(graveyard.childCount - 1).gameObject);
            }
        }

        // --- 5. Reset Temporary Game State ---
        enPassantIndex = -1;
        isPromotionPending = false;
        promotionDestinationIndex = -1;
        promotionOriginIndex = -1;
        capturedPieceValue = Piece.None;

        // --- 6. Reset UI and Highlights ---
        if (promotionPanel != null) promotionPanel.SetActive(false);
        ResetObjects();
        //LoadBoardFromHistory(pgnButtonContainer.childCount);
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

    private bool IsInsufficientMaterial()
    {
        // Lists to hold all non-King pieces for both sides
        List<int> whiteMinorPieces = new List<int>();
        List<int> blackMinorPieces = new List<int>();

        // 1. Scan the entire board and categorize all pieces
        foreach (int pieceValue in tileContent)
        {
            if (pieceValue == Piece.None) continue;

            int type = GetPieceType(pieceValue);
            int color = GetPieceColor(pieceValue);

            // If there's a Pawn, Rook, or Queen, it's NOT insufficient material.
            if (type == Piece.Pawn || type == Piece.Rook || type == Piece.Queen)
            {
                return false;
            }

            // Collect the minor pieces (Knight/Bishop)
            if (type == Piece.Knight || type == Piece.Bishop)
            {
                if (color == Piece.White)
                {
                    whiteMinorPieces.Add(type);
                }
                else
                {
                    blackMinorPieces.Add(type);
                }
            }
        }

        // 2. Analyze the remaining pieces (Kings, Knights, Bishops only)

        int whiteCount = whiteMinorPieces.Count;
        int blackCount = blackMinorPieces.Count;

        // --- CASE 1: King vs King (K vs K) ---
        // Zero minor pieces on both sides (only Kings remain)
        if (whiteCount == 0 && blackCount == 0)
        {
            return true;
        }

        // --- CASE 2: King vs King + Knight (K vs KN or KN vs K) ---
        // One Knight on one side, zero minor pieces on the other.
        if ((whiteCount == 1 && whiteMinorPieces[0] == Piece.Knight && blackCount == 0) ||
            (blackCount == 1 && blackMinorPieces[0] == Piece.Knight && whiteCount == 0))
        {
            return true;
        }

        // --- CASE 3: King vs King + Bishop (K vs KB or KB vs K) ---
        // One Bishop on one side, zero minor pieces on the other.
        if ((whiteCount == 1 && whiteMinorPieces[0] == Piece.Bishop && blackCount == 0) ||
            (blackCount == 1 && blackMinorPieces[0] == Piece.Bishop && whiteCount == 0))
        {
            return true;
        }

        // --- CASE 4: King + Bishop vs King + Bishop (KB vs KB) ---
        // Two Bishops (one per side) on squares of the SAME color (resulting in an unbreakable fortress)
        if (whiteCount == 1 && whiteMinorPieces[0] == Piece.Bishop &&
            blackCount == 1 && blackMinorPieces[0] == Piece.Bishop)
        {
            // To check for same-color bishops, we need to find their index and check the tile color.

            int whiteBishopIndex = -1;
            int blackBishopIndex = -1;

            for (int i = 0; i < tileContent.Length; i++)
            {
                if (GetPieceType(tileContent[i]) == Piece.Bishop)
                {
                    if (GetPieceColor(tileContent[i]) == Piece.White)
                    {
                        whiteBishopIndex = i;
                    }
                    else
                    {
                        blackBishopIndex = i;
                    }
                }
            }

            // Check if both Bishops are on the same colored tile (e.g., both on light squares or both on dark squares)
            // (index % 2) is a simple way to get tile color parity if index 0 is dark (0) and index 1 is light (1).
            // For a standard board where A1 (index 56) is dark, (index + row) % 2 gives color.
            // Let's use the row + col method for robustness:
            // (index/8) = row, (index%8) = col. If (row + col) % 2 is same for both, they are same color.

            int whiteBishopRowColSum = (whiteBishopIndex / 8) + (whiteBishopIndex % 8);
            int blackBishopRowColSum = (blackBishopIndex / 8) + (blackBishopIndex % 8);

            // If the parity is the same (both even or both odd), they are on the same color squares.
            if ((whiteBishopRowColSum % 2) == (blackBishopRowColSum % 2))
            {
                return true;
            }
        }

        // All other combinations are considered checkmate-possible (e.g., KN vs KN, KN vs KB, 2B vs K, etc.)
        return false;
    }

    private bool IsThreefoldRepetition()
    {
        // A position must occur 3 times total for the draw to be valid.
        // We need at least 3 positions in history to find 3 matches.
        if (positionHistory.Count < 3)
        {
            return false;
        }

        // Get the FEN of the CURRENT position (the one we just arrived at)
        string fenCurrent = positionHistory[positionHistory.Count - 1];

        // Ensure the FEN is not null (though the Count < 3 check should handle most early errors)
        if (fenCurrent == null)
        {
            return false;
        }

        string[] fenPartsCurrent = fenCurrent.Split(' ');

        // Safety check to ensure the FEN has enough parts
        if (fenPartsCurrent.Length < 4)
        {
            // This indicates a bad FEN generation, but prevents an array crash
            Debug.LogError("FEN string is too short for repetition check.");
            return false;
        }

        // Create the unique key for the position (excluding half-move clock and full-move number)
        string positionKeyCurrent = $"{fenPartsCurrent[0]} {fenPartsCurrent[1]} {fenPartsCurrent[2]} {fenPartsCurrent[3]}";

        int repetitionCount = 0;

        // Iterate through the entire history to count how many times this exact position has occurred.
        // We must check all positions, including the current one.
        for (int i = 0; i < positionHistory.Count; i++)
        {
            string fenHistory = positionHistory[i];

            if (fenHistory == null) continue;

            string[] fenPartsHistory = fenHistory.Split(' ');

            // Re-check length for safety
            if (fenPartsHistory.Length < 4) continue;

            string positionKeyHistory = $"{fenPartsHistory[0]} {fenPartsHistory[1]} {fenPartsHistory[2]} {fenPartsHistory[3]}";

            // *** SIMPLE COMPARISON ***
            if (positionKeyCurrent == positionKeyHistory)
            {
                repetitionCount++;
            }

            // If the position has occurred 3 or more times, it's a draw.
            if (repetitionCount >= 3)
            {
                return true;
            }
        }

        return false;
    }

    // ----------------------------------------------------------------------
    // --- PGN GENERATION ---
    // ----------------------------------------------------------------------

    private void UpdatePGN(int originIndex, int destIndex, int pieceValue, bool isCapture, bool isCastling, bool isPromotion = false, int promotionType = Piece.None)
    {
        string moveString = "";
        int pieceType = GetPieceType(pieceValue);
        int pieceColor = GetPieceColor(pieceValue);

        if (isPromotion)
        {
            pieceType = Piece.Pawn;
        }

        // --- 1. Construct the Move String ---
        if (isCastling)
        {
            int destFile = destIndex % 8;
            moveString = (destFile > 4) ? "O-O" : "O-O-O";
        }
        else
        {
            // Piece Letter
            if (pieceType != Piece.Pawn)
            {
                moveString += GetPieceNotation(pieceValue);
            }

            // Capture Notation
            if (isCapture)
            {
                if (pieceType == Piece.Pawn)
                {
                    char originFile = (char)('a' + (originIndex % 8));
                    moveString += originFile;
                }
                moveString += "x";
            }

            // Destination Square
            moveString += GetSquareNotation(destIndex);

            // Promotion
            if (isPromotion)
            {
                moveString += "=" + GetPieceNotation(promotionType | pieceColor);
            }
        }

        // Check / Checkmate Suffix
        int opponentColor = (pieceColor == Piece.White) ? Piece.Black : Piece.White;
        if (IsCheckmate(opponentColor))
        {
            moveString += "#";
        }
        else
        {
            int kingIndex = FindKingTile(opponentColor);
            if (IsTileAttacked(kingIndex, pieceColor))
            {
                moveString += "+";
            }
        }

        // --- 2. Format & Store (White adds the number, Black just adds the move) ---
        string finalButtonText = "";

        if (pieceColor == Piece.White)
        {
            finalButtonText = $"{fullMoveNumber}. {moveString}";
        }
        else
        {
            finalButtonText = moveString;
            fullMoveNumber++; // Increment after Black's move
        }

        // Add to logical list
        pgnHistoryList.Add(finalButtonText);

        // --- 3. Instantiate Button UI ---
        if (pgnContent != null && pgnMoveButtonPrefab != null)
        {
            GameObject newButton = Instantiate(pgnMoveButtonPrefab, pgnContent);

            // Get the index of the move AFTER it's added to the list
            int moveIndex = pgnHistoryList.Count;

            // Add listener to the button
            Button btn = newButton.GetComponent<Button>();
            if (btn != null)
            {
                // Assign the function to load this specific board state
                btn.onClick.AddListener(() => LoadBoardFromHistory(moveIndex));
            }

            // Try to set text on the button
            TextMeshProUGUI btnText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = finalButtonText;
            }

            newButton.name = $"Move_{moveIndex}_{moveString}";
        }

        // --- 4. Auto-Scroll ---
        if (pgnScrollRect != null && pgnContent != null)
        {
            // Force the layout to update immediately on the Content RectTransform.
            LayoutRebuilder.ForceRebuildLayoutImmediate(pgnContent.GetComponent<RectTransform>());

            // Set to 1f to scroll to the end (right) for horizontal lists.
            pgnScrollRect.horizontalNormalizedPosition = 1f;
        }

        // Always ensure the current move is highlighted
        HighlightCurrentMoveButton(pgnHistoryList.Count - 1);
    }

    private string GetSquareNotation(int index)
    {
        int file = index % 8;
        int rank = index / 8;

        // Convert to chess notation (Rank 0=8, Rank 7=1)
        int rankNumber = 8 - rank;
        char fileChar = (char)('a' + file);

        return $"{fileChar}{rankNumber}";
    }

    private string GetPieceNotation(int pieceValue)
    {
        int type = GetPieceType(pieceValue);
        switch (type)
        {
            case Piece.King: return "K";
            case Piece.Queen: return "Q";
            case Piece.Rook: return "R";
            case Piece.Bishop: return "B";
            case Piece.Knight: return "N";
            default: return ""; // Pawns have no letter
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

        HighlightLastMove();
        HighlightKingCheck();
    }

    public void LoadBoardFromHistory(int index)
    {
        if (selectedPGNButton != null)
            selectedPGNButton.GetComponent<Image>().enabled = false;

        if (index != 0)
        {
            selectedPGNButton = pgnButtonContainer.GetChild(index - 1).gameObject;
            selectedPGNButton.GetComponent<Image>().enabled = true;
        }

        if (index < 0 || index >= positionHistory.Count) return;

        currentHistoryIndex = index;
        isReviewing = true;

        // Restore Destination Index
        if (index < moveDestinationsHistory.Count)
            currentLastMoveIndex = moveDestinationsHistory[index];
        else
            currentLastMoveIndex = -1;

        // NEW: Restore Origin Index
        if (index < moveOriginsHistory.Count)
            currentLastMoveOriginIndex = moveOriginsHistory[index];
        else
            currentLastMoveOriginIndex = -1;

        string fen = positionHistory[index];
        ReadFENPos(fen);

        ClearAllPieceVisuals();
        RedrawPiecesFromTileContent();

        HighlightLastMove();
        HighlightKingCheck();
        HighlightCurrentMoveButton(index);
    }

    private void RecordPosition()
    {
        string fen = GenerateFEN();

        // 1. Handle History Rewrite (Branching)
        // If we are not at the end of history, we are rewriting it.
        if (currentHistoryIndex < positionHistory.Count - 1)
        {
            int indexToRemoveFrom = currentHistoryIndex + 1;
            int movesToRemove = positionHistory.Count - indexToRemoveFrom;

            if (movesToRemove > 0)
            {
                // A. Remove from Board History
                positionHistory.RemoveRange(indexToRemoveFrom, movesToRemove);

                // B. Remove from PGN History
                if (pgnHistoryList.Count > currentHistoryIndex)
                {
                    int pgnRemoveCount = pgnHistoryList.Count - currentHistoryIndex;
                    pgnHistoryList.RemoveRange(currentHistoryIndex, pgnRemoveCount);
                }

                // C. Remove from HIGHLIGHT History (Crucial Fix)
                // We must remove the "old future" moves so they don't appear in the new timeline
                if (indexToRemoveFrom < moveDestinationsHistory.Count)
                {
                    moveDestinationsHistory.RemoveRange(indexToRemoveFrom, moveDestinationsHistory.Count - indexToRemoveFrom);
                }

                if (indexToRemoveFrom < moveOriginsHistory.Count)
                {
                    moveOriginsHistory.RemoveRange(indexToRemoveFrom, moveOriginsHistory.Count - indexToRemoveFrom);
                }

                // D. Clean UI Buttons
                if (pgnContent != null)
                {
                    for (int i = pgnContent.childCount - 1; i >= currentHistoryIndex; i--)
                    {
                        Destroy(pgnContent.GetChild(i).gameObject);
                    }
                }

                // Reset move number if needed
                fullMoveNumber = (currentHistoryIndex / 2) + 1;
            }

            // We are no longer reviewing past moves, we are making new ones
            isReviewing = false;
        }

        // 2. Add New State
        positionHistory.Add(fen);

        // Add the current move to the history lists
        moveDestinationsHistory.Add(currentLastMoveIndex);
        moveOriginsHistory.Add(currentLastMoveOriginIndex);

        currentHistoryIndex = positionHistory.Count - 1;
    }

    private string GenerateFEN()
    {
        // 1. Piece Placement
        string piecePlacement = "";
        for (int rank = 0; rank < 8; rank++)
        {
            int emptyCount = 0;
            for (int file = 0; file < 8; file++)
            {
                int index = rank * 8 + file;
                int pieceValue = tileContent[index];

                if (pieceValue == Piece.None)
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        piecePlacement += emptyCount.ToString();
                        emptyCount = 0;
                    }

                    int type = GetPieceType(pieceValue);
                    int color = GetPieceColor(pieceValue);
                    char symbol = ' ';

                    switch (type)
                    {
                        case Piece.King: symbol = 'k'; break;
                        case Piece.Queen: symbol = 'q'; break;
                        case Piece.Rook: symbol = 'r'; break;
                        case Piece.Bishop: symbol = 'b'; break;
                        case Piece.Knight: symbol = 'n'; break;
                        case Piece.Pawn: symbol = 'p'; break;
                    }

                    if (color == Piece.White)
                    {
                        symbol = char.ToUpper(symbol);
                    }
                    piecePlacement += symbol;
                }
            }

            if (emptyCount > 0)
            {
                piecePlacement += emptyCount.ToString();
            }

            if (rank < 7)
            {
                piecePlacement += "/";
            }
        }

        // 2. Active Color
        string activeColor = (currentTurnColor == Piece.White) ? "w" : "b";

        // 3. Castling Rights
        string castling = "";
        if (whiteCanKSC) castling += "K";
        if (whiteCanQSC) castling += "Q";
        if (blackCanKSC) castling += "k";
        if (blackCanQSC) castling += "q";
        if (castling == "") castling = "-";

        // 4. En Passant Target Square
        string enPassantSquare = "-";
        if (enPassantIndex != -1)
        {
            // Convert index (0-63) to chess notation (e.g., 20 -> d6)
            int file = enPassantIndex % 8;
            int rank = 8 - (enPassantIndex / 8);
            enPassantSquare = ((char)('a' + file)).ToString() + rank.ToString();
        }

        // 5. Halfmove Clock (required for 50-move rule, but is part of position history)
        string halfMove = halfMoveClock.ToString();

        // 6. Fullmove Number
        string fullMove = fullMoveNumber.ToString();

        // Include fullMove at the end
        return $"{piecePlacement} {activeColor} {castling} {enPassantSquare} {halfMove} {fullMove}";
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

    private void HandleCapturedVisuals(GameObject capturedBoardGO, int pieceValue)
    {
        if (pieceValue == Piece.None)
        {
            return;
        }

        if (capturedBoardGO != null)
        {
            PoolPiece(capturedBoardGO);
        }

        int pieceType = GetPieceType(pieceValue);
        int pieceColor = GetPieceColor(pieceValue);

        // 2. Identify the correct UI container using the new helper
        Transform targetContainer = GetDeadPieceContainer(pieceType, pieceColor);

        GameObject prefab = GetPrefabForPiece(pieceType);

        if (targetContainer != null && prefab != null)
        {
            // 3. Generate a NEW piece in the UI
            GameObject uiPiece = Instantiate(prefab, targetContainer);

            // 4. Set exact size to 100x100
            RectTransform rect = uiPiece.GetComponent<RectTransform>();
            if (rect == null) rect = uiPiece.AddComponent<RectTransform>();

            rect.sizeDelta = new Vector2(100, 100);

            // 5. Standardize Scale & Rotation for UI
            uiPiece.transform.localScale = Vector3.one;
            uiPiece.transform.localRotation = Quaternion.identity;

            // 6. Set the correct color
            Image img = uiPiece.GetComponent<Image>();
            if (img != null)
            {
                img.color = (pieceColor == Piece.White) ? white : black;
            }

            // 7. Cleanup components
            if (uiPiece.GetComponent<Button>()) Destroy(uiPiece.GetComponent<Button>());
            if (img != null) img.raycastTarget = false;
        }
    }

    private Transform GetDeadPieceContainer(int pieceType, int pieceColor)
    {
        if (pieceColor == Piece.White)
        {
            switch (pieceType)
            {
                case Piece.Pawn: return whitePawnDeadContainer;
                case Piece.Knight: return whiteKnightDeadContainer;
                case Piece.Bishop: return whiteBishopDeadContainer;
                case Piece.Rook: return whiteRookDeadContainer;
                case Piece.Queen: return whiteQueenDeadContainer;
                default: return null;
            }
        }
        else // Black pieces
        {
            switch (pieceType)
            {
                case Piece.Pawn: return blackPawnDeadContainer;
                case Piece.Knight: return blackKnightDeadContainer;
                case Piece.Bishop: return blackBishopDeadContainer;
                case Piece.Rook: return blackRookDeadContainer;
                case Piece.Queen: return blackQueenDeadContainer;
                default: return null;
            }
        }
    }

    private void HighlightCurrentMoveButton(int index)
    {
        if (pgnContent == null) return;

        // Loop through all PGN buttons to set colors
        for (int i = 0; i < pgnContent.childCount; i++)
        {
            Button btn = pgnContent.GetChild(i).GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock colors = btn.colors;
                // Check if the current move is the one we are highlighting
                if (i == index)
                {
                    colors.normalColor = Color.yellow; // Highlighted color
                }
                else
                {
                    // Reset others
                    colors.normalColor = Color.white;
                }
                btn.colors = colors;
            }
        }
    }

    public void ClearAllPieceVisuals()
    {
        // 1. Clear Active Pieces from the Board
        foreach (GameObject tile in tileObjects)
        {
            Transform holder = tile.transform.GetChild(0);
            while (holder.childCount > 0)
            {
                // Get the piece
                GameObject piece = holder.GetChild(0).gameObject;

                // Send to Pool instead of Dead Container
                PoolPiece(piece);
            }
        }

        // 2. Clear the Graveyards (Dead Containers)
        Transform[] deadContainers = new Transform[] {
            whitePawnDeadContainer, whiteRookDeadContainer, whiteKnightDeadContainer, whiteBishopDeadContainer, whiteQueenDeadContainer, whiteKingDeadContainer,
            blackPawnDeadContainer, blackRookDeadContainer, blackKnightDeadContainer, blackBishopDeadContainer, blackQueenDeadContainer, blackKingDeadContainer
        };

        foreach (Transform container in deadContainers)
        {
            if (container == null) continue;

            // FIX: Iterate backward using a for loop to safely remove children from the Transform
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                // Get the piece using the current index 'i'
                GameObject piece = container.GetChild(i).gameObject;

                // PoolPiece reparents the piece, removing it from this container
                PoolPiece(piece);
            }
        }
    }

    private GameObject GetPieceFromPool(int pieceType, int pieceColor)
    {
        int pieceValue = pieceType | pieceColor;
        string searchName = pieceValue.ToString();

        // Find the first piece in the pool that matches the type/color we need
        GameObject pieceGO = piecePool.FirstOrDefault(p => p.name == searchName);

        if (pieceGO != null)
        {
            piecePool.Remove(pieceGO);
            pieceGO.SetActive(true); // Make sure it's visible
            return pieceGO;
        }

        // Create new if none exists (Initial game start)
        GameObject prefab = GetPrefabForPiece(pieceType);
        if (prefab != null)
        {
            GameObject newPieceGO = Instantiate(prefab);
            newPieceGO.name = searchName;
            Image img = newPieceGO.GetComponent<Image>();
            if (img != null)
            {
                img.color = (pieceColor == Piece.White) ? white : black;
            }
            return newPieceGO;
        }
        return null;
    }

    public void RedrawPiecesFromTileContent()
    {
        piecesCapturedInCurrentState.Clear();

        // Track how many pieces we send to graveyard during this redraw
        Dictionary<int, int> graveyardCounts = new Dictionary<int, int>();

        // 1. VISUALIZE THE BOARD (Move pieces from pool to board)
        for (int i = 0; i < tileContent.Length; i++)
        {
            int pieceValue = tileContent[i];

            if (pieceValue != Piece.None)
            {
                int pieceType = GetPieceType(pieceValue);
                int pieceColor = GetPieceColor(pieceValue);

                GameObject pieceGO = GetPieceFromPool(pieceType, pieceColor);

                if (pieceGO != null)
                {
                    Transform holder = tileObjects[i].transform.GetChild(0);
                    pieceGO.transform.SetParent(holder);
                    pieceGO.transform.localPosition = Vector3.zero;
                    pieceGO.transform.localScale = Vector3.one;
                    pieceGO.transform.localRotation = Quaternion.identity;
                    pieceGO.SetActive(true);

                    Image img = pieceGO.GetComponent<Image>();
                    if (img != null) img.raycastTarget = true;
                }
            }
        }

        // 2. HANDLE LEFTOVERS (Graveyard vs Destroy)
        foreach (GameObject remainingPiece in piecePool)
        {
            if (remainingPiece == null) continue;

            if (int.TryParse(remainingPiece.name, out int pieceValue))
            {
                int pieceType = GetPieceType(pieceValue);
                int pieceColor = GetPieceColor(pieceValue);

                // --- NEW LOGIC START ---
                // Count how many of this specific piece are currently on the board
                int countOnBoard = 0;
                for (int i = 0; i < tileContent.Length; i++)
                {
                    if (tileContent[i] != Piece.None &&
                        GetPieceType(tileContent[i]) == pieceType &&
                        GetPieceColor(tileContent[i]) == pieceColor)
                    {
                        countOnBoard++;
                    }
                }

                // Define standard chess limits
                int standardLimit = 0;
                switch (pieceType)
                {
                    case Piece.Pawn: standardLimit = 8; break;
                    case Piece.Knight: standardLimit = 2; break;
                    case Piece.Bishop: standardLimit = 2; break;
                    case Piece.Rook: standardLimit = 2; break;
                    case Piece.Queen: standardLimit = 1; break;
                    case Piece.King: standardLimit = 1; break;
                }

                // Check how many we have already sent to graveyard in this specific loop
                if (!graveyardCounts.ContainsKey(pieceValue)) graveyardCounts[pieceValue] = 0;
                int currentlyInGraveyard = graveyardCounts[pieceValue];

                // Only move to graveyard if we are actually missing pieces from the standard set
                // logic: (OnBoard + InGraveyard) < StandardLimit
                if ((countOnBoard + currentlyInGraveyard) < standardLimit)
                {
                    Transform targetContainer = GetDeadPieceContainer(pieceType, pieceColor);

                    if (targetContainer != null)
                    {
                        remainingPiece.transform.SetParent(targetContainer, false);
                        remainingPiece.transform.localPosition = Vector3.zero;
                        remainingPiece.transform.localScale = Vector3.one;
                        remainingPiece.transform.localRotation = Quaternion.identity;
                        RectTransform rect = remainingPiece.GetComponent<RectTransform>();
                        if (rect != null) rect.sizeDelta = new Vector2(100, 100);
                        remainingPiece.SetActive(true);

                        Image img = remainingPiece.GetComponent<Image>();
                        if (img != null) img.raycastTarget = false;

                        // Increment our tracking so we don't overfill the graveyard
                        graveyardCounts[pieceValue]++;
                    }
                    else
                    {
                        Destroy(remainingPiece);
                    }
                }
                else
                {
                    // If we have excess pieces (e.g. the Queen created by promotion after we undid the move), destroy it.
                    Destroy(remainingPiece);
                }
                // --- NEW LOGIC END ---
            }
            else
            {
                Destroy(remainingPiece);
            }
        }

        piecePool.Clear();
    }

    public void PoolPiece(GameObject piece)
    {
        if (piece == null || piecePool.Contains(piece)) return;

        piece.SetActive(false);
        // Reparent to the ChessManager itself, which acts as the pool holder
        piece.transform.SetParent(this.transform);
        piecePool.Add(piece);
    }

    private void ClearFutureHistory()
    {
        if (currentHistoryIndex < positionHistory.Count - 1)
        {
            int indexToRemoveFrom = currentHistoryIndex + 1;
            int movesToRemove = positionHistory.Count - indexToRemoveFrom;

            if (movesToRemove > 0)
            {
                // 1. Remove Board History
                positionHistory.RemoveRange(indexToRemoveFrom, movesToRemove);

                // 2. Remove PGN History
                if (pgnHistoryList.Count > currentHistoryIndex)
                {
                    pgnHistoryList.RemoveRange(currentHistoryIndex, pgnHistoryList.Count - currentHistoryIndex);
                }

                // 3. Remove Highlight History (THE MISSING PART)
                // We must perform safe checks to avoid out-of-range errors
                if (indexToRemoveFrom < moveDestinationsHistory.Count)
                {
                    moveDestinationsHistory.RemoveRange(indexToRemoveFrom, moveDestinationsHistory.Count - indexToRemoveFrom);
                }

                if (indexToRemoveFrom < moveOriginsHistory.Count)
                {
                    moveOriginsHistory.RemoveRange(indexToRemoveFrom, moveOriginsHistory.Count - indexToRemoveFrom);
                }

                // 4. Clear UI
                if (pgnContent != null)
                {
                    for (int i = pgnContent.childCount - 1; i >= currentHistoryIndex; i--)
                    {
                        Destroy(pgnContent.GetChild(i).gameObject);
                    }
                }
            }
        }
    }

    public bool IsMyPiece(int pieceColor)
    {
        // If selection is disabled OR set to "Both" (-1), allow everything
        if (!enableSideSelection || PlayerSide == -1) return true;

        if (PlayerSide == 0 && pieceColor == Piece.White) return true;
        if (PlayerSide == 1 && pieceColor == Piece.Black) return true;

        return false;
    }

    private IEnumerator ShowGameOverUI(string title, string subTitle, float delay = 2f)
    {
        // 1. Wait for the delay
        yield return new WaitForSeconds(delay);

        // 2. Show the UI
        resultsPanel.SetActive(true);
        winnerText.text = title;
        winConText.text = subTitle;
    }

    private void HighlightKingCheck()
    {
        // 1. Find the King of the current turn
        int kingIndex = -1;
        for (int i = 0; i < tileContent.Length; i++)
        {
            // Use your existing helper methods to find the King
            if (GetPieceType(tileContent[i]) == Piece.King && GetPieceColor(tileContent[i]) == currentTurnColor)
            {
                kingIndex = i;
                break;
            }
        }

        // 2. If King exists, check if attacked
        if (kingIndex != -1)
        {
            int opponentColor = (currentTurnColor == Piece.White) ? Piece.Black : Piece.White;

            // 3. If in check, paint it RED
            if (IsTileAttacked(kingIndex, opponentColor))
            {
                GameObject kingTile = tileObjects[kingIndex];
                Image tileImg = kingTile.GetComponent<Image>();
                if (tileImg != null)
                {
                    tileImg.color = Color.red;
                }
            }
        }
    }

    private void HighlightLastMove()
    {
        // 1. Reset ALL tiles to default
        if (defaultTileColors != null)
        {
            for (int i = 0; i < tileObjects.Length; i++)
            {
                Image img = tileObjects[i].GetComponent<Image>();
                if (img != null) img.color = defaultTileColors[i];
            }
        }

        // 2. Highlight DESTINATION
        if (currentLastMoveIndex != -1 && currentLastMoveIndex < tileObjects.Length)
        {
            Image img = tileObjects[currentLastMoveIndex].GetComponent<Image>();
            if (img != null) img.color = lastMoveColor;
        }

        // 3. Highlight ORIGIN (NEW)
        if (currentLastMoveOriginIndex != -1 && currentLastMoveOriginIndex < tileObjects.Length)
        {
            Image img = tileObjects[currentLastMoveOriginIndex].GetComponent<Image>();
            if (img != null) img.color = lastMoveColor;
        }
    }

    // ---------------------------------------------------------
    // --- UCI MOVE HANDLING ---
    // ---------------------------------------------------------

    public void PlayUCIMove(string uci)
    {
        if (uci.Length < 4)
        {
            Debug.LogError("Invalid UCI string: " + uci);
            return;
        }

        // ------------------------------------------
        // 1. Parse ORIGIN (e2)
        // ------------------------------------------
        string originStr = uci.Substring(0, 2);
        int originIndex = SquareToIndex(originStr);
        if (originIndex < 0)
        {
            Debug.LogError("Invalid origin square: " + originStr);
            return;
        }

        // ------------------------------------------
        // 2. Parse DESTINATION (e4)
        // ------------------------------------------
        string destStr = uci.Substring(2, 2);
        int destIndex = SquareToIndex(destStr);
        if (destIndex < 0)
        {
            Debug.LogError("Invalid destination square: " + destStr);
            return;
        }

        GameObject originGO = tileObjects[originIndex];
        GameObject destGO = tileObjects[destIndex];

        // ------------------------------------------
        // 3. Get piece GO from origin
        // ------------------------------------------
        Transform holder = originGO.transform.GetChild(0);
        if (holder.childCount == 0)
        {
            Debug.LogError("No piece on " + originStr);
            return;
        }

        GameObject pieceGO = holder.GetChild(0).gameObject;
        selectedPiece = pieceGO;

        // ------------------------------------------
        // 4. Handle promotion (e.g., "e7e8q")
        // ------------------------------------------
        bool isPromotion = (uci.Length == 5);
        int promotionType = Piece.None;

        if (isPromotion)
        {
            char promoChar = char.ToLower(uci[4]);
            switch (promoChar)
            {
                case 'q': promotionType = Piece.Queen; break;
                case 'r': promotionType = Piece.Rook; break;
                case 'b': promotionType = Piece.Bishop; break;
                case 'n': promotionType = Piece.Knight; break;
                default:
                    Debug.LogError("Invalid promotion character: " + promoChar);
                    return;
            }
        }

        // ------------------------------------------
        // 5. Perform the move
        // ------------------------------------------
        MovePiece(originGO, destGO);

        // ------------------------------------------
        // 6. Apply promotion automatically
        // ------------------------------------------
        if (isPromotion && isPromotionPending)
        {
            PromoteAndFinalizeMove(promotionType);
        }
    }

    private int SquareToIndex(string sq)
    {
        if (sq.Length != 2) return -1;

        char fileChar = sq[0];
        char rankChar = sq[1];

        int file = fileChar - 'a';
        int rank = rankChar - '1';

        if (file < 0 || file > 7 || rank < 0 || rank > 7)
            return -1;

        // Your indexing: rank 0 = row 7, rank 7 = row 0
        int boardRow = 7 - rank;
        return boardRow * 8 + file;
    }

    // ----------------------------------------------------------------------
    // --- BUTTONS ---
    // ----------------------------------------------------------------------

    public void GoBackOneMove()
    {
        int targetIndex = currentHistoryIndex - 1;
        if (targetIndex >= 0)
        {
            LoadBoardFromHistory(targetIndex);
        }
    }

    public void GoForwardOneMove()
    {
        int targetIndex = currentHistoryIndex + 1;
        if (targetIndex < positionHistory.Count)
        {
            LoadBoardFromHistory(targetIndex);
        }
    }

    public void BackButton()
    {
        SceneLoader.Instance.LoadNewScene("AdminScene");
    }

    public void NewGame()
    {
        string defaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq 0 1";
        StartGameFromFEN(defaultFen);
    }

    public void ResignGame()
    {
        StartCoroutine(ShowGameOverUI("You Lose!", "by Forfeit.", 0.5f));
    }

    // 0 for White, 1 for Black
    public void SelectSide(int sideIndex)
    {
        // 1. Store the selection! (0 = White, 1 = Black)
        PlayerSide = sideIndex;

        // 2. Hide UI
        if (sideSelectionPanel != null) sideSelectionPanel.SetActive(false);

        // 3. Handle Rotation
        if (sideIndex == 0) // WHITE
        {
            whiteName.text = "You";
            blackName.text = "Stockfish";
            FlipBoard(0);
        }
        else if (sideIndex == 1) // BLACK
        {
            whiteName.text = "Stockfish";
            blackName.text = "You";
            FlipBoard(180);
        }
    }

    public void FlipBoard(int degrees = 0)
    {
        Quaternion targetRotation;

        if (degrees == 180)
        {
            targetRotation = Quaternion.Euler(0, 0, 180f);
        }
        else
        {
            targetRotation = Quaternion.identity;
        }

        gamePanel.transform.localRotation = targetRotation;
        whitePanel.transform.localRotation = targetRotation;
        blackPanel.transform.localRotation = targetRotation;

        foreach (Transform child in gameBoard.transform)
        {
            child.localRotation = targetRotation;
        }
    }

    public void ToggleBoardRotation()
    {
        Quaternion rotation0 = Quaternion.identity;

        if (Quaternion.Angle(gamePanel.transform.localRotation, rotation0) < 1f)
        {
            FlipBoard(180);
        }
        else
        {
            FlipBoard(0);
        }
    }
}

public enum GameState
{
    NotStarted,
    InProgress,
    WhiteWin,
    BlackWin,
    Draw
}