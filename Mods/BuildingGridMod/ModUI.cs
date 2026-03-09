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
            private Text _buildingGridValueText;
            private Text _buildingCollisionToggleText;
            private ButtonRef _buildingCollisionToggleButton;
            private ButtonRef _buildingResetButton;

            private void BuildBuildingSection(GameObject section)
            {
                Text title = UIFactory.CreateLabel(section, "BuildingTitle", "Building", TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 1f, 0.98f), true, 15);
                UIFactory.SetLayoutElement(title.gameObject, minHeight: 22, flexibleWidth: 9999);

                GameObject gridRow = UIFactory.CreateUIObject("BuildingGridRow", section);
                UIFactory.SetLayoutElement(gridRow, minHeight: 32, flexibleWidth: 9999);

                HorizontalLayoutGroup gridLayout = gridRow.AddComponent<HorizontalLayoutGroup>();
                gridLayout.spacing = 6f;
                gridLayout.childControlWidth = true;
                gridLayout.childControlHeight = true;
                gridLayout.childForceExpandWidth = false;
                gridLayout.childForceExpandHeight = false;
                gridLayout.childAlignment = TextAnchor.MiddleLeft;

                Text gridLabel = UIFactory.CreateLabel(gridRow, "BuildingGridLabel", "Grid Size", TextAnchor.MiddleLeft, new Color(0.86f, 0.9f, 0.95f, 1f), true, 13);
                UIFactory.SetLayoutElement(gridLabel.gameObject, minWidth: 85, preferredWidth: 85, minHeight: 28, preferredHeight: 28);

                ButtonRef minusButton = UIFactory.CreateButton(gridRow, "BuildingGridMinus", "-", new Color(0.24f, 0.27f, 0.32f, 1f));
                UIFactory.SetLayoutElement(minusButton.GameObject, minWidth: 28, preferredWidth: 28, minHeight: 26, preferredHeight: 26);

                _buildingGridValueText = UIFactory.CreateLabel(gridRow, "BuildingGridValue", string.Empty, TextAnchor.MiddleCenter, new Color(0.93f, 0.98f, 0.93f, 1f), true, 13);
                UIFactory.SetLayoutElement(_buildingGridValueText.gameObject, minWidth: 85, preferredWidth: 85, minHeight: 26, preferredHeight: 26);

                ButtonRef plusButton = UIFactory.CreateButton(gridRow, "BuildingGridPlus", "+", new Color(0.24f, 0.27f, 0.32f, 1f));
                UIFactory.SetLayoutElement(plusButton.GameObject, minWidth: 28, preferredWidth: 28, minHeight: 26, preferredHeight: 26);

                minusButton.OnClick = (Action)Delegate.Combine(minusButton.OnClick, (Action)delegate
                {
                    BuildingModConfig.StepGridSize(-1);
                    RefreshBuildingControls();
                });

                plusButton.OnClick = (Action)Delegate.Combine(plusButton.OnClick, (Action)delegate
                {
                    BuildingModConfig.StepGridSize(1);
                    RefreshBuildingControls();
                });

                _buildingCollisionToggleButton = UIFactory.CreateButton(section, "BuildingCollisionToggle", string.Empty, new Color(0.18f, 0.2f, 0.32f, 1f));
                UIFactory.SetLayoutElement(_buildingCollisionToggleButton.GameObject, minHeight: 30, flexibleWidth: 9999);
                _buildingCollisionToggleText = _buildingCollisionToggleButton.ButtonText;
                _buildingCollisionToggleButton.OnClick = (Action)Delegate.Combine(_buildingCollisionToggleButton.OnClick, (Action)delegate
                {
                    BuildingModConfig.DisablePlacementCollisions = !BuildingModConfig.DisablePlacementCollisions;
                    RefreshBuildingControls();
                });

                _buildingResetButton = UIFactory.CreateButton(section, "BuildingReset", "[ Reset to Default ]", new Color(0.28f, 0.18f, 0.18f, 1f));
                UIFactory.SetLayoutElement(_buildingResetButton.GameObject, minHeight: 28, flexibleWidth: 9999);
                _buildingResetButton.OnClick = (Action)Delegate.Combine(_buildingResetButton.OnClick, (Action)delegate
                {
                    BuildingModConfig.Reset();
                    BuildingRuntimeOverrides.ForceReapply();
                    RefreshBuildingControls();
                });

                Text hint = UIFactory.CreateLabel(
                    section,
                    "BuildingHint",
                    "Grid snapping & collision bypass are applied instantly. Reset restores game defaults (0.25 grid, collisions on).",
                    TextAnchor.MiddleLeft,
                    new Color(0.75f, 0.8f, 0.86f, 0.9f),
                    true,
                    12);
                UIFactory.SetLayoutElement(hint.gameObject, minHeight: 20, flexibleWidth: 9999);

                RefreshBuildingControls();
            }

            private void RefreshBuildingControls()
            {
                if (_buildingGridValueText != null)
                {
                    _buildingGridValueText.text = BuildingModConfig.GridSizeLabel();
                }

                if (_buildingCollisionToggleText != null)
                {
                    _buildingCollisionToggleText.text = BuildingModConfig.DisablePlacementCollisions
                        ? "[x] Disable Placing Collisions"
                        : "[ ] Disable Placing Collisions";
                }

                if (_buildingCollisionToggleButton != null)
                {
                    Image image = _buildingCollisionToggleButton.GameObject.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = BuildingModConfig.DisablePlacementCollisions
                            ? new Color(0.2f, 0.36f, 0.52f, 1f)
                            : new Color(0.18f, 0.2f, 0.32f, 1f);
                    }
                }
            }
        }
    }
}
