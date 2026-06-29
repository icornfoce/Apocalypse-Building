using System.Collections.Generic;
using Simulation.Building;
using Simulation.Data;
using Simulation.Physics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Simulation.UI
{
    public class BuildingSaveManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildingSystem buildingSystem;
        [SerializeField] private UnityEngine.Camera thumbnailCamera;

        [Header("Load Data")]
        [SerializeField] private StructureData[] structures;
        [SerializeField] private MaterialData[] materials;
        [SerializeField] private GadgetData[] gadgets;

        [Header("UI")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Transform slotParent;
        [SerializeField] private BuildingSaveSlotUI slotPrefab;
        [SerializeField] private RawImage selectedPreviewImage;
        [SerializeField] private TextMeshProUGUI selectedNameText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Thumbnail")]
        [SerializeField] private int thumbnailSize = 512;

        private readonly List<GameObject> _spawnedSlots = new List<GameObject>();
        private string _selectedSaveId;

        private void Start()
        {
            RefreshSaveList();
        }

        public void SaveCurrentBuilding()
        {
            BuildingSystem bs = GetBuildingSystem();
            if (bs == null) return;

            List<BuildingSavePieceData> pieces = bs.ExportPlacedStructures();
            if (pieces.Count == 0)
            {
                SetStatus("No building to save.");
                return;
            }

            BuildingSaveData data = new BuildingSaveData
            {
                displayName = GetInputName(),
                pieces = pieces
            };

            Texture2D thumbnail = CaptureThumbnail();
            BuildingSaveStore.Save(data, thumbnail);
            if (thumbnail != null) Destroy(thumbnail);

            _selectedSaveId = data.id;
            RefreshSaveList();
            SetStatus("Saved.");
        }

        public void OpenSelectedSave()
        {
            OpenSave(_selectedSaveId);
        }

        public void OpenSave(string id)
        {
            BuildingSaveData data = BuildingSaveStore.Load(id);
            if (data == null)
            {
                SetStatus("Save not found.");
                return;
            }

            BuildingSystem bs = GetBuildingSystem();
            if (bs == null) return;

            if (SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating)
            {
                SimulationManager.Instance.StopSimulation();
            }

            _selectedSaveId = id;
            SetInputName(data.displayName);
            bool loaded = bs.ImportPlacedStructures(data.pieces, structures, materials, gadgets, true);
            RefreshSelectedPreview();
            SetStatus(loaded ? "Opened." : "Could not open save.");
        }

        public void DeleteSelectedSave()
        {
            DeleteSave(_selectedSaveId);
        }

        public void DeleteSave(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            BuildingSaveStore.Delete(id);
            if (_selectedSaveId == id)
            {
                _selectedSaveId = null;
            }

            RefreshSaveList();
            SetStatus("Deleted.");
        }

        public void RenameSelectedSave()
        {
            RenameSelectedSave(GetInputName());
        }

        public void RenameSelectedSave(string newName)
        {
            if (string.IsNullOrEmpty(_selectedSaveId)) return;

            if (BuildingSaveStore.Rename(_selectedSaveId, newName))
            {
                RefreshSaveList();
                SetStatus("Renamed.");
            }
        }

        public void SelectSave(string id)
        {
            BuildingSaveData data = BuildingSaveStore.Load(id);
            if (data == null) return;

            _selectedSaveId = id;
            SetInputName(data.displayName);
            RefreshSelectedPreview();
        }

        public void RefreshSaveList()
        {
            foreach (GameObject slot in _spawnedSlots)
            {
                if (slot != null) Destroy(slot);
            }
            _spawnedSlots.Clear();

            List<BuildingSaveSummary> summaries = BuildingSaveStore.GetSummaries();
            if (string.IsNullOrEmpty(_selectedSaveId) && summaries.Count > 0)
            {
                _selectedSaveId = summaries[0].id;
                SetInputName(summaries[0].displayName);
            }

            if (slotParent != null && slotPrefab != null)
            {
                foreach (BuildingSaveSummary summary in summaries)
                {
                    BuildingSaveSlotUI slot = Instantiate(slotPrefab, slotParent);
                    Texture2D thumbnail = BuildingSaveStore.LoadThumbnail(summary.id);
                    slot.Bind(this, summary, thumbnail);
                    _spawnedSlots.Add(slot.gameObject);
                }
            }

            RefreshSelectedPreview();
        }

        private BuildingSystem GetBuildingSystem()
        {
            BuildingSystem bs = buildingSystem != null ? buildingSystem : BuildingSystem.Instance;
            if (bs == null)
            {
                SetStatus("Building system missing.");
            }
            return bs;
        }

        private string GetInputName()
        {
            if (nameInput == null || string.IsNullOrWhiteSpace(nameInput.text))
            {
                return "Building Save";
            }

            return nameInput.text.Trim();
        }

        private void SetInputName(string value)
        {
            if (nameInput != null)
            {
                nameInput.SetTextWithoutNotify(string.IsNullOrEmpty(value) ? "Building Save" : value);
            }

            if (selectedNameText != null)
            {
                selectedNameText.text = string.IsNullOrEmpty(value) ? "Building Save" : value;
            }
        }

        private void RefreshSelectedPreview()
        {
            BuildingSaveData selected = BuildingSaveStore.Load(_selectedSaveId);
            if (selected != null)
            {
                SetInputName(selected.displayName);
            }

            if (selectedPreviewImage == null) return;

            Texture2D thumbnail = BuildingSaveStore.LoadThumbnail(_selectedSaveId);
            selectedPreviewImage.texture = thumbnail;
            selectedPreviewImage.enabled = thumbnail != null;
        }

        private Texture2D CaptureThumbnail()
        {
            UnityEngine.Camera cam = thumbnailCamera != null ? thumbnailCamera : UnityEngine.Camera.main;
            if (cam == null)
            {
                return ScreenCapture.CaptureScreenshotAsTexture();
            }

            int size = Mathf.Clamp(thumbnailSize, 128, 2048);
            RenderTexture renderTexture = new RenderTexture(size, size, 24);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = cam.targetTexture;

            cam.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            cam.Render();

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            texture.Apply();

            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            Destroy(renderTexture);

            return texture;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[BuildingSaveManager] {message}");
        }
    }
}
