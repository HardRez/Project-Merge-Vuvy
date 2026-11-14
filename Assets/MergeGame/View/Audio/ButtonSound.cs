using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Audio
{
    /// <summary>
    /// A click for buttons whose action has no sound of its own (menu and navigation). Buttons that start a game action,
    /// such as spawn or deliver, are heard through that action instead.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSound : MonoBehaviour
    {
        [SerializeField] private SoundPlayer sounds;
        [SerializeField] private SoundCue cue = SoundCue.ButtonClick;

        private Button button;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable() => button.onClick.AddListener(PlaySound);

        private void OnDisable() => button.onClick.RemoveListener(PlaySound);

        private void PlaySound()
        {
            if (sounds != null)
                sounds.Play(cue);
        }
    }
}
