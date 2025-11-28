using System;
using System.Collections;
using System.Collections.Generic;
using System.Text; // Required for StringBuilder
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// PGNRecorder - Handles both PGN Playback (Import) and PGN Export.
/// 1. RunPGN(): Plays the 'pgnInput' string on the board.
/// 2. CopyToClipboard(): Exports the current ChessManager history to clipboard AND 'currentPGNExport' variable.
/// </summary>
public class PGNRecorder : MonoBehaviour
{
    [Header("Playback Settings")]
    [TextArea(4, 20)]
    public string importPGN;

    [Tooltip("Seconds between moves during playback")]
    public float moveDelay = 0.5f;

    [Header("Export Output")]
    [Tooltip("The generated PGN string will appear here when you export.")]
    [TextArea(4, 20)]
    public string exportPGN;

    [Header("References")]
    [SerializeField] private ChessManager chessManager;

    private List<string> sanMoves = new List<string>();

    private void Start()
    {
        if (chessManager == null)
        {
            chessManager = ChessManager.Instance;
        }
    }

    // ===================================================================================
    //  SECTION 1: EXPORT PGN (Output)
    // ===================================================================================

    // Method 1: Updates the 'currentPGNExport' string variable with the latest PGN
    public void SavePGNToVariable()
    {
        if (EnsureManager())
        {
            exportPGN = GeneratePGNString();
            Debug.Log("PGN saved to internal variable.");
        }
    }

    // Method 2: Copies the current PGN to the system clipboard
    // (It updates the variable first to ensure you get the latest game state)
    public void CopyToClipboard()
    {
        // Ensure the variable is up to date before copying
        SavePGNToVariable();

        if (!string.IsNullOrEmpty(exportPGN))
        {
            GUIUtility.systemCopyBuffer = exportPGN;
            Debug.Log("PGN Copied to Clipboard:\n" + exportPGN);
        }
    }

    public string GeneratePGNString()
    {
        if (!EnsureManager()) return "";

        StringBuilder sb = new StringBuilder();

        // --- Headers ---
        sb.AppendLine("[Event \"Casual Game\"]");
        sb.AppendLine("[Site \"Unity Chess\"]");
        sb.AppendLine($"[Date \"{DateTime.Now:yyyy.MM.dd}\"]");
        sb.AppendLine("[Round \"1\"]");

        string whiteName = "White";
        string blackName = "Black";

        // Attempt to grab names from ChessManager UI
        if (chessManager.whiteName != null) whiteName = chessManager.whiteName.text;
        if (chessManager.blackName != null) blackName = chessManager.blackName.text;

        sb.AppendLine($"[White \"{whiteName}\"]");
        sb.AppendLine($"[Black \"{blackName}\"]");
        sb.AppendLine("[Result \"*\"]");
        sb.AppendLine("");

        // --- Move Text ---
        // NOTE: Ensure 'moveList' is the correct public variable in your ChessManager
        List<string> moves = chessManager.PgnHistoryList;

        if (moves != null && moves.Count > 0)
        {
            int turnCount = 1;
            for (int i = 0; i < moves.Count; i++)
            {
                // If it's White's turn (even indices), add the move number "1. "
                if (i % 2 == 0)
                {
                    sb.Append($"{turnCount}. ");
                }

                sb.Append(moves[i]);
                sb.Append(" ");

                // Increment turn counter after Black's move
                if (i % 2 != 0)
                {
                    turnCount++;
                }
            }
        }
        else
        {
            sb.Append("{No moves found}");
        }

        return sb.ToString();
    }

    // ===================================================================================
    //  SECTION 2: IMPORT/PLAYBACK PGN (Input)
    // ===================================================================================

    public void RunPGN()
    {
        if (string.IsNullOrWhiteSpace(importPGN))
        {
            Debug.LogWarning("PGNRecorder: no PGN provided in inspector.");
            return;
        }

        ParsePGN();
        StartCoroutine(PlayMovesCoroutine());
    }

    private void ParsePGN()
    {
        sanMoves.Clear();

        string s = importPGN;

        // Remove tag pairs like [Event "x"]
        s = Regex.Replace(s, "\\[.*?\\]", string.Empty, RegexOptions.Singleline);

        // Remove comments { ... } and ; to end-of-line comments
        s = Regex.Replace(s, "\\{.*?\\}", string.Empty, RegexOptions.Singleline);
        s = Regex.Replace(s, ";.*?(\\r?\\n)", "$1");

        // Remove numeric annotation glyphs ($n)
        s = Regex.Replace(s, "\\$\\d+", string.Empty);

        // Remove NAGs like ?! etc (optional)
        s = Regex.Replace(s, "(\\?|!|\\?\\!|\\!\\?)", string.Empty);

        // Remove move numbers (e.g., "1.", "23...")
        s = Regex.Replace(s, "\\d+\\.+", string.Empty);

        // Normalize whitespace and split
        string[] parts = Regex.Split(s, "\\s+");
        foreach (string p in parts)
        {
            string trimmed = p.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Skip results
            if (trimmed == "1-0" || trimmed == "0-1" || trimmed == "1/2-1/2" || trimmed == "*") continue;

            sanMoves.Add(trimmed);
        }

        Debug.Log($"PGNRecorder: Parsed {sanMoves.Count} SAN moves.");
    }

    private IEnumerator PlayMovesCoroutine()
    {
        if (!EnsureManager()) yield break;

        foreach (string san in sanMoves)
        {
            // compute legal moves at current position (list of origin/dest pairs)
            List<(int origin, int dest)> legal = BuildLegalMoveList(chessManager);

            // parse SAN and find matching legal move
            if (!TryMatchSANToLegalMove(san, legal, chessManager, out (int origin, int dest, int promotionType) match))
            {
                Debug.LogError($"PGNRecorder: Failed to match SAN '{san}' to any legal move.");
                yield break;
            }

            // Perform the move: set selectedPiece then call MovePiece(originGO, destGO)
            int originIndex = match.origin;
            int destIndex = match.dest;
            int promotionType = match.promotionType; // Piece.* or Piece.None

            GameObject originTile = chessManager.tileObjects[originIndex];
            Transform originHolder = originTile.transform.GetChild(0);
            if (originHolder.childCount == 0)
            {
                Debug.LogError($"PGNRecorder: no piece found on origin index {originIndex} for SAN '{san}'.");
                yield break;
            }

            GameObject pieceGO = originHolder.GetChild(0).gameObject;

            // Set selectedPiece and call MovePiece
            chessManager.selectedPiece = pieceGO;
            chessManager.MovePiece(originTile, chessManager.tileObjects[destIndex]);

            // If promotion was requested in SAN, finalize promotion explicitly
            if (promotionType != Piece.None)
            {
                switch (promotionType)
                {
                    case Piece.Queen: chessManager.PromoteToQueen(); break;
                    case Piece.Rook: chessManager.PromoteToRook(); break;
                    case Piece.Bishop: chessManager.PromoteToBishop(); break;
                    case Piece.Knight: chessManager.PromoteToKnight(); break;
                    default: chessManager.PromoteToQueen(); break;
                }
            }

            // Wait between moves
            yield return new WaitForSeconds(moveDelay);
        }

        Debug.Log("PGNRecorder: Finished playback.");

        // Optional: Refresh board state logic if needed
        if (chessManager.pgnButtonContainer != null)
        {
            // Force UI update if needed
            // chessManager.LoadBoardFromHistory(chessManager.pgnButtonContainer.childCount);
        }
    }

    // -------------------------
    // Logic Helpers
    // -------------------------

    private bool EnsureManager()
    {
        if (chessManager == null) chessManager = ChessManager.Instance;
        return chessManager != null;
    }

    private List<(int origin, int dest)> BuildLegalMoveList(ChessManager cm)
    {
        List<(int origin, int dest)> outList = new List<(int, int)>();

        for (int i = 0; i < cm.tileContent.Length; i++)
        {
            int pieceVal = cm.tileContent[i];
            if (pieceVal == Piece.None) continue;

            int color = cm.GetPieceColor(pieceVal);
            if (color != cm.currentTurnColor) continue;

            GameObject originTile = cm.tileObjects[i];
            Transform holder = originTile.transform.GetChild(0);
            if (holder.childCount == 0) continue;

            GameObject pieceGO = holder.GetChild(0).gameObject;

            // This will populate cm.moves with destination tile GameObjects
            cm.CheckMove(originTile, pieceGO);

            foreach (GameObject destGO in cm.moves)
            {
                int destIndex = Array.IndexOf(cm.tileObjects, destGO);
                if (destIndex >= 0)
                {
                    outList.Add((i, destIndex));
                }
            }
        }

        // Clear highlights created by CheckMove
        cm.ResetObjects();

        return outList;
    }

    private bool TryMatchSANToLegalMove(string san, List<(int origin, int dest)> legal, ChessManager cm, out (int origin, int dest, int promotionType) result)
    {
        result = (-1, -1, Piece.None);
        string s = san.Trim();

        // strip check/mate symbols
        s = s.Replace("+", "").Replace("#", "");

        // Handle castling
        if (s == "O-O" || s == "0-0")
        {
            // find king move that moves two squares to the right (White: e1->g1 +2, Black: e8->g8 +2)
            foreach (var m in legal)
            {
                int pieceVal = cm.tileContent[m.origin];
                if (cm.GetPieceType(pieceVal) == Piece.King)
                {
                    int diff = m.dest - m.origin;
                    if (diff == 2) // Kingside is +2 for both sides (60->62, 4->6)
                    {
                        result = (m.origin, m.dest, Piece.None);
                        return true;
                    }
                }
            }
            return false;
        }
        if (s == "O-O-O" || s == "0-0-0")
        {
            foreach (var m in legal)
            {
                int pieceVal = cm.tileContent[m.origin];
                if (cm.GetPieceType(pieceVal) == Piece.King)
                {
                    int diff = m.dest - m.origin;
                    if (diff == -2) // Queenside is -2 (60->58, 4->2)
                    {
                        result = (m.origin, m.dest, Piece.None);
                        return true;
                    }
                }
            }
            return false;
        }

        // Detect promotion (e8=Q or e8Q)
        int promotionType = Piece.None;
        Match promoMatch = Regex.Match(s, "([a-h][1-8])=?(Q|R|B|N)$", RegexOptions.IgnoreCase);
        if (promoMatch.Success)
        {
            string destSq = promoMatch.Groups[1].Value;
            string promoChar = promoMatch.Groups[2].Value.ToUpper();
            switch (promoChar)
            {
                case "Q": promotionType = Piece.Queen; break;
                case "R": promotionType = Piece.Rook; break;
                case "B": promotionType = Piece.Bishop; break;
                case "N": promotionType = Piece.Knight; break;
            }

            // reduce s to a pawn capture/move form for matching destination
            s = s.Substring(0, promoMatch.Groups[1].Index + promoMatch.Groups[1].Length);
        }

        // Determine piece type
        int wantedPieceType = Piece.Pawn;
        int idx = 0;
        char c0 = s.Length > 0 ? s[0] : '\0';
        if ("KQRBN".IndexOf(c0) >= 0)
        {
            wantedPieceType = CharToPieceType(c0);
            idx++;
        }

        // Now find if this is a capture (contains 'x')
        bool isCapture = s.Contains("x");

        // Extract destination square (last 2 chars that look like file+rank)
        Match destMatch = Regex.Match(s, "([a-h][1-8])$", RegexOptions.IgnoreCase);
        if (!destMatch.Success)
        {
            Debug.LogError("PGNRecorder: Could not parse destination square from SAN '" + san + "'");
            return false;
        }
        string destSquare = destMatch.Groups[1].Value.ToLower();

        // Extract disambiguation (between piece letter and 'x' or destination)
        // Example: Nbd2 -> disamb = 'b' ; R1a3 -> '1' ; Qh4e1 -> 'h4' (rare)
        string between = s.Substring(idx, s.Length - idx - destSquare.Length);
        between = between.Replace("x", "");

        // Candidate legal moves: those with matching destination and piece type
        List<(int origin, int dest)> candidates = new List<(int, int)>();
        int destIndex = SquareToIndex(destSquare);

        foreach (var m in legal)
        {
            if (m.dest != destIndex) continue;

            int originPieceVal = cm.tileContent[m.origin];
            int originPieceType = cm.GetPieceType(originPieceVal);

            if (originPieceType != wantedPieceType) continue;

            candidates.Add(m);
        }

        // Apply disambiguation filter if any
        if (!string.IsNullOrEmpty(between) && candidates.Count > 1)
        {
            List<(int origin, int dest)> filtered = new List<(int, int)>();
            foreach (var c in candidates)
            {
                int file = c.origin % 8;
                int rank = 8 - (c.origin / 8); // 1..8

                string fChar = ((char)('a' + file)).ToString();
                string rChar = rank.ToString();

                if (between.Length == 2)
                {
                    // file+rank disambiguation
                    if (between == (fChar + rChar)) filtered.Add(c);
                }
                else if (char.IsDigit(between, 0))
                {
                    if (between == rChar) filtered.Add(c);
                }
                else
                {
                    if (between == fChar) filtered.Add(c);
                }
            }
            if (filtered.Count > 0) candidates = filtered;
        }

        if (candidates.Count >= 1)
        {
            // If multiple candidates remain, typically implies ambiguity, but usually resolved by "isCapture" or specific chess rules. 
            // For now, we take the first match.
            result = (candidates[0].origin, candidates[0].dest, promotionType);
            return true;
        }

        // No candidates found
        return false;
    }

    private static int CharToPieceType(char c)
    {
        c = char.ToUpper(c);
        switch (c)
        {
            case 'K': return Piece.King;
            case 'Q': return Piece.Queen;
            case 'R': return Piece.Rook;
            case 'B': return Piece.Bishop;
            case 'N': return Piece.Knight;
            default: return Piece.Pawn;
        }
    }

    private int SquareToIndex(string sq)
    {
        if (sq.Length != 2) return -1;
        int file = sq[0] - 'a';
        int rank = sq[1] - '1';
        if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
        int boardRow = 7 - rank;
        return boardRow * 8 + file;
    }
}