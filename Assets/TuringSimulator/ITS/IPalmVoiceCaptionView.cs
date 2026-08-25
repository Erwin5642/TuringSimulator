using TuringSimulator.GameFlow.Events;

public interface IPalmVoiceCaptionView
{
    void HandleGesture(HandGesturePerformedEventData eventData);
    void HandlePartial(PartialTranscriptionEventData eventData);
    void HandleCaptureStopped(VoiceCaptureStoppedEventData eventData);
}
