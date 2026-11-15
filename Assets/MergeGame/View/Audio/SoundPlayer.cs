using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeGame.View.Audio
{
    /// <summary>
    /// Plays the screen's sound effects through one 2D source. A clip asked for several times in the same frame (four
    /// sand cells opening together, a burst of commands) is played once, so simultaneous events never stack into noise.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundPlayer : MonoBehaviour
    {
        [SerializeField] private SoundLibrary library;

        private readonly Dictionary<AudioClip, int> lastPlayedFrame = new Dictionary<AudioClip, int>();
        private AudioSource source;

        /// <summary>Raised for every clip that actually plays; lets tests observe audio without listening to it.</summary>
        public event Action<AudioClip> ClipPlayed;

        public SoundLibrary Library => library;

        private AudioSource Source => source != null ? source : source = GetComponent<AudioSource>();

        public void Play(SoundCue cue)
        {
            if (library != null && library.TryGet(cue, out var clip, out var volume))
                PlayClip(clip, volume);
        }

        /// <summary>Plays <paramref name="cue"/> after <paramref name="delaySeconds"/>, to land on a moment of an animation.</summary>
        public void Play(SoundCue cue, float delaySeconds)
        {
            if (delaySeconds <= 0f || !isActiveAndEnabled)
                Play(cue);
            else
                StartCoroutine(PlayLater(cue, delaySeconds));
        }

        /// <param name="level">The level the merge created.</param>
        public void PlayMerge(int level)
        {
            if (library != null && library.TryGetMerge(level, out var clip, out var volume))
                PlayClip(clip, volume);
        }

        private System.Collections.IEnumerator PlayLater(SoundCue cue, float delaySeconds)
        {
            for (var waited = 0f; waited < delaySeconds; waited += Time.unscaledDeltaTime)
                yield return null;
            Play(cue);
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            var frame = Time.frameCount;
            if (lastPlayedFrame.TryGetValue(clip, out var last) && last == frame)
                return;

            lastPlayedFrame[clip] = frame;
            Source.PlayOneShot(clip, volume);
            ClipPlayed?.Invoke(clip);
        }
    }
}
