using Godot;
using System;

namespace ChessPuzzles2d.Services
{
    // Decoupled C# Audio Service handling game-wide structural sound triggers
    public class AudioService
    {
        private static AudioService _instance;
        public static AudioService Instance => _instance ??= new AudioService();

        private AudioService()
        {
            MoveLoggerService.Instance.LogMessage("Audio", "AudioService subsystem mounted and ready.");
        }

        // Triggered when any legal piece move is executed on the board
        public void PlayMove()
        {
            MoveLoggerService.Instance.LogMessage("Audio", "Trigger -> PLAY_MOVE_SOUND");
            // TODO: Link actual AudioStreamPlayer node placeholder later
        }

        // Triggered when a piece captures an opponent's piece
        public void PlayCapture()
        {
            MoveLoggerService.Instance.LogMessage("Audio", "Trigger -> PLAY_CAPTURE_SOUND");
            // TODO: Link actual AudioStreamPlayer node placeholder later
        }

        // Triggered when a king is placed in check
        public void PlayCheck()
        {
            MoveLoggerService.Instance.LogMessage("Audio", "Trigger -> PLAY_CHECK_SOUND");
            // TODO: Link actual AudioStreamPlayer node placeholder later
        }

        // Triggered on UI button clicks across menus and store screens
        public void PlayUiClick()
        {
            MoveLoggerService.Instance.LogMessage("Audio", "Trigger -> PLAY_UI_CLICK_SOUND");
            // TODO: Link actual AudioStreamPlayer node placeholder later
        }
    }
}
