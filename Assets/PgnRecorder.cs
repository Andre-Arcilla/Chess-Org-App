using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// PGNRecorder - auto-plays a PGN (SAN moves) using the existing ChessManager move generation + MovePiece.
/// Designed for Option A: automatic playback (one-by-one with delay).
/// </summary>
public class PGNRecorder : MonoBehaviour
{
    [TextArea(4, 20)]
    public string pgnInput;

    [Tooltip("Seconds between moves during playback")]
    public float moveDelay = 0.5f;

    private List<string> sanMoves = new List<string>();

    public void RunPGN()
    {
        if (string.IsNullOrWhiteSpace(pgnInput))
        {
            Debug.LogWarning("PGNRecorder: no PGN provided in inspector.");
            return;
        }

        ParsePGN();
        StartCoroutine(PlayMovesCoroutine());
    }

    // -------------------------
    // 1) PGN parsing -> SAN list
    // -------------------------
    private void ParsePGN()
    {
        sanMoves.Clear();

        string s = pgnInput;

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

    // -------------------------
    // 2) Playback coroutine
    // -------------------------
    private IEnumerator PlayMovesCoroutine()
    {
        ChessManager cm = ChessManager.Instance;
        if (cm == null)
        {
            Debug.LogError("PGNRecorder: ChessManager.Instance is null.");
            yield break;
        }

        foreach (string san in sanMoves)
        {
            // compute legal moves at current position (list of origin/dest pairs)
            List<(int origin, int dest)> legal = BuildLegalMoveList(cm);

            // parse SAN and find matching legal move
            if (!TryMatchSANToLegalMove(san, legal, cm, out (int origin, int dest, int promotionType) match))
            {
                Debug.LogError($"PGNRecorder: Failed to match SAN '{san}' to any legal move.");
                yield break;
            }

            // Perform the move: set selectedPiece then call MovePiece(originGO, destGO)
            int originIndex = match.origin;
            int destIndex = match.dest;
            int promotionType = match.promotionType; // Piece.* or Piece.None

            GameObject originTile = cm.tileObjects[originIndex];
            Transform originHolder = originTile.transform.GetChild(0);
            if (originHolder.childCount == 0)
            {
                Debug.LogError($"PGNRecorder: no piece found on origin index {originIndex} for SAN '{san}'.");
                yield break;
            }

            GameObject pieceGO = originHolder.GetChild(0).gameObject;

            // Set selectedPiece and call MovePiece
            cm.selectedPiece = pieceGO;
            cm.MovePiece(originTile, cm.tileObjects[destIndex]);

            // If promotion was requested in SAN, finalize promotion explicitly
            if (promotionType != Piece.None)
            {
                switch (promotionType)
                {
                    case Piece.Queen: cm.PromoteToQueen(); break;
                    case Piece.Rook: cm.PromoteToRook(); break;
                    case Piece.Bishop: cm.PromoteToBishop(); break;
                    case Piece.Knight: cm.PromoteToKnight(); break;
                    default: cm.PromoteToQueen(); break;
                }
            }

            // Wait between moves
            yield return new WaitForSeconds(moveDelay);
        }

        Debug.Log("PGNRecorder: Finished playback.");
        cm.LoadBoardFromHistory(0);
        cm.LoadBoardFromHistory(cm.pgnButtonContainer.childCount);
        //cm.pgnScrollRect.horizontalNormalizedPosition = 0f;
        //cm.GoBackOneMove();
    }

    // -------------------------
    // 3) Build legal move list using existing CheckMove -> moves
    // Returns list of (originIndex, destIndex)
    // -------------------------
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

    // -------------------------
    // 4) SAN parsing + matching to legal moves
    //
    // Supports:
    //  - pawn moves (e4, exd5)
    //  - piece moves (Nf3, R1a3, Nbd2, etc.)
    //  - captures (x)
    //  - promotions (e8=Q or e8Q)
    //  - castling O-O and O-O-O
    // -------------------------
    private bool TryMatchSANToLegalMove(string san, List<(int origin, int dest)> legal, ChessManager cm, out (int origin, int dest, int promotionType) result)
    {
        result = (-1, -1, Piece.None);
        string s = san.Trim();

        // strip check/mate symbols
        s = s.Replace("+", "").Replace("#", "");

        // Handle castling
        if (s == "O-O" || s == "0-0")
        {
            // find king move that moves two squares to the right
            foreach (var m in legal)
            {
                int pieceVal = cm.tileContent[m.origin];
                if (cm.GetPieceType(pieceVal) == Piece.King)
                {
                    int diff = m.dest - m.origin;
                    if (diff == 2 || diff == -6) // depending on orientation, kingside should be +2 (white) or maybe other; we accept +2
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
                    if (diff == -2 || diff == -10 || diff == -2) // accept typical queenside diffs (c file)
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
            s = s.Substring(0, s.Length - promoMatch.Groups[2].Length);
            s = s.TrimEnd('=');
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

        // If multiple candidates remain, prefer non-capture if SAN didn't include 'x'
        if (candidates.Count > 1 && !isCapture)
        {
            // If some candidates are captures (destination currently occupied by opponent) we still keep them,
            // but if multiple identical moves exist, we'll pick the first.
        }

        if (candidates.Count == 1)
        {
            result = (candidates[0].origin, candidates[0].dest, promotionType);
            return true;
        }

        // If still multiple candidates, choose one that doesn't expose king (shouldn't occur because legal moves are legal)
        if (candidates.Count > 1)
        {
            result = (candidates[0].origin, candidates[0].dest, promotionType);
            return true;
        }

        // No candidates found: return false
        return false;
    }

    // -------------------------
    // Helpers
    // -------------------------
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
