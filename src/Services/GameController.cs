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
        private int _moveCount = 1;
        private string _lastWhiteMoveText = "---";
        private string _lastBlackMoveText = "---";



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

                // Unutar _Ready():
                if (DifficultyLabel != null)
                {
                    // Koristimo string.Format da bezbedno ubacimo broj nivoa u prevedeni šablon
                    DifficultyLabel.Text = string.Format(LocalizationService.Instance.Translate("LBL_DIFFICULTY"), 5);
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
            // if (DebugLabel != null)
            // {
            //     string pieceAtSquare = _boardState.GetPieceAt(row, col);
            //     DebugLabel.Text = $"[DEBUG EKRAN]: Kliknuto na polje [{row},{col}] | Figura u memoriji: '{pieceAtSquare}'";
            // }


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
                    MoveLoggerService.Instance.LogMessage("Match", $"PLAYER (White) moved from [{_selectedRow},{_selectedCol}] to [{row},{col}]");

                    // Čist šahovski zapis poteza bez dodataka
                    char fromFile = (char)('a' + _selectedCol);
                    int fromRank = 8 - _selectedRow;
                    char toFile = (char)('a' + col);
                    int toRank = 8 - row;
                    _lastWhiteMoveText = $"{fromFile}{fromRank}{toFile}{toRank}";

                    _selectedRow = -1; _selectedCol = -1;
                    _botFromRow = -1; _botFromCol = -1;
                    _botToRow = -1; _botToCol = -1;

                    RefreshDisplay();
                    UpdateTurnLabelText();
                    UpdateDebugLabelText();
                    CheckForBotTurn();
                }
                else
                {
                    // Greška ide isključivo na disk i u konzolu, ekran ostaje čist
                    MoveLoggerService.Instance.LogMessage("Match", $"ILLEGAL MOVE ATTEMPT: {illegalReason}");

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
            _moveCount = 1;
            _lastWhiteMoveText = "---";
            _lastBlackMoveText = "---";

            if (TurnLabel != null)
            {
                TurnLabel.Text = ""; // Praznimo turnLabel prema zahtevu
            }
            _isGameActive = true;
            // 1. Gasimo stari pozadinski Stockfish proces ako postoji od prošle partije
            _stockfishService?.StopEngine();

            // 2. Čitamo izabranu vrednost direktno sa slajdera (Default je 5 ako igrač nije pipnuo ništa)
            int selectedSkill = 5;
            if (DifficultySlider != null)
            {
                selectedSkill = (int)DifficultySlider.Value;
                // Ažuriramo centralnu konfiguraciju na osnovu vrednosti slajdera
                GameConfig.Instance.UpdateBotDifficulty(selectedSkill);
                DifficultySlider.Editable = false;
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
                RestartButton.Text = LocalizationService.Instance.Translate("BTN_RESTART_GAME");
                RestartButton.Visible = false;
            }

            // 6. Osvežavamo grafiku table i natpis ko je na potezu
            RefreshDisplay();
            UpdateTurnLabelText();
        }


        private void UpdateTurnLabelText()
        {
            if (TurnLabel == null || RestartButton == null) return;

            if (_boardState.IsCheckmated(_boardState.CurrentTurn))
            {
                // 1. Dinamički biramo jezik iz konfiguracije
                bool isEn = GameConfig.Instance.CurrentLanguage == GameConfig.Language.En;

                // 2. Prevodimo reči za pobednika
                string winnerColor = _boardState.CurrentTurn == ChessDotNet.Player.White ? (isEn ? "BLACK" : "CRNI") : (isEn ? "WHITE" : "BELI");
                string endText = isEn ? $"GAME OVER: CHECKMATE! WINNER: {winnerColor}" : $"KRAJ: MAT! POBEDNIK: {winnerColor}";

                TurnLabel.Text = endText;
                RestartButton.Visible = true;
                if (DifficultySlider != null) DifficultySlider.Editable = true;
            }
            else if (_boardState.IsDraw())
            {
                // Prevodimo tekst za nerešen rezultat
                bool isEn = GameConfig.Instance.CurrentLanguage == GameConfig.Language.En;
                TurnLabel.Text = isEn ? "GAME OVER: DRAW!" : "KRAJ: REZULTAT JE NEREŠEN!";

                RestartButton.Visible = true;
                if (DifficultySlider != null) DifficultySlider.Editable = true;
            }
            else
            {
                TurnLabel.Text = ""; // Tokom aktivne igre labela ostaje potpuno prazna
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
            // Uzimamo sirovi, potencijalno kontradiktorni FEN iz biblioteke
            string rawFen = _boardState.GetFen();

            // 🚀 NEPROBOJNI ČISTAČ FEN-A: Cepamo string na segmente preko razmaka
            string[] fenParts = rawFen.Split(' ');
            if (fenParts.Length >= 3)
            {
                // Indeks 2 drži rokadne markere (npr. KQkq).
                // Pošto su i beli i crni već izvršili rokade, nasilno stavljamo crticu '-' 
                // i čistimo lažne markere biblioteke da motor ne bi bacio grešku!
                fenParts[2] = "-";
            }

            // Sastavljamo očišćeni, stoprocentno legalni FEN koji Stockfish bez problema guta
            string currentFen = string.Join(" ", fenParts);

            // Upisujemo očišćeni FEN na disk radi stoprocentne provere u terminalu
            MoveLoggerService.Instance.LogMessage("Stockfish", $"Bot requested move. Clean FEN: {currentFen}");

            // Šaljemo upit motoru sa skraćenim vremenom razmišljanja (50ms) za ljudskiji nivo 0
            string bestMove = _stockfishService.GetBestMove(currentFen);

            if (string.IsNullOrEmpty(bestMove) || bestMove.Length < 4)
            {
                // Ako motor iz nekog nepoznatog razloga ipak zakaže, logujemo krah na disk
                MoveLoggerService.Instance.LogMessage("Stockfish", "[CRITICAL ERROR]: Engine returned an empty or invalid move string even with clean FEN!");
                return;
            }

            // Veštačka, ljudska pauza od 1.2 sekunde da bot ne bi povukao potez u milisekundi
            await ToSignal(GetTree().CreateTimer(1.2f), "timeout");

            // Prevodioci UCI tekstualnog poteza (npr. e7e5) u matrične indekse
            int fromCol = bestMove[0] - 'a';
            int fromRow = '8' - bestMove[1];
            int toCol = bestMove[2] - 'a';
            int toRow = '8' - bestMove[3];

            char? promoChar = bestMove.Length > 4 ? bestMove[4] : null;

            // Upisujemo tačne koordinate u memoriju kontrolera za Lichess žuti okvir poslednjeg poteza
            _botFromRow = fromRow; _botFromCol = fromCol;
            _botToRow = toRow; _botToCol = toCol;

            string dummy;
            // Izvršavamo potez crnog unutar memorije šahovske biblioteke
            bool success = _boardState.TryMakeMove(fromRow, fromCol, toRow, toCol, promoChar, out dummy);

            if (success)
            {
                // 🚀 ZVANIČAN TRIJUMF NA DISKU: Loger uspešno beleži legalan potez bota!
                MoveLoggerService.Instance.LogMessage("Match", $"BOT (Black) executed move: {bestMove}");

                // Upisujemo tačan potez bota i uvećavamo brojač poteza za sledeći krug
                _lastBlackMoveText = bestMove;

                Callable.From(() =>
                {
                    RefreshDisplay();
                    UpdateTurnLabelText();
                    UpdateDebugLabelText(); // Osvežavamo debug ekran nakon što je bot odigrao
                    _moveCount++; // Uvećavamo broj poteza tek kada ceo krug (beli + crni) bude završen
                }).CallDeferred();
            }
            else
            {
                // Ako biblioteka odbaci potez, odmah upisujemo crveni alarm u terminal
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
                // Dinamički biramo prevod za oznake na osnovu jezika u GameConfig-u
                bool isEn = GameConfig.Instance.CurrentLanguage == GameConfig.Language.En;

                string moveWord = isEn ? "Move" : "Potez";
                string whiteWord = isEn ? "White" : "Beli";
                string blackWord = isEn ? "Black" : "Crni";

                DebugLabel.Text = $"{moveWord}: {_moveCount} | {whiteWord}: {_lastWhiteMoveText} | {blackWord}: {_lastBlackMoveText}";
            }
        }

    }
}
