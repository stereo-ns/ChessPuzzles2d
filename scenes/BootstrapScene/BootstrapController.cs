using Godot;
using System;
using ChessPuzzles2d.Services;

namespace ChessPuzzles2d.Scenes
{
    // Application entry point responsible for early initialization and bootstrapping
    public partial class BootstrapController : Control
    {
        public override void _Ready()
        {
            // Triggering explicit C# singletons to force log creation on disk
            MoveLoggerService.Instance.LogMessage("Bootstrap", "Application lifecycle started.");

            // Log target language from global config shield
            string currentLang = GameConfig.Instance.CurrentLanguage.ToString();
            MoveLoggerService.Instance.LogMessage("Bootstrap", $"Global configuration locked. Language: {currentLang}");

            // Instantly transition to the Main Menu scene via our enum router
            MoveLoggerService.Instance.LogMessage("Bootstrap", "Redirecting lifecycle container to MenuScene.");
            SceneManager.Instance.SwitchTo(GetTree(), SceneManager.TargetScene.Menu);
        }
    }
}
