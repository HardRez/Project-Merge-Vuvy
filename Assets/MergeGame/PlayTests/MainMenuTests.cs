using System.Collections;
using System.Collections.Generic;
using MergeGame.PlayTests.Support;
using MergeGame.View.Audio;
using MergeGame.View.MainMenu;
using MergeGame.View.Orders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>The main menu's shop front, through the real MainMenu scene.</summary>
    public sealed class MainMenuTests
    {
        // The customers come in one after another, then play their arrival.
        private const float ArrivedSeconds = 1.8f;

        [UnityTest]
        public IEnumerator TheCustomersBehindTheCounter_AllArrive_EachWithAPop_AndIdleInTheirWaitingLoop()
        {
            yield return GameScreenDriver.LoadScene(GameScreenDriver.MainMenuScene);
            var shop = Object.FindAnyObjectByType<MenuCustomers>();
            Assert.That(shop, Is.Not.Null);
            Assert.That(shop.Customers, Has.Length.EqualTo(3));
            var sounds = Object.FindAnyObjectByType<SoundPlayer>();
            Assert.That(sounds.Library.TryGet(SoundCue.CustomerAppear, out var pop, out _), Is.True, "No clip for a customer popping in.");
            var arrivals = new List<int>();
            var pops = new List<int>();
            void Arrived(CustomerPortraitView _) => arrivals.Add(Time.frameCount);
            void Played(AudioClip clip)
            {
                if (clip == pop)
                    pops.Add(Time.frameCount);
            }

            shop.Arrived += Arrived;
            sounds.ClipPlayed += Played;
            yield return GameScreenDriver.WaitSeconds(ArrivedSeconds);
            shop.Arrived -= Arrived;
            sounds.ClipPlayed -= Played;
            Assert.That(arrivals, Has.Count.EqualTo(shop.Customers.Length), "Not every customer came in while the test watched.");
            Assert.That(pops, Is.EqualTo(arrivals), "Every customer pops in with a sound of its own, as it appears.");

            foreach (var customer in shop.Customers)
            {
                Assert.That(customer.gameObject.activeInHierarchy, Is.True, $"{customer.name} never came in.");
                Assert.That(customer.IsAnimated, Is.True, $"{customer.name} shows no animated character.");
                Assert.That(customer.CurrentAnimation, Does.EndWith("_appear").Or.EndWith("_repeat"));
                Assert.That(customer.PortraitWorldRect.height, Is.GreaterThan(customer.FrameWorldRect.height),
                    $"{customer.name} is not drawn at its full height behind the counter.");
            }
        }

        /// <summary>
        /// The game's name is written along the ribbon's arched red band: every capital is centred on the band where it
        /// stands, the tails of g and y stay on the band too, the letters keep clear of the ribbon's folded ends, and the
        /// outer letters lean with the arch. The band was measured in the 230 px ribbon art (rows of its red face at
        /// fractions of the ribbon's width); the ribbon is drawn with the art's rows 40 to 100 stretched by 34 rows.
        /// </summary>
        [UnityTest]
        public IEnumerator TheGamesName_FollowsTheRibbonsArchedBand()
        {
            const float extraRows = 34f;
            const float artHeight = 230f + extraRows;
            const float centreTolerance = 10f;
            float[] across = { 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f };
            // Rows of the art as drawn: rows 40 to 100 are stretched by the extra rows, the rows below them moved down.
            float Drawn(float row) => row <= 40f ? row : row >= 100f ? row + extraRows : row + extraRows * (row - 40f) / 60f;
            float[] faceTop = { Drawn(42f), Drawn(28f), Drawn(21f), Drawn(20f), Drawn(22f), Drawn(28f), Drawn(42f) };
            float[] faceBottom = { Drawn(128f), Drawn(115f), Drawn(107f), Drawn(104f), Drawn(106f), Drawn(113f), Drawn(127f) };

            yield return GameScreenDriver.LoadScene(GameScreenDriver.MainMenuScene);
            yield return GameScreenDriver.WaitSeconds(1f);

            var ribbon = GameObject.Find("Ribbon").GetComponent<RectTransform>();
            var name = ribbon.Find("Name").GetComponent<TMPro.TMP_Text>();
            var corners = new Vector3[4];
            ribbon.GetWorldCorners(corners);
            float Row(Vector3 local) => (corners[1].y - name.transform.TransformPoint(local).y) / (corners[1].y - corners[0].y) * artHeight;
            float Across(Vector3 local) => (name.transform.TransformPoint(local).x - corners[0].x) / (corners[2].x - corners[0].x);
            float Face(float[] rows, float x)
            {
                for (var i = 0; i < across.Length - 1; i++)
                {
                    if (x <= across[i + 1])
                        return Mathf.Lerp(rows[i], rows[i + 1], Mathf.InverseLerp(across[i], across[i + 1], x));
                }

                return rows[across.Length - 1];
            }

            float BandCentre(float x) => (Face(faceTop, x) + Face(faceBottom, x)) * 0.5f;

            // The letters as drawn: the bent quads, not TextMesh Pro's straight layout.
            var info = name.textInfo;
            Assert.That(info.characterCount, Is.EqualTo("Merge Vuvy".Length));
            var first = -1;
            var last = -1;
            for (var i = 0; i < info.characterCount; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible)
                    continue;
                var quad = Quad(info, c);
                var middle = (quad[0] + quad[1] + quad[2] + quad[3]) / 4f;
                var x = Across(middle);
                Assert.That(Across(quad[0]), Is.GreaterThan(0.22f), $"'{c.character}' runs onto the ribbon's left fold.");
                Assert.That(Across(quad[3]), Is.LessThan(0.78f), $"'{c.character}' runs onto the ribbon's right fold.");
                if (char.IsUpper(c.character))
                {
                    // A capital's quad is centred on the capital (its padding is the same all round).
                    var row = Row(middle);
                    Assert.That(Mathf.Abs(row - BandCentre(x)), Is.LessThan(centreTolerance),
                        $"'{c.character}' is centred on row {row:0} where the band's middle is row {BandCentre(x):0}.");
                }

                if (c.character == 'g' || c.character == 'y')
                {
                    // The quad reaches a little past the tail (its padding), so this is on the safe side.
                    var lowest = Mathf.Max(Row(quad[0]), Row(quad[3]));
                    Assert.That(lowest, Is.LessThan(Face(faceBottom, x)),
                        $"The tail of '{c.character}' reaches row {lowest:0}, onto the gold edge under the band (row {Face(faceBottom, x):0}).");
                }

                if (first < 0)
                    first = i;
                last = i;
            }

            var left = Quad(info, info.characterInfo[first]);
            var right = Quad(info, info.characterInfo[last]);
            Assert.That(Row(left[0]), Is.GreaterThan(Row(left[3]) + 1f), "The first letter does not lean with the arch.");
            Assert.That(Row(right[3]), Is.GreaterThan(Row(right[0]) + 1f), "The last letter does not lean with the arch.");
        }

        /// <summary>The four corners a character is drawn with: bottom left, top left, top right, bottom right.</summary>
        private static Vector3[] Quad(TMPro.TMP_TextInfo info, TMPro.TMP_CharacterInfo character)
        {
            var vertices = info.meshInfo[character.materialReferenceIndex].vertices;
            return new[]
            {
                vertices[character.vertexIndex], vertices[character.vertexIndex + 1],
                vertices[character.vertexIndex + 2], vertices[character.vertexIndex + 3]
            };
        }
    }
}
