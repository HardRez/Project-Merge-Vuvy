using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeGame.View.Audio
{
    /// <summary>Which clip plays for each cue and merge level, and the background music, so audio is edited as data.</summary>
    [CreateAssetMenu(menuName = "Merge Game/Sound Library", fileName = "SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [SerializeField] private List<CueClip> cues = new List<CueClip>();

        [Tooltip("Played when a merge creates an item; element 0 is level 2. Higher levels use the last clip.")]
        [SerializeField] private List<AudioClip> mergeClips = new List<AudioClip>();

        [Range(0f, 1f)]
        [SerializeField] private float mergeVolume = 0.8f;

        [SerializeField] private AudioClip music;

        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.35f;

        public IReadOnlyList<CueClip> Cues
        {
            get => cues;
            internal set => cues = new List<CueClip>(value);
        }

        public IReadOnlyList<AudioClip> MergeClips
        {
            get => mergeClips;
            internal set => mergeClips = new List<AudioClip>(value);
        }

        public AudioClip Music
        {
            get => music;
            internal set => music = value;
        }

        public float MusicVolume => musicVolume;

        public bool TryGet(SoundCue cue, out AudioClip clip, out float volume)
        {
            foreach (var entry in cues)
            {
                if (entry.Cue == cue && entry.Clip != null)
                {
                    clip = entry.Clip;
                    volume = entry.Volume;
                    return true;
                }
            }

            clip = null;
            volume = 0f;
            return false;
        }

        /// <param name="level">The level the merge created; merges start at level 2.</param>
        public bool TryGetMerge(int level, out AudioClip clip, out float volume)
        {
            clip = mergeClips.Count == 0 || level < 2 ? null : mergeClips[Mathf.Min(level - 2, mergeClips.Count - 1)];
            volume = mergeVolume;
            return clip != null;
        }

        [Serializable]
        public struct CueClip
        {
            [SerializeField] private SoundCue cue;
            [SerializeField] private AudioClip clip;

            [Range(0f, 1f)]
            [SerializeField] private float volume;

            public CueClip(SoundCue cue, AudioClip clip, float volume = 1f)
            {
                this.cue = cue;
                this.clip = clip;
                this.volume = volume;
            }

            public SoundCue Cue => cue;
            public AudioClip Clip => clip;
            public float Volume => volume;
        }
    }
}
