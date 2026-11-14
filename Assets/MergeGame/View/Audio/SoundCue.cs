namespace MergeGame.View.Audio
{
    /// <summary>
    /// A moment the player hears. Views play cues while they animate the commands they receive, so sound always lines up
    /// with what is on screen and the presenters stay unaware of audio.
    /// </summary>
    public enum SoundCue
    {
        ButtonClick,
        Spawn,
        PickUp,
        Land,
        Unlock,
        SandBreak,
        Discard,
        OrderComplete,
        Notice
    }
}
