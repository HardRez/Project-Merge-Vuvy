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
        Notice,

        /// <summary>A bill of an order's money lands in the score pill; each bill rings on its own.</summary>
        CashLand,

        /// <summary>An order's money bursts out of its reward as bills; once per payment.</summary>
        CashBurst,

        /// <summary>The bills of an order's money take off for the score pill; once per payment.</summary>
        CashSwoosh,

        /// <summary>A customer pops in behind the main menu's counter; each one on its own.</summary>
        CustomerAppear
    }
}
