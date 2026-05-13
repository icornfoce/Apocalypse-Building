using UnityEngine;
using Simulation.UI;

namespace Simulation.Mission
{
    /// <summary>
    /// LevelNode - จุดในฉากที่เมื่อ Player มาสัมผัสจะแสดงข้อมูลด่าน
    /// </summary>
    public class LevelNode : MonoBehaviour
    {
        [Header("Mission Settings")]
        [Tooltip("ข้อมูลด่านที่จะใช้ในโหนดนี้")]
        [SerializeField] private MissionData missionData;
        
        [Header("UI Interaction")]
        [Tooltip("ชื่อของหน้าจอ UI ที่จะเปิดถ้าด่านปลดล็อกแล้ว (ใช้ ScreenManager)")]
        [SerializeField] private string missionScreenName = "MissionInfo";
        
        [Tooltip("Prefab ที่จะแสดงถ้าด่านยังล็อกอยู่ (Locked)")]
        [SerializeField] private GameObject lockedPrefab;
        
        [Tooltip("Canvas หรือ Parent ที่จะให้ UI Prefab ไปเกิด (ถ้าว่างจะหา Canvas อัตโนมัติ)")]
        [SerializeField] private Transform uiParent;
        
        [Header("Detection")]
        [Tooltip("Tag ของ Player ที่จะใช้ตรวจจับการชน")]
        [SerializeField] private string playerTag = "Player";
        [Tooltip("ระยะที่อนุญาตให้เปิด UI ได้ (ถ้าใช้ Trigger ให้ติ๊ก Is Trigger ใน Collider)")]
        [SerializeField] private bool useTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!useTrigger) return;
            
            if (other.CompareTag(playerTag))
            {
                TriggerMissionInfo();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (useTrigger) return;

            if (collision.gameObject.CompareTag(playerTag))
            {
                TriggerMissionInfo();
            }
        }

        /// <summary>
        /// แสดงข้อมูลด่านและเตรียมความพร้อมในการเล่น
        /// </summary>
        public void TriggerMissionInfo()
        {
            if (missionData == null)
            {
                Debug.LogWarning($"[LevelNode] {gameObject.name} has no MissionData assigned!");
                return;
            }

            // 1. ตรวจสอบก่อนว่าด่านนี้ Unlock หรือยังผ่าน ScreenManager
            bool isUnlocked = true;
            if (ScreenManager.Instance != null)
            {
                // เราใช้ชื่อหน้าจอภารกิจเป็นตัวเช็ค Unlock Status
                isUnlocked = ScreenManager.Instance.IsUnlocked(missionScreenName);
            }

            // 2. ถ้า Unlock แล้ว ให้เตรียมข้อมูลด่าน
            if (isUnlocked)
            {
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.SetMission(missionData);
                    Debug.Log($"[LevelNode] Set mission to: {missionData.missionName}");
                }

                // สั่ง ScreenManager เปิดหน้าจอข้อมูลด่านปกติ
                if (ScreenManager.Instance != null)
                {
                    ScreenManager.Instance.OpenScreen(missionScreenName);
                }
            }
            else
            {
                // ถ้ายัง Lock อยู่ ให้ Instantiate Prefab แจ้งเตือนขึ้นมา
                if (lockedPrefab != null)
                {
                    // หา Parent ถ้าไม่ได้ระบุไว้
                    Transform parent = uiParent;
                    if (parent == null)
                    {
                        Canvas canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null) parent = canvas.transform;
                    }

                    GameObject instance = Instantiate(lockedPrefab, parent);
                    
                    // ส่งข้อมูล MissionData ให้กับสคริปต์ใน Prefab
                    MissionInfoDisplay display = instance.GetComponent<MissionInfoDisplay>();
                    if (display != null)
                    {
                        display.Setup(missionData);
                    }
                    
                    Debug.Log($"[LevelNode] Spawned Locked Prefab for: {missionData.missionName}");
                }
                else
                {
                    Debug.LogWarning($"[LevelNode] {missionData.missionName} is LOCKED but no lockedPrefab assigned!");
                }
            }
        }
    }
}
