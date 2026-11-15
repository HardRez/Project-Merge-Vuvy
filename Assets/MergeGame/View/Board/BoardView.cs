using System;
using System.Collections.Generic;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Data.Visuals;
using MergeGame.Presentation.Board;
using MergeGame.Presentation.Selection;
using MergeGame.View.Audio;
using MergeGame.View.Common;
using MergeGame.View.Theme;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MergeGame.View.Board
{
    /// <summary>
    /// The grid, its pooled items and their animations. The dictionary of item views changes as soon as a command
    /// arrives, so it always mirrors the board model; items that leave the board are removed from it before their exit
    /// animation starts and can no longer be addressed. Cells and items are anchored to normalised cell rects, so the
    /// grid fits any board size and screen without layout code.
    /// </summary>
    public sealed class BoardView : MonoBehaviour, IBoardView, IBoardSelectionView
    {
        [Header("Structure")]
        [Tooltip("Keeps the board at the grid's aspect ratio inside the available area.")]
        [SerializeField] private AspectRatioFitter boardFitter;

        [Tooltip("The area the cells divide; every layer below is stretched over it.")]
        [SerializeField] private RectTransform grid;

        [SerializeField] private BoardInputSurface input;
        [SerializeField] private RectTransform cellLayer;

        [Tooltip("Above the cells, below the highlights and items: sand covering closed cells.")]
        [SerializeField] private RectTransform coverLayer;
        [SerializeField] private RectTransform itemLayer;
        [SerializeField] private RectTransform effectLayer;

        [Tooltip("Above the whole screen: the dragged item and items flying to a customer.")]
        [SerializeField] private RectTransform dragLayer;

        [SerializeField] private Image cellPrefab;
        [SerializeField] private CellCover coverPrefab;

        [Tooltip("Gap between a cell's tile and its neighbours, in canvas units.")]
        [SerializeField] private float cellInset = 5f;

        [SerializeField] private DropHighlightView dropHighlight;
        [SerializeField] private SelectionMarkerView selectionMarker;
        [SerializeField] private UiTheme theme;

        [Tooltip("Optional; without it the board is silent.")]
        [SerializeField] private SoundPlayer sounds;

        [Header("Timing (seconds)")]
        [SerializeField] private float moveSeconds = 0.2f;
        [SerializeField] private float returnSeconds = 0.3f;
        [SerializeField] private float mergeFlightSeconds = 0.13f;
        [SerializeField] private float deliveryFlightSeconds = 0.55f;
        [SerializeField] private float deliveryStaggerSeconds = 0.09f;

        [Tooltip("A closed cell opened by a merge waits this long, so the merge next to it reads first.")]
        [SerializeField] private float revealDelaySeconds = 0.2f;

        private readonly Dictionary<GridPosition, ItemView> items = new Dictionary<GridPosition, ItemView>();
        private readonly List<Image> cells = new List<Image>();
        private readonly Dictionary<GridPosition, CellCover> covers = new Dictionary<GridPosition, CellCover>();
        private readonly Dictionary<GridPosition, CellCover> pouringCovers = new Dictionary<GridPosition, CellCover>();

        private IItemVisuals visuals;
        private ItemCatalog catalog;
        private ItemView.Pool itemPool;
        private PointsPopup.Pool popupPool;
        private IOrderSlotTargets deliveryTargets;

        private BoardGeometry geometry;
        private Camera eventCamera;
        private bool isDragGesture;
        private GridPosition? hoveredCell;

        /// <summary>Where the presenter put the selection marker; it is hidden while that cell's item is being dragged.</summary>
        private GridPosition? selectedCell;
        private ItemView draggedItem;
        private Vector3 grabOffset;
        private ItemView mergeTarget;

        public event Action<GridPosition> DragStarted;
        public event Action<GridPosition?> DragMoved;
        public event Action<GridPosition?> DragReleased;
        public event Action<GridPosition> CellTapped;

        public BoardSize Size => geometry.Size;
        public int CellCount => cells.Count;
        public int ItemCount => items.Count;
        public bool IsSelectionShown => selectionMarker.IsShown;
        public bool IsDropHighlightShown => dropHighlight.IsShown;
        public bool IsDragging => draggedItem != null;

        [Inject]
        public void Construct(
            IItemVisuals itemVisuals,
            ItemCatalog itemCatalog,
            ItemView.Pool items,
            PointsPopup.Pool popups,
            IOrderSlotTargets orderSlotTargets)
        {
            visuals = itemVisuals;
            catalog = itemCatalog;
            itemPool = items;
            popupPool = popups;
            deliveryTargets = orderSlotTargets;
        }

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            input.Tapped += OnTapped;
            input.DragBegan += OnDragBegan;
            input.Dragged += OnDragged;
            input.DragEnded += OnDragEnded;
            dropHighlight.Hide();
            selectionMarker.Hide();
        }

        private void OnDestroy()
        {
            input.Tapped -= OnTapped;
            input.DragBegan -= OnDragBegan;
            input.Dragged -= OnDragged;
            input.DragEnded -= OnDragEnded;
        }

        public bool TryGetItem(GridPosition position, out ItemView item) => items.TryGetValue(position, out item);

        /// <summary>The cover of a closed cell, including one still pouring out after the cell opened.</summary>
        public bool TryGetCover(GridPosition position, out CellCover cover) =>
            covers.TryGetValue(position, out cover) || pouringCovers.TryGetValue(position, out cover);

        public Vector3 GetCellWorldCenter(GridPosition position) => grid.NormalizedToWorld(geometry.GetCell(position).Center);

        /// <summary>The tile of the cell, as laid out on the board.</summary>
        public RectTransform GetCellTile(GridPosition position) => cells[position.Y * geometry.Size.Width + position.X].rectTransform;

        public void CreateGrid(BoardSize size)
        {
            geometry = new BoardGeometry(size);
            boardFitter.aspectRatio = geometry.AspectRatio;

            foreach (var cell in cells)
                Destroy(cell.gameObject);
            cells.Clear();
            foreach (var cover in covers.Values.Concat(pouringCovers.Values))
                Destroy(cover.gameObject);
            covers.Clear();
            pouringCovers.Clear();

            for (var y = 0; y < size.Height; y++)
            {
                for (var x = 0; x < size.Width; x++)
                {
                    var cell = Instantiate(cellPrefab, cellLayer);
                    cell.name = $"Cell {x},{y}";
                    cell.color = (x + y) % 2 == 0 ? theme.BoardTileLight : theme.BoardTileDark;
                    FitToCell(cell.rectTransform, new GridPosition(x, y));
                    cells.Add(cell);
                }
            }
        }

        public void PlaceItem(GridPosition position, ItemId itemId, bool isLocked) =>
            AddItem(position, item => item.Show(itemId, visuals.GetIcon(itemId), isLocked, catalog.IsMaxLevel(itemId)));

        /// <summary>The item is placed, invisible, and the cell is buried under sand.</summary>
        public void PlaceHiddenItem(GridPosition position)
        {
            AddItem(position, item => item.ShowHidden());
            if (covers.TryGetValue(position, out var stale))
                Destroy(stale.gameObject);

            var cover = Instantiate(coverPrefab, coverLayer);
            cover.name = $"Cover {position.X},{position.Y}";
            FitToCell((RectTransform)cover.transform, position);
            cover.Show(position.X * 7 + position.Y * 3);
            covers[position] = cover;
        }

        public void RevealItem(GridPosition position, ItemId itemId)
        {
            if (!items.TryGetValue(position, out var item))
                return;

            item.Reveal(itemId, visuals.GetIcon(itemId), catalog.IsMaxLevel(itemId), revealDelaySeconds);
            if (covers.TryGetValue(position, out var cover))
            {
                covers.Remove(position);
                PourCover(position, cover, item);
                sounds.TryPlay(SoundCue.SandBreak);
            }
        }

        /// <summary>Unlike opening-layout items and revealed items, a spawned item pops in and shines for a moment.</summary>
        public void SpawnItem(GridPosition position, ItemId itemId)
        {
            var item = AddItem(position, spawned => spawned.Show(itemId, visuals.GetIcon(itemId), isLocked: false, catalog.IsMaxLevel(itemId)));
            item.PlaySpawn();
            item.Accent.PlaySpawnShine(item.DisplayedIcon);
            sounds.TryPlay(SoundCue.Spawn);
        }

        public void BeginDrag(GridPosition origin)
        {
            if (!items.TryGetValue(origin, out var item))
                return;

            draggedItem = item;
            item.Lift(dragLayer);
            sounds.TryPlay(SoundCue.PickUp);
            if (selectedCell == origin)
                selectionMarker.Hide();
        }

        public void RefuseDrag(GridPosition origin)
        {
            if (covers.TryGetValue(origin, out var cover))
                cover.PlayRefuse();
            else if (items.TryGetValue(origin, out var item))
                item.PlayRefuse();
        }

        public void ShowDropHighlight(GridPosition cell, DropHighlight highlight)
        {
            dropHighlight.Show(geometry.GetCell(cell), highlight);
            var target = highlight == DropHighlight.Merge && items.TryGetValue(cell, out var item) ? item : null;
            SetMergeTarget(target);
        }

        public void ClearDropHighlight()
        {
            dropHighlight.Hide();
            SetMergeTarget(null);
        }

        public void ReturnItem(GridPosition origin)
        {
            if (!items.TryGetValue(origin, out var item))
                return;

            ForgetDrag(item);
            item.Land(itemLayer, geometry.GetCell(origin), returnSeconds);
            sounds.TryPlay(SoundCue.Land);
            if (selectedCell == origin && !selectionMarker.IsShown)
                selectionMarker.Show(geometry.GetCell(origin));
        }

        public void MoveItem(GridPosition from, GridPosition to)
        {
            var item = TakeItem(from);
            if (item == null)
                return;

            items[to] = item;
            item.Land(itemLayer, geometry.GetCell(to), moveSeconds);
            sounds.TryPlay(SoundCue.Land);
        }

        public void MergeItems(GridPosition from, GridPosition to, ItemId resultItemId, bool unlockedTarget)
        {
            var source = TakeItem(from);
            FinishPour(to);
            if (!items.TryGetValue(to, out var target))
            {
                Release(source);
                return;
            }

            target.MergeInto(resultItemId, visuals.GetIcon(resultItemId), catalog.IsMaxLevel(resultItemId), unlockedTarget, mergeFlightSeconds);
            source?.FlyAway(effectLayer, target.WorldCenter, mergeFlightSeconds, delay: 0f, endScale: 0.9f, () => Release(source));
            sounds.TryPlayMerge(resultItemId.Level);
            // The bubble bursts when the flying item lands, so its pop is timed to that moment.
            if (unlockedTarget)
                sounds.TryPlay(SoundCue.Unlock, mergeFlightSeconds);
        }

        public void ShowPointsPopup(GridPosition cell, int points)
        {
            var popup = popupPool.Spawn();
            popup.transform.SetParent(effectLayer, worldPositionStays: false);
            popup.transform.SetAsLastSibling();
            popup.Play(GetCellWorldCenter(cell), points, () => popupPool.Despawn(popup));
        }

        public void DiscardItem(GridPosition position)
        {
            var item = TakeItem(position);
            item?.Vanish(() => Release(item));
            if (item != null)
                sounds.TryPlay(SoundCue.Discard);
        }

        public void DeliverItems(int orderSlotIndex, IReadOnlyList<GridPosition> positions)
        {
            var hasTarget = deliveryTargets.TryGetDeliveryTarget(orderSlotIndex, out var target);
            for (var i = 0; i < positions.Count; i++)
            {
                var item = TakeItem(positions[i]);
                if (item == null)
                    continue;

                if (hasTarget)
                    item.FlyAway(dragLayer, target, deliveryFlightSeconds, i * deliveryStaggerSeconds, endScale: 0.45f, () => Release(item));
                else
                    item.Vanish(() => Release(item));
            }
        }

        public void ShowSelectionMarker(GridPosition position)
        {
            selectedCell = position;
            selectionMarker.Show(geometry.GetCell(position));
        }

        public void HideSelectionMarker()
        {
            selectedCell = null;
            selectionMarker.Hide();
        }

        /// <param name="identify">Gives the pooled item its identity before it is placed.</param>
        private ItemView AddItem(GridPosition position, Action<ItemView> identify)
        {
            Release(TakeItem(position));

            var item = itemPool.Spawn();
            identify(item);
            item.Place(itemLayer, geometry.GetCell(position));
            items.Add(position, item);
            return item;
        }

        /// <summary>Removes the item from the board's bookkeeping (it stays on screen for its exit animation).</summary>
        private ItemView TakeItem(GridPosition position)
        {
            if (!items.TryGetValue(position, out var item))
                return null;

            items.Remove(position);
            ForgetDrag(item);
            return item;
        }

        private void Release(ItemView item)
        {
            if (item != null && item.gameObject.activeSelf)
                itemPool.Despawn(item);
        }

        /// <summary>
        /// The sand pours above the item layer, so the item appears behind it and is uncovered grain by grain. When the
        /// cell is empty the cover is destroyed and the item, if it is still the one revealed here, settles.
        /// </summary>
        private void PourCover(GridPosition position, CellCover cover, ItemView revealed)
        {
            cover.transform.SetParent(effectLayer, worldPositionStays: false);
            FitToCell((RectTransform)cover.transform, position);
            pouringCovers[position] = cover;
            cover.Pour(revealDelaySeconds, () =>
            {
                pouringCovers.Remove(position);
                Destroy(cover.gameObject);
                if (items.TryGetValue(position, out var item) && item == revealed)
                    item.PlaySettle();
            });
        }

        /// <summary>Cuts a pour short when the cell is used before the sand is gone.</summary>
        private void FinishPour(GridPosition position)
        {
            if (pouringCovers.TryGetValue(position, out var cover))
                cover.Finish();
        }

        /// <summary>Lays a cell-sized element (tile or cover) over the cell, with the gap between neighbouring tiles.</summary>
        private void FitToCell(RectTransform element, GridPosition position)
        {
            var rect = geometry.GetCell(position);
            element.StretchOver(rect.Min, rect.Max);
            element.offsetMin = Vector2.one * cellInset;
            element.offsetMax = -Vector2.one * cellInset;
        }

        /// <summary>The item the dragged one would merge into lights up; only one at a time.</summary>
        private void SetMergeTarget(ItemView target)
        {
            if (mergeTarget == target)
                return;

            if (mergeTarget != null)
                mergeTarget.Accent.Stop(AccentKind.MergeTarget);
            mergeTarget = target;
            if (mergeTarget != null)
                mergeTarget.Accent.PlayMergeTarget(mergeTarget.DisplayedIcon);
        }

        private void ForgetDrag(ItemView item)
        {
            if (draggedItem == item)
                draggedItem = null;
        }

        private void OnTapped(Vector2 screenPoint)
        {
            var cell = FindCell(screenPoint);
            if (cell.HasValue)
                CellTapped?.Invoke(cell.Value);
        }

        private void OnDragBegan(Vector2 pressPoint)
        {
            var origin = FindCell(pressPoint);
            if (!origin.HasValue)
                return;

            isDragGesture = true;
            hoveredCell = origin;
            DragStarted?.Invoke(origin.Value);
            if (draggedItem != null)
                grabOffset = draggedItem.WorldCenter - ScreenToWorld(pressPoint);
        }

        private void OnDragged(Vector2 screenPoint)
        {
            if (!isDragGesture)
                return;

            if (draggedItem != null)
                draggedItem.DragTo(ScreenToWorld(screenPoint) + grabOffset);

            var cell = FindCell(screenPoint);
            if (cell == hoveredCell)
                return;

            hoveredCell = cell;
            DragMoved?.Invoke(cell);
        }

        private void OnDragEnded(Vector2 screenPoint)
        {
            if (!isDragGesture)
                return;

            isDragGesture = false;
            hoveredCell = null;
            var releasedItem = draggedItem;
            DragReleased?.Invoke(FindCell(screenPoint));

            // The presenter always answers a release; this only guards against an item being left floating.
            if (releasedItem != null && releasedItem == draggedItem && releasedItem.IsLifted)
                ReturnItem(PositionOf(releasedItem));
        }

        private GridPosition PositionOf(ItemView item)
        {
            foreach (var pair in items)
            {
                if (pair.Value == item)
                    return pair.Key;
            }

            throw new InvalidOperationException("The item is not on the board.");
        }

        private GridPosition? FindCell(Vector2 screenPoint)
        {
            var normalized = grid.ScreenToNormalized(screenPoint, eventCamera);
            return normalized.HasValue ? geometry.FindCell(normalized.Value) : null;
        }

        private Vector3 ScreenToWorld(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(dragLayer, screenPoint, eventCamera, out var world);
            return world;
        }
    }
}
