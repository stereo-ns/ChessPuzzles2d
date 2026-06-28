using Godot;
using ChessPuzzles2d.Services;
using ChessPuzzles2d.Utils; // 🚀 Uvozimo naš novi validator

namespace ChessPuzzles2d.Scenes
{
    // Store/Buy screen controller layout mapping transitions to gameplay container
    public partial class BuyController : Control
    {
        [Export] public Button EnterGameButton { get; set; }
        [Export] public Button BackToMenuButton { get; set; }

        public override void _Ready()
        {
            ValidationUtil.ValidateReferences("Buy",
                (EnterGameButton, nameof(EnterGameButton)),
                (BackToMenuButton, nameof(BackToMenuButton))
            );

            // 🚀 DINAČKA LOKALIZACIJA: Čitamo izabrani jezik i lepimo prevode na dugmiće!
            EnterGameButton.Text = LocalizationService.Instance.Translate("BTN_STORE");
            BackToMenuButton.Text = LocalizationService.Instance.Translate("BTN_BACK_MENU");

            MoveLoggerService.Instance.LogMessage("Buy", "Buy/Store view successfully verified and mounted inside the active viewport.");

            EnterGameButton.Pressed += OnEnterGameButtonPressed;
            BackToMenuButton.Pressed += OnBackToMenuButtonPressed;
        }


        private void OnEnterGameButtonPressed()
        {
            AudioService.Instance.PlayUiClick();
            MoveLoggerService.Instance.LogMessage("Buy", "User initialized match payload. Swapping scene to Chess Match.");
            SceneManager.Instance.SwitchTo(GetTree(), SceneManager.TargetScene.Game);
        }

        private void OnBackToMenuButtonPressed()
        {
            AudioService.Instance.PlayUiClick();
            MoveLoggerService.Instance.LogMessage("Buy", "User clicked back. Returning to Main Menu.");
            SceneManager.Instance.SwitchTo(GetTree(), SceneManager.TargetScene.Menu);
        }
    }
}
