using UnityEngine;
using Simulation.UI;
using Simulation.Core;   // GameSceneManager (ใช้โดย SceneButton ที่ย้ายมารวมไว้ไฟล์นี้)

namespace Simulation.Mission
{
    /// <summary>
    /// LevelNode — จุดในฉากที่เมื่อ Player "เดินเข้าใกล้" จะเปิด UI ข้อมูลด่าน (World Space Canvas)
    /// และซ่อนเมื่อเดินออกห่าง
    ///
    /// เปลี่ยนจากเดิมที่ใช้ Trigger/Collision → เป็นการตรวจ "ระยะ" (proximity) แทน
    /// และคุมให้ UI World Space Canvas หันหากล้องตลอดผ่าน BillboardToCamera
    /// </summary>
    public class LevelNode : MonoBehaviour
    {
        [Header("Mission")]
        [Tooltip("ข้อมูลด่านของโหนดนี้")]
        [SerializeField] private MissionData missionData;

        [Header("Proximity — เดินใกล้ = เปิด")]
        [Tooltip("ระยะที่เริ่มเปิด UI (เมตร)")]
        [SerializeField] private float activationRange = 5f;

        [Tooltip("ระยะเผื่อก่อนปิด (กัน UI กระพริบตอนยืนขอบๆ) — ปิดเมื่อไกลกว่า activationRange + ค่านี้")]
        [SerializeField] private float deactivationPadding = 1f;

        [Tooltip("Tag ของผู้เล่นที่ใช้วัดระยะ")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("ถ้าหาผู้เล่นไม่เจอ ให้ใช้ระยะจากกล้องหลักแทน")]
        [SerializeField] private bool useCameraIfNoPlayer = true;

        [Tooltip("ตรวจระยะทุกๆ กี่วินาที (0 = ทุกเฟรม) — ตั้งให้ห่างเล็กน้อยช่วยลดภาระ")]
        [SerializeField] private float checkInterval = 0.1f;

        [Header("World Space UI")]
        [Tooltip("UI (World Space Canvas) ที่จะเปิด/ปิด — แนะนำวางเป็นลูกของโหนดนี้ หรือ assign มา")]
        [SerializeField] private GameObject worldCanvas;

        [Tooltip("ให้ UI หันหากล้องตลอดเวลา (billboard)")]
        [SerializeField] private bool billboardToCamera = true;

        [Tooltip("billboard แบบตั้งตรง (หมุนเฉพาะแกน Y) — ปิด = หันเต็มขนานจอ")]
        [SerializeField] private bool billboardYAxisOnly = true;

        [Header("Scale Animation")]
        [Tooltip("ความเร็วในการย่อ/ขยาย UI")]
        [SerializeField] private float scaleSpeed = 8f;

        [Header("Lock (ถ้าใช้ระบบปลดล็อกผ่าน UIManager)")]
        [Tooltip("ต้องปลดล็อกก่อนถึงจะเปิดได้")]
        [SerializeField] private bool requireUnlock = false;

        [Tooltip("ลาก GameObject ของหน้าจอที่ต้องปลดล็อกก่อน (UIManager.IsUnlocked)")]
        [SerializeField] private GameObject missionUnlockScreen;

        [Header("Progression / Save (ถาวร)")]
        [Tooltip("ด่านนี้ปลดล็อกตั้งแต่เริ่มเกม (ติ๊กเฉพาะด่านแรก)")]
        [SerializeField] private bool unlockedByDefault = false;

        [Tooltip("Object ที่จะ 'ซ่อน' เมื่อด่านนี้ถูกปลดล็อกแล้ว (เช่น ไอคอนแม่กุญแจ)")]
        [SerializeField] private GameObject lockObject;

        [Tooltip("ไอคอนดาว (ใส่ได้สูงสุด 3 อัน เรียงซ้าย→ขวา) จะเปิดตามจำนวนดาวที่เคยได้")]
        [SerializeField] private GameObject[] starIcons;

        // ── runtime ──
        private Transform _player;
        private bool _isOpen;
        private float _timer;
        private Vector3 _originalScale;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            // ซ่อน UI ไว้ก่อนตั้งแต่เริ่ม และจำขนาดดั้งเดิมไว้
            if (worldCanvas != null)
            {
                _originalScale = worldCanvas.transform.localScale;
                worldCanvas.transform.localScale = Vector3.zero;
                worldCanvas.SetActive(false);
            }
        }

        private void Start()
        {
            // อ่านข้อมูลเซฟมาแสดงดาว + สถานะล็อก ตอนเข้าหน้าเลือกด่าน
            RefreshProgressUI();
        }

        private void Update()
        {
            // 1. ตรวจสอบ proximity (ใช้ checkInterval เพื่อลดโหลด)
            bool runCheck = true;
            if (checkInterval > 0f)
            {
                _timer -= Time.deltaTime;
                if (_timer > 0f)
                {
                    runCheck = false;
                }
                else
                {
                    _timer = checkInterval;
                }
            }

            if (runCheck)
            {
                if (TryGetWatcherPosition(out Vector3 watcher))
                {
                    float sqr = (watcher - transform.position).sqrMagnitude;
                    float openR  = activationRange;
                    float closeR = activationRange + Mathf.Max(0f, deactivationPadding);

                    if (!_isOpen && sqr <= openR * openR)
                        Open();
                    else if (_isOpen && sqr > closeR * closeR)
                        Close();
                }
            }

            // 2. อัปเดตขนาด UI ทุกเฟรมเพื่อให้การขยายดูนุ่มนวล
            UpdateScale();

            // 3. ตรวจสอบการกด Spacebar เพื่อเริ่มด่านเมื่อเดินเข้ามาใกล้ (UI เปิดอยู่)
            if (_isOpen && Input.GetKeyDown(KeyCode.Space))
            {
                StartLevel();
            }
        }

        private void UpdateScale()
        {
            if (worldCanvas == null) return;

            Vector3 targetScale = _isOpen ? _originalScale : Vector3.zero;

            // ค่อยๆ ปรับขนาดไปยังเป้าหมาย
            worldCanvas.transform.localScale = Vector3.Lerp(worldCanvas.transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

            // ถ้าโหนดปิดอยู่และขนาดหดลงจนเกือบเป็นศูนย์ (เทียบกับขนาดจริง) ให้ซ่อน Canvas
            if (!_isOpen && worldCanvas.activeSelf)
            {
                float originalSqr = _originalScale.sqrMagnitude;
                float currentSqr = worldCanvas.transform.localScale.sqrMagnitude;
                
                // ถ้าย่อเหลือต่ำกว่า 1% ของขนาดเดิม ให้ปิดการแสดงผล
                if (originalSqr > 0f && (currentSqr / originalSqr) < 0.0001f)
                {
                    worldCanvas.SetActive(false);
                    worldCanvas.transform.localScale = Vector3.zero; // รีเซ็ตเป็น 0 พอดี
                }
            }
        }

        /// <summary>หาตำแหน่งที่ใช้วัดระยะ: ผู้เล่นก่อน ถ้าไม่มีค่อยใช้กล้อง</summary>
        private bool TryGetWatcherPosition(out Vector3 pos)
        {
            if (_player == null && !string.IsNullOrEmpty(playerTag))
            {
                var go = GameObject.FindGameObjectWithTag(playerTag);
                if (go != null) _player = go.transform;
            }

            if (_player != null) { pos = _player.position; return true; }

            if (useCameraIfNoPlayer && UnityEngine.Camera.main != null)
            {
                pos = UnityEngine.Camera.main.transform.position;
                return true;
            }

            pos = default;
            return false;
        }

        private void Open()
        {
            if (missionData == null)
            {
                Debug.LogWarning($"[LevelNode] {name} ยังไม่ได้ใส่ MissionData");
                return;
            }

            // เช็คปลดล็อก (ถ้าเปิดใช้งาน)
            if (requireUnlock
                && UIManager.Instance != null
                && !UIManager.Instance.IsUnlocked(missionUnlockScreen))
            {
                return; // ยังล็อกอยู่ → ไม่เปิด
            }

            _isOpen = true;

            // อัปเดตดาว/สถานะล็อกล่าสุดทุกครั้งที่เปิดป้าย
            RefreshProgressUI();

            // เลือกด่านนี้เป็นภารกิจปัจจุบัน
            if (MissionManager.Instance != null)
                MissionManager.Instance.SetMission(missionData);

            if (worldCanvas != null)
            {
                worldCanvas.SetActive(true);

                // เติมข้อมูลด่านถ้ามี MissionInfoDisplay (รวม inactive child ด้วย)
                var display = worldCanvas.GetComponentInChildren<MissionInfoDisplay>(true);
                if (display != null) display.Setup(missionData);

                // คุมให้หันหากล้อง
                if (billboardToCamera)
                {
                    var bb = worldCanvas.GetComponent<BillboardToCamera>();
                    if (bb == null) bb = worldCanvas.AddComponent<BillboardToCamera>();
                    bb.SetYAxisOnly(billboardYAxisOnly);
                    bb.enabled = true;
                }
            }

            Debug.Log($"[LevelNode] เปิด UI ด่าน: {missionData.missionName}");
        }

        private void Close()
        {
            _isOpen = false;
        }

        /// <summary>
        /// เริ่มต้นด่านตามชื่อด่านที่กำหนดใน missionData
        /// </summary>
        public void StartLevel()
        {
            if (missionData == null)
            {
                Debug.LogError($"[LevelNode] {name} ไม่มี MissionData สำหรับเริ่มด่าน");
                return;
            }

            // กันไม่ให้เริ่มด่านที่ยังถูกล็อก
            if (!IsLevelUnlocked())
            {
                Debug.Log($"[LevelNode] ด่าน {missionData.missionName} ยังถูกล็อกอยู่ (ต้องได้อย่างน้อย 1 ดาวจากด่านก่อนหน้า)");
                return;
            }

            string targetSceneName = missionData.missionName;
            Debug.Log($"[LevelNode] เริ่มต้นด่าน: {targetSceneName}");
            GameSceneManager.Instance.LoadScene(targetSceneName);
        }

        /// <summary>ด่านนี้ปลดล็อกแล้วหรือยัง (ด่านแรกให้ติ๊ก unlockedByDefault)</summary>
        public bool IsLevelUnlocked()
        {
            if (missionData == null) return false;
            return unlockedByDefault || ProgressSave.IsUnlocked(missionData.missionName);
        }

        /// <summary>
        /// อ่านข้อมูลเซฟมาอัปเดต UI: โชว์ดาวที่เคยได้ และซ่อน lockObject เมื่อปลดล็อกแล้ว
        /// </summary>
        private void RefreshProgressUI()
        {
            if (missionData == null) return;
            string id = missionData.missionName;

            bool unlocked = unlockedByDefault || ProgressSave.IsUnlocked(id);

            // lockObject = แสดงเมื่อ "ล็อก", ซ่อนเมื่อ "ปลดล็อก"
            if (lockObject != null) lockObject.SetActive(!unlocked);

            // เปิดไอคอนดาวตามจำนวนที่เคยได้ (เก็บค่าดีที่สุด)
            int stars = ProgressSave.GetStars(id);
            if (starIcons != null)
            {
                for (int i = 0; i < starIcons.Length; i++)
                    if (starIcons[i] != null) starIcons[i].SetActive(i < stars);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, activationRange);
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, activationRange + Mathf.Max(0f, deactivationPadding));
        }
#endif
    }
}

namespace Simulation.UI
{
    /// <summary>
    /// BillboardToCamera — หมุนให้ object (เช่น World Space Canvas) หันหากล้องตลอดเวลา
    /// ใช้ LateUpdate เพื่อหันหลังกล้องขยับแล้ว (ลดอาการกระตุก)
    /// </summary>
    [DisallowMultipleComponent]
    public class BillboardToCamera : MonoBehaviour
    {
        [Tooltip("กล้องเป้าหมาย (ว่าง = ใช้ Camera.main)")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Tooltip("หันเฉพาะแกน Y (ตั้งตรง ไม่ก้ม/เงย) — เหมาะกับป้าย UI")]
        [SerializeField] private bool yAxisOnly = true;

        [Tooltip("ถ้าตัวอักษร/UI กลับด้าน ให้ติ๊กเพื่อกลับหน้า 180°")]
        [SerializeField] private bool flip = false;

        private Transform _cam;

        public void SetYAxisOnly(bool value) => yAxisOnly = value;

        private void OnEnable() => CacheCamera();

        private void CacheCamera()
        {
            if (targetCamera != null) { _cam = targetCamera.transform; return; }
            var main = UnityEngine.Camera.main;
            if (main != null)
            {
                _cam = main.transform;
            }
            else
            {
                var anyCam = FindFirstObjectByType<UnityEngine.Camera>();
                if (anyCam != null) _cam = anyCam.transform;
            }
        }

        private void LateUpdate()
        {
            if (_cam == null || (targetCamera == null && UnityEngine.Camera.main != null && _cam != UnityEngine.Camera.main.transform))
            {
                CacheCamera();
                if (_cam == null) return; // ยังไม่มีกล้อง
            }

            Quaternion rot;
            if (yAxisOnly)
            {
                // หันรอบแกน Y ให้หน้าจอชี้มาทางกล้อง โดยยังตั้งตรง
                Vector3 dir = transform.position - _cam.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 1e-6f) return; // กล้องอยู่เหนือ/ใต้พอดี
                rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
            else
            {
                // หันเต็ม: ขนานกับระนาบจอกล้อง (อ่านง่ายสุด)
                rot = _cam.rotation;
            }

            if (flip) rot *= Quaternion.Euler(0f, 180f, 0f);
            transform.rotation = rot;
        }
    }
}
