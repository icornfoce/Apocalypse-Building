using UnityEngine;
using TMPro;

namespace Simulation.UI
{
    /// <summary>
    /// ตัวควบคุมหน้าจอตั้งค่า (Options UI) จัดการสลับหน้าต่างย่อย (Tabs)
    /// ได้แก่ General, Graphics, Sound, Controls
    /// </summary>
    public class OptionsUI : MonoBehaviour
    {
        [System.Serializable]
        public class TabGroup
        {
            [Tooltip("หน้าต่างย่อย (Panel) ของแท็บนี้")]
            public GameObject panelObject;
            [Tooltip("ข้อความบนปุ่ม (สำหรับเปลี่ยนสี/สไตล์เพื่อระบุสถานะ active)")]
            public TextMeshProUGUI buttonText;
        }

        [Header("Tab Setup")]
        [SerializeField] private TabGroup generalTab;
        [SerializeField] private TabGroup graphicsTab;
        [SerializeField] private TabGroup soundTab;
        [SerializeField] private TabGroup controlsTab;

        [Header("Button Style Settings")]
        [Tooltip("สีของปุ่ม/ข้อความ เมื่อถูกเลือก")]
        [SerializeField] private Color activeColor = Color.white;
        [Tooltip("สีของปุ่ม/ข้อความ เมื่อไม่ได้เลือก")]
        [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

        private void Start()
        {
            // เปิดหน้าแรก (General) เป็นค่าเริ่มต้น
            ShowGeneral();
        }

        /// <summary>
        /// สลับแท็บไปที่ General
        /// </summary>
        public void ShowGeneral()
        {
            SwitchTab(generalTab);
        }

        /// <summary>
        /// สลับแท็บไปที่ Graphics
        /// </summary>
        public void ShowGraphics()
        {
            SwitchTab(graphicsTab);
        }

        /// <summary>
        /// สลับแท็บไปที่ Sound
        /// </summary>
        public void ShowSound()
        {
            SwitchTab(soundTab);
        }

        /// <summary>
        /// สลับแท็บไปที่ Controls
        /// </summary>
        public void ShowControls()
        {
            SwitchTab(controlsTab);
        }

        /// <summary>
        /// เมธอดหลักสำหรับสลับหน้าต่างและปรับแต่งสไตล์ปุ่ม
        /// </summary>
        private void SwitchTab(TabGroup targetTab)
        {
            if (targetTab == null) return;

            // จัดการแท็บ General
            SetTabActive(generalTab, targetTab == generalTab);

            // จัดการแท็บ Graphics
            SetTabActive(graphicsTab, targetTab == graphicsTab);

            // จัดการแท็บ Sound
            SetTabActive(soundTab, targetTab == soundTab);

            // จัดการแท็บ Controls
            SetTabActive(controlsTab, targetTab == controlsTab);
        }

        /// <summary>
        /// เปิด/ปิด Panel และเปลี่ยนสีปุ่มของแท็บตามสถานะการเลือก
        /// </summary>
        private void SetTabActive(TabGroup tab, bool isActive)
        {
            if (tab == null) return;

            // เปิด/ปิด Panel
            if (tab.panelObject != null)
            {
                tab.panelObject.SetActive(isActive);
            }

            // เปลี่ยนสีข้อความเพื่อบอกสถานะ Active/Inactive
            if (tab.buttonText != null)
            {
                tab.buttonText.color = isActive ? activeColor : inactiveColor;
            }
        }
    }
}
