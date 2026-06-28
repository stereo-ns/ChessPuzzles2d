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

        private ChessBoardState _boardState;
        private PieceAtlasService _atlasService;
        private WindowManager _windowManager;
        private InputManager _inputManager;

        private BoardView _boardView;
        private PromotionUiView _promoView;

        private int _selectedRow = -1;
        private int _selectedCol = -1;
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


            _windowManager.OnBoardResize += (tileSize) => OnBoardResize(tileSize);
            RestartButton.Pressed += () => OnRestartButtonPressed();

            _windowManager.TriggerInitialResize();
            UpdateTurnLabelText(); // Odmah ispisuje ko prvi igra
        }

        public override void _ExitTree()
        {
            if (_windowManager != null) _windowManager.OnBoardResize -= OnBoardResize;
        }

        public override void _Input(InputEvent @event)
        {
            _inputManager?.HandleInput(@event);
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
                    _selectedRow = -1; _selectedCol = -1;
                    RefreshDisplay();
                    UpdateTurnLabelText(); // AKO JE USPEH: Briše staru grešku i ispisuje novog igrača!
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
            }
        }

        private void OnRestartButtonPressed()
        {
            _boardState = new ChessBoardState();
            _selectedRow = -1; _selectedCol = -1; _isWaitingForPromotion = false;
            RestartButton.Visible = false;
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
                _boardView.Render(_boardState, _selectedRow, _selectedCol, _currentTileSize);
            }
            BoardGrid.QueueRedraw();
        }


    }
}
