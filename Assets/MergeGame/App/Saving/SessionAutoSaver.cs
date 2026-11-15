using System;
using MergeGame.Core.Events;
using MergeGame.Core.Session;
using MergeGame.Core.Spawning;
using MergeGame.Presentation.Cheats;
using UnityEngine;
using Zenject;

namespace MergeGame.App.Saving
{
    /// <summary>
    /// Saves the game screen's session whenever it changes: every session event marks it changed and the save is written
    /// once at the end of that frame, so a merge that raises several events is written once, and the progress is on
    /// disk before the player can leave the app (iOS and Android may end a backgrounded app without another frame). A
    /// new game is written on its first frame, so its orders stay the same when the app is reopened.
    /// </summary>
    public sealed class SessionAutoSaver : IInitializable, ILateTickable, IDisposable, IGameProgress
    {
        private readonly MergeGameSession session;
        private readonly IGameSaveStore store;
        private bool changed;
        private bool stopped;

        public SessionAutoSaver(MergeGameSession session, IGameSaveStore store)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Opens the saved game, or a new one when there is no save or it no longer fits the game (e.g. the content
        /// changed since it was written).
        /// </summary>
        public static MergeGameSession LoadOrStart(GameDefinition definition, IRandom random, IGameSaveStore store)
        {
            if (store.TryLoad(out var snapshot))
            {
                try
                {
                    return MergeGameSession.Resume(definition, random, snapshot);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogWarning($"[Save] The saved game no longer fits the game's content; starting a new game. {exception.Message}");
                }
            }

            return new MergeGameSession(definition, random);
        }

        public void Initialize()
        {
            session.EventRaised += OnEventRaised;
            changed = true;
        }

        public void LateTick()
        {
            if (changed)
                SaveNow();
        }

        /// <summary>Writes the session as it is now, unless the progress was reset.</summary>
        public void SaveNow()
        {
            changed = false;
            if (!stopped)
                store.Save(session.CaptureSnapshot());
        }

        public void ResetProgress()
        {
            stopped = true;
            changed = false;
            store.Delete();
        }

        public void Dispose() => session.EventRaised -= OnEventRaised;

        private void OnEventRaised(GameEvent gameEvent) => changed = true;
    }
}
