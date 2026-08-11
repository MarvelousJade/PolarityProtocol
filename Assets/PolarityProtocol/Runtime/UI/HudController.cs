using PolarityProtocol.Abilities;
using PolarityProtocol.Arena;
using PolarityProtocol.Combat;
using PolarityProtocol.Encounters;
using PolarityProtocol.Magnetics;
using PolarityProtocol.Player;
using PolarityProtocol.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolarityProtocol.UI
{
    public sealed class HudController : MonoBehaviour
    {
        private const string LayoutResource = "UI/PolarityInterface";
        private const string StatusBarResource = "UI/StatusBar";
        private const string StyleResource = "UI/PolarityStyles";
        private const string ThemeResource = "UI/PolarityRuntimeTheme";

        private GameSession session;
        private AbilityController ability;
        private PlayerMotor motor;
        private GameObject interfaceObject;
        private UIDocument document;
        private PanelSettings panelSettings;
        private VisualElement root;
        private VisualElement introScreen;
        private VisualElement gameplayScreen;
        private VisualElement pauseScreen;
        private VisualElement failureScreen;
        private VisualElement resultsScreen;
        private VisualElement damageVignette;
        private VisualElement abilityPanel;
        private VisualElement crosshair;
        private Label encounterTitle;
        private Label encounterObjective;
        private Label encounterSummary;
        private Label polarityTitle;
        private Label anchorCount;
        private Label failureTime;
        private Label failureDamage;
        private Label failureRedirects;
        private Label failureScore;
        private Label resultsTime;
        private Label resultsDamage;
        private Label resultsRedirects;
        private Label resultsScore;
        private Button startButton;
        private Button resumeButton;
        private Button pauseRestartButton;
        private Button retryButton;
        private Button resultsRestartButton;
        private Button[] introButtons;
        private Button[] pauseButtons;
        private Button[] failureButtons;
        private Button[] resultsButtons;
        private Button[] activeNavigationButtons;
        private StatusBarView healthBar;
        private StatusBarView energyBar;
        private StatusBarView dashBar;
        private StatusBarView anchorBar;
        private SessionState displayedState = (SessionState)(-1);
        private MagneticPolarity displayedPolarity = (MagneticPolarity)(-1);
        private int selectedButtonIndex;
        private int lastActivationFrame = -1;
        private int lastNavigationFrame = -1;
        private int displayedHealth = -1;
        private int displayedMaximumHealth = -1;
        private int displayedAnchorCount = -1;
        private int displayedEnemies = -1;
        private int displayedSecond = -1;
        private string displayedEncounterTitle;
        private string displayedEncounterObjective;
        private float lastNavigationVertical;
        private float damageFlash;
        private float displayedDamageFlash = -1f;
        private float nextTextRefresh;
        private bool interfaceReady;

        public void Configure(GameSession gameSession, AbilityController playerAbility, PlayerMotor playerMotor)
        {
            if (session != null && session.PlayerHealth != null)
            {
                session.PlayerHealth.Damaged -= OnPlayerDamaged;
            }

            session = gameSession;
            ability = playerAbility;
            motor = playerMotor;

            BuildInterface();
            if (session.PlayerHealth != null)
            {
                session.PlayerHealth.Damaged += OnPlayerDamaged;
            }

            if (interfaceReady)
            {
                ApplyState(session.State);
                RefreshGameplayText();
                RefreshProgress();
            }
        }

        private void Update()
        {
            if (!interfaceReady || session == null)
            {
                return;
            }

            damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.unscaledDeltaTime * 2.4f);
            if (Mathf.Abs(displayedDamageFlash - damageFlash) > 0.004f)
            {
                displayedDamageFlash = damageFlash;
                damageVignette.style.opacity = damageFlash;
            }

            if (displayedState != session.State)
            {
                ApplyState(session.State);
            }

            HandleLegacyNavigation();

            if (session.State != SessionState.Intro)
            {
                RefreshProgress();
                if (Time.unscaledTime >= nextTextRefresh)
                {
                    nextTextRefresh = Time.unscaledTime + 0.1f;
                    RefreshGameplayText();
                }
            }
        }

        private void OnDestroy()
        {
            if (session != null && session.PlayerHealth != null)
            {
                session.PlayerHealth.Damaged -= OnPlayerDamaged;
            }

            if (interfaceObject != null)
            {
                Destroy(interfaceObject);
            }

            if (panelSettings != null)
            {
                Destroy(panelSettings);
            }
        }

        private void BuildInterface()
        {
            if (interfaceReady)
            {
                return;
            }

            VisualTreeAsset layout = Resources.Load<VisualTreeAsset>(LayoutResource);
            VisualTreeAsset statusBarAsset = Resources.Load<VisualTreeAsset>(StatusBarResource);
            StyleSheet styleSheet = Resources.Load<StyleSheet>(StyleResource);
            ThemeStyleSheet theme = Resources.Load<ThemeStyleSheet>(ThemeResource);
            if (layout == null || statusBarAsset == null || styleSheet == null || theme == null)
            {
                Debug.LogError(
                    "[Polarity Protocol] UI Toolkit resources are missing. " +
                    "Expected PolarityInterface UXML, PolarityStyles USS, StatusBar UXML, and PolarityRuntimeTheme under Resources/UI.");
                enabled = false;
                return;
            }

            interfaceObject = new GameObject("Player Interface (UI Toolkit)");
            interfaceObject.transform.SetParent(transform, false);
            interfaceObject.SetActive(false);

            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "Polarity Runtime Panel Settings";
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 100f;
            panelSettings.themeStyleSheet = theme;
            panelSettings.clearDepthStencil = false;

            document = interfaceObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = layout;
            document.sortingOrder = 100f;
            interfaceObject.SetActive(true);

            root = document.rootVisualElement;
            root.styleSheets.Add(styleSheet);
            CacheElements(statusBarAsset);
            RegisterEvents();
            interfaceReady = true;
        }

        private void CacheElements(VisualTreeAsset statusBarAsset)
        {
            introScreen = root.Q<VisualElement>("intro-screen");
            gameplayScreen = root.Q<VisualElement>("gameplay-screen");
            pauseScreen = root.Q<VisualElement>("pause-screen");
            failureScreen = root.Q<VisualElement>("failure-screen");
            resultsScreen = root.Q<VisualElement>("results-screen");
            damageVignette = root.Q<VisualElement>("damage-vignette");
            abilityPanel = root.Q<VisualElement>("ability-panel");
            crosshair = root.Q<VisualElement>("crosshair");

            encounterTitle = root.Q<Label>("encounter-title");
            encounterObjective = root.Q<Label>("encounter-objective");
            encounterSummary = root.Q<Label>("encounter-summary");
            polarityTitle = root.Q<Label>("polarity-title");
            anchorCount = root.Q<Label>("anchor-count");

            failureTime = root.Q<Label>("failure-time");
            failureDamage = root.Q<Label>("failure-damage");
            failureRedirects = root.Q<Label>("failure-redirects");
            failureScore = root.Q<Label>("failure-score");
            resultsTime = root.Q<Label>("results-time");
            resultsDamage = root.Q<Label>("results-damage");
            resultsRedirects = root.Q<Label>("results-redirects");
            resultsScore = root.Q<Label>("results-score");

            startButton = root.Q<Button>("start-button");
            resumeButton = root.Q<Button>("resume-button");
            pauseRestartButton = root.Q<Button>("pause-restart-button");
            retryButton = root.Q<Button>("retry-button");
            resultsRestartButton = root.Q<Button>("results-restart-button");

            healthBar = CreateStatusBar(statusBarAsset, "health-bar-host", string.Empty, BarAccent.Push);
            energyBar = CreateStatusBar(statusBarAsset, "energy-bar-host", "FIELD ENERGY", BarAccent.Pull);
            dashBar = CreateStatusBar(statusBarAsset, "dash-bar-host", "DASH", BarAccent.Neutral);
            anchorBar = CreateStatusBar(statusBarAsset, "anchor-bar-host", "ANCHOR", BarAccent.Pull);

            introButtons = new[] { startButton };
            pauseButtons = new[] { resumeButton, pauseRestartButton };
            failureButtons = new[] { retryButton };
            resultsButtons = new[] { resultsRestartButton };
        }

        private StatusBarView CreateStatusBar(
            VisualTreeAsset asset,
            string hostName,
            string label,
            BarAccent accent)
        {
            VisualElement host = root.Q<VisualElement>(hostName);
            TemplateContainer instance = asset.Instantiate();
            instance.AddToClassList("status-bar-template");
            host.Add(instance);
            return new StatusBarView(instance, label, accent);
        }

        private void RegisterEvents()
        {
            startButton.clicked += StartFromUi;
            resumeButton.clicked += ResumeFromUi;
            pauseRestartButton.clicked += RestartFromUi;
            retryButton.clicked += RestartFromUi;
            resultsRestartButton.clicked += RestartFromUi;
            root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit, TrickleDown.TrickleDown);
        }

        private void ApplyState(SessionState state)
        {
            displayedState = state;
            SetVisible(introScreen, state == SessionState.Intro);
            SetVisible(gameplayScreen, state != SessionState.Intro);
            SetVisible(pauseScreen, state == SessionState.Paused);
            SetVisible(failureScreen, state == SessionState.Failed);
            SetVisible(resultsScreen, state == SessionState.Complete);

            switch (state)
            {
                case SessionState.Intro:
                    SetNavigationButtons(introButtons);
                    break;
                case SessionState.Paused:
                    SetNavigationButtons(pauseButtons);
                    break;
                case SessionState.Failed:
                    RefreshOutcome(
                        failureTime,
                        failureDamage,
                        failureRedirects,
                        failureScore);
                    SetNavigationButtons(failureButtons);
                    break;
                case SessionState.Complete:
                    RefreshOutcome(
                        resultsTime,
                        resultsDamage,
                        resultsRedirects,
                        resultsScore);
                    SetNavigationButtons(resultsButtons);
                    break;
                default:
                    SetNavigationButtons(null);
                    break;
            }
        }

        private void SetNavigationButtons(Button[] buttons)
        {
            if (activeNavigationButtons != null)
            {
                for (int i = 0; i < activeNavigationButtons.Length; i++)
                {
                    activeNavigationButtons[i].RemoveFromClassList("is-selected");
                }
            }

            activeNavigationButtons = buttons;
            selectedButtonIndex = 0;
            lastNavigationVertical = 0f;
            if (activeNavigationButtons != null && activeNavigationButtons.Length > 0)
            {
                root.schedule.Execute(FocusSelectedButton);
            }
            else if (root.panel?.focusController.focusedElement is VisualElement focused)
            {
                focused.Blur();
            }
        }

        private void FocusSelectedButton()
        {
            if (activeNavigationButtons == null || activeNavigationButtons.Length == 0)
            {
                return;
            }

            for (int i = 0; i < activeNavigationButtons.Length; i++)
            {
                activeNavigationButtons[i].EnableInClassList("is-selected", i == selectedButtonIndex);
            }

            activeNavigationButtons[selectedButtonIndex].Focus();
        }

        private void HandleLegacyNavigation()
        {
            if (activeNavigationButtons == null || activeNavigationButtons.Length == 0)
            {
                lastNavigationVertical = 0f;
                return;
            }

            float vertical = LegacyInput.MenuNavigationVertical;
            bool crossedThreshold = Mathf.Abs(vertical) >= 0.65f &&
                                    Mathf.Abs(lastNavigationVertical) < 0.65f;
            if (crossedThreshold)
            {
                MoveSelection(vertical > 0f ? -1 : 1);
            }
            lastNavigationVertical = vertical;

            if (LegacyInput.MenuSubmitPressed)
            {
                ActivateSelection();
            }
        }

        private void OnNavigationMove(NavigationMoveEvent navigationEvent)
        {
            int direction = navigationEvent.direction switch
            {
                NavigationMoveEvent.Direction.Up => -1,
                NavigationMoveEvent.Direction.Left => -1,
                NavigationMoveEvent.Direction.Previous => -1,
                NavigationMoveEvent.Direction.Down => 1,
                NavigationMoveEvent.Direction.Right => 1,
                NavigationMoveEvent.Direction.Next => 1,
                _ => 0
            };

            if (direction == 0 || activeNavigationButtons == null)
            {
                return;
            }

            MoveSelection(direction);
            navigationEvent.StopImmediatePropagation();
        }

        private void OnNavigationSubmit(NavigationSubmitEvent submitEvent)
        {
            if (activeNavigationButtons == null)
            {
                return;
            }

            ActivateSelection();
            submitEvent.StopImmediatePropagation();
        }

        private void MoveSelection(int direction)
        {
            if (lastNavigationFrame == Time.frameCount ||
                activeNavigationButtons == null ||
                activeNavigationButtons.Length <= 1)
            {
                return;
            }

            lastNavigationFrame = Time.frameCount;
            selectedButtonIndex = (selectedButtonIndex + direction + activeNavigationButtons.Length) %
                                  activeNavigationButtons.Length;
            FocusSelectedButton();
            FeedbackBus.Pulse(410f, 0.035f, 0.025f);
        }

        private void ActivateSelection()
        {
            if (activeNavigationButtons == null || activeNavigationButtons.Length == 0)
            {
                return;
            }

            if (root.panel?.focusController.focusedElement is Button focusedButton)
            {
                for (int i = 0; i < activeNavigationButtons.Length; i++)
                {
                    if (activeNavigationButtons[i] == focusedButton)
                    {
                        selectedButtonIndex = i;
                        break;
                    }
                }
            }

            Button selected = activeNavigationButtons[selectedButtonIndex];
            if (selected == startButton)
            {
                StartFromUi();
            }
            else if (selected == resumeButton)
            {
                ResumeFromUi();
            }
            else
            {
                RestartFromUi();
            }
        }

        private void StartFromUi()
        {
            if (!ClaimActivation() || session.State != SessionState.Intro)
            {
                return;
            }

            session.BeginRun();
        }

        private void ResumeFromUi()
        {
            if (!ClaimActivation() || session.State != SessionState.Paused)
            {
                return;
            }

            session.TogglePause();
        }

        private void RestartFromUi()
        {
            if (!ClaimActivation() ||
                session.State is not (SessionState.Paused or SessionState.Failed or SessionState.Complete))
            {
                return;
            }

            session.Restart();
        }

        private bool ClaimActivation()
        {
            if (lastActivationFrame == Time.frameCount)
            {
                return false;
            }

            lastActivationFrame = Time.frameCount;
            return true;
        }

        private void RefreshProgress()
        {
            Health health = session.PlayerHealth;
            healthBar.SetProgress(health == null ? 0f : health.Normalized);
            energyBar.SetProgress(ability.EnergyNormalized);
            dashBar.SetProgress(1f - motor.DashCooldownNormalized);
            anchorBar.SetProgress(ability.CooldownRemaining <= 0f ? 1f : 0f);
        }

        private void RefreshGameplayText()
        {
            Health health = session.PlayerHealth;
            EncounterDirector director = session.Encounters;

            int currentHealth = health == null ? 0 : Mathf.CeilToInt(health.Current);
            int maximumHealth = health == null ? 0 : Mathf.CeilToInt(health.Maximum);
            if (currentHealth != displayedHealth || maximumHealth != displayedMaximumHealth)
            {
                displayedHealth = currentHealth;
                displayedMaximumHealth = maximumHealth;
                healthBar.SetValue($"{currentHealth} / {maximumHealth}");
            }

            string title = director.CurrentTitle;
            if (displayedEncounterTitle != title)
            {
                displayedEncounterTitle = title;
                encounterTitle.text = title.ToUpperInvariant();
            }

            string objective = director.CurrentObjective;
            if (displayedEncounterObjective != objective)
            {
                displayedEncounterObjective = objective;
                encounterObjective.text = objective;
            }

            int livingEnemies = director.LivingEnemyCount;
            int elapsedSecond = Mathf.FloorToInt(session.ElapsedSeconds);
            if (livingEnemies != displayedEnemies || elapsedSecond != displayedSecond)
            {
                displayedEnemies = livingEnemies;
                displayedSecond = elapsedSecond;
                encounterSummary.text =
                    $"HOSTILES  {livingEnemies:00}      TIME  {FormatTime(elapsedSecond)}";
            }

            int activeAnchors = ability.ActiveAnchorCount;
            if (activeAnchors != displayedAnchorCount)
            {
                displayedAnchorCount = activeAnchors;
                anchorCount.text = $"{activeAnchors} / 2 ACTIVE";
            }

            MagneticPolarity polarity = ability.SelectedPolarity;
            if (polarity != displayedPolarity)
            {
                displayedPolarity = polarity;
                bool pull = polarity == MagneticPolarity.Negative;
                polarityTitle.text = $"{polarity.Label()} ANCHOR";
                abilityPanel.EnableInClassList("polarity-pull", pull);
                abilityPanel.EnableInClassList("polarity-push", !pull);
                crosshair.EnableInClassList("polarity-pull", pull);
                crosshair.EnableInClassList("polarity-push", !pull);
                energyBar.SetAccent(pull ? BarAccent.Pull : BarAccent.Push);
                anchorBar.SetAccent(pull ? BarAccent.Pull : BarAccent.Push);
            }
        }

        private void RefreshOutcome(
            Label time,
            Label damage,
            Label redirects,
            Label score)
        {
            time.text = FormatTime(Mathf.FloorToInt(session.ElapsedSeconds));
            damage.text = $"{session.DamageTaken:0}";
            redirects.text = session.RedirectCount.ToString();
            score.text = $"{session.Score:00000}";
        }

        private void OnPlayerDamaged(DamageInfo _, float amount)
        {
            damageFlash = Mathf.Clamp01(damageFlash + amount / 45f);
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static string FormatTime(int seconds)
        {
            int minutes = seconds / 60;
            int remaining = seconds % 60;
            return $"{minutes:00}:{remaining:00}";
        }

        private enum BarAccent
        {
            Neutral,
            Pull,
            Push
        }

        private sealed class StatusBarView
        {
            private readonly VisualElement fill;
            private readonly Label value;
            private float displayedProgress = -1f;
            private BarAccent displayedAccent = (BarAccent)(-1);

            public StatusBarView(VisualElement rootElement, string labelText, BarAccent accent)
            {
                fill = rootElement.Q<VisualElement>("status-fill");
                rootElement.Q<Label>("status-label").text = labelText;
                value = rootElement.Q<Label>("status-value");
                SetAccent(accent);
            }

            public void SetProgress(float normalized)
            {
                normalized = Mathf.Clamp01(normalized);
                if (Mathf.Abs(displayedProgress - normalized) < 0.001f)
                {
                    return;
                }

                displayedProgress = normalized;
                fill.style.width = Length.Percent(normalized * 100f);
            }

            public void SetValue(string text)
            {
                value.text = text;
            }

            public void SetAccent(BarAccent accent)
            {
                if (displayedAccent == accent)
                {
                    return;
                }

                displayedAccent = accent;
                fill.EnableInClassList("status-fill-neutral", accent == BarAccent.Neutral);
                fill.EnableInClassList("status-fill-pull", accent == BarAccent.Pull);
                fill.EnableInClassList("status-fill-push", accent == BarAccent.Push);
            }
        }
    }
}
