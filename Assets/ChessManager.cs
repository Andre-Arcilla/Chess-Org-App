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

        // Your initial FEN position
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

                        // Store piece integer value in the name for easy lookup
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

        // --- En passant capture check ---
        if (destinationIndex == enPassantIndex)
        {
            int capturedPawnIndex = enPassantIndex + ((GetPieceColor(int.Parse(selectedPiece.name)) == Piece.White) ? +8 : -8);

            tileContent[capturedPawnIndex] = Piece.None;
            Transform capTile = tileObjects[capturedPawnIndex].transform.GetChild(0);
            foreach (Transform child in capTile)
                Destroy(child.gameObject);
        }

        enPassantIndex = -1;

        // --- Check if move allows for an en passant ---
        if (GetPieceType(int.Parse(selectedPiece.name)) == Piece.Pawn)
        {
            int originRow = originIndex / 8;
            int destinationRow = destinationIndex / 8;

            if (Math.Abs(originRow - destinationRow) == 2)
            {
                enPassantIndex = (originIndex + destinationIndex) / 2;
            }
        }

        // Handle capture/move
        Transform destHolder = destination.transform.GetChild(0);
        foreach (Transform child in destHolder)
            Destroy(child.gameObject);

        selectedPiece.transform.SetParent(destHolder);

        // Update tileContent
        tileContent[originIndex] = Piece.None;
        tileContent[destinationIndex] = int.Parse(selectedPiece.name);

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

    // Helper: Finds the index of the King of a given color
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
                // Edge checks (must be done before incrementing index)
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
                    // Stop the ray if any piece blocks it
                    break;
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

        // --- 4. Check Pawn Attacks (Diagonal in front of the attacker) ---

        // If the attacker is WHITE, the pawns are in the row BELOW the target (index +8).
        // They attack diagonally (index +7 and index +9).
        // If the attacker is BLACK, the pawns are in the row ABOVE the target (index -8).
        // They attack diagonally (index -7 and index -9).

        // This calculates the directions *from the attacker's pawn* to the *target*.
        // Therefore, we check the squares that are *diagonal* and *one rank behind* the target
        int forwardOffset = (attackerColor == Piece.White) ? +8 : -8; // Relative direction from target back to attacker's rank
        int[] pawnCaptureDirs = { forwardOffset - 1, forwardOffset + 1 };

        foreach (int dir in pawnCaptureDirs)
        {
            int checkIndex = targetIndex + dir;

            if (checkIndex >= 0 && checkIndex < tileContent.Length)
            {
                int checkCol = checkIndex % 8;

                // Ensure the tile is indeed diagonal (prevents wrap-around on a/h files)
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
    // --- MOVEMENT GENERATION (Integration Example) ---
    // ----------------------------------------------------------------------

    // Rook moves
    private void GetRookMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        // Search movable tiles
        foreach (int dir in lateralDir)
        {
            int index = originIndex;

            while (true)
            {
                int nextRow = index / 8;
                int nextCol = index % 8;

                // Check if moving off the board
                if (dir == +1 && nextCol == 7) break;
                if (dir == -1 && nextCol == 0) break;
                if (dir == +8 && nextRow == 7) break;
                if (dir == -8 && nextRow == 0) break;

                index += dir;
                if (index < 0 || index >= tileContent.Length) break; // Should not be strictly needed, but safe

                int targetTileContent = tileContent[index];

                // --- KING SAFETY CHECK INTEGRATION ---
                if (!IsMoveLegal(originIndex, index))
                {
                    // If the move is illegal (causes self-check/pin), we cannot move here 
                    // and we cannot look further down this line, as we are pinned.
                    break;
                }

                // If the move is legal:
                if (targetTileContent != Piece.None)
                {
                    if (GetPieceColor(targetTileContent) != color)
                    {
                        moves.Add(tileObjects[index]); // Valid capture
                    }
                    break; // Stop line search, blocked by a piece (enemy captured, or friendly block)
                }

                moves.Add(tileObjects[index]); // Valid empty move
            }
        }

        // Highlight moveable tiles
        foreach (GameObject tile in moves)
        {
            tile.GetComponent<Image>().color = Color.blue;
        }
    }

    // Bishop moves
    private void GetBishopMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        // Search movable tiles
        foreach (int dir in diagonalDir)
        {
            int index = originIndex;

            while (true)
            {
                int nextRow = index / 8;
                int nextCol = index % 8;

                // Check if moving off the board
                if (dir == +9 && (nextRow == 7 || nextCol == 7)) break; // down-right
                if (dir == +7 && (nextRow == 7 || nextCol == 0)) break; // down-left
                if (dir == -9 && (nextRow == 0 || nextCol == 0)) break; // up-left
                if (dir == -7 && (nextRow == 0 || nextCol == 7)) break; // up-right

                index += dir;
                if (index < 0 || index >= tileContent.Length) break;

                int targetTileContent = tileContent[index];

                // --- KING SAFETY CHECK INTEGRATION ---
                if (!IsMoveLegal(originIndex, index))
                {
                    break; // Stop ray tracing if moving here is illegal (pinned)
                }

                if (targetTileContent != Piece.None)
                {
                    if (GetPieceColor(targetTileContent) != color)
                    {
                        moves.Add(tileObjects[index]);
                    }
                    break; // Stop line search (blocked by any piece)
                }

                moves.Add(tileObjects[index]); // Valid empty move
            }
        }

        // Highlight moveable tiles
        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

    // Knight moves
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

            // Check for valid L-shape and no wrap-around
            if ((colChange == 1 && rowChange == 2) || (colChange == 2 && rowChange == 1))
            {
                int targetPiece = tileContent[targetIndex];

                // Check if blocked by a friendly piece
                if (targetPiece != Piece.None && GetPieceColor(targetPiece) == color)
                {
                    continue;
                }

                // --- KING SAFETY CHECK INTEGRATION ---
                if (IsMoveLegal(originIndex, targetIndex))
                {
                    moves.Add(tileObjects[targetIndex]);
                }
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
        int color = GetPieceColor(int.Parse(pieceGO.name));
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        // Queen combines Rook (lateralDir) and Bishop (diagonalDir) logic
        foreach (int dir in lateralDir.Concat(diagonalDir))
        {
            int index = originIndex;

            while (true)
            {
                int nextRow = index / 8;
                int nextCol = index % 8;

                // Check if moving off the board
                // LATERAL
                if (dir == +1 && nextCol == 7) break;
                if (dir == -1 && nextCol == 0) break;
                if (dir == +8 && nextRow == 7) break;
                if (dir == -8 && nextRow == 0) break;
                // DIAGONAL
                if (dir == +9 && (nextRow == 7 || nextCol == 7)) break;
                if (dir == +7 && (nextRow == 7 || nextCol == 0)) break;
                if (dir == -9 && (nextRow == 0 || nextCol == 0)) break;
                if (dir == -7 && (nextRow == 0 || nextCol == 7)) break;

                index += dir;
                if (index < 0 || index >= tileContent.Length) break;

                int targetTileContent = tileContent[index];

                // --- KING SAFETY CHECK INTEGRATION ---
                if (!IsMoveLegal(originIndex, index))
                {
                    break; // Stop ray tracing if moving here is illegal (pinned)
                }

                if (targetTileContent != Piece.None)
                {
                    if (GetPieceColor(targetTileContent) != color)
                    {
                        moves.Add(tileObjects[index]);
                    }
                    break; // Stop line search (blocked by any piece)
                }

                moves.Add(tileObjects[index]); // Valid empty move
            }
        }

        // Highlight moveable tiles
        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

    // King moves
    private void GetKingMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        int opponentColor = (color == Piece.White) ? Piece.Black : Piece.White;
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);

        foreach (int dir in lateralDir.Concat(diagonalDir))
        {
            int index = originIndex;
            int row = index / 8;
            int col = index % 8;

            // Check if moving off the board
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

            // --- KING SAFETY CHECK INTEGRATION (Direct Attack Check) ---
            if (IsTileAttacked(index, opponentColor))
            {
                continue; // King cannot move into check
            }

            if (targetTileContent != Piece.None)
            {
                if (GetPieceColor(targetTileContent) != color)
                {
                    moves.Add(tileObjects[index]); // Enemy piece
                }
                continue; // Blocked by friendly or captured enemy
            }

            moves.Add(tileObjects[index]); // Empty square
        }

        foreach (GameObject index in moves)
        {
            index.GetComponent<Image>().color = Color.blue;
        }
    }

    // Pawn moves
    private void GetPawnMoves(GameObject originGO, GameObject pieceGO)
    {
        int color = GetPieceColor(int.Parse(pieceGO.name));
        moves.Clear();
        int originIndex = Array.IndexOf(tileObjects, originGO);
        int forwardDir = (color == Piece.White) ? -8 : +8;
        int startRow = (color == Piece.White) ? 6 : 1; // Row 7 (index 6) for white, Row 2 (index 1) for black

        // --- Forward Movement (1 or 2 steps) ---
        for (int step = 1; step <= 2; step++)
        {
            // 2-step check: must be on starting row and first square must be empty
            if (step == 2 && (originIndex / 8 != startRow || tileContent[originIndex + forwardDir] != Piece.None))
                continue;

            int targetIndex = originIndex + (forwardDir * step);

            if (targetIndex < 0 || targetIndex >= tileContent.Length) break;

            // Must be empty to move forward
            if (tileContent[targetIndex] != Piece.None) break;

            // --- KING SAFETY CHECK INTEGRATION (Forward Move) ---
            if (IsMoveLegal(originIndex, targetIndex))
            {
                moves.Add(tileObjects[targetIndex]);
            }
            else
            {
                // If the first step is illegal (pinned), the second step is also impossible
                if (step == 1) break;
            }
        }

        // --- Diagonal Captures & En Passant ---
        int[] captureDirs = { forwardDir - 1, forwardDir + 1 };

        foreach (int dir in captureDirs)
        {
            int targetIndex = originIndex + dir;

            if (targetIndex < 0 || targetIndex >= tileContent.Length) continue;

            // Check for wrap-around
            if (Math.Abs((targetIndex % 8) - (originIndex % 8)) != 1) continue;

            int targetTileContent = tileContent[targetIndex];

            bool isNormalCapture = (targetTileContent != Piece.None && GetPieceColor(targetTileContent) != color);
            bool isEnPassant = (targetIndex == enPassantIndex);

            if (isNormalCapture || isEnPassant)
            {
                // --- KING SAFETY CHECK INTEGRATION (Capture Move) ---
                if (IsMoveLegal(originIndex, targetIndex))
                {
                    moves.Add(tileObjects[targetIndex]);
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

                Color tileColor = ((row + col) % 2 == 0)
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(132, 132, 132, 255);

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
        return piece & 0b0111; // Extracts the piece type (1-7)
    }

    private int GetPieceColor(int piece)
    {
        return piece & (Piece.White | Piece.Black); // Extracts color bits (8 or 16)
    }

    public void BackButton()
    {
        // Assuming SceneLoader is available
        SceneLoader.Instance.LoadNewScene("AdminScene");
    }
}