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
            private Text _teleportStationsFoldoutText;
            private GameObject _teleportStationsScrollView;
            private GameObject _teleportStationsScrollContent;
            private bool _teleportStationsExpanded;

            private Text _teleportPlanetsFoldoutText;
            private GameObject _teleportPlanetsScrollView;
            private GameObject _teleportPlanetsScrollContent;
            private bool _teleportPlanetsExpanded;

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
        }
    }
}
