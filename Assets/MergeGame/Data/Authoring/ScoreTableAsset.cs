using System.Collections.Generic;
using UnityEngine;

namespace MergeGame.Data.Authoring
{
    /// <summary>Points for creating an item by merging, by the created item's level.</summary>
    [CreateAssetMenu(menuName = "Merge Game/Score Table", fileName = "ScoreTable")]
    public sealed class ScoreTableAsset : ScriptableObject
    {
        [Tooltip("Element 0 is level 1, element 1 is level 2, and so on. Must not decrease and must reach the longest chain's max level. " +
                 "Each chain's score multiplier is applied on top.")]
        [SerializeField] private List<int> pointsByLevel = new List<int>();

        /// <summary>Index 0 is level 1.</summary>
        public IReadOnlyList<int> PointsByLevel
        {
            get => pointsByLevel;
            internal set => pointsByLevel = new List<int>(value);
        }
    }
}
