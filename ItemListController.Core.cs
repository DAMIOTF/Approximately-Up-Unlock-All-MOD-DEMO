using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace ApproximatelyUpMod
{
    public partial class ItemListController : MonoBehaviour
    {
        internal struct ItemEntry
        {
            internal SCPrefab Prefab;
            internal EPC_SpaceshipComponent Component;
            internal string Name;
        }

        private const KeyCode ToggleKey = KeyCode.F10;
        private const bool ShowGuiOnStartup = true;
        private const double StartupShowRetryDelaySeconds = 0.5;
        private const int MaxStartupShowAttempts = 20;
        internal const int MaxMaterialsAmount = 99999;
        internal const int DefaultMaterialsAmount = 999;

        private static readonly List<ItemEntry> _allItems = new List<ItemEntry>(512);
        private static readonly System.Random _rng = new System.Random();

        private static ItemListController _activeInstance;
        private static bool _cacheReady;

        public static int MaterialsAmountOverride = DefaultMaterialsAmount;
        public static bool EnforceMaterialsAmount;

        private bool _isVisible;
        private bool _startupShowPending;
        private int _startupShowAttempts;
        private string _lastSceneName = string.Empty;

        private double _nextRefreshAt;
        private double _nextStartupShowAttemptAt;

        private int _itemsRevision;

        private UIBase _uiBase;
        private FlatModPanel _panel;

        private bool _prevCursorVisible;
        private CursorLockMode _prevLockMode;

        private void Start()
        {
            _activeInstance = this;
            _startupShowPending = ShowGuiOnStartup;

            ModLog.Info("ItemListController started.");
            EnsureUniverseUi();
            TryRefreshItems(force: true);
        }

        private void Update()
        {
            ThrusterPowerSystem.Tick();
            SyncShipTearingOverride();
            BuildingRuntimeOverrides.Tick();

            if (Input.GetKeyDown(ToggleKey))
            {
                ModLog.Info("Toggle key pressed (F10).");
                ToggleVisibility();
            }

            if (Time.realtimeSinceStartupAsDouble >= _nextRefreshAt)
            {
                TryRefreshItems(force: false);
            }

            if (_startupShowPending && Time.realtimeSinceStartupAsDouble >= _nextStartupShowAttemptAt)
            {
                TryShowGuiOnStartup();
            }

            if (_panel != null)
            {
                _panel.SyncRuntimeState();
            }

            if (_isVisible)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void OnDestroy()
        {
            if (_activeInstance == this)
            {
                _activeInstance = null;
            }
        }

        internal void NotifySceneLoaded(string sceneName)
        {
            _lastSceneName = sceneName ?? string.Empty;
            _startupShowPending = ShowGuiOnStartup;
            _nextStartupShowAttemptAt = Time.realtimeSinceStartupAsDouble + 0.1;
            _startupShowAttempts = 0;

            ModLog.Info("NotifySceneLoaded -> scheduling GUI activation for scene: " + _lastSceneName);
            EnsureUniverseUi();
        }

        private void TryShowGuiOnStartup()
        {
            _startupShowAttempts++;
            _nextStartupShowAttemptAt = Time.realtimeSinceStartupAsDouble + StartupShowRetryDelaySeconds;

            EnsureUniverseUi();
            if (_panel == null)
            {
                if (_startupShowAttempts <= 3 || _startupShowAttempts % 5 == 0)
                {
                    ModLog.Warn($"GUI startup attempt {_startupShowAttempts}: panel not ready yet.");
                }

                if (_startupShowAttempts >= MaxStartupShowAttempts)
                {
                    _startupShowPending = false;
                    ModLog.Error("GUI startup failed: panel was never created. Try manual F10 toggle.");
                }
                return;
            }

            if (!_isVisible)
            {
                SetVisibility(true, preserveCursorState: true);
                ModLog.Info($"GUI activated automatically in scene '{_lastSceneName}'.");
            }

            _startupShowPending = false;
        }
    }
}
