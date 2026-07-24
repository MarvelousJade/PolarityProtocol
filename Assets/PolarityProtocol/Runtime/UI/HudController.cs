using PolarityProtocol.Abilities;
using PolarityProtocol.AI;
using PolarityProtocol.Arena;
using PolarityProtocol.Combat;
using PolarityProtocol.Encounters;
using PolarityProtocol.Magnetics;
using PolarityProtocol.Player;
using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.UI
{
    public sealed class HudController : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        private GameSession session;
        private AbilityController ability;
        private PlayerMotor motor;
        private Texture2D white;
        private GUIStyle title;
        private GUIStyle subtitle;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle tiny;
        private GUIStyle button;
        private GUIStyle center;
        private bool stylesReady;
        private float damageFlash;

        public void Configure(GameSession gameSession, AbilityController playerAbility, PlayerMotor playerMotor)
        {
            if (session != null && session.PlayerHealth != null)
            {
                session.PlayerHealth.Damaged -= OnPlayerDamaged;
            }

            session = gameSession;
            ability = playerAbility;
            motor = playerMotor;

            if (session.PlayerHealth != null)
            {
                session.PlayerHealth.Damaged += OnPlayerDamaged;
            }
        }

        private void Update()
        {
            damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.unscaledDeltaTime * 2.4f);
        }

        private void OnDestroy()
        {
            if (session != null && session.PlayerHealth != null)
            {
                session.PlayerHealth.Damaged -= OnPlayerDamaged;
            }
        }

        private void OnGUI()
        {
            if (session == null)
            {
                return;
            }

            EnsureStyles();
            float scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            switch (session.State)
            {
                case SessionState.Intro:
                    DrawIntro();
                    break;
                case SessionState.Running:
                    DrawGameplay();
                    break;
                case SessionState.Paused:
                    DrawGameplay();
                    DrawPause();
                    break;
                case SessionState.Complete:
                    DrawGameplay();
                    DrawComplete(true);
                    break;
                case SessionState.Failed:
                    DrawGameplay();
                    DrawComplete(false);
                    break;
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawIntro()
        {
            Fill(new Rect(0f, 0f, ReferenceWidth, ReferenceHeight), new Color(0.01f, 0.025f, 0.045f, 0.88f));
            Fill(new Rect(0f, 0f, 18f, ReferenceHeight), RuntimeArt.Pull);
            Fill(new Rect(ReferenceWidth - 18f, 0f, 18f, ReferenceHeight), RuntimeArt.Push);

            GUI.Label(new Rect(170f, 150f, 1580f, 120f), "POLARITY PROTOCOL", title);
            GUI.Label(
                new Rect(420f, 275f, 1080f, 70f),
                "CONTROL THE FORCE. REWRITE THE FIGHT.",
                subtitle);

            Panel(new Rect(415f, 385f, 1090f, 320f), 0.83f);
            GUI.Label(
                new Rect(475f, 425f, 970f, 78f),
                "Deploy two magnetic anchors. Opposite polarity PULLS; matching polarity PUSHES. " +
                "Move robots into hazards and bend hostile projectiles back at their owners.",
                body);

            GUI.Label(new Rect(500f, 535f, 440f, 130f),
                "KEYBOARD + MOUSE\nWASD  Move     •     Mouse  Aim\nLMB  Fire       •     RMB  Anchor\nQ  Polarity     •     Space  Dash\nR  Recall       •     F3  Debug",
                small);
            GUI.Label(new Rect(990f, 535f, 440f, 130f),
                "CONTROLLER\nLeft Stick  Move\nRight Stick  Aim\nRB / A  Fire     •     LB  Anchor\nX  Polarity      •     B  Dash\nY  Recall         •     Menu  Pause",
                small);

            if (GUI.Button(new Rect(710f, 770f, 500f, 92f), "INITIATE  [ ENTER / A ]", button))
            {
                session.BeginRun();
            }

            GUI.Label(
                new Rect(560f, 900f, 800f, 40f),
                "THREE ENCOUNTERS  //  FIVE MINUTE COMBAT SLICE  //  F3 RUNTIME DIAGNOSTICS",
                tiny);
        }

        private void DrawGameplay()
        {
            Health health = session.PlayerHealth;
            EncounterDirector director = session.Encounters;

            DrawDamageVignette();

            Panel(new Rect(48f, 42f, 570f, 130f), 0.72f);
            GUI.Label(new Rect(75f, 52f, 515f, 52f), director.CurrentTitle.ToUpperInvariant(), subtitle);
            GUI.Label(new Rect(75f, 110f, 515f, 48f), director.CurrentObjective, small);

            Panel(new Rect(48f, 910f, 570f, 120f), 0.76f);
            GUI.Label(new Rect(76f, 925f, 160f, 30f), "INTEGRITY", small);
            DrawBar(
                new Rect(76f, 965f, 500f, 28f),
                health == null ? 0f : health.Normalized,
                RuntimeArt.Push,
                $"{(health == null ? 0f : health.Current):0} / {(health == null ? 0f : health.Maximum):0}");

            Panel(new Rect(1285f, 856f, 585f, 174f), 0.76f);
            Color polarityColor = ability.SelectedPolarity == Magnetics.MagneticPolarity.Negative
                ? RuntimeArt.Pull
                : RuntimeArt.Push;
            GUI.Label(new Rect(1315f, 875f, 320f, 48f), $"{ability.SelectedPolarity.Verb()} ANCHOR", subtitle);
            GUI.Label(
                new Rect(1650f, 884f, 180f, 30f),
                $"{ability.ActiveAnchorCount} / 2 ACTIVE",
                tiny);
            DrawBar(new Rect(1317f, 935f, 510f, 24f), ability.EnergyNormalized, polarityColor, "FIELD ENERGY");
            DrawBar(new Rect(1317f, 979f, 245f, 13f), 1f - motor.DashCooldownNormalized, Color.white, "DASH");
            DrawBar(
                new Rect(1582f, 979f, 245f, 13f),
                ability.CooldownRemaining <= 0f ? 1f : 0f,
                polarityColor,
                "ANCHOR");

            GUI.Label(
                new Rect(780f, 44f, 360f, 42f),
                $"HOSTILES  {director.LivingEnemyCount:00}      TIME  {FormatTime(session.ElapsedSeconds)}",
                center);

            Color crosshairColor = polarityColor;
            Fill(new Rect(938f, 538f, 12f, 2f), crosshairColor);
            Fill(new Rect(970f, 538f, 12f, 2f), crosshairColor);
            Fill(new Rect(959f, 517f, 2f, 12f), crosshairColor);
            Fill(new Rect(959f, 549f, 2f, 12f), crosshairColor);
            Fill(new Rect(957f, 536f, 6f, 6f), Color.white);

            GUI.Label(
                new Rect(725f, 1025f, 470f, 28f),
                "LMB FIRE   •   RMB DEPLOY   •   Q POLARITY   •   R RECALL   •   F3 DEBUG",
                tiny);

            if (DebugOverlay.Enabled)
            {
                DrawDebug();
            }
        }

        private void DrawDamageVignette()
        {
            if (damageFlash <= 0f)
            {
                return;
            }

            Color color = new Color(RuntimeArt.Push.r, RuntimeArt.Push.g, RuntimeArt.Push.b, damageFlash * 0.42f);
            const float edge = 115f;
            Fill(new Rect(0f, 0f, ReferenceWidth, edge), color);
            Fill(new Rect(0f, ReferenceHeight - edge, ReferenceWidth, edge), color);
            Fill(new Rect(0f, edge, edge, ReferenceHeight - edge * 2f), color);
            Fill(new Rect(ReferenceWidth - edge, edge, edge, ReferenceHeight - edge * 2f), color);
        }

        private void OnPlayerDamaged(DamageInfo damage, float amount)
        {
            damageFlash = Mathf.Clamp01(damageFlash + amount / 45f);
        }

        private void DrawDebug()
        {
            EnemyBrain[] enemies = FindObjectsByType<EnemyBrain>();
            ProjectilePool projectiles = ProjectilePool.Active;

            Panel(new Rect(1460f, 42f, 410f, 360f), 0.88f);
            GUI.Label(new Rect(1490f, 62f, 350f, 38f), "RUNTIME DIAGNOSTICS", subtitle);
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
                small);
            GUI.Label(new Rect(1490f, 350f, 350f, 30f), "F3  CLOSE OVERLAY", tiny);
        }

        private void DrawPause()
        {
            Fill(new Rect(0f, 0f, ReferenceWidth, ReferenceHeight), new Color(0f, 0f, 0f, 0.58f));
            Panel(new Rect(620f, 285f, 680f, 500f), 0.94f);
            GUI.Label(new Rect(700f, 335f, 520f, 70f), "SIMULATION PAUSED", title);
            GUI.Label(new Rect(735f, 455f, 450f, 85f), "Your anchor fields are suspended.\nResume when ready.", body);

            if (GUI.Button(new Rect(735f, 585f, 450f, 70f), "RESUME  [ ESC / MENU ]", button))
            {
                session.TogglePause();
            }

            if (GUI.Button(new Rect(735f, 675f, 450f, 58f), "RESTART RUN", button))
            {
                session.Restart();
            }
        }

        private void DrawComplete(bool victory)
        {
            Fill(new Rect(0f, 0f, ReferenceWidth, ReferenceHeight), new Color(0.01f, 0.02f, 0.035f, 0.78f));
            Panel(new Rect(530f, 180f, 860f, 720f), 0.96f);

            Color outcome = victory ? RuntimeArt.Pull : RuntimeArt.Push;
            Fill(new Rect(530f, 180f, 860f, 12f), outcome);
            GUI.Label(
                new Rect(620f, 245f, 680f, 90f),
                victory ? "PROTOCOL COMPLETE" : "SYSTEM COLLAPSE",
                title);
            GUI.Label(
                new Rect(680f, 355f, 560f, 45f),
                victory ? "THE ARENA IS YOUR WEAPON." : "ADAPT. REDEPLOY. TRY AGAIN.",
                subtitle);

            GUI.Label(new Rect(700f, 455f, 300f, 44f), "COMPLETION TIME", small);
            GUI.Label(new Rect(1050f, 455f, 200f, 44f), FormatTime(session.ElapsedSeconds), subtitle);
            GUI.Label(new Rect(700f, 520f, 300f, 44f), "DAMAGE TAKEN", small);
            GUI.Label(new Rect(1050f, 520f, 200f, 44f), $"{session.DamageTaken:0}", subtitle);
            GUI.Label(new Rect(700f, 585f, 300f, 44f), "REDIRECTIONS", small);
            GUI.Label(new Rect(1050f, 585f, 200f, 44f), $"{session.RedirectCount}", subtitle);
            GUI.Label(new Rect(700f, 650f, 300f, 54f), "SCORE", subtitle);
            GUI.Label(new Rect(1020f, 640f, 230f, 70f), $"{session.Score:00000}", title);

            if (GUI.Button(new Rect(720f, 760f, 480f, 72f), "RESTART  [ ENTER / A ]", button))
            {
                session.Restart();
            }
        }

        private void DrawBar(Rect rect, float normalized, Color color, string label)
        {
            Fill(rect, new Color(1f, 1f, 1f, 0.1f));
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(normalized), rect.height), color);
            GUI.Label(rect, label, tiny);
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

            title = MakeStyle(44, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            subtitle = MakeStyle(25, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            body = MakeStyle(21, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.82f, 0.9f, 0.94f));
            body.wordWrap = true;
            small = MakeStyle(18, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.74f, 0.86f, 0.9f));
            small.wordWrap = true;
            tiny = MakeStyle(13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.72f, 0.86f, 0.9f));
            center = MakeStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            button = MakeStyle(21, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            button.normal.background = white;
            button.normal.textColor = RuntimeArt.Dark;
            button.hover.background = white;
            button.hover.textColor = RuntimeArt.Push;
            button.active.background = white;
            button.active.textColor = RuntimeArt.Pull;
            button.padding = new RectOffset(20, 20, 10, 10);

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

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int remaining = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{remaining:00}";
        }
    }
}
