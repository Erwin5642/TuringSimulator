namespace ITS
{
    /// <summary>Cached compact tape and authored program snapshots for ITS <c>/ask</c>.</summary>
    public interface IItsBenchStateCache
    {
        void MarkTapeDirty();
        void MarkProgramDirty();
        AskTapeDto GetTapeForAsk();
        AskProgramDto GetProgramForAsk();
    }
}
