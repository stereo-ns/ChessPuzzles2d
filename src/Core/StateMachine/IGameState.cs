namespace ChessPuzzles2d.Core.StateMachine
{
    // Execution contract for atomized gameplay engine states
    public interface IGameState
    {
        // Executed instantly when the machine transitions INTO this state
        void StateEnter();

        // Executed on every gameplay tick or operational update loop
        void StateExecute();

        // Executed instantly when the machine transitions OUT of this state
        void StateExited();
    }
}
