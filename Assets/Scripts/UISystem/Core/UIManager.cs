using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Simulation.UI
{
    /// <summary>
    /// UIManager - ควบคุมการเปิด/ปิดหน้าจอ UI ต่างๆ ในเกม (พร้อมอนิเมชัน Fade + Scale)
    /// ใช้แบบลาก GameObject ใส่ Inspector ได้เลย ไม่ต้องพิมพ์ชื่อ string
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [System.Serializable]
        public class ScreenEntry
        {
            [Tooltip("ลาก GameObject ของหน้าจอมาใส่")]
            public GameObject screenObject;
            public bool hideOnAwake = true;
            public bool isUnlocked = false; // สำหรับด่านที่ต้องปลดล็อก
        }

        [Header("Configuration")]
        [Tooltip("ลาก GameObject ของแต่ละหน้าจอมาใส่")]
        [SerializeField] private List<ScreenEntry> screens = new List<ScreenEntry>();
        [Tooltip("หน้าจอเริ่มต้นที่จะเปิดอัตโนมัติ (ลาก GameObject มาใส่)")]
        [SerializeField] private GameObject startScreen;

        [Header("Animation Settings")]
        [Tooltip("ระยะเวลา Fade (วินาที)")]
        [SerializeField] private float fadeDuration = 0.3f;
        [Tooltip("ระยะเวลา Scale (วินาที)")]
        [SerializeField] private float scaleDuration = 0.25f;
        [Tooltip("ขนาดเริ่มต้นตอน Popup เปิดขึ้นมา (เช่น 0.8 = เริ่มจากเล็กแล้วขยาย)")]
        [SerializeField] private float scaleFrom = 0.8f;

        // ใช้ GameObject เป็น key แทน string
        private Dictionary<GameObject, ScreenEntry> _screenDict = new Dictionary<GameObject, ScreenEntry>();
        private Dictionary<GameObject, CanvasGroup> _canvasGroupCache = new Dictionary<GameObject, CanvasGroup>();
        private GameObject _currentMainScreen;

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
                    if (!_screenDict.ContainsKey(screen.screenObject))
                    {
                        _screenDict[screen.screenObject] = screen;

                        // เตรียม CanvasGroup สำหรับ Fade (ถ้ายังไม่มีจะเพิ่มให้อัตโนมัติ)
                        EnsureCanvasGroup(screen.screenObject);

                        // ซ่อนเฉพาะหน้าที่ตั้งค่าไว้
                        if (screen.hideOnAwake)
                        {
                            screen.screenObject.SetActive(false);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[UIManager] GameObject ซ้ำ: {screen.screenObject.name}");
                    }
                }
            }

            if (startScreen != null)
            {
                OpenScreen(startScreen);
            }
        }

        /// <summary>
        /// เตรียม CanvasGroup ให้กับ GameObject (ถ้ายังไม่มีจะเพิ่มอัตโนมัติ)
        /// </summary>
        private CanvasGroup EnsureCanvasGroup(GameObject obj)
        {
            if (_canvasGroupCache.TryGetValue(obj, out CanvasGroup cached))
                return cached;

            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = obj.AddComponent<CanvasGroup>();
            }
            _canvasGroupCache[obj] = cg;
            return cg;
        }

        /// <summary>
        /// ตรวจสอบและลงทะเบียนหน้าจออัตโนมัติหากยังไม่ได้อยู่ในระบบ
        /// </summary>
        private bool RegisterScreenIfMissing(GameObject screen)
        {
            if (screen == null) return false;

            // ตรวจสอบว่าเป็น Prefab หรือไม่ (ต้องเป็นวัตถุที่อยู่ใน Scene)
            if (!screen.scene.IsValid() || screen.gameObject.scene.name == null)
            {
                Debug.LogError($"[UIManager] '{screen.name}' เป็น Prefab ที่อยู่ใน Assets! กรุณาลาก GameObject จาก Hierarchy (ช่องซ้ายมือใน Scene) ไปใส่แทน");
                return false;
            }

            if (!_screenDict.ContainsKey(screen))
            {
                ScreenEntry newEntry = new ScreenEntry
                {
                    screenObject = screen,
                    hideOnAwake = false,
                    isUnlocked = true
                };
                screens.Add(newEntry);
                _screenDict[screen] = newEntry;
                EnsureCanvasGroup(screen);
                Debug.Log($"[UIManager] ลงทะเบียนหน้าจอ '{screen.name}' เข้าสู่ระบบอัตโนมัติ");
            }
            return true;
        }

        // ─────────────────────────────────────────────
        //  เปิด / ปิด หน้าจอ (พร้อม Animation)
        // ─────────────────────────────────────────────

        /// <summary>
        /// เปิดหน้าจอหลัก พร้อมอนิเมชัน Fade + Scale
        /// ลาก GameObject ที่ต้องการเปิดใส่ได้เลย
        /// </summary>
        /// <param name="screen">GameObject ของหน้าจอที่ต้องการเปิด</param>
        /// <param name="closeAllOthers">ถ้า true จะปิดทุกหน้าจอก่อนเปิดใหม่</param>
        public void OpenScreen(GameObject screen, bool closeAllOthers = true)
        {
            if (screen == null) return;
            if (!RegisterScreenIfMissing(screen)) return;

            if (closeAllOthers)
            {
                CloseAllImmediate();
            }
            else if (_currentMainScreen != null && _currentMainScreen != screen)
            {
                CloseScreenImmediate(_currentMainScreen);
            }

            AnimateOpen(screen);
            _currentMainScreen = screen;
        }

        /// <summary>
        /// เปิดหน้าจอแบบทับซ้อน (Overlay) โดยไม่ปิดหน้าจอหลัก พร้อมอนิเมชัน
        /// </summary>
        public void ShowOverlay(GameObject screen)
        {
            if (screen == null) return;
            if (!RegisterScreenIfMissing(screen)) return;

            AnimateOpen(screen);
        }

        /// <summary>
        /// ปิดหน้าจอ พร้อมอนิเมชัน Fade Out
        /// </summary>
        public void CloseScreen(GameObject screen)
        {
            if (screen == null) return;
            if (!RegisterScreenIfMissing(screen)) return;

            AnimateClose(screen);
            if (screen == _currentMainScreen) _currentMainScreen = null;
        }

        /// <summary>
        /// สลับเปิด/ปิด หน้าจอ
        /// </summary>
        public void ToggleScreen(GameObject screen)
        {
            if (screen == null) return;
            if (!RegisterScreenIfMissing(screen)) return;

            if (screen.activeSelf)
                CloseScreen(screen);
            else
                ShowOverlay(screen);
        }

        /// <summary>
        /// ปิดทุกหน้าจอ
        /// </summary>
        public void CloseAll()
        {
            CloseAllImmediate();
        }

        public bool IsScreenOpen(GameObject screen)
        {
            if (screen != null)
            {
                return screen.activeSelf;
            }
            return false;
        }

        // ─────────────────────────────────────────────
        //  ระบบล็อก / ปลดล็อก
        // ─────────────────────────────────────────────

        /// <summary>
        /// สั่งปลดล็อกด่าน/หน้าจอ (ลาก GameObject มาใส่ได้เลย)
        /// </summary>
        public void UnlockScreen(GameObject screen)
        {
            if (screen == null) return;
            if (!RegisterScreenIfMissing(screen)) return;

            if (_screenDict.TryGetValue(screen, out ScreenEntry entry))
            {
                entry.isUnlocked = true;
                Debug.Log($"[UIManager] ปลดล็อก '{screen.name}' สำเร็จ!");
            }
        }

        /// <summary>
        /// เช็คว่าหน้าจอ/ด่านนี้ปลดล็อกแล้วหรือยัง
        /// </summary>
        public bool IsUnlocked(GameObject screen)
        {
            if (screen == null) return false;
            if (!RegisterScreenIfMissing(screen)) return false;

            if (_screenDict.TryGetValue(screen, out ScreenEntry entry))
            {
                return entry.isUnlocked;
            }
            return false;
        }

        // ─────────────────────────────────────────────
        //  Animation Helpers
        // ─────────────────────────────────────────────

        /// <summary>
        /// อนิเมชันเปิด: Fade In + Scale จากเล็กไปใหญ่
        /// </summary>
        private void AnimateOpen(GameObject screen)
        {
            var cg = EnsureCanvasGroup(screen);

            // Kill tween เก่าที่ค้างอยู่
            cg.DOKill();
            screen.transform.DOKill();

            // ตั้งค่าเริ่มต้นก่อนแสดง
            cg.alpha = 0f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
            screen.transform.localScale = Vector3.one * scaleFrom;

            // เปิด GameObject
            screen.SetActive(true);

            // เล่นอนิเมชัน Fade + Scale พร้อมกัน
            cg.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
            screen.transform.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// อนิเมชันปิด: Fade Out + Scale จากใหญ่ไปเล็ก แล้วปิด GameObject
        /// </summary>
        private void AnimateClose(GameObject screen)
        {
            if (screen == null || !screen.activeSelf) return;

            var cg = EnsureCanvasGroup(screen);

            // Kill tween เก่า
            cg.DOKill();
            screen.transform.DOKill();

            // ป้องกันการคลิกระหว่างปิด
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // เล่นอนิเมชัน Fade Out + Scale Down
            cg.DOFade(0f, fadeDuration).SetEase(Ease.InQuad);
            screen.transform.DOScale(Vector3.one * scaleFrom, scaleDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    screen.SetActive(false);
                    screen.transform.localScale = Vector3.one; // รีเซ็ตสเกลกลับ
                });
        }

        /// <summary>
        /// ปิดหน้าจอทันที ไม่มีอนิเมชัน
        /// </summary>
        private void CloseScreenImmediate(GameObject screen)
        {
            if (screen == null) return;

            var cg = EnsureCanvasGroup(screen);

            cg.DOKill();
            screen.transform.DOKill();

            cg.alpha = 0f;
            screen.transform.localScale = Vector3.one;
            screen.SetActive(false);
        }

        /// <summary>
        /// ปิดทุกหน้าจอทันที ไม่มีอนิเมชัน
        /// </summary>
        private void CloseAllImmediate()
        {
            foreach (var kvp in _screenDict)
            {
                var obj = kvp.Key;
                if (obj != null)
                {
                    var cg = EnsureCanvasGroup(obj);
                    cg.DOKill();
                    obj.transform.DOKill();
                    cg.alpha = 0f;
                    obj.transform.localScale = Vector3.one;
                    obj.SetActive(false);
                }
            }
            _currentMainScreen = null;
        }
    }
}
