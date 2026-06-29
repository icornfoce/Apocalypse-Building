using Simulation.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Simulation.UI
{
    public class BuildingSaveSlotUI : MonoBehaviour
    {
        [SerializeField] private Button openButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private RawImage thumbnailImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI detailText;

        private BuildingSaveManager _manager;
        private BuildingSaveSummary _summary;

        private void Awake()
        {
            AutoAssignReferences();
        }

        public void Bind(BuildingSaveManager manager, BuildingSaveSummary summary, Texture2D thumbnail)
        {
            AutoAssignReferences();

            _manager = manager;
            _summary = summary;

            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(summary.displayName) ? "Untitled Save" : summary.displayName;
            }

            if (detailText != null)
            {
                detailText.text = $"{summary.pieceCount} pieces";
            }

            if (thumbnailImage != null)
            {
                thumbnailImage.texture = thumbnail;
                thumbnailImage.enabled = thumbnail != null;
            }

            if (openButton != null)
            {
                openButton.onClick.RemoveAllListeners();
                openButton.onClick.AddListener(Open);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(Delete);
            }
        }

        private void AutoAssignReferences()
        {
            if (openButton == null)
            {
                openButton = GetComponent<Button>();
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

        private void Open()
        {
            if (_manager != null && _summary != null)
            {
                _manager.OpenSave(_summary.id);
            }
        }

        private void Delete()
        {
            if (_manager != null && _summary != null)
            {
                _manager.DeleteSave(_summary.id);
            }
        }
    }
}
