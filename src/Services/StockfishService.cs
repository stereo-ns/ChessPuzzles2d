using System;
using System.Diagnostics;
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

                // ZAKLJUČAVANJE TEŽINE: Odmah na startu procesa!
                SendCommand($"setoption name Skill Level value {skillLevel}");

                string line;
                while ((line = _output.ReadLine()) != null)
                {
                    if (line == "uciok") break;
                }
                GD.Print($"[STOCKFISH]: Bot spreman i zaključan na nivou: {skillLevel}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[STOCKFISH GREŠKA]: {ex.Message}");
            }
        }

        public void SendCommand(string command)
        {
            if (_input == null) return;
            _input.WriteLine(command);
            _input.Flush();
        }

        public string GetBestMove(string fenPosition, int thinkTimeMs = 300)
        {
            if (_input == null || _output == null) return string.Empty;

            SendCommand($"position fen {fenPosition}");
            SendCommand($"go movetime {thinkTimeMs}");

            string line;
            while ((line = _output.ReadLine()) != null)
            {
                if (line.StartsWith("bestmove"))
                {
                    string[] tokens = line.Split(' ');
                    if (tokens.Length > 1) return tokens[1];
                }
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
