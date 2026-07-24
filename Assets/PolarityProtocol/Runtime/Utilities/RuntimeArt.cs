using UnityEngine;

namespace PolarityProtocol.Utilities
{
    public static class RuntimeArt
    {
        public static readonly Color Pull = new(0.18f, 0.87f, 1f);
        public static readonly Color Push = new(1f, 0.25f, 0.37f);
        public static readonly Color Gold = new(1f, 0.78f, 0.25f);
        public static readonly Color Dark = new(0.025f, 0.045f, 0.07f);
        public static readonly Color Steel = new(0.12f, 0.2f, 0.26f);
        public static readonly Color Slate = new(0.055f, 0.085f, 0.12f);

        public static Material Material(Color color, float emission = 0f, bool transparent = false)
        {
            Shader shader = Shader.Find(transparent ? "Sprites/Default" : "Standard");
            if (shader == null && !transparent)
            {
                shader = Shader.Find("Legacy Shaders/Diffuse");
            }

            if (shader == null)
            {
                shader = Shader.Find("Hidden/InternalErrorShader");
            }

            Material material = new(shader)
            {
                color = color
            };

            if (!transparent && material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", emission > 0f ? 0.32f : 0.18f);
            }

            if (!transparent && material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", emission > 0f ? 0.72f : 0.48f);
            }

            if (emission > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }

            return material;
        }

        public static GameObject Primitive(
            PrimitiveType primitive,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            bool keepCollider = true,
            float emission = 0f)
        {
            GameObject gameObject = GameObject.CreatePrimitive(primitive);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.sharedMaterial = Material(color, emission);

            if (!keepCollider)
            {
                Object.Destroy(gameObject.GetComponent<Collider>());
            }

            return gameObject;
        }

        public static LineRenderer Ring(
            Transform parent,
            float radius,
            Color color,
            float width = 0.045f,
            int segments = 64)
        {
            GameObject ringObject = new("Range Ring");
            ringObject.transform.SetParent(parent, false);
            ringObject.transform.localPosition = new Vector3(0f, 0.035f, 0f);

            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = false;
            line.positionCount = segments;
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = Material(color, 1.5f, true);

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            return line;
        }

        public static TextMesh Label(
            Transform parent,
            string text,
            Vector3 localPosition,
            Color color,
            int fontSize = 42,
            float characterSize = 0.08f)
        {
            GameObject labelObject = new("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;

            return textMesh;
        }
    }
}
