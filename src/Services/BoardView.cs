using Godot;
using System.Collections.Generic;
using ChessPuzzles2d.Core;
using ChessPuzzles2d.Services;
using ChessDotNet;

namespace ChessPuzzles2d.Views
{
    // Clean View class responsible strictly for rendering the 8x8 grid container layout
    public class BoardView
    {
        private readonly GridContainer _grid;
        private readonly PieceAtlasService _atlas;

        public BoardView(GridContainer grid, PieceAtlasService atlas)
        {
            _grid = grid;
            _atlas = atlas;
        }

        public void Render(ChessBoardState boardState, int selectedRow, int selectedCol, float tileSize,
                           int botFromRow = -1, int botFromCol = -1, int botToRow = -1, int botToCol = -1)
        {
            if (_grid == null) return;

            // Generate valid moves lookup table for high-performance highlight rendering
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

            int childIndex = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Control cell = null;

                    // Dynamic element layout container check
                    if (childIndex < _grid.GetChildCount())
                    {
                        var potentialCell = _grid.GetChild(childIndex);

                        // Safety validator to filter out corrupt nodes inside the GridContainer
                        if (potentialCell == null || !potentialCell.HasNode("Background") || !potentialCell.HasNode("Piece"))
                        {
                            potentialCell?.QueueFree();
                            _grid.RemoveChild(potentialCell);

                            cell = new Control();
                            cell.Name = $"Square_{r}_{c}";

                            ColorRect bg = new ColorRect();
                            bg.Name = "Background";
                            cell.AddChild(bg);

                            TextureRect pieceTex = new TextureRect();
                            pieceTex.Name = "Piece";
                            pieceTex.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                            pieceTex.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
                            cell.AddChild(pieceTex);

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
                        // Factory fallback to construct the grid array if children counts match zero on boot
                        cell = new Control();
                        cell.Name = $"Square_{r}_{c}";

                        ColorRect bg = new ColorRect();
                        bg.Name = "Background";
                        cell.AddChild(bg);

                        TextureRect pieceTex = new TextureRect();
                        pieceTex.Name = "Piece";
                        pieceTex.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                        pieceTex.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
                        cell.AddChild(pieceTex);

                        _grid.AddChild(cell);
                    }
                    childIndex++;

                    // References are guaranteed to exist beyond this line due to structural checks above
                    var background = cell.GetNode<ColorRect>("Background");
                    var pieceTexture = cell.GetNode<TextureRect>("Piece");

                    // Scale component constraints dynamically inside the engine viewport
                    cell.CustomMinimumSize = new Vector2(tileSize, tileSize);
                    background.CustomMinimumSize = new Vector2(tileSize, tileSize);
                    background.Size = new Vector2(tileSize, tileSize);
                    pieceTexture.Size = new Vector2(tileSize, tileSize);

                    ColorRect dot = cell.HasNode("LegalDot") ? cell.GetNode<ColorRect>("LegalDot") : null;
                    Color baseColor = (r + c) % 2 == 0 ? new Color("#f0d9b5") : new Color("#b58863");

                    string piece = boardState.GetPieceAt(r, c);

                    bool isWhiteTurn = boardState.CurrentTurn == ChessDotNet.Player.White;
                    bool isBlackTurn = boardState.CurrentTurn == ChessDotNet.Player.Black;

                    bool isSquareEmpty = (piece == ".");
                    bool isWhitePiece = !isSquareEmpty && (piece == piece.ToUpper());
                    bool isBlackPiece = !isSquareEmpty && (piece == piece.ToLower());

                    // Nasilno izbacujemo stare zaostale čvorove ako postoje
                    if (background.HasNode("InnerMask"))
                    {
                        var oldMask = background.GetNode("InnerMask");
                        background.RemoveChild(oldMask);
                        oldMask.QueueFree();
                    }

                    // 🚀 VIZUELNI REFAKTOR: Kombinovana matematika boja
                    Color cellColor = baseColor;

                    if (r == selectedRow && c == selectedCol)
                    {
                        cellColor = new Color("#f7ec74"); // Selektovano polje igrača (čvrsta žuta)
                    }
                    else if ((r == botFromRow && c == botFromCol) || (r == botToRow && c == botToCol))
                    {
                        // 🚀 PREFINJENA POLUPROZIRNA ŽUTA: Koristimo 35% prozirnosti (0.35f) 
                        // koja se prelepo stapa i sa svetlim i sa tamnim fabričkim poljima!
                        Color botTraceColor = new Color("#cdd26a", 0.35f);
                        cellColor = cellColor.Blend(botTraceColor);
                    }

                    // Ako je polje istovremeno pod napadom, prelivamo poluprozirni crveni sloj bez ikakvih vizuelnih bagova
                    if (validSquares.Contains($"{r},{c}") && ((isWhiteTurn && isBlackPiece) || (isBlackTurn && isWhitePiece)))
                    {
                        Color attackOverlay = new Color(0.9f, 0.3f, 0.3f, 0.5f);
                        cellColor = cellColor.Blend(attackOverlay);
                    }

                    // Primenjujemo upeglanu, prozirnu boju direktno na Godot čvor
                    background.Color = cellColor;

                    // Render texture textures mapping cleanly into active UI viewports
                    if (isSquareEmpty)
                    {
                        pieceTexture.Visible = false;
                    }
                    else
                    {
                        pieceTexture.Texture = _atlas.GetPieceTexture(piece);
                        pieceTexture.Visible = true;
                    }

                    // Render sub-node pathing indicators for legal move navigation dots
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
        }
    }
}
