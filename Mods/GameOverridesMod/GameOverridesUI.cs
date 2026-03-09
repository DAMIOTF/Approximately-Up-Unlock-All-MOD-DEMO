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
            private Text _shipTearingToggleText;
            private Text _playerGodmodeToggleText;

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
        }
    }
}
