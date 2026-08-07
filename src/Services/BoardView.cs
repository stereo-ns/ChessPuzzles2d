using Godot;
using System.Collections.Generic;
using ChessPuzzles2d.Core;
using ChessPuzzles2d.Services;
using ChessDotNet;

namespace ChessPuzzles2d.Views
{
    // View klasa: crta 8x8 pozadinu/legal-dot sloj kroz GridContainer,
    // i odvojeno drzi PiecesLayer overlay sloj za figure koje mogu slobodno
    // da se pozicioniraju/animiraju bez GridContainer auto-layout-a.
    public class BoardView
    {
        private readonly GridContainer _grid;
        private readonly PieceAtlasService _atlas;
        private Control _piecesLayer;

        public BoardView(GridContainer grid, PieceAtlasService atlas)
        {
            _grid = grid;
            _atlas = atlas;
            EnsurePiecesLayer();

            // TopLevel = true -> GridContainer ovaj cvor izuzima iz svog auto-layout-a
            // (potvrdjeno Godot 4 ponasanje), pa mozemo slobodno da mu postavljamo
            // Position/GlobalPosition bez da nas GridContainer vrati na "svoju" celiju.
            _piecesLayer = new Control();
            _piecesLayer.Name = "PiecesLayer";
            _piecesLayer.TopLevel = true;
            _piecesLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
            _grid.AddChild(_piecesLayer);
        }

        private void SyncPiecesLayerTransform()
        {
            // Uvek drzi PiecesLayer na poslednjem mestu medju decom _grid-a, da nas
            // childIndex-based petlja dole nikad ne pomesa sa pravim Square_r_c celijama.
            if (_piecesLayer.GetParent() == _grid && _grid.GetChildCount() > 0)
            {
                _grid.MoveChild(_piecesLayer, _grid.GetChildCount() - 1);
            }

            _piecesLayer.GlobalPosition = _grid.GlobalPosition;
            _piecesLayer.Size = _grid.Size;
        }

        private void EnsurePiecesLayer()
        {
            if (_piecesLayer != null && GodotObject.IsInstanceValid(_piecesLayer))
                return;

            _piecesLayer = new Control();
            _piecesLayer.Name = "PiecesLayer";
            _piecesLayer.TopLevel = true;
            _piecesLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
            _grid.AddChild(_piecesLayer);
        }

        public void Render(ChessBoardState boardState, int selectedRow, int selectedCol, float tileSize,
            int botFromRow = -1, int botFromCol = -1, int botToRow = -1, int botToCol = -1)
        {
            if (_grid == null) return;

            EnsurePiecesLayer();
            // SyncPiecesLayerTransform();

            HashSet<string> validSquares = new HashSet<string>();
            if (selectedRow != -1 && selectedCol != -1)
            {
                var validMoves = boardState.GetValidMovesForPiece(selectedRow, selectedCol);
                if (validMoves != null)
                {
                    foreach (var pos in validMoves)
                    {
                        int r = 8 - pos.Rank;
                        int c = (int)pos.File;
                        validSquares.Add($"{r},{c}");
                    }
                }
            }

            // Stateless rebuild figura svaki put — jednostavno i predvidljivo.
            // (Render() se poziva samo van animacije, pa nema rizika da obrisemo
            // cvor koji je trenutno "u letu" — AnimateMove ne zove Render().)
            foreach (Node child in _piecesLayer.GetChildren())
            {
                _piecesLayer.RemoveChild(child);
                child.Free();
            }

            int childIndex = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Control cell = null;

                    // Preskoci PiecesLayer gde god se trenutno nalazi medju decom _grid-a
                    while (childIndex < _grid.GetChildCount() && _grid.GetChild(childIndex) == _piecesLayer)
                    {
                        childIndex++;
                    }

                    if (childIndex < _grid.GetChildCount())
                    {
                        var potentialCell = _grid.GetChild(childIndex);

                        if (potentialCell == null || !(potentialCell is Control) || !potentialCell.HasNode("Background"))
                        {
                            potentialCell?.QueueFree();
                            if (potentialCell != null) _grid.RemoveChild(potentialCell);

                            cell = new Control();
                            cell.Name = $"Square_{r}_{c}";
                            ColorRect bg = new ColorRect();
                            bg.Name = "Background";
                            cell.AddChild(bg);
                            _grid.AddChild(cell);
                            _grid.MoveChild(cell, childIndex);
                        }
                        else
                        {
                            cell = potentialCell as Control;
                        }
                    }
                    else
                    {
                        cell = new Control();
                        cell.Name = $"Square_{r}_{c}";
                        ColorRect bg = new ColorRect();
                        bg.Name = "Background";
                        cell.AddChild(bg);
                        _grid.AddChild(cell);
                        _grid.MoveChild(cell, childIndex);
                    }

                    childIndex++;

                    var background = cell.GetNode<ColorRect>("Background");

                    cell.CustomMinimumSize = new Vector2(tileSize, tileSize);
                    background.CustomMinimumSize = new Vector2(tileSize, tileSize);
                    background.Size = new Vector2(tileSize, tileSize);

                    ColorRect dot = cell.HasNode("LegalDot") ? cell.GetNode<ColorRect>("LegalDot") : null;

                    Color baseColor = (r + c) % 2 == 0 ? new Color("#f0d9b5") : new Color("#b58863");

                    string piece = boardState.GetPieceAt(r, c);
                    bool isWhiteTurn = boardState.CurrentTurn == ChessDotNet.Player.White;
                    bool isBlackTurn = boardState.CurrentTurn == ChessDotNet.Player.Black;
                    bool isSquareEmpty = (piece == ".");
                    bool isWhitePiece = !isSquareEmpty && (piece == piece.ToUpper());
                    bool isBlackPiece = !isSquareEmpty && (piece == piece.ToLower());

                    if (background.HasNode("InnerMask"))
                    {
                        var oldMask = background.GetNode("InnerMask");
                        background.RemoveChild(oldMask);
                        oldMask.QueueFree();
                    }

                    Color cellColor = baseColor;
                    if (r == selectedRow && c == selectedCol)
                    {
                        cellColor = new Color("#f7ec74");
                    }
                    else if ((r == botFromRow && c == botFromCol) || (r == botToRow && c == botToCol))
                    {
                        Color botTraceColor = new Color("#cdd26a", 0.35f);
                        cellColor = cellColor.Blend(botTraceColor);
                    }

                    if (validSquares.Contains($"{r},{c}") && ((isWhiteTurn && isBlackPiece) || (isBlackTurn && isWhitePiece)))
                    {
                        Color attackOverlay = new Color(0.85f, 0.25f, 0.25f, 0.85f);
                        cellColor = cellColor.Blend(attackOverlay);
                    }

                    background.Color = cellColor;

                    // Figura ide u PiecesLayer, pixel-pozicionirana — NE kao dete
                    // GridContainer celije (izbegava auto-layout snap-back problem).
                    if (!isSquareEmpty)
                    {
                        var pieceTexture = new TextureRect();
                        pieceTexture.Name = $"P_{r}_{c}";
                        pieceTexture.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                        pieceTexture.Size = new Vector2(tileSize, tileSize);
                        pieceTexture.Position = new Vector2(c * tileSize, r * tileSize);
                        pieceTexture.Texture = _atlas.GetPieceTexture(piece);
                        pieceTexture.MouseFilter = Control.MouseFilterEnum.Ignore;
                        _piecesLayer.AddChild(pieceTexture);
                    }

                    if (validSquares.Contains($"{r},{c}") && isSquareEmpty)
                    {
                        bool isNewDot = false;
                        if (dot == null)
                        {
                            dot = new ColorRect();
                            dot.Name = "LegalDot";
                            cell.AddChild(dot);
                            isNewDot = true;
                        }

                        float dotSize = tileSize * 0.25f;
                        dot.CustomMinimumSize = new Vector2(dotSize, dotSize);
                        dot.Size = new Vector2(dotSize, dotSize);
                        dot.Position = new Vector2((tileSize - dotSize) / 2f, (tileSize - dotSize) / 2f);

                        if (isNewDot)
                        {
                            dot.Color = new Color(0.1f, 0.1f, 0.1f, 0.0f);
                            dot.Visible = true;
                            Tween tween = _grid.CreateTween();
                            tween.TweenProperty(dot, "color", new Color(0.1f, 0.1f, 0.1f, 0.4f), 0.2f)
                                .SetTrans(Tween.TransitionType.Cubic)
                                .SetEase(Tween.EaseType.Out);
                        }
                        else
                        {
                            dot.Color = new Color(0.1f, 0.1f, 0.1f, 0.4f);
                            dot.Visible = true;
                        }
                    }
                    else if (dot != null)
                    {
                        dot.Visible = false;
                    }
                }
            }
            // NOVO: PiecesLayer mora biti POSLEDNJI element medju decom _grid-a
            // NAKON sto su sve Square celije dodate/rearanzirane ovom prilikom —
            // inace GridContainer rezervise prazan slot na njegovoj staroj poziciji
            // i pomera ceo raspored za jedno mesto (bug vidljiv samo na prvom renderu).
            if (_piecesLayer.GetParent() == _grid)
            {
                _grid.MoveChild(_piecesLayer, _grid.GetChildCount() - 1);
            }
            // NOVO: odlozi citanje _grid.GlobalPosition/Size za sledeci idle frame,
            // da Godot-ov Container layout sistem stigne da zavrsi svoj sort pass
            // (bitno bas na PRVI render, kad _grid tek dobija svoju prvu decu).
            Callable.From(SyncPiecesLayerTransform).CallDeferred();
        }

        // Animira pomeranje figure sa (fromRow,fromCol) na (toRow,toCol).
        // Poziva se UMESTO Render()-a odmah nakon uspesnog poteza — koristi cvorove
        // koji su vec vizuelno na tabli od proslog Render() poziva (pre nego sto je
        // stanje table vec izmenjeno u memoriji, ali pre novog crtanja).
        public void AnimateMove(int fromRow, int fromCol, int toRow, int toCol, float tileSize, System.Action onComplete)
        {
            EnsurePiecesLayer();
            SyncPiecesLayerTransform();

            var movingNode = _piecesLayer.GetNodeOrNull<TextureRect>($"P_{fromRow}_{fromCol}");

            if (movingNode == null)
            {
                // Nemamo sta da animiramo (npr. prvi render posle resize-a) — samo nastavi.
                onComplete?.Invoke();
                return;
            }

            var capturedNode = _piecesLayer.GetNodeOrNull<TextureRect>($"P_{toRow}_{toCol}");

            Vector2 targetPos = new Vector2(toCol * tileSize, toRow * tileSize);

            Tween tween = _piecesLayer.CreateTween();
            tween.SetParallel(true);

            tween.TweenProperty(movingNode, "position", targetPos, 0.2f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);

            if (capturedNode != null)
            {
                tween.TweenProperty(capturedNode, "modulate:a", 0f, 0.15f)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.In);
            }

            tween.Chain().TweenCallback(Callable.From(() =>
            {
                capturedNode?.QueueFree();
                onComplete?.Invoke();
            }));
        }
    }
}