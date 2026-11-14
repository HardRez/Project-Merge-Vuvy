using System.Collections.Generic;
using MergeGame.Data.Validation;
using UnityEngine;

namespace MergeGame.Data.Authoring
{
    /// <summary>One playable configuration: board, content, scoring, orders and opening layout.</summary>
    [CreateAssetMenu(menuName = "Merge Game/Game Config", fileName = "GameConfig")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        [Tooltip("Number of columns.")]
        [Min(1)]
        [SerializeField] private int boardWidth = 5;

        [Tooltip("Number of rows.")]
        [Min(1)]
        [SerializeField] private int boardHeight = 5;

        [Tooltip("Every chain in the game. List order is the order of the spawn weight roll and of any chain lists in the UI.")]
        [SerializeField] private List<ItemChainAsset> chains = new List<ItemChainAsset>();

        [Tooltip("Merge points by created level; must reach the longest chain's max level.")]
        [SerializeField] private ScoreTableAsset scoreTable;

        [Tooltip("Optional. Leave empty for a game without customer orders.")]
        [SerializeField] private OrderSettingsAsset orders;

        [Tooltip("Optional. Leave empty to start with an empty board.")]
        [SerializeField] private BoardLayoutAsset initialLayout;

        public int BoardWidth
        {
            get => boardWidth;
            internal set => boardWidth = value;
        }

        public int BoardHeight
        {
            get => boardHeight;
            internal set => boardHeight = value;
        }

        public IReadOnlyList<ItemChainAsset> Chains
        {
            get => chains;
            internal set => chains = new List<ItemChainAsset>(value);
        }

        public ScoreTableAsset ScoreTable
        {
            get => scoreTable;
            internal set => scoreTable = value;
        }

        /// <summary>Null when the game has no customer orders.</summary>
        public OrderSettingsAsset Orders
        {
            get => orders;
            internal set => orders = value;
        }

        /// <summary>Null when the game starts with an empty board.</summary>
        public BoardLayoutAsset InitialLayout
        {
            get => initialLayout;
            internal set => initialLayout = value;
        }

        [ContextMenu("Validate")]
        private void ValidateFromContextMenu() => ValidationLog.Report(this, GameConfigValidator.Validate(this), reportSuccess: true);

#if UNITY_EDITOR
        private void OnValidate() => ValidationLog.ReportFromOnValidate(this, () => GameConfigValidator.Validate(this));
#endif
    }
}
