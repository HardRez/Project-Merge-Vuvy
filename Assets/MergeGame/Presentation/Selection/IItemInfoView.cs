using System;

namespace MergeGame.Presentation.Selection
{
    /// <summary>The bar describing the selected item.</summary>
    public interface IItemInfoView
    {
        event Action DiscardRequested;

        void ShowItem(ItemInfo info);

        /// <summary>A closed box is selected; nothing about its content may be shown.</summary>
        void ShowHiddenItem();

        /// <summary>Nothing is selected; e.g. invite the player to tap an item.</summary>
        void ShowHint();
    }
}
