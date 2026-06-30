using Godot;
using System;
using System.Collections.Generic;
using ChessPuzzles2d.Core;
using ChessPuzzles2d.Views;
using ChessPuzzles2d.Services;

namespace ChessPuzzles2d.Services
{
    public partial class GameController : Node
    {
        [Export] public Label TurnLabel;
        [Export] public TextEdit DebugText;
        [Export] public GridContainer BoardGrid;
        [Export] public Button PlayButton;

        private ChessBoardState _boardState;
        private BoardView _boardView;
        private PieceAtlasService _atlasService;
        private InputManager _inputManager;
        private StockfishService _stockfishService;

        private int _selectedRow = -1;
        private int _selectedCol = -1;
        private bool _isGameActive = false;

        private int _botFromRow = -1;
        private int _botFromCol = -1;
        private int _botToRow = -1;
        private int _botToCol = -1;

        private int _promoFromRow = -1;
        private int _promoFromCol = -1;
        private int _promoToRow = -1;
        private int _promoToCol = -1;
        private bool _isPromotionActive = false;

        private int _botSkillLevel = 0;

        // 🚀 LISTA ZA PRAĆENJE PGN ISTORIJE POTEZA NA EKRANU
        private List<string> _moveHistory = new List<string>();

        public override void _Ready()
        {
            GD.Print("=== ChessPuzzles2d: Inicijalizacija sistema ===");

            _atlasService = new PieceAtlasService();
            _boardView = new BoardView(BoardGrid, _atlasService);
            _boardState = new ChessBoardState();
            _inputManager = new InputManager(BoardGrid, this);

            _stockfishService = new StockfishService();
            _stockfishService.StartEngine(_botSkillLevel);

            // Ako postoji payload iz prodavnice, automatski pokrećemo meč
            var payload = SceneManager.Instance.GetPayload();
            if (payload != null)
            {
                OnPlayButtonPressed();
            }
            else
            {
                RefreshDisplay();
                UpdateTurnLabelText();
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (_boardState == null || _boardState.CurrentTurn == ChessDotNet.Player.Black)
            {
                return;
            }

            if (_isGameActive && !_isPromotionActive)
            {
                _inputManager?.HandleInput(@event);
            }
        }
        public void OnPlayButtonPressed()
        {
            if (_isGameActive) return;

            _isGameActive = true;
            _moveHistory.Clear();
            GameConfig.Instance.ResetMoveCount();

            // 🚀 FIKS ZA ANDROID: Dugme se trenutno sakriva i gasi na klik!
            if (PlayButton != null)
            {
                PlayButton.Visible = false;
                PlayButton.Disabled = true;
            }

            MoveLoggerService.Instance.LogMessage("Match", "Partija uspešno startovana i aktivirana.");

            RefreshDisplay();
            UpdateTurnLabelText();
            CheckForBotTurn();
        }

        public void HandleSquareSelection(int row, int col)
        {
            if (!_isGameActive || _isPromotionActive) return;

            if (_selectedRow == -1 && _selectedCol == -1)
            {
                string piece = _boardState.GetPieceAt(row, col);
                bool isWhitePiece = piece != "." && piece == piece.ToUpper();

                if (isWhitePiece)
                {
                    _selectedRow = row;
                    _selectedCol = col;
                    RefreshDisplay();
                }
            }
            else
            {
                if (row == _selectedRow && col == _selectedCol)
                {
                    _selectedRow = -1;
                    _selectedCol = -1;
                    RefreshDisplay();
                    return;
                }

                string illegalReason;
                string moveNotation = GetMoveNotation(_selectedRow, _selectedCol, row, col);

                bool moveSuccessful = _boardState.TryMakeMove(_selectedRow, _selectedCol, row, col, null, out illegalReason);

                if (moveSuccessful)
                {
                    // Beležimo potez belog u našu PGN listu za ekran
                    _moveHistory.Add(moveNotation);

                    MoveLoggerService.Instance.LogMessage("Match", $"PLAYER (White) moved: {moveNotation}");

                    _selectedRow = -1; _selectedCol = -1;
                    _botFromRow = -1; _botFromCol = -1;
                    _botToRow = -1; _botToCol = -1;

                    RefreshDisplay();
                    UpdateTurnLabelText();
                    CheckForBotTurn();
                }
                else if (illegalReason == "PROMOCIJA")
                {
                    _isPromotionActive = true;
                    _promoFromRow = _selectedRow; _promoFromCol = _selectedCol;
                    _promoToRow = row; _promoToCol = col;
                    _selectedRow = -1; _selectedCol = -1;

                    MoveLoggerService.Instance.LogMessage("Match", "Aktivirano stanje promocije pešaka.");
                }
                else
                {
                    _selectedRow = -1; _selectedCol = -1;
                    RefreshDisplay();
                }
            }
        }

        private async void RunBotMove()
        {
            string rawFen = _boardState.GetFen();
            string[] fenParts = rawFen.Split(' ');
            if (fenParts.Length >= 3)
            {
                fenParts[2] = "-";
            }
            string currentFen = string.Join(" ", fenParts);

            MoveLoggerService.Instance.LogMessage("Stockfish", $"Bot requested move. Clean FEN: {currentFen}");

            string bestMove = _stockfishService.GetBestMove(currentFen, 50);

            if (string.IsNullOrEmpty(bestMove) || bestMove.Length < 4)
            {
                MoveLoggerService.Instance.LogMessage("Stockfish", "[CRITICAL ERROR]: Bot engine failed to return a move!");
                return;
            }

            await ToSignal(GetTree().CreateTimer(1.2f), "timeout");

            int fromCol = bestMove[0] - 'a';
            int fromRow = '8' - bestMove[1];
            int toCol = bestMove[2] - 'a';
            int toRow = '8' - bestMove[3];

            char? promoChar = bestMove.Length > 4 ? bestMove[4] : null;

            _botFromRow = fromRow; _botFromCol = fromCol;
            _botToRow = toRow; _botToCol = toCol;

            string dummy;
            bool success = _boardState.TryMakeMove(fromRow, fromCol, toRow, toCol, promoChar, out dummy);

            if (success)
            {
                // Beležimo potez crnog u našu PGN listu za ekran
                _moveHistory.Add(bestMove.Substring(0, 4));

                int fullmoveNumber = GameConfig.Instance.MoveCount;
                MoveLoggerService.Instance.LogMessage("Match", $"Potez br. {fullmoveNumber} završen. BOT moved: {bestMove}");

                // Krug je kompletan, uvećavamo brojač za sledeći potez belog
                GameConfig.Instance.IncrementMove();

                Callable.From(() =>
                {
                    RefreshDisplay();
                    UpdateTurnLabelText();
                }).CallDeferred();
            }
            else
            {
                MoveLoggerService.Instance.LogMessage("Match", $"[CRITICAL ERROR]: Bot illegal move attempt: {bestMove}");
            }
        }
        private void CheckForBotTurn()
        {
            if (_isGameActive && _boardState.CurrentTurn == ChessDotNet.Player.Black)
            {
                RunBotMove();
            }
        }

        public void RefreshDisplay()
        {
            _boardView?.Render(_boardState, _selectedRow, _selectedCol, 100f, _botFromRow, _botFromCol, _botToRow, _botToCol);
            RenderMoveHistoryFeed(); // Osvežavamo PGN ispis na korisničkom ekranu
        }

        private void UpdateTurnLabelText()
        {
            // 🚀 PROČIŠĆAVANJE: Labela "Na potezu" ostaje potpuno prazna i čista po tvom zahtevu!
            if (TurnLabel != null)
            {
                TurnLabel.Text = "";
            }
        }

        // 🚀 METODA KOJA FORMIRA I CRTA STRUKTURNU PGN ISTORIJU POTEZA NA EKRANU
        private void RenderMoveHistoryFeed()
        {
            if (DebugText == null) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int pairCount = 1;

            for (int i = 0; i < _moveHistory.Count; i += 2)
            {
                string whiteMove = _moveHistory[i];
                string blackMove = (i + 1 < _moveHistory.Count) ? _moveHistory[i + 1] : "";

                sb.Append($"{pairCount}. {whiteMove} {blackMove}\n");
                pairCount++;
            }

            DebugText.Text = sb.ToString();
        }

        private string GetMoveNotation(int fR, int fC, int tR, int tC)
        {
            char fFile = (char)('a' + fC);
            int fRank = 8 - fR;
            char tFile = (char)('a' + tC);
            int tRank = 8 - tR;
            return $"{fFile}{fRank}{tFile}{tRank}";
        }

        public void OnRestartButtonPressed()
        {
            GameConfig.Instance.ResetMoveCount();
            _moveHistory.Clear();
            _isGameActive = false;
            _isPromotionActive = false;
            _selectedRow = -1; _selectedCol = -1;
            _botFromRow = -1; _botFromCol = -1;
            _botToRow = -1; _botToCol = -1;

            // Vraćamo Play dugme u vidljivo i aktivno stanje za novi početak meča
            if (PlayButton != null)
            {
                PlayButton.Visible = true;
                PlayButton.Disabled = false;
            }

            _boardState = new ChessBoardState();
            MoveLoggerService.Instance.LogMessage("Match", "Sistem restartovan. Čekam klik na Play dugme.");

            RefreshDisplay();
            UpdateTurnLabelText();
        }
    }
}
