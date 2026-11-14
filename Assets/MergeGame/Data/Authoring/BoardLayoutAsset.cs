using System.Collections.Generic;
using UnityEngine;

namespace MergeGame.Data.Authoring
{
    /// <summary>The items on the board when a game starts.</summary>
    [CreateAssetMenu(menuName = "Merge Game/Board Layout", fileName = "BoardLayout")]
    public sealed class BoardLayoutAsset : ScriptableObject
    {
        [Tooltip("At most one item per cell; every cell must lie inside the board of the game config that uses this layout.")]
        [SerializeField] private List<InitialPlacementEntry> placements = new List<InitialPlacementEntry>();

        public IReadOnlyList<InitialPlacementEntry> Placements
        {
            get => placements;
            internal set => placements = new List<InitialPlacementEntry>(value);
        }
    }
}
