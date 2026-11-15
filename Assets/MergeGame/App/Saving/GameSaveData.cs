using System;
using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
using MergeGame.Core.Session;

namespace MergeGame.App.Saving
{
    /// <summary>
    /// The save file's JSON shape, written and read with <see cref="UnityEngine.JsonUtility"/>: plain public fields and
    /// lists only, which Unity serializes the same way on every platform, IL2CPP builds for iOS and Android included,
    /// without reflection that code stripping could break. <see cref="version"/> lets a later build migrate an older file.
    /// </summary>
    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        /// <summary>When the file was written, as ISO 8601 UTC; informational only.</summary>
        public string savedAtUtc;

        public int score;
        public int nextOrderId = 1;
        public List<SavedItem> items = new List<SavedItem>();
        public List<SavedOrder> orders = new List<SavedOrder>();

        public static GameSaveData From(GameSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var data = new GameSaveData
            {
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                score = snapshot.Score,
                nextOrderId = snapshot.NextOrderId
            };
            foreach (var item in snapshot.Items)
            {
                data.items.Add(new SavedItem
                {
                    x = item.Position.X,
                    y = item.Position.Y,
                    chain = item.ItemId.ChainId,
                    level = item.ItemId.Level,
                    locked = item.IsLocked,
                    hidden = item.IsHidden
                });
            }

            foreach (var order in snapshot.Orders)
            {
                var saved = new SavedOrder { id = order.Id, customer = order.CustomerIndex, reward = order.RewardPoints };
                foreach (var requirement in order.Requirements)
                    saved.requirements.Add(new SavedItemId { chain = requirement.ChainId, level = requirement.Level });
                data.orders.Add(saved);
            }

            return data;
        }

        /// <summary>Turns the file back into a snapshot; whether it fits the game is checked by <see cref="MergeGameSession.Resume"/>.</summary>
        /// <exception cref="FormatException">The file is from an unknown version or holds values no game can have.</exception>
        public GameSnapshot ToSnapshot()
        {
            if (version < 1 || version > CurrentVersion)
                throw new FormatException($"Save version {version} is not supported (this build reads 1..{CurrentVersion}).");

            try
            {
                var placements = new List<InitialPlacement>(items?.Count ?? 0);
                foreach (var item in items ?? new List<SavedItem>())
                {
                    if (item == null)
                        throw new FormatException("The save holds an empty item.");
                    placements.Add(new InitialPlacement(new GridPosition(item.x, item.y), new ItemId(item.chain, item.level), item.locked, item.hidden));
                }

                var savedOrders = new List<Order>(orders?.Count ?? 0);
                foreach (var order in orders ?? new List<SavedOrder>())
                {
                    if (order == null)
                        throw new FormatException("The save holds an empty order.");
                    var requirements = new List<ItemId>(order.requirements?.Count ?? 0);
                    foreach (var requirement in order.requirements ?? new List<SavedItemId>())
                    {
                        if (requirement == null)
                            throw new FormatException($"Saved order {order.id} holds an empty requirement.");
                        requirements.Add(new ItemId(requirement.chain, requirement.level));
                    }

                    savedOrders.Add(new Order(order.id, order.customer, requirements, order.reward));
                }

                return new GameSnapshot(score, placements, savedOrders, nextOrderId);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException($"The save holds invalid data: {exception.Message}", exception);
            }
        }
    }

    [Serializable]
    public sealed class SavedItem
    {
        public int x;
        public int y;
        public string chain;
        public int level;
        public bool locked;
        public bool hidden;
    }

    [Serializable]
    public sealed class SavedOrder
    {
        public int id;
        public int customer;
        public List<SavedItemId> requirements = new List<SavedItemId>();

        /// <summary>Kept so the file reads clearly; a resumed order is repriced from the current content.</summary>
        public int reward;
    }

    [Serializable]
    public sealed class SavedItemId
    {
        public string chain;
        public int level;
    }
}
