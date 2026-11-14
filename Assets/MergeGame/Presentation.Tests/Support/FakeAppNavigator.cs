using System.Collections.Generic;
using MergeGame.Presentation.Navigation;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeAppNavigator : IAppNavigator
    {
        public List<string> Requests { get; } = new List<string>();

        public void OpenGame() => Requests.Add(nameof(OpenGame));
        public void OpenMainMenu() => Requests.Add(nameof(OpenMainMenu));
        public void Quit() => Requests.Add(nameof(Quit));
    }
}
