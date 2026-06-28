using Godot;
using ChessPuzzles2d.Services;
using ChessPuzzles2d.Utils; // 🚀 Uvozimo naš novi validator

namespace ChessPuzzles2d.Scenes
{
    // Main Menu controller driving language selection and early lifecycle transitions
    public partial class MenuController : Control
    {
        [Export] public Button LanguageSrButton { get; set; }
        [Export] public Button LanguageEnButton { get; set; }

        public override void _Ready()
        {
            // 🚀 POPRAVKA: Čist poziv void metode bez if-a i bez znaka uzvika!
            ValidationUtil.ValidateReferences("Menu",
                (LanguageSrButton, nameof(LanguageSrButton)),
                (LanguageEnButton, nameof(LanguageEnButton))
            );

            // Odavde nadole kôd je 100% bezbedan jer je gornja linija neprobojna giljotina
            MoveLoggerService.Instance.LogMessage("Menu", "Main Menu language interface successfully verified and mounted.");

            LanguageSrButton.Pressed += OnSrButtonPressed;
            LanguageEnButton.Pressed += OnEnButtonPressed;
        }


        private void OnSrButtonPressed()
        {
            GameConfig.Instance.CurrentLanguage = GameConfig.Language.Sr;
            MoveLoggerService.Instance.LogMessage("Menu", "Language locked to: SERBIAN");

            AudioService.Instance.PlayUiClick();
            SceneManager.Instance.SwitchTo(GetTree(), SceneManager.TargetScene.Buy);
        }

        private void OnEnButtonPressed()
        {
            GameConfig.Instance.CurrentLanguage = GameConfig.Language.En;
            MoveLoggerService.Instance.LogMessage("Menu", "Language locked to: ENGLISH");

            AudioService.Instance.PlayUiClick();
            SceneManager.Instance.SwitchTo(GetTree(), SceneManager.TargetScene.Buy);
        }
    }
}
