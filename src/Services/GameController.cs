using Godot;
using System;
using ChessPuzzles2d.Core;
using ChessPuzzles2d.Views;
using System.Collections.Generic;

namespace ChessPuzzles2d.Services
{
    public partial class GameController : Node2D
    {
        [Export] public GridContainer BoardGrid { get; set; }
        [Export] public Texture2D ChessPiecesAtlas { get; set; }
        [Export] public Label TurnLabel { get; set; }
        [Export] public Button RestartButton { get; set; }
        [Export] public Label DebugLabel { get; set; }
        [Export] public HSlider DifficultySlider { get; set; }
        [Export] public Label DifficultyLabel { get; set; }

        private ChessBoardState _boardState;
        private PieceAtlasService _atlasService;
        private WindowManager _windowManager;
        private InputManager _inputManager;
        private BoardView _boardView;
        private PromotionUiView _promoView;
        private StockfishService _stockfishService;

        private int _selectedRow = -1;
        private int _selectedCol = -1;

        private int _botFromRow = -1, _botFromCol = -1;
        private int _botToRow = -1, _botToCol = -1;

        int selectedSkill = 5;

        private bool _isGameActive = false;
        private bool _isAnimating = false; // NOVO: blokira input dok se figura vizuelno pomera

        private int _moveCount = 1;
        private string _lastWhiteMoveText = "---";
        private string _lastBlackMoveText = "---";

        private float _currentTileSize = 81f;

        private bool _isWaitingForPromotion = false;
        private int _promoFromRow, _promoFromCol, _promoToRow, _promoToCol;

        public override void _Ready()
        {
            string OSName = Godot.OS.GetName();
            GD.Print($"=== TEST - OPERATIVNI SISTEM JE: [{OSName}] ===");

            if (BoardGrid == null || ChessPiecesAtlas == null || TurnLabel == null || RestartButton == null)
            {
                GD.PrintErr("[KRITIČNA GREŠKA]: Nedostaju reference u Inspectoru!");
                return;
            }

            _boardState = new ChessBoardState();
            _atlasService = new PieceAtlasService(ChessPiecesAtlas);
            _boardView = new BoardView(BoardGrid, _atlasService);
            _promoView = new PromotionUiView(BoardGrid, _atlasService);
            _windowManager = new WindowManager(GetViewport());
            _inputManager = new InputManager(BoardGrid);

            _inputManager.OnSquareSelected += (row, col) => HandleSquareSelection(row, col);

            if (DifficultySlider != null)
            {
                DifficultySlider.MinValue = 0;
                DifficultySlider.MaxValue = 20;
                DifficultySlider.Step = 1;
                DifficultySlider.Value = 5;
                DifficultySlider.Editable = true;

                DifficultySlider.ValueChanged += OnSliderValueChanged;

                if (DifficultyLabel != null)
                {
                    DifficultyLabel.Text = string.Format(LocalizationService.Instance.Translate("LBL_DIFFICULTY"), 5);
                }
            }

            _windowManager.OnBoardResize += (tileSize) => OnBoardResize(tileSize);
            RestartButton.Pressed += () => OnRestartButtonPressed();
            _windowManager.TriggerInitialResize();

            UpdateTurnLabelText();

            _stockfishService = new StockfishService();
            _stockfishService.StartEngine(5);

            if (RestartButton != null)
            {
                RestartButton.Text = LocalizationService.Instance.Translate("BTN_STORE");
                RestartButton.Visible = true;
            }
        }

        public override void _ExitTree()
        {
            if (_windowManager != null) _windowManager.OnBoardResize -= OnBoardResize;
            _stockfishService?.StopEngine();
        }

        public override void _Input(InputEvent @event)
        {
            if (_isGameActive)
            {
                _inputManager?.HandleInput(@event);
            }
        }

        private void OnBoardResize(float tileSize)
        {
            _currentTileSize = tileSize;
            RefreshDisplay();
        }

        private void HandleSquareSelection(int row, int col)
        {
            // NOVO: dok se figura animira, ignorisi klikove potpuno
            if (_isAnimating) return;

            if (_boardState.IsCheckmated(_boardState.CurrentTurn) || _boardState.IsDraw()) return;

            if (_isWaitingForPromotion)
            {
                HandlePromoInput(row, col);
                return;
            }

            string clickedPiece = _boardState.GetPieceAt(row, col);

            if (_selectedRow != -1 && _selectedCol != -1)
            {
                if (_selectedRow == row && _selectedCol == col)
                {
                    _selectedRow = -1; _selectedCol = -1;
                    RefreshDisplay(); return;
                }

                int fromRow = _selectedRow, fromCol = _selectedCol;

                string illegalReason;
                bool moveSuccessful = _boardState.TryMakeMove(_selectedRow, _selectedCol, row, col, null, out illegalReason);

                if (!moveSuccessful && illegalReason == "PROMOCIJA")
                {
                    _isWaitingForPromotion = true;
                    _promoFromRow = _selectedRow; _promoFromCol = _selectedCol;
                    _promoToRow = row; _promoToCol = col;
                    _selectedRow = -1; _selectedCol = -1;
                    RefreshDisplay();
                    if (TurnLabel != null) TurnLabel.Text = "IZABERITE FIGURU NA TABLI!";
                    return;
                }

                if (moveSuccessful)
                {
                    MoveLoggerService.Instance.LogMessage("Match", $"PLAYER (White) moved from [{fromRow},{fromCol}] to [{row},{col}]");

                    char fromFile = (char)('a' + fromCol);
                    int fromRank = 8 - fromRow;
                    char toFile = (char)('a' + col);
                    int toRank = 8 - row;
                    _lastWhiteMoveText = $"{fromFile}{fromRank}{toFile}{toRank}";

                    _selectedRow = -1; _selectedCol = -1;
                    _botFromRow = -1; _botFromCol = -1;
                    _botToRow = -1; _botToCol = -1;

                    // NOVO: animiraj umesto trenutnog RefreshDisplay()-a
                    _isAnimating = true;
                    _boardView.AnimateMove(fromRow, fromCol, row, col, _currentTileSize, () =>
                    {
                        _isAnimating = false;
                        RefreshDisplay();
                        UpdateTurnLabelText();
                        UpdateDebugLabelText();
                        CheckForBotTurn();
                    });
                }
                else
                {
                    MoveLoggerService.Instance.LogMessage("Match", $"ILLEGAL MOVE ATTEMPT: {illegalReason}");
                    _selectedRow = -1; _selectedCol = -1;
                    RefreshDisplay();
                }
            }
            else
            {
                if (clickedPiece != ".")
                {
                    bool isWhitePiece = char.IsUpper(clickedPiece[0]);
                    bool isWhiteTurn = _boardState.CurrentTurn == ChessDotNet.Player.White;

                    if ((isWhiteTurn && !isWhitePiece) || (!isWhiteTurn && isWhitePiece))
                    {
                        MoveLoggerService.Instance.LogMessage("Match", "SELECTION ERROR: Not player's turn or wrong piece color.");
                        return;
                    }

                    _selectedRow = row; _selectedCol = col;
                    RefreshDisplay();
                }
            }
        }

        private void HandlePromoInput(int row, int col)
        {
            if (col != _promoToCol) return;

            char? selectedChoice = null;
            int relativeRow = _promoToRow == 0 ? row : (7 - row);
            switch (relativeRow)
            {
                case 0: selectedChoice = 'Q'; break;
                case 1: selectedChoice = 'N'; break;
                case 2: selectedChoice = 'R'; break;
                case 3: selectedChoice = 'B'; break;
            }

            if (selectedChoice != null)
            {
                int fromRow = _promoFromRow, fromCol = _promoFromCol;
                int toRow = _promoToRow, toCol = _promoToCol;

                string dummy;
                _boardState.TryMakeMove(_promoFromRow, _promoFromCol, _promoToRow, _promoToCol, selectedChoice, out dummy);
                _isWaitingForPromotion = false;

                // NOVO: animiraj promociju kao normalan potez (pesak "sklizne" pa se
                // na sledecem Render()-u prikaze kao promovisana figura)
                _isAnimating = true;
                _boardView.AnimateMove(fromRow, fromCol, toRow, toCol, _currentTileSize, () =>
                {
                    _isAnimating = false;
                    RefreshDisplay();
                    UpdateTurnLabelText();
                    CheckForBotTurn();
                });
            }
        }

        private void OnRestartButtonPressed()
        {
            _moveCount = 1;
            _lastWhiteMoveText = "---";
            _lastBlackMoveText = "---";

            if (TurnLabel != null)
            {
                TurnLabel.Text = "";
            }

            _isGameActive = true;
            _isAnimating = false;

            _stockfishService?.StopEngine();

            int selectedSkill = 5;
            if (DifficultySlider != null)
            {
                selectedSkill = (int)DifficultySlider.Value;
                GameConfig.Instance.UpdateBotDifficulty(selectedSkill);
                DifficultySlider.Editable = false;
            }

            _boardState = new ChessBoardState();
            _stockfishService = new StockfishService();
            _stockfishService.StartEngine(selectedSkill);

            _selectedRow = -1; _selectedCol = -1; _isWaitingForPromotion = false;
            _botFromRow = -1; _botFromCol = -1; _botToRow = -1; _botToCol = -1;

            if (RestartButton != null)
            {
                RestartButton.Text = LocalizationService.Instance.Translate("BTN_RESTART_GAME");
                RestartButton.Visible = false;
            }

            RefreshDisplay();
            UpdateTurnLabelText();
        }

        private void UpdateTurnLabelText()
        {
            if (TurnLabel == null || RestartButton == null) return;

            if (_boardState.IsCheckmated(_boardState.CurrentTurn))
            {
                bool isEn = GameConfig.Instance.CurrentLanguage == GameConfig.Language.En;
                string winnerColor = _boardState.CurrentTurn == ChessDotNet.Player.White ? (isEn ? "BLACK" : "CRNI") : (isEn ? "WHITE" : "BELI");
                string endText = isEn ? $"GAME OVER: CHECKMATE! WINNER: {winnerColor}" : $"KRAJ: MAT! POBEDNIK: {winnerColor}";
                TurnLabel.Text = endText;
                RestartButton.Visible = true;
                if (DifficultySlider != null) DifficultySlider.Editable = true;
            }
            else if (_boardState.IsDraw())
            {
                bool isEn = GameConfig.Instance.CurrentLanguage == GameConfig.Language.En;
                TurnLabel.Text = isEn ? "GAME OVER: DRAW!" : "KRAJ: REZULTAT JE NEREŠEN!";
                RestartButton.Visible = true;
                if (DifficultySlider != null) DifficultySlider.Editable = true;
            }
            else
            {
                TurnLabel.Text = "";
            }
        }

        private void RefreshDisplay()
        {
            if (_isWaitingForPromotion)
            {
                _promoView.RenderOverlay(_boardState, _promoToRow, _promoToCol, _promoFromRow, _promoFromCol, _currentTileSize);
            }
            else
            {
                _boardView.Render(_boardState, _selectedRow, _selectedCol, _currentTileSize, _botFromRow, _botFromCol, _botToRow, _botToCol);
            }

            BoardGrid.QueueRedraw();
        }

        private void CheckForBotTurn()
        {
            if (_boardState.CurrentTurn == ChessDotNet.Player.Black && !_boardState.IsCheckmated(ChessDotNet.Player.Black) && !_boardState.IsDraw())
            {
                Callable.From(RunBotMove).CallDeferred();
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

            string bestMove = _stockfishService.GetBestMove(currentFen);

            if (string.IsNullOrEmpty(bestMove) || bestMove.Length < 4)
            {
                MoveLoggerService.Instance.LogMessage("Stockfish", "[CRITICAL ERROR]: Engine returned an empty or invalid move string even with clean FEN!");
                return;
            }

            await ToSignal(GetTree().CreateTimer(1.2f), "timeout");

            int fromCol = bestMove[0] - 'a';
            int fromRow = '8' - bestMove[1];
            int toCol = bestMove[2] - 'a';
            int toRow = '8' - bestMove[3];
            char? promoChar = bestMove.Length > 4 ? bestMove[4] : null;

            string dummy;
            bool success = _boardState.TryMakeMove(fromRow, fromCol, toRow, toCol, promoChar, out dummy);

            if (success)
            {
                MoveLoggerService.Instance.LogMessage("Match", $"BOT (Black) executed move: {bestMove}");
                _lastBlackMoveText = bestMove;

                // NOVO: postavi highlight koordinate PRE animacije, da se zuti trag
                // pojavi tacno kad figura vizuelno stigne (u finalnom RefreshDisplay-u)
                _botFromRow = fromRow; _botFromCol = fromCol;
                _botToRow = toRow; _botToCol = toCol;

                Callable.From(() =>
                {
                    _isAnimating = true;
                    _boardView.AnimateMove(fromRow, fromCol, toRow, toCol, _currentTileSize, () =>
                    {
                        _isAnimating = false;
                        RefreshDisplay();
                        UpdateTurnLabelText();
                        UpdateDebugLabelText();
                        _moveCount++;
                    });
                }).CallDeferred();
            }
            else
            {
                MoveLoggerService.Instance.LogMessage("Match", $"[CRITICAL ERROR]: Bot attempted illegal move according to ChessDotNet: {bestMove}");
            }
        }

        private void OnSliderValueChanged(double value)
        {
            if (DifficultyLabel != null)
            {
                DifficultyLabel.Text = string.Format(LocalizationService.Instance.Translate("LBL_DIFFICULTY"), (int)value);
            }
        }

        private void UpdateDebugLabelText()
        {
            if (DebugLabel != null)
            {
                bool isEn = GameConfig.Instance.CurrentLanguage == GameConfig.Language.En;
                string moveWord = isEn ? "Move" : "Potez";
                string whiteWord = isEn ? "White" : "Beli";
                string blackWord = isEn ? "Black" : "Crni";
                DebugLabel.Text = $"{moveWord}: {_moveCount} | {whiteWord}: {_lastWhiteMoveText} | {blackWord}: {_lastBlackMoveText}";
            }
        }
    }
}