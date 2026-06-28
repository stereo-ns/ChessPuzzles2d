using Godot;
using System.Collections.Generic;

namespace ChessPuzzles2d.Services
{
    // Decoupled C# scene router with zero hardcoded engine string tracking
    public class SceneManager
    {
        private static SceneManager _instance;
        public static SceneManager Instance => _instance ??= new SceneManager();

        public enum TargetScene
        {
            Bootstrap,
            Menu,
            Buy,
            Game
        }

        private readonly Dictionary<TargetScene, string> _sceneRegistry = new Dictionary<TargetScene, string>
        {
            { TargetScene.Bootstrap, "res://scenes/BootstrapScene/Bootstrap.tscn" },
            { TargetScene.Menu, "res://scenes/MenuScene/Menu.tscn" },
            { TargetScene.Buy, "res://scenes/BuyScene/Buy.tscn" },
            { TargetScene.Game, "res://scenes/Main.tscn" } // Glavna šahovska igra
        };

        private SceneManager()
        {
            GD.Print("[BOOTSTRAP]: SceneManager initialization successful.");
        }

        public void SwitchTo(SceneTree tree, TargetScene target)
        {
            if (tree == null || !_sceneRegistry.ContainsKey(target))
            {
                GD.PrintErr($"[SCENE MANAGER ERROR]: Cannot switch to scene {target}");
                return;
            }

            string path = _sceneRegistry[target];
            MoveLoggerService.Instance.LogMessage("SceneManager", $"Requesting transition to -> {target}");

            // 🚀 BEZBEDNOSNI OKIDAČ: Koristimo CallDeferred da nateramo motor 
            // da zamenu scene izvrši na kraju frejma, kada stablo završi sve svoje interne poslove!
            tree.CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
        }

    }
}
