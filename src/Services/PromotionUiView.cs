using Godot;
using System;
using ChessPuzzles2d.Core;

namespace ChessPuzzles2d.Services
{
    public class PromotionUiView
    {
        private readonly GridContainer _boardGrid;
        private readonly PieceAtlasService _atlasService;
        private readonly Color _selectedColor = Color.FromHtml("baca44");
        private readonly Color _lightColor = Color.FromHtml("f0d9b5");
        private readonly Color _darkColor = Color.FromHtml("b58863");

        public PromotionUiView(GridContainer boardGrid, PieceAtlasService atlasService)
        {
            _boardGrid = boardGrid ?? throw new ArgumentNullException(nameof(boardGrid));
            _atlasService = atlasService ?? throw new ArgumentNullException(nameof(atlasService));
        }

        public void RenderOverlay(ChessBoardState boardState, int promoToRow, int promoToCol, int promoFromRow, int promoFromCol, float tileSize)
        {
            foreach (Node child in _boardGrid.GetChildren())
            {
                _boardGrid.RemoveChild(child);
                child.Free();
            }

            Vector2 tileMinSize = new Vector2(tileSize, tileSize);

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    ColorRect tile = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };

                    bool isPromoTile = col == promoToCol && (promoToRow == 0 ? row <= 3 : row >= 4);

                    if (isPromoTile) tile.Color = _selectedColor;
                    else
                    {
                        bool isLight = (row + col) % 2 == 0;
                        tile.Color = isLight ? _lightColor : _darkColor;
                    }

                    tile.CustomMinimumSize = tileMinSize;
                    tile.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
                    tile.SizeFlagsVertical = Control.SizeFlags.Expand | Control.SizeFlags.Fill;

                    string pieceCode = ".";

                    if (isPromoTile)
                    {
                        int relativeRow = promoToRow == 0 ? row : (7 - row);
                        string letter = relativeRow switch { 0 => "Q", 1 => "N", 2 => "R", 3 => "B", _ => "." };
                        pieceCode = boardState.CurrentTurn == ChessDotNet.Player.White ? letter.ToUpper() : letter.ToLower();
                    }
                    else if (row == promoFromRow && col == promoFromCol)
                    {
                        pieceCode = "."; // Sakrivamo starog pešaka
                    }
                    else
                    {
                        pieceCode = boardState.GetPieceAt(row, col);
                    }

                    if (pieceCode != "." && _atlasService.IsAtlasLoaded)
                    {
                        TextureRect pieceIcon = new TextureRect
                        {
                            Texture = _atlasService.GetPieceTexture(pieceCode),
                            MouseFilter = Control.MouseFilterEnum.Ignore,
                            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
                            CustomMinimumSize = tileMinSize,
                            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
                            Position = Vector2.Zero
                        };
                        tile.AddChild(pieceIcon);
                    }

                    _boardGrid.AddChild(tile);
                }
            }
        }
    }
}
