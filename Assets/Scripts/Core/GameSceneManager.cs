using UnityEngine;
using UnityEngine.SceneManagement;
// alias กัน ambiguous ระหว่างคลาสของเรากับ UnityEngine.SceneManagement.SceneManager
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Simulation.Core
{
    /// <summary>
    /// GameSceneManager — ตัวจัดการการ "ย้ายซีน" (Unity Scene) ของเกม
    ///
    /// • Singleton แบบ DontDestroyOnLoad: มีตัวเดียว อยู่ข้ามซีนได้ (สร้างอัตโนมัติถ้ายังไม่มี)
    /// • โหลดซีน "ด้วยชื่อ" (scene ต้องถูกเพิ่มใน Build Settings ก่อน)
    /// • โหลดแบบทันที (LoadScene, Single mode)
    /// • มี validation + error log ที่ชัดเจน (กันพลาดเรื่องลืมใส่ซีนใน Build Settings ซึ่งพบบ่อยสุด)
    ///
    /// ใช้จากโค้ด:   GameSceneManager.Instance.LoadScene("Lv.1");
    /// ใช้จากปุ่ม UI: ใส่คอมโพเนนต์ SceneButton บนปุ่ม แล้วระบุชื่อซีน (คลาส SceneButton อยู่ใน LevelNode.cs)
    /// </summary>
    [DisallowMultipleComponent]
    public class GameSceneManager : MonoBehaviour
    {
        // ── Singleton (lazy + persistent) ──
        private static GameSceneManager _instance;

        /// <summary>
        /// เข้าถึงตัวจัดการซีน — ถ้ายังไม่มีในฉากจะสร้างให้อัตโนมัติ จึงเรียกได้เสมอโดยไม่ NullRef
        /// </summary>
        public static GameSceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // หาในซีนก่อน เผื่อมีวางไว้แล้ว
                    _instance = FindFirstObjectByType<GameSceneManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameSceneManager");
                        _instance = go.AddComponent<GameSceneManager>();
                    }
                }
                return _instance;
            }
        }


        [Header("Debug")]
        [SerializeField] private bool logTransitions = true;

        /// <summary>true ระหว่างกำลังโหลดซีน (กันสั่งโหลดซ้อน)</summary>
        public bool IsLoading { get; private set; }

        /// <summary>ชื่อซีนที่ active อยู่ตอนนี้</summary>
        public string CurrentSceneName => USceneManager.GetActiveScene().name;

        // ── Events ──
        /// <summary>เรียกก่อนเริ่มโหลดซีนใหม่ (ส่งชื่อซีนปลายทาง) — เหมาะกับใส่เอฟเฟกต์เฟด/เซฟเกม</summary>
        public event System.Action<string> OnBeforeSceneLoad;
        /// <summary>เรียกหลังซีนใหม่โหลดเสร็จ (ส่งชื่อซีนที่โหลด)</summary>
        public event System.Action<string> OnAfterSceneLoad;

        // ────────────────────────────────────────────────────────────────
        // Lifecycle
        // ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // บังคับให้มีตัวเดียว + อยู่ข้ามซีน
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null);          // ต้องเป็น root object ก่อนเรียก DontDestroyOnLoad
            DontDestroyOnLoad(gameObject);

            USceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            // ถอด event เฉพาะ instance ที่เป็นตัวจริง (กัน unsubscribe ของตัวที่ถูก Destroy ทิ้ง)
            if (_instance == this)
            {
                USceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            IsLoading = false;
            if (logTransitions) Debug.Log($"[GameSceneManager] โหลดซีนเสร็จ: {scene.name}");
            OnAfterSceneLoad?.Invoke(scene.name);
        }

        // ────────────────────────────────────────────────────────────────
        // Public API — โหลดซีนด้วยชื่อ (โหลดทันที)
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// โหลดซีนตามชื่อ (ต้องอยู่ใน Build Settings)
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[GameSceneManager] LoadScene: sceneName ว่างเปล่า");
                return;
            }

            if (IsLoading)
            {
                Debug.LogWarning($"[GameSceneManager] กำลังโหลดซีนอื่นอยู่ — ข้ามคำสั่งโหลด '{sceneName}'");
                return;
            }

            // ตรวจว่าซีนอยู่ใน Build Settings จริงไหม (กันพลาดที่พบบ่อยที่สุด)
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[GameSceneManager] ไม่พบซีน '{sceneName}' ใน Build Settings!\n" +
                    "วิธีแก้: File > Build Settings (หรือ Build Profiles) แล้วลากไฟล์ซีนเข้าช่อง " +
                    "'Scenes In Build' และตรวจให้สะกดชื่อตรงกัน (case-sensitive ไม่ต้องใส่ .unity).");
                return;
            }

            if (logTransitions)
                Debug.Log($"[GameSceneManager] ย้ายซีน: {CurrentSceneName} → {sceneName}");

            IsLoading = true;
            OnBeforeSceneLoad?.Invoke(sceneName);

            // โหลดทันทีตามที่เลือกไว้ (Single = ปิดซีนเดิมแล้วเปิดซีนใหม่)
            USceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        /// <summary>โหลดซีนปัจจุบันใหม่ (เริ่มด่านใหม่/ลองใหม่)</summary>
        public void ReloadCurrentScene()
        {
            // โหลดด้วย buildIndex ของซีนปัจจุบันโดยตรง เพื่อให้ reload ได้เสมอแม้จะกำลังเทสต์อยู่
            Scene active = USceneManager.GetActiveScene();
            if (IsLoading)
            {
                Debug.LogWarning("[GameSceneManager] กำลังโหลดซีนอยู่ — ข้ามคำสั่ง reload");
                return;
            }

            IsLoading = true;
            OnBeforeSceneLoad?.Invoke(active.name);
            if (logTransitions) Debug.Log($"[GameSceneManager] โหลดซีนปัจจุบันใหม่: {active.name}");

            // ใช้ buildIndex ถ้ามี (เชื่อถือได้สุด) ไม่งั้น fallback เป็นชื่อ
            if (active.buildIndex >= 0)
                USceneManager.LoadScene(active.buildIndex, LoadSceneMode.Single);
            else
                USceneManager.LoadScene(active.name, LoadSceneMode.Single);
        }

        public void QuitGame()
        {
            if (logTransitions) Debug.Log("[GameSceneManager] ออกจากเกม");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
