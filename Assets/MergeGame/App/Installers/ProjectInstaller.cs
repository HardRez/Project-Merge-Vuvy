using System;
using MergeGame.App.Navigation;
using MergeGame.Core.Spawning;
using MergeGame.Data.Authoring;
using MergeGame.Presentation.Navigation;
using UnityEngine;
using Zenject;

namespace MergeGame.App.Installers
{
    /// <summary>Bindings that outlive scenes: what to play, how to switch screens and the random source.</summary>
    public sealed class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private GameConfigAsset gameConfig;
        [SerializeField] private SceneNames scenes = new SceneNames();

        [Tooltip("Fixes the random sequence (spawns, orders) for reproducible sessions; 0 picks a new seed every launch.")]
        [SerializeField] private int randomSeed;

        public override void InstallBindings()
        {
            if (gameConfig == null)
                throw new InvalidOperationException($"{nameof(ProjectInstaller)} on '{name}' has no game config assigned.");

            Container.Bind<GameConfigAsset>().FromInstance(gameConfig).AsSingle();
            Container.Bind<IAppNavigator>().To<SceneNavigator>().AsSingle().WithArguments(scenes);
            Container.Bind<IRandom>().FromInstance(new SystemRandom(randomSeed != 0 ? randomSeed : Environment.TickCount)).AsSingle();
        }
    }
}
