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

        [Header("Lock (ถ้าใช้ระบบปลดล็อกผ่าน ScreenManager)")]
        [Tooltip("ต้องปลดล็อกก่อนถึงจะเปิดได้")]
        [SerializeField] private bool requireUnlock = false;

        [Tooltip("คีย์ชื่อที่ใช้เช็คสถานะปลดล็อก (ScreenManager.IsUnlocked)")]
        [SerializeField] private string missionUnlockKey = "MissionInfo";

        // ── runtime ──
        private Transform _player;
        private bool _isOpen;
        private float _timer;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            // ซ่อน UI ไว้ก่อนตั้งแต่เริ่ม
            if (worldCanvas != null) worldCanvas.SetActive(false);
        }

        private void Update()
        {
            // throttle การตรวจระยะ
            if (checkInterval > 0f)
            {
                _timer -= Time.deltaTime;
                if (_timer > 0f) return;
                _timer = checkInterval;
            }

            if (!TryGetWatcherPosition(out Vector3 watcher)) return;

            float sqr = (watcher - transform.position).sqrMagnitude;
            float openR  = activationRange;
            float closeR = activationRange + Mathf.Max(0f, deactivationPadding);

            if (!_isOpen && sqr <= openR * openR)
                Open();
            else if (_isOpen && sqr > closeR * closeR)
                Close();
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
                && ScreenManager.Instance != null
                && !ScreenManager.Instance.IsUnlocked(missionUnlockKey))
            {
                return; // ยังล็อกอยู่ → ไม่เปิด
            }

            _isOpen = true;

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
            if (worldCanvas != null) worldCanvas.SetActive(false);
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

            string targetSceneName = missionData.missionName;
            Debug.Log($"[LevelNode] เริ่มต้นด่าน: {targetSceneName}");
            GameSceneManager.Instance.LoadScene(targetSceneName);
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
            if (_cam == null)
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
