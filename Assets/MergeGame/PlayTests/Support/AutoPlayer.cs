using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.Core.Rules;
using MergeGame.Core.Session;

namespace MergeGame.PlayTests.Support
{
    /// <summary>
    /// A simple bot that plays session commands (merge the lowest pair, else spawn, else discard) to reach states that
    /// take many moves, such as a deliverable order. The views follow through the presenters as in real play, which also
    /// stresses them with many overlapping animations in a single frame.
    /// </summary>
    internal static class AutoPlayer
    {
        public static bool PlayUntilAnOrderIsDeliverable(MergeGameSession session, int maxLevelToCreate, int maxMoves = 5000)
        {
            return PlayUntil(session, maxLevelToCreate, () => session.ActiveOrders.Any(order => session.CanDeliver(order.Id)), maxMoves);
        }

        /// <summary>Builds until <paramref name="isReached"/> holds; false if it never does within the move budget.</summary>
        public static bool PlayUntil(MergeGameSession session, int maxLevelToCreate, System.Func<bool> isReached, int maxMoves = 5000)
        {
            for (var move = 0; move < maxMoves; move++)
            {
                if (isReached())
                    return true;
                if (!TryBuild(session, maxLevelToCreate))
                    return false;
            }

            return isReached();
        }

        /// <summary>Plays <paramref name="moves"/> commands at once, delivering every order that becomes ready.</summary>
        public static void PlayMoves(MergeGameSession session, int maxLevelToCreate, int moves)
        {
            for (var move = 0; move < moves; move++)
            {
                var ready = session.ActiveOrders.FirstOrDefault(order => session.CanDeliver(order.Id));
                if (ready != null)
                    session.DeliverOrder(ready.Id);
                else if (!TryBuild(session, maxLevelToCreate))
                    return;
            }
        }

        private static bool TryBuild(MergeGameSession session, int maxLevelToCreate)
        {
            if (TryMergeLowestPair(session, maxLevelToCreate))
                return true;
            if (!session.CanSpawn)
                return TryDiscardLowest(session);

            session.Spawn();
            return true;
        }

        private static bool TryMergeLowestPair(MergeGameSession session, int maxLevelToCreate)
        {
            var candidates = session.Board.Positions
                .Where(position => session.Board.TryGetItem(position, out var item) && !item.IsLocked && item.ItemId.Level < maxLevelToCreate)
                .OrderBy(position => session.Board.GetItem(position).ItemId.Level);

            foreach (var from in candidates)
            {
                foreach (var to in session.Board.Positions)
                {
                    if (to != from && session.PreviewDrop(from, to).Kind == DropOutcomeKind.Merge)
                    {
                        session.Drop(from, to);
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryDiscardLowest(MergeGameSession session)
        {
            var lowest = session.Board.Positions
                .Where(session.CanDiscard)
                .OrderBy(position => session.Board.GetItem(position).ItemId.Level)
                .Cast<GridPosition?>()
                .FirstOrDefault();
            return lowest.HasValue && session.Discard(lowest.Value).Succeeded;
        }
    }
}
