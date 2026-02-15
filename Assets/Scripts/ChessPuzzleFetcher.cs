using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
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

    [Header("Popup")]
    [SerializeField] private GameObject popupObject;

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
        // 1. Show Loading Screen immediately (Alpha 1)
        SetPopupAlpha(1f, "Loading Puzzle...");
        float startTime = Time.time;
        float timeoutDuration = 5f; // Max wait time

        bool puzzleFound = false;
        int attempts = 0;
        int maxAttempts = 5;

        // Loop until we find a new puzzle or run out of attempts/time
        while (!puzzleFound && attempts < maxAttempts)
        {
            // Check timeout
            if (Time.time - startTime > timeoutDuration)
            {
                Debug.LogWarning("Puzzle fetch timed out.");
                break;
            }

            attempts++;

            using (UnityWebRequest webRequest = UnityWebRequest.Get(API_URL))
            {
                webRequest.SetRequestHeader("User-Agent", "UnityChessStudentProject/1.0");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error fetching puzzle: {webRequest.error}");
                    SetPopupAlpha(1f, "Connection Error");
                    yield return new WaitForSeconds(2f);
                    break;
                }
                else
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    PuzzleData puzzle = JsonUtility.FromJson<PuzzleData>(jsonResponse);

                    if (puzzle != null)
                    {
                        // Check for duplicate
                        if (!string.IsNullOrEmpty(lastFetchedFEN) && puzzle.fen == lastFetchedFEN)
                        {
                            Debug.Log($"Fetched duplicate puzzle (Attempt {attempts}/{maxAttempts}). Retrying...");
                            continue;
                        }

                        // Success!
                        lastFetchedPGN = puzzle.pgn;
                        lastFetchedFEN = puzzle.fen;

                        ProcessPuzzle(puzzle);
                        puzzleFound = true;
                    }
                }
            }
        }

        // 2. Handle failure to find NEW puzzle
        if (!puzzleFound)
        {
            SetPopupAlpha(1f, "No new puzzles found.");
            yield return new WaitForSeconds(2f); // Show message for 2s
        }
        else
        {
            // 3. Enforce minimum wait (0.75s) only if successful
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < 0.75f)
            {
                yield return new WaitForSeconds(0.75f - elapsedTime);
            }
        }

        // 4. Fade out
        yield return FadePopup(1f, 0f, 0.25f);
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
    }

    private List<string> ParsePGNMoves(string pgnText)
    {
        List<string> moves = new List<string>();

        string s = Regex.Replace(pgnText, @"\[.*?\]", "");
        s = Regex.Replace(s, @"\{.*?\}", "");
        s = Regex.Replace(s, @"\d+\.+", "");
        s = s.Replace("\r", " ").Replace("\n", " ");

        string[] parts = s.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in parts)
        {
            string trimmed = p.Trim();
            if (trimmed == "1-0" || trimmed == "0-1" || trimmed == "1/2-1/2" || trimmed == "*") continue;

            if (!string.IsNullOrEmpty(trimmed)) moves.Add(trimmed);
        }
        return moves;
    }

    // --- Helper Methods for Popup ---

    private void SetPopupAlpha(float alpha, string message = "")
    {
        if (popupObject == null) return;

        var group = popupObject.GetComponent<CanvasGroup>();
        var text = popupObject.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null && !string.IsNullOrEmpty(message))
            text.text = message;

        if (group != null)
        {
            group.alpha = alpha;
            group.blocksRaycasts = (alpha > 0);
        }
        else
        {
            popupObject.SetActive(alpha > 0);
        }
    }

    private IEnumerator FadePopup(float startAlpha, float endAlpha, float duration)
    {
        if (popupObject == null) yield break;

        var group = popupObject.GetComponent<CanvasGroup>();
        if (group == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        group.alpha = endAlpha;
        group.blocksRaycasts = (endAlpha > 0);
    }
}