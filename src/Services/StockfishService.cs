using System;
using System.Collections.Generic;
using Godot;
using System.IO;
using System.Reflection;
using System.Threading.Tasks; // Neophodno za Task.Run asinhronu petlju bota
using Wasmtime;


namespace ChessPuzzles2d.Services
{
    public class StockfishService
    {
        private Wasmtime.Engine _engine;
        private Wasmtime.Store _store;
        private Wasmtime.Linker _linker;
        private Wasmtime.Instance _instance;
        private string _inputFilePath;
        private string _outputFilePath;

        // Zacementirana tačna lokacija sa tvog Slackware find-a!

        public void StartEngine(int skillLevel)
        {
            try
            {
                _engine = new Wasmtime.Engine();
                _linker = new Wasmtime.Linker(_engine);

                _store = new Wasmtime.Store(_engine);

                // 1. POSTAVLJANJE PRIVREMENIH FAJLOVA ZA UCI KOMUNIKACIJU
                _inputFilePath = Godot.ProjectSettings.GlobalizePath("user://wasm_stdin.txt");
                _outputFilePath = Godot.ProjectSettings.GlobalizePath("user://wasm_stdout.txt");

                // Brišemo staru istoriju i pravimo čiste prazne fajlove pri startu meča
                File.WriteAllText(_inputFilePath, string.Empty);
                File.WriteAllText(_outputFilePath, string.Empty);

                // Konfigurišemo WASI da koristi ove fajlove kao Standard Input i Output
                var wasiConfig = new WasiConfiguration();
                wasiConfig.WithStandardInput(_inputFilePath);
                wasiConfig.WithStandardOutput(_outputFilePath);

                _store.SetWasiConfiguration(wasiConfig);
                _linker.DefineWasi();

                // 2. Čitanje ugrađenog stockfish.wasm fajla iz resursa sklopa
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("stockfish.wasm"))
                {
                    if (stream == null)
                    {
                        MoveLoggerService.Instance.LogMessage("Stockfish", "[KRITIČNA GREŠKA]: Resurs 'stockfish.wasm' nije pronađen u sklopu!");
                        return;
                    }

                    var module = Wasmtime.Module.FromStream(_engine, "stockfish.wasm", stream);
                    _instance = _linker.Instantiate(_store, module);
                }

                // 3. POPRAVKA: GetAction prima isključivo JEDAN argument (ime funkcije)
                var mainAction = _instance.GetAction("_start");
                if (mainAction != null)
                {
                    // Pokrećemo akciju unutar pozadinske niti bez ikakvih argumenata
                    Task.Run(() => mainAction());
                }

                // Aktivacija UCI interfejsa
                SendCommand("uci");

                // 4. Podešavanje parametara težine
                if (skillLevel < 5)
                {
                    SendCommand("setoption name UCI_LimitStrength value true");
                    SendCommand("setoption name UCI_Elo value 1320");
                    SendCommand("setoption name Use NNUE value false");
                    SendCommand("setoption name Skill Level value 0");
                }
                else
                {
                    SendCommand("setoption name UCI_LimitStrength value false");
                    SendCommand("setoption name Use NNUE value true");
                    SendCommand("setoption name Skill Level value " + skillLevel);
                }

                SendCommand("isready");
                MoveLoggerService.Instance.LogMessage("Stockfish", $"[WASM CONFIRMED]: SF 17.1 Lite podignut na nivou: {skillLevel}");
            }
            catch (Exception ex)
            {
                MoveLoggerService.Instance.LogMessage("Stockfish", $"[CRITICAL ERROR] in Wasm StartEngine: {ex.Message}");
            }
        }

        public void SendCommand(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(_inputFilePath)) return;
                // Upisujemo komandu u fajl i dodajemo novi red da motor zna da je komanda završena
                File.AppendAllText(_inputFilePath, command + "\n");
            }
            catch (Exception ex)
            {
                MoveLoggerService.Instance.LogMessage("Stockfish", $"[GREŠKA UPISA]: {ex.Message}");
            }
        }

        public string GetBestMove(string fenPosition)
        {
            if (string.IsNullOrEmpty(_outputFilePath)) return string.Empty;

            SendCommand($"position fen {fenPosition}");

            int currentLevel = GameConfig.Instance.BotSkillLevel;

            if (currentLevel < 5)
            {
                SendCommand("setoption name MultiPV value 4");
                SendCommand("go movetime 20");
            }
            else
            {
                SendCommand("setoption name MultiPV value 1");
                int timeLimit = GameConfig.Instance.BotThinkTimeMilliseconds;
                SendCommand($"go movetime {timeLimit}");
            }

            List<string> foundMoves = new List<string>();

            // Čitamo izlazni fajl u petlji dok ne stigne potvrda o najboljem potezu
            bool thinking = true;
            int timeoutCheck = 0;

            while (thinking && timeoutCheck < 200) // Sigurnosni kočioni mehanizam od ~2 sekunde max
            {
                System.Threading.Thread.Sleep(10); // Kratka pauza da ne opteretimo procesor
                timeoutCheck++;

                try
                {
                    if (!File.Exists(_outputFilePath)) continue;

                    // Čitamo sve generisane linije iz fajla
                    string[] lines = File.ReadAllLines(_outputFilePath);
                    foreach (string line in lines)
                    {
                        if (line.Contains(" pv "))
                        {
                            string[] tokens = line.Split(' ');
                            for (int i = 0; i < tokens.Length; i++)
                            {
                                if (tokens[i] == "pv" && i + 1 < tokens.Length)
                                {
                                    string candidateMove = tokens[i + 1];
                                    if (!foundMoves.Contains(candidateMove) && candidateMove.Length >= 4)
                                    {
                                        foundMoves.Add(candidateMove);
                                    }
                                }
                            }
                        }

                        if (line.StartsWith("bestmove"))
                        {
                            thinking = false;
                            if (currentLevel >= 5)
                            {
                                string[] tokens = line.Split(' ');
                                if (tokens.Length > 1) return tokens[1];
                            }
                            break;
                        }
                    }
                }
                catch
                {
                    // Tiho preskačemo ako je fajl bio zaključan tokom upisa u tom milisekunde frejmu
                }
            }

            // DINAMIČKA MATEMATIKA SABOTAŽE ZA NIVOE 0 - 4 (Kôd koji smo upeglali)
            if (currentLevel < 5 && foundMoves.Count > 0)
            {
                int blunderIndex = 0;
                switch (currentLevel)
                {
                    case 0: blunderIndex = Math.Min(3, foundMoves.Count - 1); break;
                    case 1: blunderIndex = Math.Min(2, foundMoves.Count - 1); break;
                    case 2:
                        blunderIndex = Math.Min(1, foundMoves.Count - 1);
                        if (blunderIndex == 1 && foundMoves.Count > 2) blunderIndex = 2;
                        break;
                    case 3: blunderIndex = Math.Min(1, foundMoves.Count - 1); break;
                    case 4:
                        blunderIndex = 0;
                        if (foundMoves.Count > 1) blunderIndex = 1;
                        break;
                }

                MoveLoggerService.Instance.LogMessage("Stockfish", $"[LEVEL CONTROL]: Level {currentLevel} triggered. Selected move index {blunderIndex}: '{foundMoves[blunderIndex]}'");
                return foundMoves[blunderIndex];
            }

            return string.Empty;
        }


        public void StopEngine()
        {
            try
            {
                SendCommand("quit");

                // Bezbedno oslobađamo Wasmtime resurse iz memorije
                _instance = null;
                _store?.Dispose();
                _engine?.Dispose();

                // Brišemo privremene tekstualne fajlove sa diska
                if (File.Exists(_inputFilePath)) File.Delete(_inputFilePath);
                if (File.Exists(_outputFilePath)) File.Delete(_outputFilePath);

                GD.Print("[STOCKFISH WASM]: Memorija i privremene datoteke uspešno očišćeni.");
            }
            catch
            {
                // Tiho prizemljenje
            }
        }

    }
}
