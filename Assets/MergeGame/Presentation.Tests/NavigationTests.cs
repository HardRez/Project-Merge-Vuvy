using MergeGame.Presentation.MainMenu;
using MergeGame.Presentation.Navigation;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;

namespace MergeGame.Presentation.Tests
{
    public sealed class NavigationTests
    {
        [Test]
        public void BackToMenu_OpensTheMainMenu()
        {
            var view = new FakeGameMenuView();
            var navigator = new FakeAppNavigator();
            using var presenter = new GameNavigationPresenter(view, navigator);
            presenter.Initialize();

            view.PressBackToMenu();

            Assert.That(navigator.Requests, Is.EqualTo(new[] { nameof(IAppNavigator.OpenMainMenu) }));
        }

        [Test]
        public void MainMenuPlay_OpensTheGame()
        {
            var view = new FakeMainMenuView();
            var navigator = new FakeAppNavigator();
            using var presenter = new MainMenuPresenter(view, navigator);
            presenter.Initialize();

            view.PressPlay();

            Assert.That(navigator.Requests, Is.EqualTo(new[] { nameof(IAppNavigator.OpenGame) }));
        }

        [Test]
        public void MainMenuQuit_QuitsTheApplication()
        {
            var view = new FakeMainMenuView();
            var navigator = new FakeAppNavigator();
            using var presenter = new MainMenuPresenter(view, navigator);
            presenter.Initialize();

            view.PressQuit();

            Assert.That(navigator.Requests, Is.EqualTo(new[] { nameof(IAppNavigator.Quit) }));
        }

        [Test]
        public void DisposedPresenters_NoLongerNavigate()
        {
            var gameMenu = new FakeGameMenuView();
            var mainMenu = new FakeMainMenuView();
            var navigator = new FakeAppNavigator();
            var gamePresenter = new GameNavigationPresenter(gameMenu, navigator);
            var menuPresenter = new MainMenuPresenter(mainMenu, navigator);
            gamePresenter.Initialize();
            menuPresenter.Initialize();

            gamePresenter.Dispose();
            menuPresenter.Dispose();
            gameMenu.PressBackToMenu();
            mainMenu.PressPlay();
            mainMenu.PressQuit();

            Assert.That(navigator.Requests, Is.Empty);
            Assert.That(gameMenu.HasInputListeners, Is.False);
            Assert.That(mainMenu.HasInputListeners, Is.False);
        }
    }
}
