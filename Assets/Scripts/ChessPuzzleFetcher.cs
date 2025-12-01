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
                    lastFetchedPGN = puzzle.pgn;
                    lastFetchedFEN = puzzle.fen;

                    ProcessPuzzle(puzzle);
                }
            }
        }
    }

    private void ProcessPuzzle(PuzzleData puzzle)
    {
        if (ChessManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(puzzle.fen))
            {
                ChessManager.Instance.StartGameFromFEN(puzzle.fen);

                string[] parts = puzzle.fen.Split(' ');
                if (parts.Length > 1)
                {
                    bool isWhiteTurn = (parts[1] == "w");
                    int side = isWhiteTurn ? 0 : 1;

                    ChessManager.Instance.SelectSide(side);
                }
            }

            List<string> solutionMoves = ParsePGNMoves(puzzle.pgn);

            ChessManager.Instance.StartPuzzleMode(solutionMoves);
        }
        else
        {
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