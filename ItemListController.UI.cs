using System;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace ApproximatelyUpMod
{
    public partial class ItemListController
    {
        private static bool _universeInitRequested;
        private static bool _universeReady;

        private static void UniverseLog(string message, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                ModLog.Error("UniverseLib: " + message);
                return;
            }

            if (type == LogType.Warning)
            {
                ModLog.Warn("UniverseLib: " + message);
                return;
            }

            ModLog.Info("UniverseLib: " + message);
        }

        private void EnsureUniverseUi()
        {
            if (_uiBase != null)
            {
                return;
            }

            if (!_universeInitRequested)
            {
                _universeInitRequested = true;
                ModLog.Info("Initializing UniverseLib UI...");
                Universe.Init(OnUniverseInitialized, UniverseLog);
                return;
            }

            if (_universeReady)
            {
                BuildUi();
            }
        }

        private void OnUniverseInitialized()
        {
            _universeReady = true;
            ModLog.Info("UniverseLib initialization callback fired.");
            BuildUi();
        }

        private void BuildUi()
        {
            if (_uiBase != null)
            {
                return;
            }

            _uiBase = UniversalUI.RegisterUI("ApproximatelyUpMOD_UI", () => { });
            _panel = new FlatModPanel(_uiBase, this);
            _panel.SetActive(false);
            ConfigureCanvasForVisibility();
            ModLog.Info("GUI panel created.");
        }

        private void ConfigureCanvasForVisibility()
        {
            if (_uiBase == null || _uiBase.RootObject == null)
            {
                return;
            }

            // Force high sort order to avoid being hidden by game HUD canvases on some setups.
            Canvas[] canvases = _uiBase.RootObject.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                canvas.overrideSorting = true;
                if (canvas.sortingOrder < 30000)
                {
                    canvas.sortingOrder = 30000;
                }
            }

            ModLog.Info("Canvas visibility override applied to UniverseLib UI.");
        }

        private void ToggleVisibility()
        {
            EnsureUniverseUi();
            if (_panel == null)
            {
                ModLog.Warn("ToggleVisibility ignored: panel not created yet.");
                return;
            }

            SetVisibility(!_isVisible, preserveCursorState: true);
        }

        private void SetVisibility(bool visible, bool preserveCursorState)
        {
            if (_panel == null)
            {
                return;
            }

            if (visible)
            {
                if (preserveCursorState)
                {
                    _prevCursorVisible = Cursor.visible;
                    _prevLockMode = Cursor.lockState;
                }

                _isVisible = true;
                _panel.SetActive(true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            _isVisible = false;
            _panel.SetActive(false);
            Cursor.visible = _prevCursorVisible;
            Cursor.lockState = _prevLockMode;
        }

        private void HandlePanelClosed()
        {
            if (!_isVisible)
            {
                return;
            }

            _isVisible = false;
            Cursor.visible = _prevCursorVisible;
            Cursor.lockState = _prevLockMode;
        }

        private sealed partial class FlatModPanel : PanelBase
        {
            private ItemListController _owner;

            private Text _foldoutText;
            private Text _itemsCountText;
            private Text _itemsLoadingText;
            private GameObject _itemsScrollView;
            private GameObject _itemsScrollContent;
            private ButtonRef _unlockButton;

            private int _lastBuiltRevision = -1;
            private bool _itemsExpanded;

            public override string Name => "Approximately Up MOD by: DMTF";
            public override int MinWidth => 460;
            public override int MinHeight => 640;
            public override Vector2 DefaultAnchorMin => new Vector2(0f, 1f);
            public override Vector2 DefaultAnchorMax => new Vector2(0f, 1f);
            public override Vector2 DefaultPosition => new Vector2(20f, -20f);

            public FlatModPanel(UIBase owner, ItemListController controller)
                : base(owner)
            {
                _owner = controller;
            }

            private ItemListController OwnerController
            {
                get
                {
                    return _owner ?? _activeInstance;
                }
            }

            protected override void OnClosePanelClicked()
            {
                base.OnClosePanelClicked();
                ItemListController owner = OwnerController;
                owner?.HandlePanelClosed();
            }

            protected override void ConstructPanelContent()
            {
                GameObject body = UIFactory.CreateVerticalGroup(
                    ContentRoot,
                    "Body",
                    false,
                    false,
                    true,
                    true,
                    8,
                    new Vector4(10f, 10f, 10f, 10f),
                    new Color(0.1f, 0.1f, 0.11f, 0.96f));
                UIFactory.SetLayoutElement(body, flexibleWidth: 9999, flexibleHeight: 9999);

                GameObject safetySection = CreateSection(body, string.Empty);
                BuildShipSafetyControls(safetySection);

                GameObject resourcesSection = CreateSection(body, "Resources");
                BuildMaterialsControls(resourcesSection);

                GameObject itemsSection = CreateSection(body, string.Empty);
                BuildItemsSection(itemsSection);

                GameObject actionsSection = CreateSection(body, "Actions");
                BuildActionsSection(actionsSection);

                GameObject buildingSection = CreateSection(body, string.Empty);
                BuildBuildingSection(buildingSection);

                GameObject thrusterPowerSection = CreateSection(body, string.Empty);
                BuildThrusterPowerSection(thrusterPowerSection);

                GameObject teleportStationsSection = CreateSection(body, string.Empty);
                BuildTeleportStationsSection(teleportStationsSection);

                GameObject teleportPlanetsSection = CreateSection(body, string.Empty);
                BuildTeleportPlanetsSection(teleportPlanetsSection);

                GameObject spacer = UIFactory.CreateUIObject("FooterSpacer", body);
                UIFactory.SetLayoutElement(spacer, flexibleHeight: 9999, minHeight: 4);

                GameObject footerSection = UIFactory.CreateVerticalGroup(
                    body,
                    "FooterSection",
                    false,
                    false,
                    true,
                    true,
                    0,
                    new Vector4(8f, 8f, 8f, 8f),
                    new Color(0.14f, 0.14f, 0.15f, 1f));
                Text footer = UIFactory.CreateLabel(footerSection, "FooterText", "discord: dmtftf", TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f), true, 14);
                UIFactory.SetLayoutElement(footer.gameObject, flexibleWidth: 9999, minHeight: 24);

                SyncRuntimeState();
                RebuildItems();
            }

            public void SyncRuntimeState()
            {
                ItemListController owner = OwnerController;
                if (owner == null)
                {
                    return;
                }

                if (_lastBuiltRevision != owner._itemsRevision)
                {
                    RebuildItems();
                }

                if (_itemsLoadingText != null)
                {
                    _itemsLoadingText.text = _cacheReady ? string.Empty : "Loading item list from Core...";
                }

                if (_lastThrusterPowerRevision != ThrusterPowerSystem.Revision)
                {
                    RebuildThrusterPowerControls();
                }

                RefreshBuildingControls();
            }

            public void RebuildItems()
            {
                ItemListController owner = OwnerController;
                if (owner == null || _itemsScrollContent == null)
                {
                    return;
                }

                _lastBuiltRevision = owner._itemsRevision;
                _itemsCountText.text = "Items available: " + _allItems.Count;

                for (int i = _itemsScrollContent.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = _itemsScrollContent.transform.GetChild(i);
                    UnityEngine.Object.Destroy(child.gameObject);
                }

                if (_allItems.Count == 0)
                {
                    Text empty = UIFactory.CreateLabel(_itemsScrollContent, "ItemsEmpty", "No items available.", TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.8f, 0.9f), true, 13);
                    UIFactory.SetLayoutElement(empty.gameObject, minHeight: 24, flexibleWidth: 9999);
                    return;
                }

                for (int i = 0; i < _allItems.Count; i++)
                {
                    ItemEntry entry = _allItems[i];
                    ButtonRef button = UIFactory.CreateButton(_itemsScrollContent, "ItemButton_" + i, entry.Name, new Color(0.2f, 0.22f, 0.26f, 1f));
                    UIFactory.SetLayoutElement(button.GameObject, minHeight: 26, flexibleHeight: 0, flexibleWidth: 9999);

                    ItemEntry selected = entry;
                    button.OnClick = (Action)Delegate.Combine(button.OnClick, (Action)(() => owner.AssignToFirstHotbar(selected)));
                }
            }

            private GameObject CreateSection(GameObject parent, string title)
            {
                GameObject section = UIFactory.CreateVerticalGroup(
                    parent,
                    title + "Section",
                    false,
                    false,
                    true,
                    true,
                    5,
                    new Vector4(10f, 10f, 10f, 10f),
                    new Color(0.14f, 0.14f, 0.15f, 1f));

                if (!string.IsNullOrEmpty(title))
                {
                    Text label = UIFactory.CreateLabel(section, title + "Title", title, TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 1f, 0.98f), true, 15);
                    UIFactory.SetLayoutElement(label.gameObject, minHeight: 22, flexibleWidth: 9999);
                }

                return section;
            }

            private void BuildItemsSection(GameObject section)
            {
                ButtonRef foldoutButton = UIFactory.CreateButton(section, "ItemsFoldout", string.Empty, new Color(0.2f, 0.2f, 0.23f, 1f));
                UIFactory.SetLayoutElement(foldoutButton.GameObject, minHeight: 28, flexibleWidth: 9999);
                _foldoutText = foldoutButton.ButtonText;
                foldoutButton.OnClick = (Action)Delegate.Combine(foldoutButton.OnClick, (Action)delegate
                {
                    _itemsExpanded = !_itemsExpanded;
                    RefreshFoldoutState();
                });

                _itemsCountText = UIFactory.CreateLabel(section, "ItemsCount", "Items available: 0", TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.9f, 0.95f), true, 13);
                UIFactory.SetLayoutElement(_itemsCountText.gameObject, minHeight: 20, flexibleWidth: 9999);

                _itemsLoadingText = UIFactory.CreateLabel(section, "ItemsStatus", string.Empty, TextAnchor.MiddleLeft, new Color(0.7f, 0.75f, 0.8f, 0.9f), true, 12);
                UIFactory.SetLayoutElement(_itemsLoadingText.gameObject, minHeight: 20, flexibleWidth: 9999);

                UniverseLib.UI.Widgets.AutoSliderScrollbar autoScrollbar;
                _itemsScrollView = UIFactory.CreateScrollView(section, "ItemsScrollView", out _itemsScrollContent, out autoScrollbar, new Color(0.11f, 0.12f, 0.14f, 1f));
                UIFactory.SetLayoutElement(_itemsScrollView, minHeight: 300, preferredHeight: 300, flexibleHeight: 0, flexibleWidth: 9999);

                _unlockButton = UIFactory.CreateButton(section, "UnlockAllItems", "Unlock All Items", new Color(0.22f, 0.32f, 0.24f, 1f));
                UIFactory.SetLayoutElement(_unlockButton.GameObject, minHeight: 30, flexibleWidth: 9999);
                _unlockButton.OnClick = (Action)Delegate.Combine(_unlockButton.OnClick, (Action)delegate
                {
                    ItemListController owner = OwnerController;
                    owner?.UnlockAllItems();
                });

                _itemsExpanded = false;
                RefreshFoldoutState();
            }

            private void BuildActionsSection(GameObject section)
            {
                ButtonRef refreshButton = UIFactory.CreateButton(section, "RefreshButton", "Refresh list", new Color(0.24f, 0.28f, 0.33f, 1f));
                UIFactory.SetLayoutElement(refreshButton.GameObject, minHeight: 30, flexibleWidth: 9999);
                refreshButton.OnClick = (Action)Delegate.Combine(refreshButton.OnClick, (Action)delegate
                {
                    ItemListController owner = OwnerController;
                    owner?.TryRefreshItems(force: true);
                });
            }

            private void RefreshFoldoutState()
            {
                _foldoutText.text = _itemsExpanded ? "▼ Items" : "▶ Items";
                _itemsScrollView.SetActive(_itemsExpanded);
            }
        }
    }
}
