using System.Collections;
using System.Linq;
using MergeGame.Data.Visuals;
using MergeGame.Presentation.Orders;
using MergeGame.View.Animation;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeGame.View.Orders
{
    /// <summary>
    /// The customer of an order: an animated Spine portrait when the customer has one, otherwise its still avatar. It
    /// stands behind the shop counter, which is drawn in front of it and hides its lower body: its frame is the space
    /// above the counter, and the character is scaled taller than the frame so it reaches down behind the counter.
    /// <para>
    /// A customer arrives with the pop-in animation of its mood (<c>appear</c> while waiting, <c>progress</c> while part
    /// of the order is on the board, <c>finished</c> when it can be delivered) and then loops that mood. A later change of
    /// mood cross-fades to the new loop instead, because the pop-ins start from nothing; a happier mood also hops. A ready
    /// customer glows, a tap makes it hop, and a delivered order plays <c>leave</c>.
    /// </para>
    /// </summary>
    public sealed class CustomerPortraitView : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("The space above the counter the character stands in; framing and hops are measured in its height.")]
        [SerializeField] private RectTransform frame;

        [Tooltip("Carries the character; sits at the frame's centre and hops.")]
        [SerializeField] private RectTransform actor;

        [SerializeField] private SkeletonGraphic portrait;
        [SerializeField] private Image avatar;

        [Tooltip("Fades the whole customer (character, glow) out when it leaves.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Glows behind the character while the order can be delivered.")]
        [SerializeField] private Image readyGlow;

        [Header("Framing")]
        [Tooltip("The character's resting height (first frame of its waiting loop), in frame heights; the part beyond one frame " +
                 "hangs below the frame, behind the counter. Larger shows less of the body.")]
        [Min(0.1f)]
        [SerializeField] private float heightInFrames = 1.25f;

        [Tooltip("Gap between the top of the frame and the top of the character, in frame heights; negative lets the head rise " +
                 "above the frame.")]
        [SerializeField] private float headroom = 0.02f;

        [Tooltip("The top part of the resting pose, as a share of its height, that is centred horizontally: the head, so " +
                 "characters whose head is off their body's centre (a raised arm, a hat) still face the middle of the frame.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float centredTop = 0.4f;

        [Header("Reactions")]
        [SerializeField] private float hopSeconds = 0.36f;

        [Tooltip("How high a hop lifts the character, in frame heights.")]
        [SerializeField] private float hopHeight = 0.08f;

        [SerializeField] private float glowPulseSeconds = 1.4f;

        [Range(0f, 1f)]
        [SerializeField] private float glowAlpha = 0.9f;

        [SerializeField] private float glowFadeSeconds = 0.25f;

        [Tooltip("How fast a leaving customer fades out; the next customer comes in only after it is gone.")]
        [SerializeField] private float leaveFadeSeconds = 0.35f;

        private CustomerAnimationSet animations;
        private CustomerMood mood;
        private TweenChannel hop;
        private TweenChannel glow;
        private TweenChannel fade;
        private bool glowing;

        // The resting pose in the skeleton's own space, measured when the character arrives.
        private float restingHeight;
        private float restingTop;
        private float restingHeadCentre;

        /// <summary>The space above the counter the customer stands in, in world space.</summary>
        public Rect FrameWorldRect => WorldRect(frame, frame.rect);

        /// <summary>The character as drawn now (its mesh bounds), in world space; empty without an animated portrait.</summary>
        public Rect PortraitWorldRect
        {
            get
            {
                if (!IsAnimated)
                    return Rect.zero;
                var bounds = portrait.GetLastMesh().bounds;
                return WorldRect(portrait.rectTransform, Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y));
            }
        }

        /// <summary>True while an animated portrait is shown (false for a still avatar or a portrait without the needed animations).</summary>
        public bool IsAnimated => animations != null && portrait.gameObject.activeSelf;

        /// <summary>The animation the portrait is playing now; null without an animated portrait.</summary>
        public string CurrentAnimation => IsAnimated ? portrait.AnimationState.GetCurrent(0)?.Animation.Name : null;

        /// <summary>True while the customer glows as ready (from the moment it turns on, false from the moment its fade-out starts).</summary>
        public bool IsGlowing => glowing;

        /// <summary>True while the still avatar is shown instead of a portrait.</summary>
        public bool ShowsAvatar => avatar.gameObject.activeSelf;

        private TweenChannel Hop => hop ??= new TweenChannel(this);
        private TweenChannel Glow => glow ??= new TweenChannel(this);
        private TweenChannel Fade => fade ??= new TweenChannel(this);

        /// <summary>How visible the customer is: 1 while it is here, fading to 0 as it leaves.</summary>
        public float Opacity => group.alpha;

        /// <summary>Shows a customer who just arrived with an order.</summary>
        public void Arrive(SkeletonDataAsset skeleton, Sprite still, CustomerMood customerMood)
        {
            mood = customerMood;
            Fade.Stop();
            group.alpha = 1f;
            animations = Load(skeleton);
            avatar.sprite = still;
            avatar.gameObject.SetActive(animations == null && still != null);
            portrait.gameObject.SetActive(animations != null);
            StopHop();
            SetGlow(mood == CustomerMood.Ready, fade: false);
            if (animations == null)
                return;

            var state = portrait.AnimationState;
            state.SetAnimation(0, Entry(mood), false);
            state.AddAnimation(0, Loop(mood), true, 0f);

            // The mesh still holds the resting pose the framing measured; draw the pop-in's first frame now, or the
            // whole character flashes up for one frame before popping in.
            portrait.Skeleton.SetToSetupPose();
            state.Apply(portrait.Skeleton);
            portrait.Skeleton.UpdateWorldTransform();
            portrait.UpdateMesh();
        }

        /// <summary>The same customer now feels differently about the board.</summary>
        public void ChangeMood(CustomerMood customerMood)
        {
            if (customerMood == mood)
                return;
            var happier = customerMood > mood;
            mood = customerMood;
            SetGlow(mood == CustomerMood.Ready, fade: true);
            if (animations != null)
                portrait.AnimationState.SetAnimation(0, Loop(mood), true);
            if (happier)
                PlayHop(mood == CustomerMood.Ready ? 1.4f : 1f);
        }

        /// <summary>The order was delivered; the customer says goodbye and fades out while it does.</summary>
        public void Leave()
        {
            SetGlow(false, fade: true);
            if (animations != null)
                portrait.AnimationState.SetAnimation(0, animations.Leave, false);
            if (!Fade.Play(Tween.Run(leaveFadeSeconds, Ease.InQuad, t => group.alpha = 1f - t)))
                group.alpha = 0f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsAnimated || ShowsAvatar)
                PlayHop(1f);
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
        /// Measures the character's resting pose (the first frame of its waiting loop; not the setup pose, as some
        /// characters are only placed in frame by their animations) and frames it.
        /// </summary>
        private void FrameRestingPose(Spine.Animation resting)
        {
            var skeleton = portrait.Skeleton;
            skeleton.SetToSetupPose();
            resting.Apply(skeleton, 0f, 0f, false, null, 1f, Spine.MixBlend.Setup, Spine.MixDirection.In);
            skeleton.UpdateWorldTransform();
            portrait.UpdateMesh();
            var mesh = portrait.GetLastMesh();
            var bounds = mesh.bounds;
            restingHeight = bounds.size.y;
            restingTop = bounds.max.y;
            restingHeadCentre = HorizontalCentreAbove(mesh, bounds.max.y - bounds.size.y * centredTop, bounds.center.x);
            ApplyFraming();
        }

        /// <summary>
        /// Scales the resting pose to <see cref="heightInFrames"/> frames tall, its top <see cref="headroom"/> below the
        /// frame's top and its head (<see cref="centredTop"/>) centred. The actor's origin is the frame's centre.
        /// </summary>
        private void ApplyFraming()
        {
            if (restingHeight <= 0f)
                return;
            var frameHeight = frame.rect.height;
            var scale = frameHeight * heightInFrames / restingHeight;
            var top = frameHeight * (0.5f - headroom);
            var rect = portrait.rectTransform;
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = new Vector2(-restingHeadCentre * scale, top - restingTop * scale);
        }

        // The frame follows the counter's layout (taller screens give it more height, and the layout may settle after the
        // customer arrived), so the character is framed again whenever this slot is resized.
        private void OnRectTransformDimensionsChange()
        {
            if (animations != null)
                ApplyFraming();
        }

        private static Rect WorldRect(Transform space, Rect local)
        {
            var min = space.TransformPoint(local.min);
            var max = space.TransformPoint(local.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static float HorizontalCentreAbove(Mesh mesh, float height, float fallback)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var vertex in mesh.vertices)
            {
                if (vertex.y < height)
                    continue;
                min = Mathf.Min(min, vertex.x);
                max = Mathf.Max(max, vertex.x);
            }

            return min <= max ? (min + max) * 0.5f : fallback;
        }

        private void PlayHop(float strength)
        {
            if (!Hop.Play(HopAnimation(strength)))
                StopHop();
        }

        private void StopHop()
        {
            Hop.Stop();
            actor.anchoredPosition = Vector2.zero;
            actor.localScale = Vector3.one;
        }

        /// <summary>Stretches up off the ground, comes down and lands with a small squash.</summary>
        private IEnumerator HopAnimation(float strength)
        {
            var lift = frame.rect.height * hopHeight * strength;
            yield return Tween.Run(hopSeconds, Ease.Linear, t =>
            {
                var rise = Ease.Bump(Mathf.Min(1f, t / 0.8f));
                var squash = t > 0.8f ? Ease.Bump((t - 0.8f) / 0.2f) : 0f;
                actor.anchoredPosition = new Vector2(0f, lift * rise);
                var stretch = 0.05f * strength * rise - 0.06f * strength * squash;
                actor.localScale = new Vector3(1f - stretch * 0.6f, 1f + stretch, 1f);
            });
            StopHop();
        }

        private void SetGlow(bool on, bool fade)
        {
            glowing = on;
            if (on)
            {
                if (readyGlow.gameObject.activeSelf && fade)
                    return;
                readyGlow.gameObject.SetActive(true);
                if (!Glow.Play(GlowAnimation(fade)))
                    SetGlowAlpha(glowAlpha);
                return;
            }

            if (!readyGlow.gameObject.activeSelf)
                return;
            if (!fade || !Glow.Play(FadeGlowOut()))
                HideGlow();
        }

        private IEnumerator GlowAnimation(bool fadeIn)
        {
            if (fadeIn)
                yield return Tween.Run(glowFadeSeconds, Ease.OutQuad, t => SetGlowAlpha(glowAlpha * t));
            yield return Tween.Loop(() => Tween.Run(glowPulseSeconds, Ease.Linear,
                t => SetGlowAlpha(glowAlpha * (1f - 0.35f * Ease.Bump(t)))));
        }

        private IEnumerator FadeGlowOut()
        {
            var from = readyGlow.color.a;
            yield return Tween.Run(glowFadeSeconds, Ease.OutQuad, t => SetGlowAlpha(from * (1f - t)));
            HideGlow();
        }

        private void HideGlow()
        {
            Glow.Stop();
            SetGlowAlpha(0f);
            readyGlow.gameObject.SetActive(false);
        }

        private void SetGlowAlpha(float alpha)
        {
            var color = readyGlow.color;
            color.a = alpha;
            readyGlow.color = color;
        }

        private string Entry(CustomerMood customerMood) => customerMood switch
        {
            CustomerMood.Hopeful => animations.Progress,
            CustomerMood.Ready => animations.Finished,
            _ => animations.Appear
        };

        private string Loop(CustomerMood customerMood) => customerMood switch
        {
            CustomerMood.Hopeful => animations.ProgressLoop,
            CustomerMood.Ready => animations.FinishedLoop,
            _ => animations.AppearLoop
        };
    }
}
