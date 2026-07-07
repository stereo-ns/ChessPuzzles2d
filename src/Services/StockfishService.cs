using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace ChessPuzzles2d.Services
{
    public class StockfishService
    {
        private Process _botProcess;
        private StreamWriter _processInput;
        private StreamReader _processOutput;

        private readonly List<string> _engineOutputLines = new List<string>();
        private readonly object _lockObject = new object();
        private bool _isOutputLoopRunning;

        public void StartEngine(int skillLevel)
        {
            try
            {
                _engineOutputLines.Clear();
                string executablePath = "";

                if (OS.HasFeature("android"))
                {
                    // REŠENJE: Puna i apsolutna interna putanja aplikacije na Androidu
                    string userDir = ProjectSettings.GlobalizePath("user://");
                    executablePath = Path.Combine(userDir, "stockfish_android");

                    // Kopiramo tekstualni fajl iz Godot resursa na lokalni disk telefona
                    if (!File.Exists(executablePath))
                    {
                        using (var godotFile = Godot.FileAccess.Open("res://data/stockfish_android.txt", Godot.FileAccess.ModeFlags.Read))
                        {
                            if (godotFile != null)
                            {
                                byte[] binaryData = godotFile.GetBuffer((long)godotFile.GetLength());
                                File.WriteAllBytes(executablePath, binaryData);
                                GD.Print("Stockfish === [ANDROID]: Successfully extracted binary bytes from APK to storage. ===");
                            }
                            else
                            {
                                GD.Print("Stockfish === [ERROR]: Godot.FileAccess could not open res://data/stockfish_android.txt! ===");
                            }
                        }
                    }

                    // Dodajemo izvršna prava fajlu na disku telefona
                    var chmodProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{executablePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    chmodProcess?.WaitForExit();

                    GD.Print("Stockfish === [ANDROID]: Unpacked successfully to user:// and chmod +x completed. ===");
                }

                else
                {
                    // Logika za vaš Slackware računar
                    executablePath = "/usr/games/stockfish";
                }

                // POKRETANJE PROCESA (Identično i za računar i za telefon)
                _botProcess = new Process();
                _botProcess.StartInfo.FileName = executablePath;
                _botProcess.StartInfo.UseShellExecute = false;
                _botProcess.StartInfo.RedirectStandardInput = true;
                _botProcess.StartInfo.RedirectStandardOutput = true;
                _botProcess.StartInfo.CreateNoWindow = true;

                _botProcess.Start();
                _processInput = _botProcess.StandardInput;
                _processOutput = _botProcess.StandardOutput;

                _isOutputLoopRunning = true;
                Task.Run(() =>
                {
                    while (_isOutputLoopRunning && _processOutput != null)
                    {
                        string line = _processOutput.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            lock (_lockObject)
                            {
                                _engineOutputLines.Add(line);
                            }
                        }
                    }
                });

                // Slanje standardnog UCI protokola (Sada uvek sadrži reč Stockfish na početku)
                SendCommand("uci");

                if (skillLevel < 5)
                {
                    SendCommand("setoption name UCI_LimitStrength value true");
                    SendCommand("setoption name UCI_Elo value 1320");
                    SendCommand("setoption name Use NNUE value false");
                    SendCommand("setoption name Skill Level value " + skillLevel);
                }
                else
                {
                    SendCommand("setoption name UCI_LimitStrength value false");
                    SendCommand("setoption name Use NNUE value true");
                    SendCommand("setoption name Skill Level value " + skillLevel);
                }

                SendCommand("isready");
                GD.Print("Stockfish === [ENGINE READY]: Process pipeline active for Level: " + skillLevel + " ===");
            }
            catch (Exception ex)
            {
                GD.Print("Stockfish === [CRITICAL ERROR] in StartEngine: " + ex.Message + " ===");
            }
        }

        public void SendCommand(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command)) return;

                lock (_lockObject)
                {
                    _processInput?.WriteLine(command);
                    GD.Print("Stockfish === [UCI IN]: " + command + " ===");
                }
            }
            catch (Exception ex)
            {
                GD.Print("Stockfish === [WRITE ERROR]: " + ex.Message + " ===");
            }
        }

        public string GetBestMove(string fenPosition)
        {
            lock (_lockObject)
            {
                _engineOutputLines.Clear();
            }

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
            bool thinking = true;
            int timeoutCheck = 0;

            while (thinking && timeoutCheck < 200)
            {
                System.Threading.Thread.Sleep(10);
                timeoutCheck++;

                lock (_lockObject)
                {
                    for (int i = 0; i < _engineOutputLines.Count; i++)
                    {
                        string line = _engineOutputLines[i];

                        if (line.Contains(" pv "))
                        {
                            string[] tokens = line.Split(' ');
                            for (int j = 0; j < tokens.Length; j++)
                            {
                                if (tokens[j] == "pv" && j + 1 < tokens.Length)
                                {
                                    string candidateMove = tokens[j + 1];
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
            }

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

                GD.Print("Stockfish === [LEVEL CONTROL]: Alternative index " + blunderIndex + ": " + foundMoves[blunderIndex] + " ===");
                return foundMoves[blunderIndex];
            }

            return string.Empty;
        }

        public void StopEngine()
        {
            try
            {
                SendCommand("quit");
                _isOutputLoopRunning = false;

                if (_botProcess != null && !_botProcess.HasExited)
                {
                    _botProcess.Kill();
                    _botProcess.Dispose();
                }

                _processInput = null;
                _processOutput = null;

                lock (_lockObject) { _engineOutputLines.Clear(); }
                GD.Print("Stockfish === [STOCKFISH ENGINE]: Process pipeline closed successfully. ===");
            }
            catch
            {
                // Tiho gašenje resursa
            }
        }
    }
}
