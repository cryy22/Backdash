namespace Backdash.Backends;

/*
 * The SessionCallbacks structure contains the callback functions that
 * your application must implement.  Backdash will periodically call these
 * functions during the game.  All callback functions must be implemented.
 */
public interface IRollbackHandler<TGameState> where TGameState : notnull
{
    /*
     The client should allocate a buffer, copy the
     * entire contents of the current game state into it, and copy the
     * length into the *len parameter.  Optionally, the client can compute
     * a checksum of the data and store it in the *checksum argument.
     */
    bool SaveGameState(int frame, ref int checksum, out TGameState buffer);

    /*
     * Backdash will call this function at the beginning
     * of a rollback.  The buffer and len parameters contain a previously
     * saved state returned from the save_game_state function.  The client
     * should make the current game state match the state contained in the
     * buffer.
     */
    bool LoadGameState(in TGameState buffer);

    /*
     * Used in diagnostic testing.
     */
    bool LogGameState(string filename, in TGameState buffer);

    /*
     * advance_frame - Called during a rollback.  You should advance your game
     * state by exactly one frame.  Before each frame, call SynchronizeInput
     * to retrieve the inputs you should use for that frame.  After each frame,
     * you should call AdvanceFrame to notify Backdash that you're
     * finished.
     */
    bool AdvanceFrame();

    /*
     * Notification that something has happened.
     */
    bool OnEvent(BackdashEvent info);
}
