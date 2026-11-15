using System.Collections;
using System.Collections.Generic;
using MergeGame.PlayTests.Support;
using MergeGame.View.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>The views play the right cue for what the player does, and the music runs across screens.</summary>
    public sealed class SoundTests
    {
        private readonly List<AudioClip> played = new List<AudioClip>();
        private GameScreenDriver game;
        private SoundPlayer sounds;

        [TearDown]
        public void StopListening()
        {
            if (sounds != null)
                sounds.ClipPlayed -= played.Add;
            // NUnit shares one fixture instance between its tests.
            played.Clear();
            sounds = null;
        }

        [UnityTest]
        public IEnumerator Spawning_PlaysTheSpawnSound()
        {
            yield return OpenGameAndListen();

            game.ClickSpawn();
            yield return null;

            Assert.That(played, Has.Member(Clip(SoundCue.Spawn)));
        }

        [UnityTest]
        public IEnumerator DragMerge_PlaysPickUp_ThenTheMergeSoundOfTheNewLevel()
        {
            yield return OpenGameAndListen();
            for (var spawns = 0; !game.TryFindMergePair(out _, out _) && spawns < 20; spawns++)
            {
                game.ClickSpawn();
                yield return null;
            }

            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True);
            game.Board.TryGetItem(to, out var target);
            var resultLevel = target.ItemId.Level + 1;
            yield return GameScreenDriver.WaitSeconds(0.5f);
            played.Clear();

            yield return game.DragAndDrop(from, to);

            Assert.That(played, Has.Member(Clip(SoundCue.PickUp)));
            Assert.That(sounds.Library.TryGetMerge(resultLevel, out var mergeClip, out _), Is.True);
            Assert.That(played.IndexOf(mergeClip), Is.GreaterThan(played.IndexOf(Clip(SoundCue.PickUp))));
        }

        [UnityTest]
        public IEnumerator FreeingABubbledItemNextToSand_PlaysThePop_AndTheSandBreakingOnce()
        {
            yield return OpenGameAndListen();
            Assert.That(game.TryFindCoverOpeningMerge(out var from, out var to), Is.True);

            yield return game.DragAndDrop(from, to);
            // The pop lands with the bubble bursting, once the other item has flown in.
            yield return GameScreenDriver.WaitSeconds(0.4f);

            Assert.That(played, Has.Member(Clip(SoundCue.Unlock)));
            Assert.That(played.FindAll(clip => clip == Clip(SoundCue.SandBreak)), Has.Count.EqualTo(1),
                "Cells opening together should sound once, not once per cell.");
        }

        [UnityTest]
        public IEnumerator Music_KeepsPlayingFromTheMenuIntoTheGame()
        {
            yield return GameScreenDriver.LoadScene(GameScreenDriver.MainMenuScene);
            yield return null;
            var music = Object.FindAnyObjectByType<MusicPlayer>();
            Assert.That(music, Is.Not.Null, "The project context has no music player.");
            var source = music.GetComponent<AudioSource>();
            Assert.That(source.clip, Is.Not.Null);
            Assert.That(source.loop, Is.True);

            yield return GameScreenDriver.OpenGame(driver => game = driver);

            Assert.That(Object.FindAnyObjectByType<MusicPlayer>(), Is.SameAs(music), "The music restarted with the scene.");
        }

        private IEnumerator OpenGameAndListen()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            sounds = Object.FindAnyObjectByType<SoundPlayer>();
            Assert.That(sounds, Is.Not.Null, "The game scene has no sound player.");
            sounds.ClipPlayed += played.Add;
        }

        private AudioClip Clip(SoundCue cue)
        {
            Assert.That(sounds.Library.TryGet(cue, out var clip, out _), Is.True, $"No clip for {cue}.");
            return clip;
        }
    }
}
