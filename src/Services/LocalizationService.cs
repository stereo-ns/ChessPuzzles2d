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
                { "BTN_RESTART_GAME", "RESTART" },
                { "LBL_CHOOSE_MODE", "IZABERITE TEŽINU I KLIKNITE START!" },
                { "LBL_TURN_WHITE", "NA POTEZU: BELI" },
                { "LBL_TURN_BLACK", "RAČUNAR RAZMIŠLJA..." },
                { "LBL_PROMOTION", "IZABERITE FIGURU NA TABLI!" },
                { "LBL_DIFFICULTY", "Izabrana težina: Nivo {0}" }
            };

            // === ENGLISH LANGUAGE MAP ===
            var enMap = new Dictionary<string, string>
            {
                { "BTN_STORE", "START MATCH" }, // 🚀 FIKS: Engleski prevod za pokretanje
                { "BTN_BACK_MENU", "BACK TO MENU" },
                { "BTN_START_GAME", "START MATCH" },
                { "BTN_RESTART_GAME", "RESTART" },
                { "LBL_CHOOSE_MODE", "SELECT DIFFICULTY AND CLICK START!" },
                { "LBL_TURN_WHITE", "TURN: WHITE" },
                { "LBL_TURN_BLACK", "BOT IS THINKING..." },
                { "LBL_PROMOTION", "CHOOSE PROMOTION PIECE ON THE BOARD!" },
                { "LBL_DIFFICULTY", "Selected Difficulty: Level {0}" }
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
