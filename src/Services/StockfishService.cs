using System;
using System.Diagnostics;
using System.Collections.Generic;
using Godot;

namespace ChessPuzzles2d.Services
{
    public class StockfishService
    {
        private Process _process;
        private System.IO.StreamWriter _input;
        private System.IO.StreamReader _output;

        // Zacementirana tačna lokacija sa tvog Slackware find-a!
        private readonly string _enginePath = "/usr/games/stockfish";

        public void StartEngine(int skillLevel)
        {
            try
            {
                _process = new Process();
                _process.StartInfo.FileName = _enginePath;
                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.RedirectStandardInput = true;
                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.CreateNoWindow = true;

                _process.Start();

                _input = _process.StandardInput;
                _output = _process.StandardOutput;

                SendCommand("uci");

                string line;
                while ((line = _output.ReadLine()) != null)
                {
                    if (line.Trim() == "uciok") break;
                }

                // AMATERSKA ZONA (Nivoi 0 - 4)
                if (skillLevel < 5)
                {
                    SendCommand("setoption name UCI_LimitStrength value true");
                    SendCommand("setoption name UCI_Elo value 1320");
                    SendCommand("setoption name Use NNUE value false"); // Gasimo neuronsku mrežu za slabije nivoe
                    SendCommand("setoption name Skill Level value 0");
                }
                // MAJSTORSKA ZONA (Nivoi 5 - 20)
                else
                {
                    SendCommand("setoption name UCI_LimitStrength value false");
                    SendCommand("setoption name Use NNUE value true"); // Palimo mrežu da vas "dere"
                    SendCommand("setoption name Skill Level value " + skillLevel);
                }

                SendCommand("isready");

                bool isConfirmed = false;
                while ((line = _output.ReadLine()) != null)
                {
                    if (line.Trim() == "readyok")
                    {
                        isConfirmed = true;
                        break;
                    }
                }

                if (isConfirmed)
                {
                    MoveLoggerService.Instance.LogMessage("Stockfish", $"[UCI CONFIRMED]: SF 17.1 configured for Level: {skillLevel}");
                }
            }
            catch (Exception ex)
            {
                MoveLoggerService.Instance.LogMessage("Stockfish", $"[CRITICAL ERROR] in StartEngine: {ex.Message}");
            }
        }


        public void SendCommand(string command)
        {
            if (_input == null) return;
            _input.WriteLine(command);
            _input.Flush();
        }

        public string GetBestMove(string fenPosition)
        {
            if (_input == null || _output == null) return string.Empty;

            SendCommand($"position fen {fenPosition}");

            int currentLevel = GameConfig.Instance.BotSkillLevel;

            if (currentLevel < 5)
            {
                SendCommand("setoption name MultiPV value 4");
                SendCommand("go movetime 20");
            }
            else
            {
                // POPRAVKA: Vraćamo fabrički 1 najbolji potez za visoke nivoe
                SendCommand("setoption name MultiPV value 1");

                // POPRAVKA: Čitamo tačno vreme proračuna iz GameConfig-a i šaljemo motoru
                int timeLimit = GameConfig.Instance.BotThinkTimeMilliseconds;
                SendCommand($"go movetime {timeLimit}");
            }

            List<string> foundMoves = new List<string>();
            string line;

            while ((line = _output.ReadLine()) != null)
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
                    if (currentLevel >= 5)
                    {
                        string[] tokens = line.Split(' ');
                        if (tokens.Length > 1) return tokens[1]; // Vraćamo tačan string poteza
                    }
                    break;
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
                _process?.WaitForExit(300);
                _process?.Close();
                GD.Print("[STOCKFISH]: Proces ugašen.");
            }
            catch
            {
                _process?.Kill();
            }
        }
    }
}
