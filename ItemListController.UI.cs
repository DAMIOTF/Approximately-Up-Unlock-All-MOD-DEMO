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

        private sealed class FlatModPanel : PanelBase
        {
            private ItemListController _owner;

            private InputField _materialsAmountInput;
            private Text _foldoutText;
            private Text _shipTearingToggleText;
            private Text _playerGodmodeToggleText;
            private Text _itemsCountText;
            private Text _itemsLoadingText;
            private GameObject _itemsScrollView;
            private GameObject _itemsScrollContent;
            private ButtonRef _unlockButton;
            private Text _teleportStationsFoldoutText;
            private GameObject _teleportStationsScrollView;
            private GameObject _teleportStationsScrollContent;
            private bool _teleportStationsExpanded;

            private Text _teleportPlanetsFoldoutText;
            private GameObject _teleportPlanetsScrollView;
            private GameObject _teleportPlanetsScrollContent;
            private bool _teleportPlanetsExpanded;

            private Text _thrusterPowerFoldoutText;
            private GameObject _thrusterPowerScrollView;
            private GameObject _thrusterPowerScrollContent;
            private bool _thrusterPowerExpanded;
            private int _lastThrusterPowerRevision = -1;

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

            private void BuildMaterialsControls(GameObject section)
            {
                GameObject amountRow = UIFactory.CreateUIObject("SetMaterialsRow", section);
                UIFactory.SetLayoutElement(amountRow, minHeight: 32, flexibleWidth: 9999);

                HorizontalLayoutGroup rowLayout = amountRow.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 6f;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;

                Text amountLabel = UIFactory.CreateLabel(amountRow, "SetMaterialsAmountLabel", "Amount:", TextAnchor.MiddleLeft, new Color(0.86f, 0.9f, 0.95f, 1f), true, 13);
                UIFactory.SetLayoutElement(amountLabel.gameObject, minWidth: 62, preferredWidth: 62, minHeight: 30, preferredHeight: 30);

                GameObject inputRoot = UIFactory.CreateUIObject("SetMaterialsInput", amountRow);
                Image inputBackground = inputRoot.AddComponent<Image>();
                inputBackground.color = new Color(0.08f, 0.09f, 0.11f, 1f);
                UIFactory.SetLayoutElement(inputRoot, minWidth: 140, preferredWidth: 140, minHeight: 30, preferredHeight: 30);

                Text inputText = UIFactory.CreateLabel(inputRoot, "SetMaterialsInputText", DefaultMaterialsAmount.ToString(), TextAnchor.MiddleCenter, new Color(0.92f, 0.96f, 1f, 1f), true, 14);
                inputText.raycastTarget = false;
                RectTransform inputTextRect = inputText.rectTransform;
                inputTextRect.anchorMin = Vector2.zero;
                inputTextRect.anchorMax = Vector2.one;
                inputTextRect.offsetMin = new Vector2(8f, 3f);
                inputTextRect.offsetMax = new Vector2(-8f, -3f);

                Text placeholder = UIFactory.CreateLabel(inputRoot, "SetMaterialsPlaceholder", "1-99999", TextAnchor.MiddleCenter, new Color(0.6f, 0.66f, 0.73f, 0.75f), true, 13);
                placeholder.raycastTarget = false;
                RectTransform placeholderRect = placeholder.rectTransform;
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.offsetMin = new Vector2(8f, 3f);
                placeholderRect.offsetMax = new Vector2(-8f, -3f);

                _materialsAmountInput = inputRoot.AddComponent<InputField>();
                _materialsAmountInput.targetGraphic = inputBackground;
                _materialsAmountInput.textComponent = inputText;
                _materialsAmountInput.placeholder = placeholder;
                _materialsAmountInput.characterLimit = 5;
                _materialsAmountInput.contentType = InputField.ContentType.IntegerNumber;
                _materialsAmountInput.lineType = InputField.LineType.SingleLine;
                _materialsAmountInput.text = DefaultMaterialsAmount.ToString();

                ButtonRef setButton = UIFactory.CreateButton(amountRow, "SetMaterialsButton", "Set amount", new Color(0.2f, 0.26f, 0.18f, 1f));
                UIFactory.SetLayoutElement(setButton.GameObject, minWidth: 180, preferredWidth: 180, minHeight: 30, preferredHeight: 30);
                setButton.OnClick = (Action)Delegate.Combine(setButton.OnClick, (Action)delegate
                {
                    ItemListController owner = OwnerController;
                    if (owner == null)
                    {
                        return;
                    }

                    owner.ApplyMaterialsAmountFromUi(_materialsAmountInput != null ? _materialsAmountInput.text : null);
                });

                Text hint = UIFactory.CreateLabel(section, "SetMaterialsHint", "Set all parts to value (max 99999)", TextAnchor.MiddleLeft, new Color(0.75f, 0.8f, 0.86f, 0.9f), true, 12);
                UIFactory.SetLayoutElement(hint.gameObject, minHeight: 20, flexibleWidth: 9999);
            }

            private void BuildShipSafetyControls(GameObject section)
            {
                ButtonRef shipTearingButton = UIFactory.CreateButton(section, "ShipTearingToggle", string.Empty, new Color(0.26f, 0.18f, 0.18f, 1f));
                UIFactory.SetLayoutElement(shipTearingButton.GameObject, minHeight: 30, flexibleWidth: 9999);
                _shipTearingToggleText = shipTearingButton.ButtonText;

                shipTearingButton.OnClick = (Action)Delegate.Combine(shipTearingButton.OnClick, (Action)delegate
                {
                    DisableShipTearingBySpeed = !DisableShipTearingBySpeed;

                    ItemListController owner = OwnerController;
                    owner?.SyncShipTearingOverride();
                    RefreshShipSafetyControls(shipTearingButton, null);
                });

                ButtonRef godmodeButton = UIFactory.CreateButton(section, "PlayerGodmodeToggle", string.Empty, new Color(0.18f, 0.2f, 0.32f, 1f));
                UIFactory.SetLayoutElement(godmodeButton.GameObject, minHeight: 30, flexibleWidth: 9999);
                _playerGodmodeToggleText = godmodeButton.ButtonText;

                godmodeButton.OnClick = (Action)Delegate.Combine(godmodeButton.OnClick, (Action)delegate
                {
                    EnablePlayerGodmode = !EnablePlayerGodmode;

                    ItemListController owner = OwnerController;
                    owner?.SyncShipTearingOverride();
                    RefreshShipSafetyControls(null, godmodeButton);
                });

                RefreshShipSafetyControls(shipTearingButton, godmodeButton);
            }

            private void RefreshShipSafetyControls(ButtonRef shipTearingButton, ButtonRef godmodeButton)
            {
                if (_shipTearingToggleText != null)
                {
                    _shipTearingToggleText.text = DisableShipTearingBySpeed
                        ? "[x] Disable ship tearing by speed"
                        : "[ ] Disable ship tearing by speed";
                }

                if (_playerGodmodeToggleText != null)
                {
                    _playerGodmodeToggleText.text = EnablePlayerGodmode
                        ? "[x] Godmode (prevent player death)"
                        : "[ ] Godmode (prevent player death)";
                }

                if (shipTearingButton != null)
                {
                    Image image = shipTearingButton.GameObject.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = DisableShipTearingBySpeed
                            ? new Color(0.17f, 0.34f, 0.2f, 1f)
                            : new Color(0.26f, 0.18f, 0.18f, 1f);
                    }
                }

                if (godmodeButton != null)
                {
                    Image image = godmodeButton.GameObject.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = EnablePlayerGodmode
                            ? new Color(0.2f, 0.36f, 0.52f, 1f)
                            : new Color(0.18f, 0.2f, 0.32f, 1f);
                    }
                }
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

            private void BuildThrusterPowerSection(GameObject section)
            {
                ButtonRef foldoutButton = UIFactory.CreateButton(section, "ThrusterPowerFoldout", string.Empty, new Color(0.2f, 0.2f, 0.23f, 1f));
                UIFactory.SetLayoutElement(foldoutButton.GameObject, minHeight: 28, flexibleWidth: 9999);
                _thrusterPowerFoldoutText = foldoutButton.ButtonText;
                foldoutButton.OnClick = (Action)Delegate.Combine(foldoutButton.OnClick, (Action)delegate
                {
                    _thrusterPowerExpanded = !_thrusterPowerExpanded;
                    RefreshThrusterPowerFoldoutState();
                });

                UniverseLib.UI.Widgets.AutoSliderScrollbar autoScrollbar;
                _thrusterPowerScrollView = UIFactory.CreateScrollView(section, "ThrusterPowerScrollView", out _thrusterPowerScrollContent, out autoScrollbar, new Color(0.11f, 0.12f, 0.14f, 1f));
                UIFactory.SetLayoutElement(_thrusterPowerScrollView, minHeight: 190, preferredHeight: 190, flexibleHeight: 0, flexibleWidth: 9999);

                Text hint = UIFactory.CreateLabel(
                    section,
                    "ThrusterPowerHint",
                    "Use - and + to change engine force multiplier (range: 1x-10x).",
                    TextAnchor.MiddleLeft,
                    new Color(0.75f, 0.8f, 0.86f, 0.9f),
                    true,
                    12);
                UIFactory.SetLayoutElement(hint.gameObject, minHeight: 20, flexibleWidth: 9999);

                _thrusterPowerExpanded = false;
                RefreshThrusterPowerFoldoutState();
                RebuildThrusterPowerControls();
            }

            private void RebuildThrusterPowerControls()
            {
                if (_thrusterPowerScrollContent == null)
                {
                    return;
                }

                _lastThrusterPowerRevision = ThrusterPowerSystem.Revision;

                for (int i = _thrusterPowerScrollContent.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = _thrusterPowerScrollContent.transform.GetChild(i);
                    UnityEngine.Object.Destroy(child.gameObject);
                }

                var entries = ThrusterPowerSystem.GetUiEntries();
                if (entries.Count == 0)
                {
                    Text empty = UIFactory.CreateLabel(
                        _thrusterPowerScrollContent,
                        "ThrusterPowerEmpty",
                        "No active thrusters detected yet. Enter spaceship/build mode first.",
                        TextAnchor.MiddleLeft,
                        new Color(0.8f, 0.8f, 0.8f, 0.9f),
                        true,
                        13);
                    UIFactory.SetLayoutElement(empty.gameObject, minHeight: 26, flexibleWidth: 9999);
                    return;
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    ThrusterPowerSystem.UiEntry entry = entries[i];
                    CreateThrusterPowerRow(i, entry);
                }
            }

            private void CreateThrusterPowerRow(int index, ThrusterPowerSystem.UiEntry entry)
            {
                GameObject row = UIFactory.CreateUIObject("ThrusterPowerRow_" + index, _thrusterPowerScrollContent);
                UIFactory.SetLayoutElement(row, minHeight: 62, flexibleWidth: 9999);

                VerticalLayoutGroup rowVertical = row.AddComponent<VerticalLayoutGroup>();
                rowVertical.spacing = 4f;
                rowVertical.padding = new RectOffset(2, 2, 2, 2);
                rowVertical.childAlignment = TextAnchor.MiddleLeft;
                rowVertical.childControlHeight = true;
                rowVertical.childControlWidth = true;
                rowVertical.childForceExpandHeight = false;
                rowVertical.childForceExpandWidth = true;

                Text nameLabel = UIFactory.CreateLabel(
                    row,
                    "ThrusterName_" + index,
                    entry.Name,
                    TextAnchor.MiddleLeft,
                    new Color(0.92f, 0.96f, 1f, 0.98f),
                    true,
                    13);
                UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 20, flexibleWidth: 9999);

                GameObject controls = UIFactory.CreateUIObject("ThrusterControls_" + index, row);
                UIFactory.SetLayoutElement(controls, minHeight: 30, flexibleWidth: 9999);

                HorizontalLayoutGroup controlsLayout = controls.AddComponent<HorizontalLayoutGroup>();
                controlsLayout.spacing = 6f;
                controlsLayout.childAlignment = TextAnchor.MiddleLeft;
                controlsLayout.childControlHeight = true;
                controlsLayout.childControlWidth = true;
                controlsLayout.childForceExpandWidth = true;
                controlsLayout.childForceExpandHeight = false;

                ButtonRef minusButton = UIFactory.CreateButton(controls, "ThrusterMinus_" + index, "-", new Color(0.24f, 0.27f, 0.32f, 1f));
                UIFactory.SetLayoutElement(minusButton.GameObject, minWidth: 28, preferredWidth: 28, minHeight: 24, preferredHeight: 24);

                ButtonRef plusButton = UIFactory.CreateButton(controls, "ThrusterPlus_" + index, "+", new Color(0.24f, 0.27f, 0.32f, 1f));
                UIFactory.SetLayoutElement(plusButton.GameObject, minWidth: 28, preferredWidth: 28, minHeight: 24, preferredHeight: 24);

                Text valueLabel = UIFactory.CreateLabel(
                    controls,
                    "ThrusterValue_" + index,
                    string.Empty,
                    TextAnchor.MiddleCenter,
                    new Color(0.93f, 0.98f, 0.93f, 1f),
                    true,
                    13);
                UIFactory.SetLayoutElement(valueLabel.gameObject, minWidth: 120, preferredWidth: 120, minHeight: 24, preferredHeight: 24, flexibleWidth: 9999);

                ulong prefabHash = entry.PrefabHash;
                int currentMultiplier = Mathf.Clamp(Mathf.RoundToInt(entry.Multiplier), 1, 10);

                Action syncValue = delegate
                {
                    valueLabel.text = "Power: x" + currentMultiplier;
                    ThrusterPowerSystem.SetMultiplier(prefabHash, currentMultiplier);
                };

                syncValue();

                minusButton.OnClick = (Action)Delegate.Combine(minusButton.OnClick, (Action)delegate
                {
                    currentMultiplier = Mathf.Max(1, currentMultiplier - 1);
                    syncValue();
                });

                plusButton.OnClick = (Action)Delegate.Combine(plusButton.OnClick, (Action)delegate
                {
                    currentMultiplier = Mathf.Min(10, currentMultiplier + 1);
                    syncValue();
                });
            }

            private void BuildTeleportStationsSection(GameObject section)
            {
                ButtonRef foldoutButton = UIFactory.CreateButton(section, "TeleportStationsFoldout", string.Empty, new Color(0.2f, 0.2f, 0.23f, 1f));
                UIFactory.SetLayoutElement(foldoutButton.GameObject, minHeight: 28, flexibleWidth: 9999);
                _teleportStationsFoldoutText = foldoutButton.ButtonText;
                foldoutButton.OnClick = (Action)Delegate.Combine(foldoutButton.OnClick, (Action)delegate
                {
                    _teleportStationsExpanded = !_teleportStationsExpanded;
                    RefreshTeleportStationsFoldoutState();
                });

                UniverseLib.UI.Widgets.AutoSliderScrollbar autoScrollbar;
                _teleportStationsScrollView = UIFactory.CreateScrollView(section, "TeleportStationsScrollView", out _teleportStationsScrollContent, out autoScrollbar, new Color(0.11f, 0.12f, 0.14f, 1f));
                UIFactory.SetLayoutElement(_teleportStationsScrollView, minHeight: 170, preferredHeight: 170, flexibleHeight: 0, flexibleWidth: 9999);

                ItemListController owner = OwnerController;
                if (owner != null)
                {
                    for (int i = 0; i < _teleportStationIds.Length; i++)
                    {
                        UniverseLocationID target = _teleportStationIds[i];
                        bool unavailableInDemo = _demoUnavailableStationTargets.Contains(target);
                        string buttonText = target.ToString();
                        Color buttonColor = unavailableInDemo
                            ? new Color(0.12f, 0.12f, 0.13f, 1f)
                            : new Color(0.22f, 0.24f, 0.28f, 1f);

                        ButtonRef button = UIFactory.CreateButton(_teleportStationsScrollContent, "TeleportStationButton_" + target, buttonText, buttonColor);
                        UIFactory.SetLayoutElement(button.GameObject, minHeight: 26, flexibleWidth: 9999);

                        if (unavailableInDemo)
                        {
                            Button uiButton = button.GameObject.GetComponent<Button>();
                            if (uiButton != null)
                            {
                                uiButton.interactable = false;
                            }

                            Text notAvailableLabel = UIFactory.CreateLabel(
                                _teleportStationsScrollContent,
                                "TeleportStationUnavailable_" + target,
                                "not available in demo :(",
                                TextAnchor.MiddleLeft,
                                new Color(1f, 0.33f, 0.33f, 1f),
                                true,
                                12);
                            UIFactory.SetLayoutElement(notAvailableLabel.gameObject, minHeight: 18, flexibleWidth: 9999);
                        }
                        else
                        {
                            button.OnClick = (Action)Delegate.Combine(button.OnClick, (Action)(() => owner.TeleportToStation(target)));
                        }
                    }
                }

                _teleportStationsExpanded = false;
                RefreshTeleportStationsFoldoutState();
            }

            private void BuildTeleportPlanetsSection(GameObject section)
            {
                ButtonRef foldoutButton = UIFactory.CreateButton(section, "TeleportPlanetsFoldout", string.Empty, new Color(0.2f, 0.2f, 0.23f, 1f));
                UIFactory.SetLayoutElement(foldoutButton.GameObject, minHeight: 28, flexibleWidth: 9999);
                _teleportPlanetsFoldoutText = foldoutButton.ButtonText;
                foldoutButton.OnClick = (Action)Delegate.Combine(foldoutButton.OnClick, (Action)delegate
                {
                    _teleportPlanetsExpanded = !_teleportPlanetsExpanded;
                    RefreshTeleportPlanetsFoldoutState();
                });

                UniverseLib.UI.Widgets.AutoSliderScrollbar autoScrollbar;
                _teleportPlanetsScrollView = UIFactory.CreateScrollView(section, "TeleportPlanetsScrollView", out _teleportPlanetsScrollContent, out autoScrollbar, new Color(0.11f, 0.12f, 0.14f, 1f));
                UIFactory.SetLayoutElement(_teleportPlanetsScrollView, minHeight: 190, preferredHeight: 190, flexibleHeight: 0, flexibleWidth: 9999);

                ItemListController owner = OwnerController;
                if (owner != null)
                {
                    for (int i = 0; i < _teleportPlanetIds.Length; i++)
                    {
                        UniverseLocationID target = _teleportPlanetIds[i];
                        bool unavailableInDemo = _demoUnavailablePlanetTargets.Contains(target);
                        string buttonText = target.ToString();
                        Color buttonColor = unavailableInDemo
                            ? new Color(0.12f, 0.12f, 0.13f, 1f)
                            : new Color(0.22f, 0.24f, 0.28f, 1f);

                        ButtonRef button = UIFactory.CreateButton(_teleportPlanetsScrollContent, "TeleportPlanetButton_" + target, buttonText, buttonColor);
                        UIFactory.SetLayoutElement(button.GameObject, minHeight: 26, flexibleWidth: 9999);

                        if (unavailableInDemo)
                        {
                            Button uiButton = button.GameObject.GetComponent<Button>();
                            if (uiButton != null)
                            {
                                uiButton.interactable = false;
                            }

                            Text notAvailableLabel = UIFactory.CreateLabel(
                                _teleportPlanetsScrollContent,
                                "TeleportPlanetUnavailable_" + target,
                                "not available in demo :(",
                                TextAnchor.MiddleLeft,
                                new Color(1f, 0.33f, 0.33f, 1f),
                                true,
                                12);
                            UIFactory.SetLayoutElement(notAvailableLabel.gameObject, minHeight: 18, flexibleWidth: 9999);
                        }
                        else
                        {
                            button.OnClick = (Action)Delegate.Combine(button.OnClick, (Action)(() => owner.TeleportToPlanet(target)));
                        }
                    }
                }

                _teleportPlanetsExpanded = false;
                RefreshTeleportPlanetsFoldoutState();
            }

            private void RefreshFoldoutState()
            {
                _foldoutText.text = _itemsExpanded ? "▼ Items" : "▶ Items";
                _itemsScrollView.SetActive(_itemsExpanded);
            }

            private void RefreshTeleportStationsFoldoutState()
            {
                if (_teleportStationsFoldoutText != null)
                {
                    _teleportStationsFoldoutText.text = _teleportStationsExpanded ? "▼ Teleport to station" : "▶ Teleport to station";
                }

                if (_teleportStationsScrollView != null)
                {
                    _teleportStationsScrollView.SetActive(_teleportStationsExpanded);
                }
            }

            private void RefreshTeleportPlanetsFoldoutState()
            {
                if (_teleportPlanetsFoldoutText != null)
                {
                    _teleportPlanetsFoldoutText.text = _teleportPlanetsExpanded ? "▼ Teleport to planet" : "▶ Teleport to planet";
                }

                if (_teleportPlanetsScrollView != null)
                {
                    _teleportPlanetsScrollView.SetActive(_teleportPlanetsExpanded);
                }
            }

            private void RefreshThrusterPowerFoldoutState()
            {
                if (_thrusterPowerFoldoutText != null)
                {
                    _thrusterPowerFoldoutText.text = _thrusterPowerExpanded ? "▼ Thruster power" : "▶ Thruster power";
                }

                if (_thrusterPowerScrollView != null)
                {
                    _thrusterPowerScrollView.SetActive(_thrusterPowerExpanded);
                }
            }
        }
    }
}
