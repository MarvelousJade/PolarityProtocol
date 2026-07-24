using PolarityProtocol.Combat;
using PolarityProtocol.Encounters;
using PolarityProtocol.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolarityProtocol.Arena
{
    public enum SessionState
    {
        Intro,
        Running,
        Paused,
        Complete,
        Failed
    }

    public sealed class GameSession : MonoBehaviour
    {
        private Health playerHealth;
        private EncounterDirector encounterDirector;
        private float startedAt;
        private float finishedAt;

        public static GameSession Active { get; private set; }
        public SessionState State { get; private set; } = SessionState.Intro;
        public bool IsRunning => State == SessionState.Running;
        public float ElapsedSeconds => State is SessionState.Complete or SessionState.Failed
            ? finishedAt - startedAt
            : Mathf.Max(0f, Time.time - startedAt);
        public float DamageTaken { get; private set; }
        public int RedirectCount { get; private set; }
        public int Score
        {
            get
            {
                float timePenalty = ElapsedSeconds * 7f;
                float damagePenalty = DamageTaken * 10f;
                float redirectBonus = RedirectCount * 350f;
                return Mathf.Max(0, Mathf.RoundToInt(10000f + redirectBonus - timePenalty - damagePenalty));
            }
        }

        public Health PlayerHealth => playerHealth;
        public EncounterDirector Encounters => encounterDirector;

        private void Awake()
        {
            Active = this;
            Time.timeScale = 1f;
            SetCursor(false);
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }

            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (State == SessionState.Intro)
            {
                if (Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.JoystickButton0))
                {
                    BeginRun();
                }
                return;
            }

            if (State is SessionState.Complete or SessionState.Failed)
            {
                if (Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.JoystickButton0))
                {
                    Restart();
                }
                return;
            }

            if (LegacyInput.PausePressed)
            {
                TogglePause();
            }
        }

        public void Configure(Health controlledPlayer, EncounterDirector director)
        {
            playerHealth = controlledPlayer;
            encounterDirector = director;
            playerHealth.Damaged += OnPlayerDamaged;
            playerHealth.Died += OnPlayerDied;
        }

        public void BeginRun()
        {
            if (State != SessionState.Intro)
            {
                return;
            }

            State = SessionState.Running;
            startedAt = Time.time;
            DamageTaken = 0f;
            RedirectCount = 0;
            SetCursor(true);
            encounterDirector.Begin();
            FeedbackBus.Pulse(330f, 0.12f, 0.09f);
        }

        public void TogglePause()
        {
            if (State == SessionState.Running)
            {
                State = SessionState.Paused;
                Time.timeScale = 0f;
                SetCursor(false);
            }
            else if (State == SessionState.Paused)
            {
                State = SessionState.Running;
                Time.timeScale = 1f;
                SetCursor(true);
            }
        }

        public void CompleteRun()
        {
            if (State != SessionState.Running)
            {
                return;
            }

            finishedAt = Time.time;
            State = SessionState.Complete;
            SetCursor(false);
            FeedbackBus.Pulse(520f, 0.32f, 0.13f);
        }

        public void RegisterRedirect()
        {
            RedirectCount++;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnPlayerDamaged(DamageInfo _, float amount)
        {
            DamageTaken += amount;
        }

        private void OnPlayerDied(Health _, DamageInfo __)
        {
            if (State is not SessionState.Running)
            {
                return;
            }

            finishedAt = Time.time;
            State = SessionState.Failed;
            SetCursor(false);
            FeedbackBus.Pulse(70f, 0.4f, 0.16f);
        }

        private static void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
