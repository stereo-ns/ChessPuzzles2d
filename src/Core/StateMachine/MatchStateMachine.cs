using ChessPuzzles2d.Services;

namespace ChessPuzzles2d.Core.StateMachine
{
    // Isolated finite state machine manager responsible for gameplay flow execution
    public class MatchStateMachine
    {
        // Tracks the currently active structural state in memory
        public IGameState CurrentState { get; private set; }

        // Core transition engine method that forces strict lifecycle triggers
        public void ChangeState(IGameState newState)
        {
            if (newState == null) return;

            // 1. Fire exit hook on the old state if it exists
            if (CurrentState != null)
            {
                MoveLoggerService.Instance.LogMessage("StateMachine", $"Exiting state: {CurrentState.GetType().Name}");
                CurrentState.StateExited();
            }

            // 2. Swapping the reference pointer
            CurrentState = newState;

            // 3. Fire enter hook on the newly mounted state
            MoveLoggerService.Instance.LogMessage("StateMachine", $"Entering state: {CurrentState.GetType().Name}");
            CurrentState.StateEnter();
        }

        // Ticks the operational execution loop of the mounted state
        public void Update()
        {
            CurrentState?.StateExecute();
        }
    }
}
