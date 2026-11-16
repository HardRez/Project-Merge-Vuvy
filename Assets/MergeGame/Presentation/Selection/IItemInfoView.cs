using System;

namespace MergeGame.Presentation.Selection
{
    public interface IItemInfoView
    {
        event Action DiscardRequested;

        void ShowItem(ItemInfo info);

        /// <summary>A sand-covered cell is selected; nothing about its content may be shown.</summary>
        void ShowHiddenItem();

        /// <summary>Nothing is selected; e.g. invite the player to tap an item.</summary>
        void ShowHint();
    }
}
