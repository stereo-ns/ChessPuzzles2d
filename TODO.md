# ChessPuzzles2d - Technical and Feature TODO List

## 🔴 HIGH PRIORITY: State Machine Core Implementation
- [ ] Implement centralized `MatchFlowController` to act as the absolute game referee.
- [ ] Prevent `PlayerTurnState` from exiting if an illegal move is attempted.
- [ ] Explicitly map clean lifecycle states: `Enter`, `Execute`, `Exit` for all containers.

## 🎨 VISUAL & UI IMPROVEMENTS (Next Session Focus)
- [ ] **FEATURE**: Convert old debug text into a professional Move History Feed (`1. e2e4 e7e5`).
- [ ] **FEATURE**: Clean up active layout - clear the "Na potezu" label for future usage.
- [ ] **FEATURE**: Add a bottom `HBoxContainer` to dynamically render captured/captured pieces.
- [ ] **BUG**: Fix the stupid visual artifact/glitch during pawn promotion overlay activation and refreshing.
- [ ] **BUG**: Fix color blending math for squares that are simultaneously targeted by the player and occupied/played by the Bot.
- [ ] Hook dynamic localization string mappings directly into the Chess Match UI labels.

## ⚙️ FUTURE FEATURES & EXTENSIONS
- [ ] **FEATURE**: Add an "Offer Draw" button in the Match UI to instantly end endless shuffling loops against Level 0.
- [ ] **FEATURE**: Add an "UNDO" button in the Match UI that rolls back exactly 2 moves (Bot's turn + Player's blunder).
- [ ] **FEATURE**: Add side selection (White vs Black pieces) on the Main Menu interface before initializing the match payload.
- [ ] Integrate local storage configurations into `GameConfig` via static C# properties.
