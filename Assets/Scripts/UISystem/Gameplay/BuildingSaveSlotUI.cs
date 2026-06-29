using Simulation.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Simulation.UI
{
    public class BuildingSaveSlotUI : MonoBehaviour
    {
        [SerializeField] private Button saveButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private TMP_InputField slotNameInput;
        [SerializeField] private RawImage thumbnailImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI detailText;

        private BuildingSaveManager _manager;
        private BuildingSaveSummary _summary;
        private bool _hasSave;

        private void Awake()
        {
            AutoAssignReferences();
        }

        public void Bind(BuildingSaveManager manager, BuildingSaveSummary summary, Texture2D thumbnail)
        {
            AutoAssignReferences();

            _manager = manager;
            _summary = summary;
            _hasSave = summary != null && !string.IsNullOrEmpty(summary.id);

            string displayName = _hasSave && !string.IsNullOrEmpty(summary.displayName)
                ? summary.displayName
                : "Empty Slot";

            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (slotNameInput != null)
            {
                slotNameInput.SetTextWithoutNotify(displayName);
            }

            if (detailText != null)
            {
                detailText.text = _hasSave ? $"{summary.pieceCount} pieces" : "No save";
            }

            if (thumbnailImage != null)
            {
                thumbnailImage.texture = thumbnail;
                thumbnailImage.enabled = thumbnail != null;
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(Save);
            }

            if (openButton != null)
            {
                openButton.onClick.RemoveAllListeners();
                openButton.onClick.AddListener(Open);
                openButton.interactable = _hasSave;
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(Delete);
                deleteButton.interactable = true;
            }
        }

        public void BindEmpty(BuildingSaveManager manager)
        {
            Bind(manager, null, null);
        }

        private void AutoAssignReferences()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            if (saveButton == null)
            {
                saveButton = FindButtonByName(buttons, "save");
            }

            if (openButton == null)
            {
                openButton = FindButtonByName(buttons, "load") ?? FindButtonByName(buttons, "open");
            }

            if (deleteButton == null)
            {
                deleteButton = FindButtonByName(buttons, "delete") ?? FindButtonByName(buttons, "remove");
            }

            if (saveButton == null && openButton == null && deleteButton == null)
            {
                openButton = GetComponent<Button>();
            }

            if (saveButton == null && buttons.Length >= 3)
            {
                saveButton = buttons[0];
            }

            if (openButton == null && buttons.Length >= 2)
            {
                openButton = buttons[buttons.Length >= 3 ? 1 : 0];
            }

            if (deleteButton == null && buttons.Length >= 2)
            {
                deleteButton = buttons[buttons.Length >= 3 ? 2 : 1];
            }

            if (slotNameInput == null)
            {
                slotNameInput = GetComponentInChildren<TMP_InputField>(true);
            }

            if (thumbnailImage == null)
            {
                thumbnailImage = GetComponentInChildren<RawImage>(true);
            }

            if (nameText == null || detailText == null)
            {
                TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
                if (nameText == null && texts.Length > 0) nameText = texts[0];
                if (detailText == null && texts.Length > 1) detailText = texts[1];
            }
        }

        private Button FindButtonByName(Button[] buttons, string key)
        {
            foreach (Button button in buttons)
            {
                if (button == null) continue;
                if (button.name.ToLowerInvariant().Contains(key)) return button;
            }

            return null;
        }

        private string GetSlotName()
        {
            if (slotNameInput != null && !string.IsNullOrWhiteSpace(slotNameInput.text))
            {
                return slotNameInput.text.Trim();
            }

            if (_summary != null && !string.IsNullOrEmpty(_summary.displayName))
            {
                return _summary.displayName;
            }

            return "Building Save";
        }

        public void Save()
        {
            if (_manager == null) return;

            string id = _hasSave && _summary != null ? _summary.id : null;
            _manager.SaveToSlot(this, id, GetSlotName());
        }

        public void Open()
        {
            if (_manager != null && _hasSave && _summary != null)
            {
                _manager.OpenSave(_summary.id);
            }
        }

        public void Delete()
        {
            if (_manager != null)
            {
                _manager.DeleteSlot(this, _hasSave && _summary != null ? _summary.id : null);
            }
        }
    }
}
