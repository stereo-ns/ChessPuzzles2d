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
        private bool _isGameActive = false; // Na samom startu igra miruje i zaključana je!


        private float _currentTileSize = 81f;

        private bool _isWaitingForPromotion = false;
        private int _promoFromRow, _promoFromCol, _promoToRow, _promoToCol;

        public override void _Ready()
        {
            GD.Print("=== ChessPuzzles2d: Inicijalizacija sistema ===");

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

            // LAMBDA VEZA ZA MOBILNI BILD
            _inputManager.OnSquareSelected += (row, col) => HandleSquareSelection(row, col);
            // Inicijalizujemo slajder na startu igre
            if (DifficultySlider != null)
            {
                DifficultySlider.MinValue = 0;
                DifficultySlider.MaxValue = 20;
                DifficultySlider.Step = 1;
                DifficultySlider.Value = 5;
                DifficultySlider.Editable = true;

                // Kačimo čist delegat za promenu vrednosti
                DifficultySlider.ValueChanged += OnSliderValueChanged;

                // Postavljamo početni tekst čim se igra upali
                if (DifficultyLabel != null)
                {
                    DifficultyLabel.Text = "Izabrana težina: Nivo 5";
                }
            }


            _windowManager.OnBoardResize += (tileSize) => OnBoardResize(tileSize);
            RestartButton.Pressed += () => OnRestartButtonPressed();

            _windowManager.TriggerInitialResize();
            UpdateTurnLabelText(); // Odmah ispisuje ko prvi igra
            _stockfishService = new StockfishService();
            _stockfishService.StartEngine(5);
            if (RestartButton != null)
            {
                RestartButton.Text = "KRENI IGRU"; // Menjamo tekst iz Restart u New Game mod
                RestartButton.Visible = true;      // Prisno palimo vidljivost na startu ekrana!
            }
        }

        public override void _ExitTree()
        {
            if (_windowManager != null) _windowManager.OnBoardResize -= OnBoardResize;
            _stockfishService?.StopEngine();
        }

        public override void _Input(InputEvent @event)
        {
            // ŠTIT: Ako igrač nije kliknuo na početak, _isGameActive je false i tabla potpuno ignoriše dodire!
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
            if (DebugLabel != null)
            {
                string pieceAtSquare = _boardState.GetPieceAt(row, col);
                DebugLabel.Text = $"[DEBUG EKRAN]: Kliknuto na polje [{row},{col}] | Figura u memoriji: '{pieceAtSquare}'";
            }

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
                    // 🚀 LOGOVANJE POTEZA IGRAČA:
                    MoveLoggerService.Instance.LogMessage("Match", $"PLAYER (White) moved from [{_selectedRow},{_selectedCol}] to [{row},{col}]");

                    _selectedRow = -1; _selectedCol = -1;
                    _botFromRow = -1; _botFromCol = -1;
                    _botToRow = -1; _botToCol = -1;

                    RefreshDisplay();
                    UpdateTurnLabelText();
                    CheckForBotTurn();
                }
                else
                {
                    if (TurnLabel != null) TurnLabel.Text = $"GREŠKA: {illegalReason.ToUpper()}";
                    _selectedRow = -1; _selectedCol = -1;
                    RefreshDisplay();
                }
            }
            else
            {
                if (clickedPiece != ".")
                {
                    // POPRAVKA: Uzimamo prvi karakter stringa clickedPiece[0] da dobijemo sirovi char!
                    bool isWhitePiece = char.IsUpper(clickedPiece[0]);
                    bool isWhiteTurn = _boardState.CurrentTurn == ChessDotNet.Player.White;

                    if ((isWhiteTurn && !isWhitePiece) || (!isWhiteTurn && isWhitePiece))
                    {
                        if (TurnLabel != null) TurnLabel.Text = "GREŠKA: NIJE VAŠ RED!";
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
                string dummy;
                _boardState.TryMakeMove(_promoFromRow, _promoFromCol, _promoToRow, _promoToCol, selectedChoice, out dummy);
                _isWaitingForPromotion = false;
                RefreshDisplay();
                UpdateTurnLabelText();
                CheckForBotTurn();
            }
        }

        private void OnRestartButtonPressed()
        {
            _isGameActive = true;
            // 1. Gasimo stari pozadinski Stockfish proces ako postoji od prošle partije
            _stockfishService?.StopEngine();

            // 2. Čitamo izabranu vrednost direktno sa slajdera (Default je 5 ako igrač nije pipnuo ništa)
            int selectedSkill = 5;
            if (DifficultySlider != null)
            {
                selectedSkill = (int)DifficultySlider.Value;
                DifficultySlider.Editable = false; // Zaključavamo slajder tokom partije da nema varanja!
                // DifficultySlider.ReleaseFocus();
                // DifficultySlider.FocusMode = Control.FocusModeEnum.None;
            }

            // 3. Budimo novu šahovsku tablu u memoriji i palimo bota sa tačnim nivoom (0-20)
            _boardState = new ChessBoardState();
            _stockfishService = new StockfishService();
            _stockfishService.StartEngine(selectedSkill);

            // 4. Resetujemo sve selekcije i čistimo plavi Lichess trag botovog poteza sa prošle partije
            _selectedRow = -1; _selectedCol = -1; _isWaitingForPromotion = false;
            _botFromRow = -1; _botFromCol = -1; _botToRow = -1; _botToCol = -1;

            // 5. Menjamo natpis na dugmetu u "RESTART" i sakrivamo ga dok se partija zvanično ne završi
            if (RestartButton != null)
            {
                RestartButton.Text = "RESTART";
                RestartButton.Visible = false;
            }

            // 6. Osvežavamo grafiku table i natpis ko je na potezu
            RefreshDisplay();
            UpdateTurnLabelText();
        }


        private void UpdateTurnLabelText()
        {
            if (TurnLabel == null || RestartButton == null) return;

            string koIgra = _boardState.CurrentTurn == ChessDotNet.Player.White ? "BELI" : "CRNI";

            if (_boardState.IsCheckmated(_boardState.CurrentTurn))
            {
                string winner = _boardState.CurrentTurn == ChessDotNet.Player.White ? "CRNI" : "BELI";
                TurnLabel.Text = $"KRAJ: MAT! POBEDNIK JE {winner}!";
                RestartButton.Visible = true;
            }
            else if (_boardState.IsDraw())
            {
                TurnLabel.Text = "KRAJ: REZULTAT JE NEREŠEN!";
                RestartButton.Visible = true;
            }
            // PROVERA ŠAHA: Pozivamo stabilnu metodu iz tvog omotača
            else if (_boardState.IsInCheck(_boardState.CurrentTurn))
            {
                TurnLabel.Text = $"ŠAH! NA POTEZU: {koIgra}";
            }
            else
            {
                TurnLabel.Text = $"NA POTEZU: {koIgra}";
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
                // FIKS: Brišemo 'validMoves' sa kraja jer BoardView sve računa sam unutra!
                // _boardView.Render(_boardState, _selectedRow, _selectedCol, _currentTileSize);
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
            string currentFen = _boardState.GetFen();
            MoveLoggerService.Instance.LogMessage("Stockfish", $"Bot requested move. Current FEN: {currentFen}");

            string bestMove = _stockfishService.GetBestMove(currentFen, 300);

            if (string.IsNullOrEmpty(bestMove) || bestMove.Length < 4)
            {
                // 🚀 LOGOVANJE GREŠKE MOTORA (Ako vrati prazno):
                MoveLoggerService.Instance.LogMessage("Stockfish", "[CRITICAL ERROR]: Engine returned an empty or invalid move string!");
                return;
            }

            // 🚀 VEŠTAČKA PAUZA: Čekamo 1.2 sekunde da bot odglumi ljudsko razmišljanje
            await ToSignal(GetTree().CreateTimer(1.2f), "timeout");

            int fromCol = bestMove[0] - 'a';
            int fromRow = '8' - bestMove[1];
            int toCol = bestMove[2] - 'a';
            int toRow = '8' - bestMove[3];

            char? promoChar = bestMove.Length > 4 ? bestMove[4] : null;

            // Beležimo koordinate za plavi Lichess trag
            _botFromRow = fromRow; _botFromCol = fromCol;
            _botToRow = toRow; _botToCol = toCol;

            string dummy;
            bool success = _boardState.TryMakeMove(fromRow, fromCol, toRow, toCol, promoChar, out dummy);

            if (success)
            {
                MoveLoggerService.Instance.LogMessage("Match", $"BOT (Black) executed move: {bestMove} (Translated to: [{fromRow},{fromCol}] -> [{toRow},{toCol}])");
                Callable.From(() =>
                {
                    RefreshDisplay();
                    UpdateTurnLabelText();
                }).CallDeferred();
            }
            else
            {
                // 🚀 LOGOVANJE NELEGALNOG POTEZA BOTA:
                MoveLoggerService.Instance.LogMessage("Match", $"[CRITICAL ERROR]: Bot attempted illegal move according to ChessDotNet: {bestMove}");
            }
        }
        private void OnSliderValueChanged(double value)
        {
            if (DifficultyLabel != null)
            {
                DifficultyLabel.Text = $"Izabrana težina: Nivo {value}";
            }
        }

    }
}
