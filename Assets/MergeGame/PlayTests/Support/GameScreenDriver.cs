using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.Core.Session;
using MergeGame.View.Board;
using MergeGame.View.Notices;
using MergeGame.View.Orders;
using MergeGame.View.Score;
using MergeGame.View.Selection;
using MergeGame.View.Spawning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace MergeGame.PlayTests.Support
{
    /// <summary>Plays the real Game scene through its views, the way a player would.</summary>
    internal sealed class GameScreenDriver
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        private GameScreenDriver()
        {
            Board = Object.FindAnyObjectByType<BoardView>();
            Input = Board.GetComponentInChildren<BoardInputSurface>();
            Spawn = Object.FindAnyObjectByType<SpawnView>();
            Score = Object.FindAnyObjectByType<ScoreView>();
            Orders = Object.FindAnyObjectByType<OrdersView>();
            Info = Object.FindAnyObjectByType<ItemInfoView>();
            Notices = Object.FindAnyObjectByType<NoticeView>();
            Camera = Board.GetComponentInParent<Canvas>().rootCanvas.worldCamera;
        }

        public BoardView Board { get; }
        public BoardInputSurface Input { get; }
        public SpawnView Spawn { get; }
        public ScoreView Score { get; }
        public OrdersView Orders { get; }
        public ItemInfoView Info { get; }
        public NoticeView Notices { get; }
        public Camera Camera { get; }

        /// <summary>The session behind the screen; for test set-up and assertions, never to drive the UI.</summary>
        public MergeGameSession Session => Resolve<MergeGameSession>();

        /// <summary>A binding of the game scene's container; for set-up and assertions, never to drive the UI.</summary>
        public T Resolve<T>() => Object.FindAnyObjectByType<SceneContext>().Container.Resolve<T>();

        public static IEnumerator LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);

            // Awake (injection) and Start (presenter initialization) of the new scene.
            yield return null;
            yield return null;
        }

        public static IEnumerator OpenGame(Action<GameScreenDriver> ready)
        {
            yield return LoadScene(GameScene);
            ready(new GameScreenDriver());
        }

        public static void Click(Selectable selectable)
        {
            var eventData = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(selectable.gameObject, eventData, ExecuteEvents.pointerClickHandler);
        }

        public static IEnumerator WaitSeconds(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
                yield return null;
        }

        public void ClickSpawn() => Click(Spawn.GetComponentInChildren<Button>());

        public Vector2 ScreenPointOf(GridPosition cell) => RectTransformUtility.WorldToScreenPoint(Camera, Board.GetCellWorldCenter(cell));

        public void Tap(GridPosition cell) => PointerGesture.Tap(Input.gameObject, ScreenPointOf(cell));

        /// <summary>Presses on <paramref name="from"/> and drags in steps onto <paramref name="to"/>, without releasing.</summary>
        public IEnumerator DragOnto(GridPosition from, GridPosition to, Action<PointerGesture> holding, int steps = 6)
        {
            var start = ScreenPointOf(from);
            var end = ScreenPointOf(to);
            var gesture = PointerGesture.Press(Input.gameObject, start);
            gesture.BeginDrag();
            for (var step = 1; step <= steps; step++)
            {
                gesture.MoveTo(Vector2.Lerp(start, end, step / (float)steps));
                yield return null;
            }

            holding(gesture);
        }

        public IEnumerator DragAndDrop(GridPosition from, GridPosition to)
        {
            PointerGesture held = null;
            yield return DragOnto(from, to, gesture => held = gesture);
            held.Release();
        }

        public IEnumerable<GridPosition> Cells()
        {
            for (var y = 0; y < Board.Size.Height; y++)
            {
                for (var x = 0; x < Board.Size.Width; x++)
                    yield return new GridPosition(x, y);
            }
        }

        /// <summary>Finds an unlocked item to drag and another item satisfying <paramref name="isTarget"/>.</summary>
        public bool TryFindPair(Func<ItemView, ItemView, bool> isTarget, out GridPosition from, out GridPosition to)
        {
            foreach (var source in Cells())
            {
                if (!Board.TryGetItem(source, out var dragged) || dragged.IsLocked)
                    continue;

                foreach (var target in Cells())
                {
                    if (target != source && Board.TryGetItem(target, out var other) && isTarget(dragged, other))
                    {
                        from = source;
                        to = target;
                        return true;
                    }
                }
            }

            from = to = default;
            return false;
        }

        public bool TryFindMergePair(out GridPosition from, out GridPosition to) =>
            TryFindPair(CanMerge, out from, out to);

        /// <summary>A merge onto a webbed item that has a closed box next to it, so the merge opens the box.</summary>
        public bool TryFindBoxOpeningMerge(out GridPosition from, out GridPosition to)
        {
            foreach (var target in Cells())
            {
                if (!Board.TryGetItem(target, out var webbed) || !webbed.IsLocked || webbed.IsHidden || !Neighbours(target).Any(IsBox))
                    continue;
                if (TryFindPair((dragged, other) => other == webbed && CanMerge(dragged, other), out from, out to))
                    return true;
            }

            from = to = default;
            return false;
        }

        /// <summary>The cells whose item an active order would take, by the session's own matching.</summary>
        public HashSet<GridPosition> OrderMatchedCells()
        {
            var session = Session;
            var cells = new HashSet<GridPosition>();
            foreach (var order in session.ActiveOrders)
            {
                foreach (var cell in session.GetRequirementMatches(order.Id))
                {
                    if (cell.HasValue)
                        cells.Add(cell.Value);
                }
            }

            return cells;
        }

        public bool IsBox(GridPosition cell) => Board.TryGetItem(cell, out var item) && item.IsHidden;

        /// <summary>Equal, visible items below the last level: dropping one on the other merges them.</summary>
        public static bool CanMerge(ItemView dragged, ItemView target) =>
            !target.IsHidden && dragged.ItemId == target.ItemId && !dragged.IsMaxLevel;

        public static IEnumerable<GridPosition> Neighbours(GridPosition cell)
        {
            yield return new GridPosition(cell.X, cell.Y - 1);
            yield return new GridPosition(cell.X - 1, cell.Y);
            yield return new GridPosition(cell.X + 1, cell.Y);
            yield return new GridPosition(cell.X, cell.Y + 1);
        }

        public GridPosition FirstEmptyCell()
        {
            foreach (var cell in Cells())
            {
                if (!Board.TryGetItem(cell, out _))
                    return cell;
            }

            throw new InvalidOperationException("The board is full.");
        }
    }
}
