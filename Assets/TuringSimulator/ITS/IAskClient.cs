/// <summary>
/// Contract for posting student questions to the ITS <c>/ask</c> endpoint.
/// </summary>
public interface IAskClient
{
    bool CanPostAsk { get; }
}
