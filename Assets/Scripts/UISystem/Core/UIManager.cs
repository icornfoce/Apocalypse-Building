using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Simulation.UI
{
    /// <summary>
    /// UIManager - ควบคุมการเปิด/ปิดหน้าจอ UI ต่างๆ ในเกม (พร้อมอนิเมชัน Fade)
    /// ลาก GameObject ใส่ List ใน Inspector แล้วตั้งชื่อ Screen ได้เลย
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [System.Serializable]
        public class ScreenEntry
        {
            public string screenName;
            public GameObject screenObject;
            public bool hideOnAwake = true;
            public bool isUnlocked = false; // สำหรับด่านที่ต้องปลดล็อก
        }

        [Header("Configuration")]
        [Tooltip("ลาก GameObject ของแต่ละหน้าจอมาใส่ แล้วตั้งชื่อ screenName")]
        [SerializeField] private List<ScreenEntry> screens = new List<ScreenEntry>();
        [Tooltip("ชื่อหน้าจอเริ่มต้นที่จะเปิดอัตโนมัติ")]
        [SerializeField] private string startScreen = "";

        [Header("Animation Settings")]
        [Tooltip("ระยะเวลา Fade (วินาที)")]
        [SerializeField] private float fadeDuration = 0.3f;
        [Tooltip("ระยะเวลา Scale (วินาที)")]
        [SerializeField] private float scaleDuration = 0.25f;
        [Tooltip("ขนาดเริ่มต้นตอน Popup เปิดขึ้นมา (เช่น 0.8 = เริ่มจากเล็กแล้วขยาย)")]
        [SerializeField] private float scaleFrom = 0.8f;

        private Dictionary<string, ScreenEntry> _screenDict = new Dictionary<string, ScreenEntry>();
        private Dictionary<string, CanvasGroup> _canvasGroupCache = new Dictionary<string, CanvasGroup>();
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

        private void Start()
        {
            // สร้าง Dictionary จาก List ที่ลากวางใน Inspector
            foreach (var screen in screens)
            {
                if (screen.screenObject != null)
                {
                    if (!_screenDict.ContainsKey(screen.screenName))
                    {
                        _screenDict[screen.screenName] = screen;

                        // เตรียม CanvasGroup สำหรับ Fade (ถ้ายังไม่มีจะเพิ่มให้อัตโนมัติ)
                        EnsureCanvasGroup(screen.screenName, screen.screenObject);

                        // ซ่อนเฉพาะหน้าที่ตั้งค่าไว้
                        if (screen.hideOnAwake)
                        {
                            screen.screenObject.SetActive(false);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[UIManager] ชื่อหน้าจอซ้ำ: {screen.screenName}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(startScreen))
            {
                OpenScreen(startScreen);
            }
        }

        /// <summary>
        /// เตรียม CanvasGroup ให้กับ GameObject (ถ้ายังไม่มีจะเพิ่มอัตโนมัติ)
        /// </summary>
        private CanvasGroup EnsureCanvasGroup(string name, GameObject obj)
        {
            if (_canvasGroupCache.TryGetValue(name, out CanvasGroup cached))
                return cached;

            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = obj.AddComponent<CanvasGroup>();
            }
            _canvasGroupCache[name] = cg;
            return cg;
        }

        // ─────────────────────────────────────────────
        //  เปิด / ปิด หน้าจอ (พร้อม Animation)
        // ─────────────────────────────────────────────

        /// <summary>
        /// เปิดหน้าจอหลัก พร้อมอนิเมชัน Fade + Scale
        /// </summary>
        /// <param name="name">ชื่อหน้าจอ</param>
        /// <param name="closeAllOthers">ถ้า true จะปิดทุกหน้าจอก่อนเปิดใหม่</param>
        public void OpenScreen(string name, bool closeAllOthers = true)
        {
            if (!_screenDict.ContainsKey(name))
            {
                Debug.LogWarning($"[UIManager] ไม่พบหน้าจอชื่อ '{name}'!");
                return;
            }

            if (closeAllOthers)
            {
                CloseAllImmediate();
            }
            else if (!string.IsNullOrEmpty(_currentMainScreen) && _currentMainScreen != name)
            {
                CloseScreenImmediate(_currentMainScreen);
            }

            AnimateOpen(name);
            _currentMainScreen = name;
        }

        /// <summary>
        /// เปิดหน้าจอแบบทับซ้อน (Overlay) โดยไม่ปิดหน้าจอหลัก พร้อมอนิเมชัน
        /// </summary>
        public void ShowOverlay(string name)
        {
            if (_screenDict.ContainsKey(name))
            {
                AnimateOpen(name);
            }
        }

        /// <summary>
        /// ปิดหน้าจอ พร้อมอนิเมชัน Fade Out
        /// </summary>
        public void CloseScreen(string name)
        {
            if (_screenDict.ContainsKey(name))
            {
                AnimateClose(name);
                if (name == _currentMainScreen) _currentMainScreen = "";
            }
        }

        /// <summary>
        /// สลับเปิด/ปิด หน้าจอ
        /// </summary>
        public void ToggleScreen(string name)
        {
            if (_screenDict.TryGetValue(name, out ScreenEntry entry))
            {
                if (entry.screenObject.activeSelf)
                    CloseScreen(name);
                else
                    ShowOverlay(name);
            }
        }

        /// <summary>
        /// ปิดทุกหน้าจอ (ไม่มีอนิเมชัน — ใช้ภายในก่อนเปิดหน้าจอใหม่)
        /// </summary>
        public void CloseAll()
        {
            CloseAllImmediate();
        }

        public bool IsScreenOpen(string name)
        {
            if (_screenDict.TryGetValue(name, out ScreenEntry entry))
            {
                return entry.screenObject.activeSelf;
            }
            return false;
        }

        // ─────────────────────────────────────────────
        //  ระบบล็อก / ปลดล็อก
        // ─────────────────────────────────────────────

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
                    Debug.Log($"[UIManager] ปลดล็อก '{name}' สำเร็จ!");
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

        // ─────────────────────────────────────────────
        //  Animation Helpers
        // ─────────────────────────────────────────────

        /// <summary>
        /// อนิเมชันเปิด: Fade In + Scale จากเล็กไปใหญ่
        /// </summary>
        private void AnimateOpen(string name)
        {
            var entry = _screenDict[name];
            var obj = entry.screenObject;
            var cg = EnsureCanvasGroup(name, obj);

            // Kill tween เก่าที่ค้างอยู่
            cg.DOKill();
            obj.transform.DOKill();

            // ตั้งค่าเริ่มต้นก่อนแสดง
            cg.alpha = 0f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
            obj.transform.localScale = Vector3.one * scaleFrom;

            // เปิด GameObject
            obj.SetActive(true);

            // เล่นอนิเมชัน Fade + Scale พร้อมกัน
            cg.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
            obj.transform.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// อนิเมชันปิด: Fade Out + Scale จากใหญ่ไปเล็ก แล้วปิด GameObject
        /// </summary>
        private void AnimateClose(string name)
        {
            if (!_screenDict.ContainsKey(name)) return;

            var entry = _screenDict[name];
            var obj = entry.screenObject;

            if (!obj.activeSelf) return;

            var cg = EnsureCanvasGroup(name, obj);

            // Kill tween เก่า
            cg.DOKill();
            obj.transform.DOKill();

            // ป้องกันการคลิกระหว่างปิด
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // เล่นอนิเมชัน Fade Out + Scale Down
            cg.DOFade(0f, fadeDuration).SetEase(Ease.InQuad);
            obj.transform.DOScale(Vector3.one * scaleFrom, scaleDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    obj.SetActive(false);
                    obj.transform.localScale = Vector3.one; // รีเซ็ตสเกลกลับ
                });
        }

        /// <summary>
        /// ปิดหน้าจอทันที ไม่มีอนิเมชัน (ใช้ภายในเพื่อเคลียร์ก่อนเปิดหน้าจอใหม่)
        /// </summary>
        private void CloseScreenImmediate(string name)
        {
            if (!_screenDict.ContainsKey(name)) return;

            var entry = _screenDict[name];
            var obj = entry.screenObject;
            var cg = EnsureCanvasGroup(name, obj);

            cg.DOKill();
            obj.transform.DOKill();

            cg.alpha = 0f;
            obj.transform.localScale = Vector3.one;
            obj.SetActive(false);
        }

        /// <summary>
        /// ปิดทุกหน้าจอทันที ไม่มีอนิเมชัน
        /// </summary>
        private void CloseAllImmediate()
        {
            foreach (var kvp in _screenDict)
            {
                var obj = kvp.Value.screenObject;
                if (obj != null)
                {
                    var cg = EnsureCanvasGroup(kvp.Key, obj);
                    cg.DOKill();
                    obj.transform.DOKill();
                    cg.alpha = 0f;
                    obj.transform.localScale = Vector3.one;
                    obj.SetActive(false);
                }
            }
            _currentMainScreen = "";
        }
    }
}
