using Godot;
using System;
using ChessPuzzles2d.Services;

namespace ChessPuzzles2d.Utils
{
    // High-utility validation helper responsible for early detection of unassigned Inspector references
    public static class ValidationUtil
    {
        // Validates a batch of Godot objects and forces an immediate application crash if any node is null
        public static void ValidateReferences(string context, params (Node node, string nodeName)[] references)
        {
            if (references == null) return;

            foreach (var item in references)
            {
                if (item.node == null)
                {
                    string errorMessage = $"[FATAL SCENE ERROR] inside '{context}': Inspector reference for '{item.nodeName}' is MISSING (null)! Execution halted.";

                    // 1. Zapisujemo poslednji krik u naš fajl na disku pre nego što sve eksplodira
                    MoveLoggerService.Instance.LogMessage("Validation", errorMessage);

                    // 2. 🚀 CRVENI VRISAK: Bacamo eksplicitan sistemski izuzetak koji momentalno 
                    // zamrzava igru, boji Godot debugger u crveno i prisilno zaustavlja aplikaciju!
                    throw new InvalidOperationException(errorMessage);
                }
            }
        }
    }
}
