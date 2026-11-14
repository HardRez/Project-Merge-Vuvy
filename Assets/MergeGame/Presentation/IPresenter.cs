using System;

namespace MergeGame.Presentation
{
    /// <summary>
    /// Lifecycle every presenter shares. Constructors only store dependencies; <see cref="Initialize"/> renders the
    /// starting state and starts listening, <see cref="IDisposable.Dispose"/> stops all listening so the presenter never
    /// touches its views again. The composition root drives both.
    /// </summary>
    public interface IPresenter : IDisposable
    {
        void Initialize();
    }
}
