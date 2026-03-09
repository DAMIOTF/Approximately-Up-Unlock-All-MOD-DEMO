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
            private InputField _materialsAmountInput;

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
        }
    }
}
