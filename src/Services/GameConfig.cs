using Godot;

namespace ChessPuzzles2d.Services
{
    // Clean C# Singleton independent of Godot's Autoload tab
    public class GameConfig
    {
        private static GameConfig _instance;
        public static GameConfig Instance => _instance ??= new GameConfig();

        public enum Language { En, Sr }
        public Language CurrentLanguage { get; set; } = Language.Sr;

        private GameConfig()
        {
            GD.Print("[BOOTSTRAP]: GameConfig initialization successful.");
        }
    }
}
