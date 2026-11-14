using System.Collections.Generic;
using MergeGame.Data.Validation;
using UnityEngine;

namespace MergeGame.Data.Authoring
{
    /// <summary>
    /// One merge chain. Adding a chain means creating one of these and listing it in a <see cref="GameConfigAsset"/>;
    /// adding a level means adding an element to <see cref="Levels"/>. No code refers to specific chains.
    /// </summary>
    [CreateAssetMenu(menuName = "Merge Game/Item Chain", fileName = "NewItemChain")]
    public sealed class ItemChainAsset : ScriptableObject
    {
        [Tooltip("Stable identifier used by the game logic and save data. Must be unique within a game config; do not rename once shipped.")]
        [SerializeField] private string id = string.Empty;

        [Tooltip("Player-facing name of the whole chain.")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("Relative chance of the spawn button picking this chain, compared with the other chains' weights. 0 = never spawned.")]
        [Min(0)]
        [SerializeField] private int spawnWeight = 1;

        [Tooltip("Scales the score table points of every merge and order item in this chain (1 = table value).")]
        [Min(0.01f)]
        [SerializeField] private float scoreMultiplier = 1f;

        [Tooltip("Element 0 is level 1. Merging two items of the same level makes the next one; the last element is the max level.")]
        [SerializeField] private List<ItemLevelEntry> levels = new List<ItemLevelEntry>();

        public string Id
        {
            get => id;
            internal set => id = value;
        }

        public string DisplayName
        {
            get => displayName;
            internal set => displayName = value;
        }

        public int SpawnWeight
        {
            get => spawnWeight;
            internal set => spawnWeight = value;
        }

        public float ScoreMultiplier
        {
            get => scoreMultiplier;
            internal set => scoreMultiplier = value;
        }

        /// <summary>Index 0 is level 1.</summary>
        public IReadOnlyList<ItemLevelEntry> Levels
        {
            get => levels;
            internal set => levels = new List<ItemLevelEntry>(value);
        }

        public int MaxLevel => levels.Count;

        [ContextMenu("Validate")]
        private void ValidateFromContextMenu() => ValidationLog.Report(this, GameConfigValidator.ValidateChain(this), reportSuccess: true);

#if UNITY_EDITOR
        private void OnValidate() => ValidationLog.ReportFromOnValidate(this, () => GameConfigValidator.ValidateChain(this));
#endif
    }
}
