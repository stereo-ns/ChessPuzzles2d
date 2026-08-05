using System.Collections.Generic;
using Godot;

namespace ChessPuzzles2d.Services
{
    // Clean C# Singleton responsible for localized string dictionary mapping
    public class LocalizationService
    {
        private static LocalizationService _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        // High-fidelity dictionary holding language maps
        private readonly Dictionary<GameConfig.Language, Dictionary<string, string>> _locales;

        private LocalizationService()
        {
            _locales = new Dictionary<GameConfig.Language, Dictionary<string, string>>();
            InitializeTranslations();

            MoveLoggerService.Instance.LogMessage("Locale", "LocalizationService dictionary successfully built.");
        }

        // Explicit internal population of string identifiers
        private void InitializeTranslations()
        {
            // === SERBIAN LANGUAGE MAP ===
            var srMap = new Dictionary<string, string>
            {
                { "BTN_STORE", "ZAPOČNI IGRU" },
                { "BTN_BACK_MENU", "NAZAD U MENI" },
                { "LBL_DIFFICULTY", "Izabrana težina: Nivo {0}" },
                { "LBL_PROMOTION", "IZABERITE FIGURU NA TABLI!" },
                { "BTN_RESTART_GAME", "RESTART" },
                { "BTN_UNDO", "VRATI POTEZ" },
                { "BTN_RESIGN", "PREDAJA" },
                { "LBL_CHECKMATE_WIN", "KRAJ: MAT! POBEDNIK: {0}" },
                { "LBL_RESIGN_WIN", "KRAJ: PREDAJA! POBEDNIK: {0}" },
                { "LBL_DRAW", "KRAJ: REZULTAT JE NEREŠEN!" },
                { "LBL_WINNER_WHITE", "BELI" },
                { "LBL_WINNER_BLACK", "CRNI" },
                { "DBG_STATUS_LINE", "Potez: {0} | Beli: {1} | Crni: {2}" },
            };

            // === ENGLISH LANGUAGE MAP ===
            var enMap = new Dictionary<string, string>
            {
                { "BTN_STORE", "START MATCH" }, // 🚀 FIKS: Engleski prevod za pokretanje
                { "BTN_BACK_MENU", "BACK TO MENU" },
                { "LBL_DIFFICULTY", "Selected Difficulty: Level {0}" },
                { "LBL_PROMOTION", "IZABERITE FIGURU NA TABLI!" },
                { "BTN_RESTART_GAME", "RESTART" },
                { "BTN_UNDO", "UNDO" },
                { "BTN_RESIGN", "RESIGN" },
                { "LBL_CHECKMATE_WIN", "GAME OVER: CHECKMATE! WINNER: {0}" },
                { "LBL_RESIGN_WIN", "GAME OVER: RESIGNATION! WINNER: {0}" },
                { "LBL_DRAW", "GAME OVER: DRAW!" },
                { "LBL_WINNER_WHITE", "WHITE" },
                { "LBL_WINNER_BLACK", "BLACK" },
                { "DBG_STATUS_LINE", "Move: {0} | White: {1} | Black: {2}" },
            };

            _locales[GameConfig.Language.Sr] = srMap;
            _locales[GameConfig.Language.En] = enMap;
        }

        // Resolves the translated string based on active global config context
        public string Translate(string key)
        {
            GameConfig.Language activeLang = GameConfig.Instance.CurrentLanguage;

            if (_locales.ContainsKey(activeLang) && _locales[activeLang].ContainsKey(key))
            {
                return _locales[activeLang][key];
            }

            GD.PrintErr($"[LOCALE ERROR]: Key '{key}' not found for language {activeLang}!");
            return key; // Fallback to display the raw key if validation fails
        }
    }
}
