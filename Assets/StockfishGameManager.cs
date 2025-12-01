using System.Threading.Tasks;
using UnityEngine;

public class StockfishGameManager : MonoBehaviour
{
    public static StockfishGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    [SerializeField] public int depth = 5;
    [SerializeField] private string gameString;
    [SerializeField] private string reply;

    private StockfishManager sfg;

    private void Start()
    {
        sfg = StockfishManager.Instance;
        NewGame();
    }

    public async void NewGame()
    {
        await sfg.SendCommandAwaitResult("ucinewgame");
        await sfg.SendCommandAwaitResult("isready");
        gameString = "position startpos moves";
    }

    public async void SendMove(string UCImove)
    {
        Debug.Log("wtf " + depth);
        // 1. Construct the string
        gameString += " " + UCImove;

        // 2. Send Position
        sfg.SendUciCommand(gameString);

        // 3. Send Go (Attempt to get bestmove)
        string engineMove = null;
        string bestMoveLine = await sfg.SendGoAndWaitForBestMove("go depth " + depth);

        if (!string.IsNullOrEmpty(bestMoveLine))
        {
            string[] parts = bestMoveLine.Split(' ');
            if (parts.Length > 1)
            {
                engineMove = parts[1];
            }
        }

        // Fallback if Stockfish fails
        if (string.IsNullOrEmpty(engineMove))
        {
            Debug.LogError("Stockfish failed to return a move. Falling back to Random.");
            engineMove = ChessManager.Instance.GetRandomLegalUCIMove();

            // If even random move fails, game is over
            if (string.IsNullOrEmpty(engineMove)) return;
        }

        // 1. Get the starting square of the engine's move (e.g., "e7" from "e7e5")
        string startSquare = engineMove.Substring(0, 2);
        int startIndex = GetSquareIndexFromUCI(startSquare);

        // 2. Get the piece and its color
        // (Assumes GetPieceColor is public in ChessManager)
        int pieceAtOrigin = ChessManager.Instance.tileContent[startIndex];
        int pieceColor = ChessManager.Instance.GetPieceColor(pieceAtOrigin);

        // 3. Get the Human Player's Color
        // (Assumes PlayerSide 0 is White, 1 is Black. Modify if your logic differs)
        int playerColor = (ChessManager.Instance.PlayerSide == 0) ? Piece.White : Piece.Black;

        // 4. CANCEL if the Engine tries to move YOUR piece
        if (pieceColor == playerColor)
        {
            Debug.LogError("Stockfish failed to return a move. Falling back to Random.");
            engineMove = ChessManager.Instance.GetRandomLegalUCIMove();

            // If even random move fails, game is over
            if (string.IsNullOrEmpty(engineMove)) return;
        }

        await Task.Delay(750);

        // Safety check: Ensure the game/object still exists after the delay
        if (this == null) return;

        // 4. Handle the move (Apply to gameString and UI)
        reply = engineMove;
        gameString += " " + engineMove;

        ChessManager.Instance.PlayUCIMove(engineMove);
    }
    private int GetSquareIndexFromUCI(string square)
    {
        int file = square[0] - 'a';       // 'a'->0, 'b'->1 ...
        int rank = square[1] - '1';       // '1'->0, '2'->1 ...

        // Convert to your board's index formula: (7 - rank) * 8 + file
        // (Based on your ReadFENPos logic where index 0 is top-left)
        return (7 - rank) * 8 + file;
    }
}
