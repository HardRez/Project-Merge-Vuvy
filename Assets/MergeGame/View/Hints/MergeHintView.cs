using MergeGame.Core.Grid;
using MergeGame.Presentation.Hints;
using MergeGame.View.Board;
using UnityEngine;

namespace MergeGame.View.Hints
{
    /// <summary>
    /// Points out a possible merge after the player has been idle: the two items pulse toward each other once, in sync,
    /// and come back to rest by themselves. A new hint or <see cref="HideMergeHint"/> cancels one still playing. Items
    /// that leave the board or get picked up drop their accent themselves, so a hint never leaves residue on them.
    /// </summary>
    public sealed class MergeHintView : MonoBehaviour, IMergeHintView
    {
        [SerializeField] private BoardView board;

        private ItemView source;
        private ItemView target;

        /// <summary>True while the last hint's pulse is still playing on its items.</summary>
        public bool IsPlaying => IsHinting(source) || IsHinting(target);

        public void ShowMergeHint(GridPosition from, GridPosition to)
        {
            HideMergeHint();
            if (!board.TryGetItem(from, out var fromItem) || !board.TryGetItem(to, out var toItem))
                return;

            source = fromItem;
            target = toItem;
            var towardTarget = target.WorldCenter - source.WorldCenter;
            source.Accent.PlayMergeHint(DirectionInParentOf(source, towardTarget));
            target.Accent.PlayMergeHint(DirectionInParentOf(target, -towardTarget));
        }

        public void HideMergeHint()
        {
            if (source != null)
                source.Accent.Stop(AccentKind.MergeHint);
            if (target != null)
                target.Accent.Stop(AccentKind.MergeHint);
            source = null;
            target = null;
        }

        private static bool IsHinting(ItemView item) => item != null && item.Accent.Current == AccentKind.MergeHint;

        private static Vector2 DirectionInParentOf(ItemView item, Vector3 worldDirection) =>
            item.transform.InverseTransformVector(worldDirection);
    }
}
