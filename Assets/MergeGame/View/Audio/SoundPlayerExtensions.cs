namespace MergeGame.View.Audio
{
    /// <summary>Sound is optional for every view: a view without a player stays silent instead of failing.</summary>
    public static class SoundPlayerExtensions
    {
        public static void TryPlay(this SoundPlayer player, SoundCue cue)
        {
            if (player != null)
                player.Play(cue);
        }

        public static void TryPlay(this SoundPlayer player, SoundCue cue, float delaySeconds)
        {
            if (player != null)
                player.Play(cue, delaySeconds);
        }

        public static void TryPlayMerge(this SoundPlayer player, int level)
        {
            if (player != null)
                player.PlayMerge(level);
        }
    }
}
