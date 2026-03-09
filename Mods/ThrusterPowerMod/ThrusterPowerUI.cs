using System;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace ApproximatelyUpMod
{
    public partial class ItemListController
    {
        private sealed partial class FlatModPanel
        {
            private Text _thrusterPowerFoldoutText;
            private GameObject _thrusterPowerScrollView;
            private GameObject _thrusterPowerScrollContent;
            private bool _thrusterPowerExpanded;
            private int _lastThrusterPowerRevision = -1;

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

            internal void RebuildThrusterPowerControls()
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
