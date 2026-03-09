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
            private InputFieldRef _wirelessChannelsInput;
            private Text _wirelessChannelsStatusText;

            private InputFieldRef[] _cablePowerInputs;
            private Text[] _cablePowerDefaultTexts;
            private int _lastCableDiscoveryRevision = -1;

            private void BuildElectricsSection(GameObject section)
            {
                // ── Wireless Transmitter ─────────────────────────────────────
                Text wirelessTitle = UIFactory.CreateLabel(section, "WirelessTitle", "Wireless Transmitter", TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 1f, 0.98f), true, 15);
                UIFactory.SetLayoutElement(wirelessTitle.gameObject, minHeight: 22, flexibleWidth: 9999);

                GameObject wirelessRow = UIFactory.CreateUIObject("WirelessChannelsRow", section);
                UIFactory.SetLayoutElement(wirelessRow, minHeight: 32, flexibleWidth: 9999);

                HorizontalLayoutGroup wirelessLayout = wirelessRow.AddComponent<HorizontalLayoutGroup>();
                wirelessLayout.spacing = 6f;
                wirelessLayout.childControlWidth = true;
                wirelessLayout.childControlHeight = true;
                wirelessLayout.childForceExpandWidth = false;
                wirelessLayout.childForceExpandHeight = false;
                wirelessLayout.childAlignment = TextAnchor.MiddleLeft;

                Text wirelessLabel = UIFactory.CreateLabel(wirelessRow, "WirelessChannelsLabel", "Max channels:", TextAnchor.MiddleLeft, new Color(0.86f, 0.9f, 0.95f, 1f), true, 13);
                UIFactory.SetLayoutElement(wirelessLabel.gameObject, minWidth: 100, preferredWidth: 100, minHeight: 28, preferredHeight: 28);

                _wirelessChannelsInput = CreateNumberInput(wirelessRow, "WirelessChannelsInput", WirelessTransmitterSystem.DesiredMaxChannels.ToString(), 4);

                ButtonRef wirelessApplyButton = UIFactory.CreateButton(wirelessRow, "WirelessChannelsApply", "Apply", new Color(0.2f, 0.26f, 0.18f, 1f));
                UIFactory.SetLayoutElement(wirelessApplyButton.GameObject, minWidth: 60, preferredWidth: 60, minHeight: 28, preferredHeight: 28);
                wirelessApplyButton.OnClick = (Action)Delegate.Combine(wirelessApplyButton.OnClick, (Action)delegate
                {
                    int value;
                    if (_wirelessChannelsInput != null && int.TryParse(_wirelessChannelsInput.Component.text.Trim(), out value))
                    {
                        WirelessTransmitterSystem.SetMaxChannels(value);
                        SyncWirelessInputToCurrentValue();
                        RefreshWirelessChannelsDisplay();
                    }
                });

                ButtonRef wirelessResetButton = UIFactory.CreateButton(wirelessRow, "WirelessChannelsReset", "Reset", new Color(0.28f, 0.18f, 0.18f, 1f));
                UIFactory.SetLayoutElement(wirelessResetButton.GameObject, minWidth: 55, preferredWidth: 55, minHeight: 28, preferredHeight: 28);
                wirelessResetButton.OnClick = (Action)Delegate.Combine(wirelessResetButton.OnClick, (Action)delegate
                {
                    WirelessTransmitterSystem.Reset();
                    SyncWirelessInputToCurrentValue();
                    RefreshWirelessChannelsDisplay();
                });

                _wirelessChannelsStatusText = UIFactory.CreateLabel(section, "WirelessChannelsStatus",
                    $"Current: {WirelessTransmitterSystem.DesiredMaxChannels} channels  (default: {WirelessTransmitterSystem.DefaultMaxChannels})",
                    TextAnchor.MiddleLeft, new Color(0.75f, 0.8f, 0.86f, 0.9f), true, 12);
                UIFactory.SetLayoutElement(_wirelessChannelsStatusText.gameObject, minHeight: 18, flexibleWidth: 9999);

                // Separator
                GameObject sep = UIFactory.CreateUIObject("ElectricsSep", section);
                UIFactory.SetLayoutElement(sep, minHeight: 6, flexibleWidth: 9999);

                // ── Cable Max Power ──────────────────────────────────────────
                Text cableTitle = UIFactory.CreateLabel(section, "CablePowerTitle", "Cable Max Power (P/s)", TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 1f, 0.98f), true, 15);
                UIFactory.SetLayoutElement(cableTitle.gameObject, minHeight: 22, flexibleWidth: 9999);

                CablePowerSystem.CableTypeEntry[] entries = CablePowerSystem.Entries;
                _cablePowerInputs = new InputFieldRef[entries.Length];
                _cablePowerDefaultTexts = new Text[entries.Length];

                for (int idx = 0; idx < entries.Length; idx++)
                {
                    CablePowerSystem.CableTypeEntry entry = entries[idx];
                    int capturedIdx = idx;

                    GameObject row = UIFactory.CreateUIObject("CablePowerRow_" + idx, section);
                    UIFactory.SetLayoutElement(row, minHeight: 32, flexibleWidth: 9999);

                    HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                    rowLayout.spacing = 6f;
                    rowLayout.childControlWidth = true;
                    rowLayout.childControlHeight = true;
                    rowLayout.childForceExpandWidth = false;
                    rowLayout.childForceExpandHeight = false;
                    rowLayout.childAlignment = TextAnchor.MiddleLeft;

                    Text rowLabel = UIFactory.CreateLabel(row, "CablePowerLabel_" + idx, entry.Label + ":", TextAnchor.MiddleLeft, new Color(0.86f, 0.9f, 0.95f, 1f), true, 13);
                    UIFactory.SetLayoutElement(rowLabel.gameObject, minWidth: 130, preferredWidth: 130, minHeight: 28, preferredHeight: 28);

                    string inputDefault = entry.OverrideMaxPower > 0f
                        ? Mathf.RoundToInt(entry.OverrideMaxPower).ToString()
                        : (entry.OriginalDiscovered ? Mathf.RoundToInt(entry.OriginalMaxPower).ToString() : "");
                    _cablePowerInputs[capturedIdx] = CreateNumberInput(row, "CablePowerInput_" + idx, inputDefault, 7);

                    ButtonRef applyButton = UIFactory.CreateButton(row, "CablePowerApply_" + idx, "Apply", new Color(0.2f, 0.26f, 0.18f, 1f));
                    UIFactory.SetLayoutElement(applyButton.GameObject, minWidth: 60, preferredWidth: 60, minHeight: 28, preferredHeight: 28);
                    applyButton.OnClick = (Action)Delegate.Combine(applyButton.OnClick, (Action)delegate
                    {
                        InputFieldRef input = _cablePowerInputs[capturedIdx];
                        float value;
                        if (input != null && float.TryParse(input.Component.text.Trim(), out value) && value > 0f)
                        {
                            CablePowerSystem.Entries[capturedIdx].OverrideMaxPower = value;
                            CablePowerSystem.ForceRescan();
                            RefreshCablePowerDisplay();
                        }
                    });

                    ButtonRef resetButton = UIFactory.CreateButton(row, "CablePowerReset_" + idx, "Reset", new Color(0.28f, 0.18f, 0.18f, 1f));
                    UIFactory.SetLayoutElement(resetButton.GameObject, minWidth: 55, preferredWidth: 55, minHeight: 28, preferredHeight: 28);
                    resetButton.OnClick = (Action)Delegate.Combine(resetButton.OnClick, (Action)delegate
                    {
                        CablePowerSystem.Entries[capturedIdx].OverrideMaxPower = 0f;
                        CablePowerSystem.ForceRescan();
                        RefreshCablePowerDisplay();
                    });

                    string defaultLabel = entry.OriginalDiscovered
                        ? $"default: {Mathf.RoundToInt(entry.OriginalMaxPower)} P/s"
                        : "default: not yet discovered (place cables first)";
                    _cablePowerDefaultTexts[capturedIdx] = UIFactory.CreateLabel(section, "CablePowerDefault_" + idx, defaultLabel,
                        TextAnchor.MiddleLeft, new Color(0.7f, 0.75f, 0.8f, 0.85f), true, 11);
                    UIFactory.SetLayoutElement(_cablePowerDefaultTexts[capturedIdx].gameObject, minHeight: 16, flexibleWidth: 9999);
                }

                Text hint = UIFactory.CreateLabel(section, "ElectricsHint",
                    "Cable changes apply to all placed cables instantly. New cables also get the override within ~0.4s.",
                    TextAnchor.MiddleLeft, new Color(0.75f, 0.8f, 0.86f, 0.9f), true, 12);
                UIFactory.SetLayoutElement(hint.gameObject, minHeight: 20, flexibleWidth: 9999);
            }

            private InputFieldRef CreateNumberInput(GameObject parent, string name, string defaultText, int maxLen)
            {
                InputFieldRef field = UIFactory.CreateInputField(parent, name, "number...");
                UIFactory.SetLayoutElement(field.GameObject, minWidth: 90, preferredWidth: 90, minHeight: 28, preferredHeight: 28);
                field.Component.characterLimit = maxLen;
                field.Component.contentType = InputField.ContentType.DecimalNumber;
                field.Component.lineType = InputField.LineType.SingleLine;
                if (!string.IsNullOrEmpty(defaultText))
                {
                    field.Component.text = defaultText;
                }
                return field;
            }

            internal void RefreshElectricsControls()
            {
                RefreshWirelessChannelsDisplay();

                // Refresh cable defaults if new cables were discovered
                if (_lastCableDiscoveryRevision != CablePowerSystem.DiscoveryRevision)
                {
                    _lastCableDiscoveryRevision = CablePowerSystem.DiscoveryRevision;
                    RefreshCablePowerDisplay();
                }
            }

            private void RefreshWirelessChannelsDisplay()
            {
                if (_wirelessChannelsStatusText != null)
                {
                    _wirelessChannelsStatusText.text =
                        $"Current: {WirelessTransmitterSystem.DesiredMaxChannels} channels  (default: {WirelessTransmitterSystem.DefaultMaxChannels})";
                }
            }

            private void SyncWirelessInputToCurrentValue()
            {
                if (_wirelessChannelsInput != null)
                {
                    _wirelessChannelsInput.Component.text = WirelessTransmitterSystem.DesiredMaxChannels.ToString();
                }
            }

            private void RefreshCablePowerDisplay()
            {
                CablePowerSystem.CableTypeEntry[] entries = CablePowerSystem.Entries;
                if (_cablePowerDefaultTexts == null || _cablePowerInputs == null)
                {
                    return;
                }

                for (int i = 0; i < entries.Length; i++)
                {
                    CablePowerSystem.CableTypeEntry entry = entries[i];

                    if (_cablePowerDefaultTexts[i] != null)
                    {
                        _cablePowerDefaultTexts[i].text = entry.OriginalDiscovered
                            ? $"default: {Mathf.RoundToInt(entry.OriginalMaxPower)} P/s"
                            : "default: not yet discovered (place cables first)";
                    }
                }
            }
        }
    }
}
