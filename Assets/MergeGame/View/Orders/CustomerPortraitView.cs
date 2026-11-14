using System.Linq;
using MergeGame.Data.Visuals;
using MergeGame.Presentation.Orders;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Orders
{
    /// <summary>
    /// The customer of an order card inside its round frame: an animated Spine portrait when the customer has one,
    /// otherwise its still avatar. A new customer plays its <c>appear</c> animation, a change of mood plays that mood's
    /// entry (<c>progress</c>, <c>finished</c>) and every state then loops; a delivered order plays <c>leave</c>.
    /// </summary>
    public sealed class CustomerPortraitView : MonoBehaviour
    {
        [Tooltip("The round, masked area the customer is framed in.")]
        [SerializeField] private RectTransform frame;

        [SerializeField] private SkeletonGraphic portrait;
        [SerializeField] private Image avatar;

        [Header("Framing")]
        [Tooltip("The character's resting height (first frame of its waiting loop), in frame heights; larger shows less of the body.")]
        [Min(0.1f)]
        [SerializeField] private float heightInFrames = 1.7f;

        [Tooltip("Gap between the top of the frame and the top of the character, in frame heights.")]
        [SerializeField] private float headroom = 0.04f;

        private CustomerAnimationSet animations;
        private CustomerMood mood;

        /// <summary>Where delivered items fly to, in world space.</summary>
        public Vector3 Center => frame.TransformPoint(frame.rect.center);

        /// <summary>True while an animated portrait is shown (false for a still avatar or a portrait without the needed animations).</summary>
        public bool IsAnimated => animations != null && portrait.gameObject.activeSelf;

        /// <summary>The animation the portrait is playing now; null without an animated portrait.</summary>
        public string CurrentAnimation => IsAnimated ? portrait.AnimationState.GetCurrent(0)?.Animation.Name : null;

        /// <summary>Shows a customer who just arrived with an order.</summary>
        public void Arrive(SkeletonDataAsset skeleton, Sprite still, CustomerMood customerMood)
        {
            mood = customerMood;
            animations = Load(skeleton);
            avatar.sprite = still;
            avatar.gameObject.SetActive(animations == null && still != null);
            portrait.gameObject.SetActive(animations != null);
            if (animations == null)
                return;

            var state = portrait.AnimationState;
            state.SetAnimation(0, animations.Appear, false);
            state.AddAnimation(0, Loop(mood), true, 0f);
        }

        /// <summary>The same customer now feels differently about the board.</summary>
        public void ChangeMood(CustomerMood customerMood)
        {
            if (customerMood == mood)
                return;
            mood = customerMood;
            if (animations == null)
                return;

            var state = portrait.AnimationState;
            var entry = Entry(mood);
            if (entry == null)
            {
                state.SetAnimation(0, Loop(mood), true);
                return;
            }

            state.SetAnimation(0, entry, false);
            state.AddAnimation(0, Loop(mood), true, 0f);
        }

        /// <summary>The order was delivered; the customer says goodbye and stays on its last frame.</summary>
        public void Leave()
        {
            if (animations != null)
                portrait.AnimationState.SetAnimation(0, animations.Leave, false);
        }

        private CustomerAnimationSet Load(SkeletonDataAsset skeleton)
        {
            if (skeleton == null)
                return null;

            portrait.skeletonDataAsset = skeleton;
            portrait.Initialize(true);
            var data = portrait.IsValid ? portrait.Skeleton.Data : null;
            if (data == null)
                return null;

            if (!CustomerAnimationSet.TryResolve(data.Animations.Items.Take(data.Animations.Count).Select(animation => animation.Name), out var set, out var missing))
            {
                Debug.LogWarning($"Customer portrait '{skeleton.name}' has no animation ending in _{string.Join(", _", missing)}; showing the avatar instead.", this);
                return null;
            }

            FrameRestingPose(data.FindAnimation(set.AppearLoop));
            return set;
        }

        /// <summary>
        /// Scales the character so its resting pose is <see cref="heightInFrames"/> frames tall, top-aligned and centred.
        /// Not the setup pose: some characters are only placed in frame by their animations.
        /// </summary>
        private void FrameRestingPose(Spine.Animation resting)
        {
            var skeleton = portrait.Skeleton;
            skeleton.SetToSetupPose();
            resting.Apply(skeleton, 0f, 0f, false, null, 1f, Spine.MixBlend.Setup, Spine.MixDirection.In);
            skeleton.UpdateWorldTransform();
            portrait.UpdateMesh();
            var bounds = portrait.GetLastMesh().bounds;
            if (bounds.size.y <= 0f)
                return;

            var frameHeight = frame.rect.height;
            var scale = frameHeight * heightInFrames / bounds.size.y;
            var top = frameHeight * (0.5f - headroom);
            var rect = portrait.rectTransform;
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = new Vector2(-bounds.center.x * scale, top - bounds.max.y * scale);
        }

        private string Entry(CustomerMood customerMood) => customerMood switch
        {
            CustomerMood.Hopeful => animations.Progress,
            CustomerMood.Ready => animations.Finished,
            _ => null
        };

        private string Loop(CustomerMood customerMood) => customerMood switch
        {
            CustomerMood.Hopeful => animations.ProgressLoop,
            CustomerMood.Ready => animations.FinishedLoop,
            _ => animations.AppearLoop
        };
    }
}
