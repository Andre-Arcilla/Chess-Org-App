using System;
using System.Collections;
using System.Collections.Generic;
using System.Text; // Required for StringBuilder
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

/// <summary>
/// PGNRecorder - Handles both PGN Playback (Import) and PGN Export.
/// 1. RunPGN(): Plays the 'pgnInput' string on the board.
/// 2. CopyToClipboard(): Exports the current ChessManager history to clipboard AND 'exportPGN' variable.
/// </summary>
public class PGNRecorder : MonoBehaviour
{
    [Header("Playback Settings")]
    [TextArea(4, 20)]
    public string importPGN;
    [SerializeField] private TMP_InputField importInput;

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

    public void SavePGNToVariable()
    {
        if (EnsureManager())
        {
            exportPGN = GeneratePGNString();
            Debug.Log("PGN saved to internal variable.");
        }
    }

    public void CopyToClipboard()
    {
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

        // 1. Determine Result String based on GameState
        string resultStr = GetGameResult();

        // --- Headers ---
        sb.AppendLine("[Event \"Casual Game\"]");
        sb.AppendLine("[Site \"Unity Chess\"]");
        sb.AppendLine($"[Date \"{DateTime.Now:yyyy.MM.dd}\"]");
        sb.AppendLine("[Round \"1\"]");

        string whiteName = "White";
        string blackName = "Black";

        if (chessManager.whiteName != null) whiteName = chessManager.whiteName.text;
        if (chessManager.blackName != null) blackName = chessManager.blackName.text;

        sb.AppendLine($"[White \"{whiteName}\"]");
        sb.AppendLine($"[Black \"{blackName}\"]");
        sb.AppendLine($"[Result \"{resultStr}\"]");
        sb.AppendLine(""); // Blank line required between headers and moves

        // --- Move Text ---
        // Ensure 'pgnHistoryList' is PUBLIC in ChessManager.cs
        List<string> moves = chessManager.PgnHistoryList;

        if (moves != null && moves.Count > 0)
        {
            int turnCount = 1;
            for (int i = 0; i < moves.Count; i++)
            {
                // Remove existing numbering if present
                string rawMove = moves[i];
                string cleanMove = Regex.Replace(rawMove, @"^\d+\.+\s*", "");

                // Add "1. " before White's moves (even indices)
                if (i % 2 == 0)
                {
                    sb.Append($"{turnCount}. ");
                }

                sb.Append(cleanMove);
                sb.Append(" ");

                // Increment turn count after Black's move
                if (i % 2 != 0)
                {
                    turnCount++;
                }
            }

            // Append result at the end of the moves
            sb.Append(resultStr);
        }
        else
        {
            sb.Append(resultStr);
        }

        return sb.ToString();
    }

    private string GetGameResult()
    {
        if (chessManager == null) return "*";

        // Access the public CurrentState from ChessManager
        // Ensure you have defined 'public enum GameState' and 'public GameState CurrentState' in ChessManager
        switch (chessManager.CurrentState)
        {
            case GameState.WhiteWin:
                return "1-0";
            case GameState.BlackWin:
                return "0-1";
            case GameState.Draw:
                return "1/2-1/2";
            case GameState.InProgress:
                return "*";
            case GameState.NotStarted:
                return "*";
            default:
                return "*";
        }
    }

    // ===================================================================================
    //  SECTION 2: IMPORT/PLAYBACK PGN (Input)
    // ===================================================================================

    public void RunPGN()
    {
        importPGN = importInput.text;

        if (string.IsNullOrWhiteSpace(importPGN))
        {
            Debug.LogWarning("PGNRecorder: no PGN provided in inspector.");
            return;
        }

        chessManager.NewGame();
        ParsePGN();
        StartCoroutine(PlayMovesCoroutine());
    }

    private void ParsePGN()
    {
        sanMoves.Clear();

        string s = importPGN;

        // Remove tag pairs, comments, annotation glyphs, NAGs
        s = Regex.Replace(s, "\\[.*?\\]", string.Empty, RegexOptions.Singleline);
        s = Regex.Replace(s, "\\{.*?\\}", string.Empty, RegexOptions.Singleline);
        s = Regex.Replace(s, ";.*?(\\r?\\n)", "$1");
        s = Regex.Replace(s, "\\$\\d+", string.Empty);
        s = Regex.Replace(s, "(\\?|!|\\?\\!|\\!\\?)", string.Empty);

        // Remove move numbers
        s = Regex.Replace(s, "\\d+\\.+", string.Empty);

        string[] parts = Regex.Split(s, "\\s+");
        foreach (string p in parts)
        {
            string trimmed = p.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Skip result markers in the input string
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
            List<(int origin, int dest)> legal = BuildLegalMoveList(chessManager);

            if (!TryMatchSANToLegalMove(san, legal, chessManager, out (int origin, int dest, int promotionType) match))
            {
                Debug.LogError($"PGNRecorder: Failed to match SAN '{san}' to any legal move.");
                yield break;
            }

            int originIndex = match.origin;
            int destIndex = match.dest;
            int promotionType = match.promotionType;

            GameObject originTile = chessManager.tileObjects[originIndex];
            Transform originHolder = originTile.transform.GetChild(0);
            if (originHolder.childCount == 0) yield break;

            GameObject pieceGO = originHolder.GetChild(0).gameObject;

            chessManager.selectedPiece = pieceGO;
            chessManager.MovePiece(originTile, chessManager.tileObjects[destIndex]);

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

            yield return new WaitForSeconds(moveDelay);
        }

        Debug.Log("PGNRecorder: Finished playback.");
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
            if (cm.GetPieceColor(pieceVal) != cm.currentTurnColor) continue;

            GameObject originTile = cm.tileObjects[i];
            Transform holder = originTile.transform.GetChild(0);
            if (holder.childCount == 0) continue;
            GameObject pieceGO = holder.GetChild(0).gameObject;

            cm.CheckMove(originTile, pieceGO);

            foreach (GameObject destGO in cm.moves)
            {
                int destIndex = Array.IndexOf(cm.tileObjects, destGO);
                if (destIndex >= 0) outList.Add((i, destIndex));
            }
        }
        cm.ResetObjects();
        return outList;
    }

    private bool TryMatchSANToLegalMove(string san, List<(int origin, int dest)> legal, ChessManager cm, out (int origin, int dest, int promotionType) result)
    {
        result = (-1, -1, Piece.None);
        string s = san.Trim();
        s = s.Replace("+", "").Replace("#", "");

        if (s == "O-O" || s == "0-0")
        {
            foreach (var m in legal)
            {
                if (cm.GetPieceType(cm.tileContent[m.origin]) == Piece.King)
                {
                    int diff = m.dest - m.origin;
                    if (diff == 2) { result = (m.origin, m.dest, Piece.None); return true; }
                }
            }
            return false;
        }
        if (s == "O-O-O" || s == "0-0-0")
        {
            foreach (var m in legal)
            {
                if (cm.GetPieceType(cm.tileContent[m.origin]) == Piece.King)
                {
                    int diff = m.dest - m.origin;
                    if (diff == -2) { result = (m.origin, m.dest, Piece.None); return true; }
                }
            }
            return false;
        }

        int promotionType = Piece.None;
        Match promoMatch = Regex.Match(s, "([a-h][1-8])=?(Q|R|B|N)$", RegexOptions.IgnoreCase);
        if (promoMatch.Success)
        {
            s = s.Substring(0, promoMatch.Groups[1].Index + promoMatch.Groups[1].Length);
            string promoChar = promoMatch.Groups[2].Value.ToUpper();
            if (promoChar == "Q") promotionType = Piece.Queen;
            else if (promoChar == "R") promotionType = Piece.Rook;
            else if (promoChar == "B") promotionType = Piece.Bishop;
            else if (promoChar == "N") promotionType = Piece.Knight;
        }

        int wantedPieceType = Piece.Pawn;
        int idx = 0;
        char c0 = s.Length > 0 ? s[0] : '\0';
        if ("KQRBN".IndexOf(c0) >= 0) { wantedPieceType = CharToPieceType(c0); idx++; }

        Match destMatch = Regex.Match(s, "([a-h][1-8])$", RegexOptions.IgnoreCase);
        if (!destMatch.Success) return false;
        string destSquare = destMatch.Groups[1].Value.ToLower();
        string between = s.Substring(idx, s.Length - idx - destSquare.Length).Replace("x", "");

        List<(int origin, int dest)> candidates = new List<(int, int)>();
        int destIndex = SquareToIndex(destSquare);

        foreach (var m in legal)
        {
            if (m.dest != destIndex) continue;
            if (cm.GetPieceType(cm.tileContent[m.origin]) != wantedPieceType) continue;
            candidates.Add(m);
        }

        if (!string.IsNullOrEmpty(between) && candidates.Count > 1)
        {
            List<(int origin, int dest)> filtered = new List<(int, int)>();
            foreach (var c in candidates)
            {
                int file = c.origin % 8;
                int rank = 8 - (c.origin / 8);
                string fChar = ((char)('a' + file)).ToString();
                string rChar = rank.ToString();

                if (between == (fChar + rChar)) filtered.Add(c);
                else if (between == rChar) filtered.Add(c);
                else if (between == fChar) filtered.Add(c);
            }
            if (filtered.Count > 0) candidates = filtered;
        }

        if (candidates.Count >= 1)
        {
            result = (candidates[0].origin, candidates[0].dest, promotionType);
            return true;
        }
        return false;
    }

    private static int CharToPieceType(char c)
    {
        switch (char.ToUpper(c))
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