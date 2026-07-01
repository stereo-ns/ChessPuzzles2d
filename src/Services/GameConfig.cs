using Godot;

namespace ChessPuzzles2d.Services
{
    public class GameConfig
    {
        private static GameConfig _instance;
        public static GameConfig Instance => _instance ??= new GameConfig();

        public enum Language { En, Sr }
        public Language CurrentLanguage { get; set; } = Language.Sr;

        // Centralizovani parametri za kontrolu težine Stockfish motora
        public int BotSkillLevel { get; set; } = 5;
        public int BotMaxSearchDepth { get; set; } = 5;
        public int BotThinkTimeMilliseconds { get; set; } = 50;

        private GameConfig()
        {
            GD.Print("[BOOTSTRAP]: GameConfig initialization successful.");
        }

        // Metoda koja na osnovu slajdera (0-20) postavlja ispravne srazmere težine
        public void UpdateBotDifficulty(int sliderValue)
        {
            BotSkillLevel = sliderValue;

            // Na nivou 0, bot vidi samo 1 potez unapred (depth 1) i misli minimalno
            if (sliderValue == 0)
            {
                BotMaxSearchDepth = 1;
                BotThinkTimeMilliseconds = 20;
            }
            else
            {
                // Skaliranje dubine: npr. nivo 1-5 dobija dubinu 3, viši nivoi više
                BotMaxSearchDepth = sliderValue < 5 ? 3 : (sliderValue / 2) + 2;
                BotThinkTimeMilliseconds = 50 + (sliderValue * 15);
            }
        }
    }
}
