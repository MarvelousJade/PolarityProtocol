using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Arena
{
    public sealed class HazardPulse : MonoBehaviour
    {
        private Material[] materials;

        private void Awake()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            materials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                materials[i] = renderers[i].material;
            }
        }

        private void Update()
        {
            float glow = 1.3f + (Mathf.Sin(Time.time * 4.5f) + 1f) * 0.8f;
            Color color = Color.Lerp(RuntimeArt.Push, RuntimeArt.Gold, (Mathf.Sin(Time.time * 2f) + 1f) * 0.18f);
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                material.color = color;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * glow);
                }
            }
        }
    }
}
