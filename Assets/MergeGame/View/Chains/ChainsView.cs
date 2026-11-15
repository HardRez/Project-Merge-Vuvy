using System.Collections.Generic;
using MergeGame.Data.Visuals;
using MergeGame.Presentation.Chains;
using MergeGame.View.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MergeGame.View.Chains
{
    /// <summary>
    /// The info button and its popup (<see cref="PopupWindow"/>): a tab for every item chain, showing the chain's best
    /// item, and under the tabs the chosen chain from its first level to its last, big, in two rows (the second row one
    /// shorter or as long). Tabs and level cells are copied from templates in the popup. The first chain is chosen at
    /// first; the choice is kept while the popup is closed and opened again.
    /// </summary>
    public sealed class ChainsView : MonoBehaviour, IChainsView
    {
        [SerializeField] private Button openButton;
        [SerializeField] private PopupWindow popup;

        [SerializeField] private RectTransform tabContainer;
        [SerializeField] private ChainTabView tabTemplate;

        [Tooltip("The chosen chain's name, between lace flourishes.")]
        [SerializeField] private TMP_Text chainTitle;

        [Tooltip("The rows the chosen chain's levels are laid out in: the first half of the levels, then the rest.")]
        [SerializeField] private RectTransform firstRow;
        [SerializeField] private RectTransform secondRow;
        [SerializeField] private ChainLevelCellView cellTemplate;

        private readonly List<ChainTabView> tabs = new List<ChainTabView>();
        private readonly List<ChainLevelCellView> cells = new List<ChainLevelCellView>();
        private IReadOnlyList<ChainLevels> chains = new List<ChainLevels>();
        private IItemVisuals visuals;

        public bool IsOpen => popup.IsOpen;
        public Button OpenButton => openButton;
        public Button CloseButton => popup.CloseButton;
        public IReadOnlyList<ChainTabView> Tabs => tabs;
        public IReadOnlyList<ChainLevelCellView> Cells => cells;
        public string ChainTitle => chainTitle.text;

        /// <summary>The chain shown now, as an index into the chains shown; -1 before any.</summary>
        public int ChosenIndex { get; private set; } = -1;

        [Inject]
        public void Construct(IItemVisuals itemVisuals) => visuals = itemVisuals;

        private void Awake()
        {
            openButton.onClick.AddListener(popup.Open);
            tabTemplate.gameObject.SetActive(false);
            cellTemplate.gameObject.SetActive(false);
        }

        public void ShowChains(IReadOnlyList<ChainLevels> chainLevels)
        {
            chains = chainLevels;
            foreach (var tab in tabs)
                Discard(tab.gameObject);
            tabs.Clear();

            for (var i = 0; i < chains.Count; i++)
            {
                var chain = chains[i];
                var tab = Instantiate(tabTemplate, tabContainer);
                tab.name = chain.ChainId;
                tab.gameObject.SetActive(true);
                tab.Show(visuals.GetIcon(chain.Levels[chain.Levels.Count - 1]), visuals.GetChainDisplayName(chain.ChainId));
                var index = i;
                tab.Clicked += () => Choose(index);
                tabs.Add(tab);
            }

            Choose(Mathf.Clamp(ChosenIndex, 0, chains.Count - 1));
        }

        /// <summary>Shows the chain at <paramref name="index"/> under the tabs.</summary>
        public void Choose(int index)
        {
            if (index < 0 || index >= chains.Count)
                return;

            ChosenIndex = index;
            for (var i = 0; i < tabs.Count; i++)
                tabs[i].SetChosen(i == index);

            var chain = chains[index];
            chainTitle.text = visuals.GetChainDisplayName(chain.ChainId);
            foreach (var cell in cells)
                Discard(cell.gameObject);
            cells.Clear();

            var levels = chain.Levels;
            var firstRowLength = (levels.Count + 1) / 2;
            for (var i = 0; i < levels.Count; i++)
            {
                var inFirstRow = i < firstRowLength;
                var cell = Instantiate(cellTemplate, inFirstRow ? firstRow : secondRow);
                cell.name = $"Level {i + 1}";
                cell.gameObject.SetActive(true);
                var endsRow = i == firstRowLength - 1 || i == levels.Count - 1;
                cell.Show(visuals.GetIcon(levels[i]), i + 1, isLast: i == levels.Count - 1, visuals.GetDisplayName(levels[i]), pointsOn: !endsRow);
                cells.Add(cell);
            }
        }

        // Hidden at once, so the rows lay out without it in this very frame; destroyed at the end of the frame.
        private static void Discard(GameObject copy)
        {
            copy.SetActive(false);
            Destroy(copy);
        }
    }
}
