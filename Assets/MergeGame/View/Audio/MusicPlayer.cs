using UnityEngine;

namespace MergeGame.View.Audio
{
    /// <summary>
    /// Loops the background music. It lives on the project context, which outlives scenes, so the music keeps playing
    /// from the main menu into the game and back without restarting.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private SoundLibrary library;

        private void Start()
        {
            if (library == null || library.Music == null)
                return;

            var source = GetComponent<AudioSource>();
            source.clip = library.Music;
            source.volume = library.MusicVolume;
            source.loop = true;
            source.playOnAwake = false;
            source.Play();
        }
    }
}
