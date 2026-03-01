using System.Collections;

public interface ICommand
{
    /// <summary> Kick off execution. </summary>
    IEnumerator Execute();

    /// <summary> Called by the scheduler to pause processing. </summary>
    void Pause();

    /// <summary> Called by the scheduler to resume after a pause. </summary>
    void Resume();

    /// <summary> Immediately abort and clean up. </summary>
    void Halt();

    /// <summary> Reset any internal state to allow re‑execution. </summary>
    void Reset();
}