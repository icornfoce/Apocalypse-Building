using UnityEngine;
using System.Collections.Generic;

namespace Simulation.UI
{
    /// <summary>
    /// ScreenManager - ควบคุมการเปิด/ปิดหน้าจอ UI ต่างๆ ในเกม
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [System.Serializable]
        public class ScreenEntry
        {
            public string screenName;
            public GameObject screenObject;
            public bool hideOnAwake = true;
            public bool isUnlocked = false; // สำหรับด่านที่ต้องปลดล็อก
        }

        [Header("Configuration")]
        [SerializeField] private List<ScreenEntry> screens = new List<ScreenEntry>();
        [SerializeField] private string startScreen = "";

        private Dictionary<string, GameObject> _screenDict = new Dictionary<string, GameObject>();
        private string _currentMainScreen;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Initialize dictionary and hide all screens
        private void Start()
        {
            foreach (var screen in screens)
            {
                if (screen.screenObject != null)
                {
                    if (!_screenDict.ContainsKey(screen.screenName))
                    {
                        _screenDict[screen.screenName] = screen.screenObject;
                        
                        // ซ่อนเฉพาะหน้าที่ตั้งค่าไว้ (ปกติจะซ่อนเพื่อกันการทับซ้อน)
                        if (screen.hideOnAwake)
                        {
                            screen.screenObject.SetActive(false);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ScreenManager] Duplicate screen name: {screen.screenName}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(startScreen))
            {
                OpenScreen(startScreen);
            }
        }

        /// <summary>
        /// เปิดหน้าจอหลัก (จะปิดหน้าจอหลักเดิมหรือทุกหน้าจอตามเงื่อนไข)
        /// </summary>
        /// <param name="name">ชื่อหน้าจอ</param>
        /// <param name="closeAllOthers">ถ้า true จะปิดทุกหน้าจอที่เปิดอยู่ก่อนเปิดใหม่</param>
        public void OpenScreen(string name, bool closeAllOthers = true)
        {
            if (!_screenDict.ContainsKey(name))
            {
                Debug.LogWarning($"[ScreenManager] Screen '{name}' not found!");
                return;
            }

            if (closeAllOthers)
            {
                CloseAll();
            }
            else if (!string.IsNullOrEmpty(_currentMainScreen))
            {
                _screenDict[_currentMainScreen].SetActive(false);
            }

            _screenDict[name].SetActive(true);
            _currentMainScreen = name;
            
            Debug.Log($"[ScreenManager] Opened Screen: {name}");
        }

        /// <summary>
        /// เปิดหน้าจอแบบทับซ้อน (Overlay) โดยไม่ปิดหน้าจอหลัก
        /// </summary>
        public void ShowOverlay(string name)
        {
            if (_screenDict.TryGetValue(name, out GameObject screen))
            {
                screen.SetActive(true);
            }
        }

        /// <summary>
        /// ปิดหน้าจอ (ทั้งแบบหลักและ Overlay)
        /// </summary>
        public void CloseScreen(string name)
        {
            if (_screenDict.TryGetValue(name, out GameObject screen))
            {
                screen.SetActive(false);
                if (name == _currentMainScreen) _currentMainScreen = "";
            }
        }

        /// <summary>
        /// สลับสถานะ เปิด/ปิด หน้าจอ
        /// </summary>
        public void ToggleScreen(string name)
        {
            if (_screenDict.TryGetValue(name, out GameObject screen))
            {
                if (screen.activeSelf) CloseScreen(name);
                else ShowOverlay(name);
            }
        }

        public void CloseAll()
        {
            foreach (var screen in _screenDict.Values)
            {
                if (screen != null) screen.SetActive(false);
            }
            _currentMainScreen = "";
        }

        public bool IsScreenOpen(string name)
        {
            if (_screenDict.TryGetValue(name, out GameObject screen))
            {
                return screen.activeSelf;
            }
            return false;
        }

        /// <summary>
        /// สั่งปลดล็อกด่าน/หน้าจอ ตามชื่อที่ระบุ
        /// </summary>
        public void UnlockScreen(string name)
        {
            foreach (var screen in screens)
            {
                if (screen.screenName == name)
                {
                    screen.isUnlocked = true;
                    Debug.Log($"[ScreenManager] Screen '{name}' has been Unlocked!");
                    return;
                }
            }
        }

        /// <summary>
        /// เช็คว่าหน้าจอ/ด่านนี้ปลดล็อกแล้วหรือยัง
        /// </summary>
        public bool IsUnlocked(string name)
        {
            foreach (var screen in screens)
            {
                if (screen.screenName == name) return screen.isUnlocked;
            }
            return false;
        }
    }
}
