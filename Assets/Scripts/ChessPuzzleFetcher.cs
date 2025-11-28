using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class ChessPuzzleFetcher : MonoBehaviour
{
    private const string API_URL = "https://api.chess.com/pub/puzzle/random";

    // Data structure to match Chess.com JSON response
    [System.Serializable]
    public class PuzzleData
    {
        public string title;
        public string url;
        public string publish_time;
        public string fen;
        public string pgn;
        public string image;
    }

    [Header("Debug")]
    [SerializeField] private string lastFetchedPGN;
    [SerializeField] private string lastFetchedFEN;

    private void Start()
    {
        GetRandomPuzzle();
    }

    // Call this function from a Button OnClick event
    public void GetRandomPuzzle()
    {
        StartCoroutine(FetchPuzzleCoroutine());
    }

    private IEnumerator FetchPuzzleCoroutine()
    {
        Debug.Log("Fetching random puzzle from Chess.com...");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(API_URL))
        {
            // Chess.com requests a User-Agent to identify the app.
            webRequest.SetRequestHeader("User-Agent", "UnityChessStudentProject/1.0");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching puzzle: {webRequest.error}");
            }
            else
            {
                string jsonResponse = webRequest.downloadHandler.text;
                PuzzleData puzzle = JsonUtility.FromJson<PuzzleData>(jsonResponse);

                if (puzzle != null)
                {
                    Debug.Log($"Puzzle Fetched: {puzzle.title} | FEN: {puzzle.fen}");

                    lastFetchedPGN = puzzle.pgn;
                    lastFetchedFEN = puzzle.fen;

                    ProcessPuzzle(puzzle);
                }
                else
                {
                    Debug.LogError("Failed to parse puzzle data.");
                }
            }
        }
    }

    private void ProcessPuzzle(PuzzleData puzzle)
    {
        if (ChessManager.Instance != null)
        {
            // 1. Setup the Board using FEN
            if (!string.IsNullOrEmpty(puzzle.fen))
            {
                ChessManager.Instance.StartGameFromFEN(puzzle.fen);

                // 2. Flip the Board based on whose turn it is
                // FEN structure: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
                string[] parts = puzzle.fen.Split(' ');
                if (parts.Length > 1)
                {
                    bool isWhiteTurn = (parts[1] == "w");
                    int side = isWhiteTurn ? 0 : 1; // 0 = White, 1 = Black

                    // This method handles PlayerSide variable, UI hiding, and Board Rotation
                    ChessManager.Instance.SelectSide(side);
                }
            }

            // 3. Parse the PGN to get the solution moves
            List<string> solutionMoves = ParsePGNMoves(puzzle.pgn);

            // 4. Start Puzzle Mode in Manager (to validate user moves)
            ChessManager.Instance.StartPuzzleMode(solutionMoves);
        }
        else
        {
            Debug.LogError("ChessManager not found! Cannot play puzzle.");
        }
    }

    private List<string> ParsePGNMoves(string pgnText)
    {
        List<string> moves = new List<string>();

        // Clean PGN: Remove tags [], comments {}, move numbers 1., newlines
        string s = Regex.Replace(pgnText, @"\[.*?\]", "");
        s = Regex.Replace(s, @"\{.*?\}", "");
        s = Regex.Replace(s, @"\d+\.+", "");
        s = s.Replace("\r", " ").Replace("\n", " ");

        // Split by spaces and filter
        string[] parts = s.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in parts)
        {
            string trimmed = p.Trim();
            // Skip result markers
            if (trimmed == "1-0" || trimmed == "0-1" || trimmed == "1/2-1/2" || trimmed == "*") continue;

            if (!string.IsNullOrEmpty(trimmed)) moves.Add(trimmed);
        }
        return moves;
    }
}