using UnityEngine;
using TMPro;
using Simulation.Mission;

namespace Simulation.UI
{
    /// <summary>
    /// สคริปต์สำหรับติดที่ UI Prefab เพื่อแสดงข้อมูลด่าน (ใช้ได้ทั้งหน้าข้อมูลด่านปกติและหน้า Locked)
    /// </summary>
    public class MissionInfoDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI missionNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        /// <summary>
        /// รับข้อมูลจาก MissionData มาแสดงผลบน UI
        /// </summary>
        public void Setup(MissionData data)
        {
            if (data == null) return;

            if (missionNameText != null) 
                missionNameText.text = data.missionName;
                
            if (descriptionText != null) 
                descriptionText.text = data.description;
        }

        /// <summary>
        /// ปิดหน้าต่าง (ลบตัวเองทิ้ง)
        /// </summary>
        public void Close()
        {
            Destroy(gameObject);
        }
    }
}
