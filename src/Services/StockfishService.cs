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

                // 1. Budimo motor i uvodimo UCI protokol
                SendCommand("uci");

                // 2. 🚀 OBAVEZNO ČEKANJE: Motor mora da završi inicijalizaciju pre nego što primi opcije!
                string line;
                while ((line = _output.ReadLine()) != null)
                {
                    if (line.Trim() == "uciok") break;
                }

                // 3. Sada kada je motor spreman, bezbedno zaključavamo nivo težine (0-20)
                SendCommand($"setoption name Skill Level value {skillLevel}");

                // 4. 🚀 STRUČNA POTVRDA: Pitamo motor da li je uspešno primenio podešavanje
                SendCommand("isready");

                // 5. Blokirajući čitamo strim dok nam Stockfish zvanično ne vrati 'readyok'
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
                    // Upisujemo zvaničan trijumf i u naš fajl na disku
                    MoveLoggerService.Instance.LogMessage("Stockfish", $"[UCI CONFIRMED]: Engine successfully applied Skill Level: {skillLevel}");
                }
                else
                {
                    MoveLoggerService.Instance.LogMessage("Stockfish", $"[WARNING]: Engine readyok handshake failed for Level: {skillLevel}");
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
