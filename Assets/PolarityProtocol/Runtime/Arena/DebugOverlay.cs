using PolarityProtocol.AI;
using PolarityProtocol.Combat;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Arena
{
    public sealed class DebugOverlay : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        private Texture2D white;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle footer;
        private bool stylesReady;

        public static bool Enabled { get; private set; }

        private void Update()
        {
            if (LegacyInput.DebugPressed)
            {
                Enabled = !Enabled;
                FeedbackBus.Pulse(Enabled ? 620f : 190f, 0.06f, 0.05f);
            }
        }

        private void OnGUI()
        {
            if (!Enabled)
            {
                return;
            }

            EnsureStyles();
            EnemyBrain[] enemies = FindObjectsByType<EnemyBrain>();
            ProjectilePool projectiles = ProjectilePool.Active;

            float scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            Panel(new Rect(1460f, 42f, 410f, 360f), 0.9f);
            GUI.Label(new Rect(1490f, 62f, 350f, 38f), "RUNTIME DIAGNOSTICS", heading);
            GUI.Label(
                new Rect(1490f, 112f, 350f, 220f),
                $"MAGNETIC FORCE VECTORS     ON\n" +
                $"ENEMY STATE LABELS          ON\n" +
                $"ATTACK / PERCEPTION RANGES  ON\n\n" +
                $"ACTIVE ENEMIES      {enemies.Length}\n" +
                $"ACTIVE PROJECTILES  {projectiles?.ActiveCount ?? 0}\n" +
                $"POOLED PROJECTILES  {projectiles?.AvailableCount ?? 0}\n" +
                $"TOTAL ALLOCATED     {projectiles?.TotalCreated ?? 0}\n" +
                $"FIXED STEP          {Time.fixedDeltaTime * 1000f:0.0} ms\n" +
                $"FRAME RATE          {(Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f):0}",
                body);
            GUI.Label(new Rect(1490f, 350f, 350f, 30f), "F3  CLOSE OVERLAY", footer);

            GUI.matrix = previousMatrix;
        }

        private void OnDestroy()
        {
            Enabled = false;
            if (white != null)
            {
                Destroy(white);
            }
        }

        private void Panel(Rect rect, float opacity)
        {
            Fill(rect, new Color(0.018f, 0.038f, 0.06f, opacity));
            Fill(new Rect(rect.x, rect.y, 4f, rect.height), new Color(0.32f, 0.8f, 0.92f, 0.8f));
        }

        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (stylesReady)
            {
                return;
            }

            white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            white.SetPixel(0, 0, Color.white);
            white.Apply();

            heading = MakeStyle(25, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            body = MakeStyle(18, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.74f, 0.86f, 0.9f));
            footer = MakeStyle(13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.72f, 0.86f, 0.9f));
            stylesReady = true;
        }

        private static GUIStyle MakeStyle(int fontSize, FontStyle fontStyle, TextAnchor anchor, Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = anchor,
                normal = { textColor = color }
            };
        }
    }
}
