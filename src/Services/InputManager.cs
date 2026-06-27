using Godot;
using System;

namespace ChessPuzzles2d.Services
{
    public class InputManager
    {
        private readonly Control _boardGrid;
        public event Action<int, int> OnSquareSelected;

        public InputManager(Control boardGrid)
        {
            _boardGrid = boardGrid ?? throw new ArgumentNullException(nameof(boardGrid));
        }

        public void HandleInput(InputEvent @event)
        {
            // POZITIVAN FILTER: Hvatamo isključivo potvrđeno stanje otpuštanja (onReleased)
            var mouseEvent = @event as InputEventMouseButton;
            if (mouseEvent != null && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                // Koristimo čistu i pozitivnu Godot metodu umesto negativnog "if (!...)" filtra!
                if (mouseEvent.IsReleased())
                {
                    CalculateSquare(mouseEvent.GlobalPosition);
                }
                return;
            }
        }

        private void CalculateSquare(Vector2 position)
        {
            Rect2 boardRect = _boardGrid.GetGlobalRect();

            if (boardRect.HasPoint(position))
            {
                float relativeX = position.X - boardRect.Position.X;
                float relativeY = position.Y - boardRect.Position.Y;

                // Koristimo širinu table za stabilan Portrait proračun na ekranu
                float tileSize = boardRect.Size.X / 8f;

                int col = (int)(relativeX / tileSize);
                int row = (int)(relativeY / tileSize);

                if (row >= 0 && row < 8 && col >= 0 && col < 8)
                {
                    OnSquareSelected?.Invoke(row, col);
                }
            }
        }
    }
}
