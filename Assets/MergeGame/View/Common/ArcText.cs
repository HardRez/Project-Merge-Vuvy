using TMPro;
using UnityEngine;

namespace MergeGame.View.Common
{
    /// <summary>
    /// Bends a TextMesh Pro text along a circle, as if written on an arched ribbon: each letter is moved down by how far
    /// the circle has dropped below its top at the letter's position and turned to follow the circle there. The middle
    /// of the text stays where TextMesh Pro put it. The text is bent again whenever it is rebuilt.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class ArcText : MonoBehaviour
    {
        [Tooltip("The circle's radius, in the text's own units; larger bends less. The circle's top is at the text's middle.")]
        [Min(1f)]
        [SerializeField] private float radius = 2000f;

        private TMP_Text text;
        private bool bending;

        private void OnEnable()
        {
            text = GetComponent<TMP_Text>();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            Bend();
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            if (text != null)
                text.ForceMeshUpdate();
        }

        private void OnTextChanged(Object changed)
        {
            if (!bending && changed == text)
                Bend();
        }

        private void Bend()
        {
            if (text == null)
                return;

            bending = true;
            try
            {
                // Starts from the straight text every time, so bending never piles up.
                text.ForceMeshUpdate();
                var info = text.textInfo;
                var middle = text.rectTransform.rect.center.x;
                for (var i = 0; i < info.characterCount; i++)
                {
                    var character = info.characterInfo[i];
                    if (!character.isVisible)
                        continue;

                    var vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                    var first = character.vertexIndex;
                    // Each letter turns about the middle of its foot, on the baseline.
                    var pivot = new Vector3((character.bottomLeft.x + character.bottomRight.x) * 0.5f, character.baseLine, 0f);
                    var along = Mathf.Clamp(pivot.x - middle, -radius, radius);
                    var drop = radius - Mathf.Sqrt(radius * radius - along * along);
                    var turn = Quaternion.Euler(0f, 0f, -Mathf.Asin(along / radius) * Mathf.Rad2Deg);
                    for (var v = 0; v < 4; v++)
                        vertices[first + v] = pivot + turn * (vertices[first + v] - pivot) + Vector3.down * drop;
                }

                text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }
            finally
            {
                bending = false;
            }
        }
    }
}
