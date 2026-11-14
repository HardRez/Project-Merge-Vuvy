namespace MergeGame.Presentation
{
    /// <summary>
    /// A presenter whose behaviour depends on elapsed time. The composition root calls <see cref="Advance"/> once per
    /// frame, so presenters need no clocks or timers and tests can step time explicitly.
    /// </summary>
    public interface ITimeDriven
    {
        void Advance(float deltaSeconds);
    }
}
