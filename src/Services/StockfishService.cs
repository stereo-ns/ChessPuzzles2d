using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
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

        // private string _androidBigNnuePath;
        private string _androidSmallNnuePath;
        private string _androidNativeLibDir;

        public void StartEngine(int skillLevel)
        {
            try
            {
                _engineOutputLines.Clear();
                string executablePath = "";

                if (OS.HasFeature("android"))
                {
                    string userDir = ProjectSettings.GlobalizePath("user://");

                    // _androidBigNnuePath = CopyNnueIfNeeded(userDir, "nn-1c0000000000.nnue");
                    _androidSmallNnuePath = CopyNnueIfNeeded(userDir, "nn-37f18f62d772.nnue");

                    // Android 10+ blokira izvršavanje fajlova iz user:// (W^X/noexec).
                    // Jedina exec-enabled putanja je nativeLibraryDir, gde sistem sam
                    // ekstraktuje .so fajlove iz APK-a (lib/arm64-v8a/).
                    var androidHelper = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("AndroidHelper");
                    string nativeLibDir = (string)androidHelper.Call("get_native_lib_dir");

                    if (string.IsNullOrEmpty(nativeLibDir))
                    {
                        GD.Print("Stockfish === [ANDROID ERROR]: nativeLibDir je prazan! Proveri AndroidHelper.gd ===");
                    }

                    executablePath = Path.Combine(nativeLibDir, "libstockfish_lite.so");
                    _androidNativeLibDir = nativeLibDir;
                    GD.Print("Stockfish === [ANDROID]: Using native lib dir: " + nativeLibDir + " ===");
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
                _botProcess.StartInfo.RedirectStandardError = true;
                _botProcess.StartInfo.CreateNoWindow = true;
                _botProcess.StartInfo.WorkingDirectory = OS.HasFeature("android")
                    ? ProjectSettings.GlobalizePath("user://")
                    : "/usr/games";

                if (OS.HasFeature("android") && !string.IsNullOrEmpty(_androidNativeLibDir))
                {
                    _botProcess.StartInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = _androidNativeLibDir;
                }
                _botProcess.Start();

                _processInput = _botProcess.StandardInput;
                _processOutput = _botProcess.StandardOutput;

                // Čitanje stderr u pozadini - korisno ako engine ikad padne, da vidimo zašto
                Task.Run(() =>
                {
                    string errLine;
                    while ((errLine = _botProcess.StandardError.ReadLine()) != null)
                    {
                        GD.Print("Stockfish === [STDERR]: " + errLine + " ===");
                    }
                });

                _isOutputLoopRunning = true;
                Task.Run(() =>
                {
                    while (_isOutputLoopRunning && _processOutput != null)
                    {
                        string line = _processOutput.ReadLine();
                        if (line == null) break; // stream zatvoren / proces mrtav

                        GD.Print("Stockfish === [RAW OUT]: " + line + " ===");

                        if (!string.IsNullOrEmpty(line))
                        {
                            lock (_lockObject)
                            {
                                _engineOutputLines.Add(line);
                            }
                        }
                    }
                });

                // Slanje standardnog UCI protokola
                SendCommand("uci");

                if (OS.HasFeature("android"))
                {
                    // SendCommand($"setoption name EvalFile value {_androidBigNnuePath}");
                    SendCommand($"setoption name EvalFileSmall value {_androidSmallNnuePath}");
                }

                if (skillLevel < 5)
                {
                    SendCommand("setoption name UCI_LimitStrength value true");
                    SendCommand("setoption name UCI_Elo value 1320");
                    SendCommand("setoption name Skill Level value " + skillLevel);
                }
                else
                {
                    SendCommand("setoption name UCI_LimitStrength value false");
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

        private string CopyNnueIfNeeded(string userDir, string nnueFileName)
        {
            string destPath = Path.Combine(userDir, nnueFileName);

            if (!File.Exists(destPath))
            {
                using (var godotFile = Godot.FileAccess.Open($"res://data/{nnueFileName}", Godot.FileAccess.ModeFlags.Read))
                {
                    if (godotFile != null)
                    {
                        byte[] netData = godotFile.GetBuffer((long)godotFile.GetLength());
                        File.WriteAllBytes(destPath, netData);
                        GD.Print($"Stockfish === [ANDROID]: {nnueFileName} copied, size: {netData.Length} bytes ===");
                    }
                    else
                    {
                        GD.Print($"Stockfish === [ANDROID ERROR]: res://data/{nnueFileName} NE POSTOJI u APK-u! ===");
                    }
                }
            }
            else
            {
                GD.Print($"Stockfish === [ANDROID]: {nnueFileName} vec postoji, velicina: {new FileInfo(destPath).Length} bytes ===");
            }

            return destPath;
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
                int lowLevelMoveTime = OS.HasFeature("android")
                    ? GameConfig.Instance.LowLevelMoveTimeAndroid
                    : GameConfig.Instance.LowLevelMoveTimeDesktop;
                SendCommand($"go movetime {lowLevelMoveTime}");
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
            const string movePattern = "^[a-h][1-8][a-h][1-8][qrbn]?$";

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
                                    if (Regex.IsMatch(candidateMove, movePattern) && !foundMoves.Contains(candidateMove))
                                    {
                                        foundMoves.Add(candidateMove);
                                    }
                                }
                            }
                        }

                        if (line.StartsWith("bestmove"))
                        {
                            thinking = false;

                            string[] tokens = line.Split(' ');

                            if (currentLevel >= 5)
                            {
                                if (tokens.Length > 1) return tokens[1];
                            }
                            else if (foundMoves.Count == 0 && tokens.Length > 1 && Regex.IsMatch(tokens[1], movePattern))
                            {
                                foundMoves.Add(tokens[1]);
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