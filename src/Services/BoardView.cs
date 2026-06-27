using Godot;
using System;
using ChessPuzzles2d.Core;

namespace ChessPuzzles2d.Services
{
    public class BoardView
    {
        private readonly GridContainer _boardGrid;
        private readonly PieceAtlasService _atlasService;
        private readonly Color _lightColor = Color.FromHtml("f0d9b5");
        private readonly Color _darkColor = Color.FromHtml("b58863");
        private readonly Color _selectedColor = Color.FromHtml("baca44");

        public BoardView(GridContainer boardGrid, PieceAtlasService atlasService)
        {
            _boardGrid = boardGrid ?? throw new ArgumentNullException(nameof(boardGrid));
            _atlasService = atlasService ?? throw new ArgumentNullException(nameof(atlasService));
        }

        public void Render(ChessBoardState boardState, int selectedRow, int selectedCol, float tileSize)
        {
            // Čistimo grid bez odlaganja frejmova
            foreach (Node child in _boardGrid.GetChildren())
            {
                // _boardGrid.RemoveChild(child);
                child.QueueFree();
            }

            Vector2 tileMinSize = new Vector2(tileSize, tileSize);

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    ColorRect tile = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };

                    // Bojenje selekcije
                    if (row == selectedRow && col == selectedCol)
                    {
                        tile.Color = _selectedColor;
                    }
                    else
                    {
                        bool isLight = (row + col) % 2 == 0;
                        tile.Color = isLight ? _lightColor : _darkColor;
                    }

                    tile.CustomMinimumSize = tileMinSize;
                    tile.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
                    tile.SizeFlagsVertical = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
                    tile.Name = $"Tile_{row}_{col}";

                    string pieceCode = boardState.GetPieceAt(row, col);

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
