# ChessPuzzles2d - Technical and Feature TODO List

## 🔴 HIGH PRIORITY: State Machine Core Implementation
- [ ] Implement centralized `MatchFlowController` to act as the absolute game referee.
- [ ] Prevent `PlayerTurnState` from exiting if an illegal or self-checking move is attempted.
- [ ] Explicitly map clean lifecycle states: `Enter`, `Execute`, `Exit` for all containers.

## 🎨 VISUAL & UI IMPROVEMENTS
- [ ] **REFACTOR**: Replace the ugly dual-blue squares for the Bot's last move with a refined, professional asset or subtle outline.
- [ ] Hook dynamic localization string mappings directly into the Chess Match UI labels.

## ⚙️ FUTURE FEATURES & EXTENSIONS
- [ ] **FEATURE**: Add side selection (White vs Black pieces) on the Main Menu interface before initializing the match payload.
- [ ] Integrate local storage configurations into `GameConfig` via static C# properties.
## ⚙️ FUTURE FEATURES & EXTENSIONS
- [ ] **FEATURE**: Add an "UNDO" button in the Match UI that rolls back exactly 2 moves (Bot's turn + Player's blunder).
- [ ] Add side selection (White vs Black pieces) on the Main Menu interface.
