using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Simulation.Mission;

namespace Simulation.UI
{
    /// <summary>
    /// สคริปต์ควบคุม UI สำหรับเลือกและเสกภัยพิบัติด้วยตัวเอง
    /// </summary>
    public class DisasterTriggerUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Dropdown disasterDropdown;
        [SerializeField] private Button triggerButton;
        [SerializeField] private Button stopAllButton;

        [Header("Disaster Assets")]
        [Tooltip("ลากไฟล์ DisasterData ที่ต้องการให้เลือกใน Dropdown มาใส่ที่นี่")]
        [SerializeField] private List<DisasterData> disasterList = new List<DisasterData>();

        private void Start()
        {
            InitializeDropdown();

            if (triggerButton != null)
            {
                triggerButton.onClick.AddListener(OnTriggerClick);
            }

            if (stopAllButton != null)
            {
                stopAllButton.onClick.AddListener(OnStopAllClick);
            }
        }

        private void InitializeDropdown()
        {
            if (disasterDropdown == null) return;

            disasterDropdown.ClearOptions();
            List<string> options = new List<string>();

            // ถ้าไม่มีไฟล์ระบุใน List ให้ใส่ตัวแจ้งเตือน
            if (disasterList == null || disasterList.Count == 0)
            {
                options.Add("No Disasters Loaded");
                disasterDropdown.AddOptions(options);
                if (triggerButton != null) triggerButton.interactable = false;
                return;
            }

            foreach (var disaster in disasterList)
            {
                if (disaster != null)
                {
                    options.Add(disaster.disasterName);
                }
            }

            disasterDropdown.AddOptions(options);
            if (triggerButton != null) triggerButton.interactable = true;
        }

        private void OnTriggerClick()
        {
            if (disasterDropdown == null || MissionManager.Instance == null) return;
            if (disasterList == null || disasterList.Count == 0) return;

            int selectedIndex = disasterDropdown.value;
            if (selectedIndex >= 0 && selectedIndex < disasterList.Count)
            {
                DisasterData selectedDisaster = disasterList[selectedIndex];
                if (selectedDisaster != null)
                {
                    MissionManager.Instance.TriggerDisasterDirectly(selectedDisaster);
                }
            }
        }

        private void OnStopAllClick()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.StopAllDisastersPublic();
            }
        }
    }
}
