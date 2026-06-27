using Godot;
using System;

namespace ChessPuzzles2d.Services
{
    public class WindowManager
    {
        private readonly Viewport _viewport;
        public event Action<float> OnBoardResize;

        public WindowManager(Viewport viewport)
        {
            _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            _viewport.SizeChanged += HandleSizeChanged;
        }

        public void Cleanup()
        {
            if (_viewport != null)
            {
                _viewport.SizeChanged -= HandleSizeChanged;
            }
        }

        public void TriggerInitialResize()
        {
            HandleSizeChanged();
        }

        private void HandleSizeChanged()
        {
            Vector2 viewportSize = _viewport.GetVisibleRect().Size;
            float tileSize;

            // AKO JE EKRAN USPRAVAN (Portrait): Širina diktira veličinu table!
            if (viewportSize.X < viewportSize.Y)
            {
                // Uzimamo širinu ekrana, ostavljamo malo praznog prostora sa strana (margine)
                // i delimo sa 8 polja. Tabla će se savršeno raširiti od ivice do ivice!
                tileSize = viewportSize.X / 8f;
            }
            // AKO JE EKRAN VODORAVAN (Landscape): Visina diktira veličinu table!
            else
            {
                tileSize = viewportSize.Y / 8f;
            }

            // Šaljemo izračunatu bezbednu veličinu polja našem GameController-u
            OnBoardResize?.Invoke(tileSize);
        }
    }
}
