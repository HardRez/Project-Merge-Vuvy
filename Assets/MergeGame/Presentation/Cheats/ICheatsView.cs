using System;

namespace MergeGame.Presentation.Cheats
{
    /// <summary>The cheats popup, opened from the settings. Opening and closing it is the view's own business.</summary>
    public interface ICheatsView
    {
        /// <summary>The player asked to throw the saved game away and start over.</summary>
        event Action ResetSaveRequested;
    }
}
