using System;
using System.Collections.Generic;
using MergeGame.Presentation;
using Zenject;

namespace MergeGame.App.Lifecycle
{
    /// <summary>
    /// Drives a screen's presenters: initializes them in the given order once the scene is injected, and on teardown
    /// disposes them in reverse order, then the resources they shared (e.g. the session event stream).
    /// </summary>
    public sealed class PresenterLifecycle : IInitializable, IDisposable
    {
        private readonly IReadOnlyList<IPresenter> presenters;
        private readonly IReadOnlyList<IDisposable> sharedResources;
        private int initializedCount;

        public PresenterLifecycle(IReadOnlyList<IPresenter> presenters, params IDisposable[] disposeAfterPresenters)
        {
            this.presenters = presenters ?? throw new ArgumentNullException(nameof(presenters));
            sharedResources = disposeAfterPresenters ?? Array.Empty<IDisposable>();
        }

        public void Initialize()
        {
            foreach (var presenter in presenters)
            {
                presenter.Initialize();
                initializedCount++;
            }
        }

        public void Dispose()
        {
            // Only presenters that were initialized are disposed, should an Initialize have thrown.
            for (var i = initializedCount - 1; i >= 0; i--)
                presenters[i].Dispose();
            initializedCount = 0;

            foreach (var resource in sharedResources)
                resource.Dispose();
        }
    }
}
