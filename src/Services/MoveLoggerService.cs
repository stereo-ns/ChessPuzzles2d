using Godot;
using System;
using System.IO;

namespace ChessPuzzles2d.Services
{
    // C# Black box for persistent file IO session logging
    public class MoveLoggerService
    {
        private static MoveLoggerService _instance;
        public static MoveLoggerService Instance => _instance ??= new MoveLoggerService();

        private readonly string _logFilePath;

        private MoveLoggerService()
        {
            // Translates Godot user path to absolute Linux system path safely
            _logFilePath = ProjectSettings.GlobalizePath("user://chess_system.log");

            try
            {
                File.WriteAllText(_logFilePath, $"=== CHESS SYSTEM LOG SESSION STARTED: {DateTime.Now} ===\n");
                GD.Print($"[BOOTSTRAP]: MoveLoggerService active. Output: {_logFilePath}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[LOGGER ERROR]: Cannot open log stream: {ex.Message}");
            }
        }

        public void LogMessage(string context, string message)
        {
            string formattedLine = $"[{DateTime.Now:HH:mm:ss.fff}] [{context.ToUpper()}]: {message}";

            // Console print
            GD.Print(formattedLine);

            // Hard disk write for 'tail -f' debugging in Slackware terminal
            try
            {
                File.AppendAllText(_logFilePath, formattedLine + "\n");
            }
            catch
            {
                // Soft landing if system file IO is locked
            }
        }
    }
}
