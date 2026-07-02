using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Simulation.Tutorial
{
    /// <summary>
    /// ชนิดของภารกิจ tutorial แต่ละข้อ (ใช้เลือกเงื่อนไข "ทำสำเร็จ" ให้ตรงกับปุ่มจริงในเกม)
    /// เพิ่มชนิดใหม่ได้เรื่อย ๆ แล้วไปเพิ่ม case ใน CheckStep()
    /// </summary>
    public enum TutorialStepType
    {
        SlideLeftRight,   // หมุนกล้องซ้าย "และ" ขวา (คลิกขวาค้างลากเมาส์แนวนอน)
        // ── เผื่ออนาคต ── (ยังไม่ implement เงื่อนไข ให้เพิ่มใน CheckStep ได้เลย)
        ZoomInOut,
        SwitchFloor,
        PlaceStructure,
        StartSimulation,
        Custom            // ทำสำเร็จเองผ่าน CompleteStep(index) จากภายนอก
    }

    /// <summary>ข้อมูล 1 ข้อของ tutorial</summary>
    [Serializable]
    public class TutorialStep
    {
        [Tooltip("ไอดีสั้น ๆ ไว้อ้างอิง (เช่น slide_lr)")]
        public string id = "step";
        [Tooltip("หัวข้อ/คำอธิบายที่จะโชว์บน UI")]
        [TextArea] public string title = "เลื่อนซ้าย–เลื่อนขวา";
        [Tooltip("เงื่อนไขการทำสำเร็จของข้อนี้")]
        public TutorialStepType type = TutorialStepType.SlideLeftRight;

        [Tooltip("เรียกเมื่อข้อนี้ทำสำเร็จ — ลาก UI (เช่นเปิดเครื่องหมายถูก) มาต่อได้เลยใน Inspector")]
        public UnityEvent onCompleted;

        [NonSerialized] public bool completed;
    }

    /// <summary>
    /// ระบบ Tutorial แบบไล่ทีละข้อ
    /// - เก็บลิสต์ข้อ (steps) + เงื่อนไขการทำสำเร็จ
    /// - พอผู้เล่นทำครบเงื่อนไข จะ "ติ๊กถูก" ให้อัตโนมัติ (ยิง event ให้ UI)
    /// - UI ทำเองแยกต่างหาก: subscribe OnStepCompleted / OnAllCompleted หรือใช้ UnityEvent onCompleted ของแต่ละข้อ
    ///
    /// ข้อแรกที่ทำไว้ให้: "เลื่อนซ้าย–เลื่อนขวา" = หมุนกล้องด้วยคลิกขวาค้างลากเมาส์ ให้ครบทั้งสองทาง
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("Steps")]
        [Tooltip("รายการข้อ tutorial (ข้อแรกตั้งเป็น SlideLeftRight ไว้แล้ว)")]
        [SerializeField]
        private List<TutorialStep> steps = new List<TutorialStep>
        {
            new TutorialStep { id = "slide_lr", title = "เลื่อนซ้าย–เลื่อนขวา", type = TutorialStepType.SlideLeftRight }
        };

        [Header("Behaviour")]
        [Tooltip("เริ่มตรวจ tutorial ทันทีตอน Start")]
        [SerializeField] private bool playOnStart = true;
        [Tooltip("ไล่ทีละข้อตามลำดับ (ต้องทำข้อก่อนหน้าให้เสร็จก่อน). ปิด = ตรวจทุกข้อพร้อมกัน")]
        [SerializeField] private bool sequential = true;

        [Header("Slide Left/Right Settings")]
        [Tooltip("ปริมาณการลากสะสม (แต่ละทาง) ที่ถือว่า 'เลื่อน' สำเร็จ — มากขึ้น = ต้องลากไกลขึ้น")]
        [SerializeField] private float slideThreshold = 1.5f;
        [Tooltip("นับการลากเมาส์ตอนคลิกขวาค้าง (วิธีหมุนกล้องของเกม)")]
        [SerializeField] private bool countRightMouseDrag = true;
        [Tooltip("นับปุ่มลูกศร/A-D (แกน Horizontal) ด้วย — เปิดถ้า UI ของคุณให้เลื่อนด้วยคีย์บอร์ด")]
        [SerializeField] private bool countHorizontalAxis = false;

        [Header("Persistence")]
        [Tooltip("จำความคืบหน้าไว้ใน PlayerPrefs (เปิดแล้วจะไม่ต้องทำ tutorial ซ้ำ)")]
        [SerializeField] private bool persistProgress = false;
        [SerializeField] private string saveKeyPrefix = "tutorial_done_";

        // ── Events (ให้ UI subscribe) ──
        /// <summary>ยิงเมื่อข้อ index ทำสำเร็จ</summary>
        public event Action<int> OnStepCompleted;
        /// <summary>ยิงเมื่อทุกข้อทำสำเร็จครบ</summary>
        public event Action OnAllCompleted;
        /// <summary>ยิงเมื่อ "ข้อที่กำลังทำ" เปลี่ยน (index ใหม่, -1 = จบหมดแล้ว)</summary>
        public event Action<int> OnActiveStepChanged;

        // ── Runtime ──
        private bool _running;
        private float _slideLeftAmount;
        private float _slideRightAmount;
        private int _lastActiveIndex = -2;

        // ── Public read API (ให้ UI อ่านสถานะไปแสดง) ──
        public IReadOnlyList<TutorialStep> Steps => steps;
        public bool IsRunning => _running;
        public int StepCount => steps.Count;
        public bool IsStepCompleted(int index) => index >= 0 && index < steps.Count && steps[index].completed;

        /// <summary>ข้อที่กำลังทำอยู่ (ข้อแรกที่ยังไม่เสร็จ) — คืน -1 ถ้าเสร็จหมดแล้ว</summary>
        public int ActiveStepIndex
        {
            get
            {
                for (int i = 0; i < steps.Count; i++)
                    if (!steps[i].completed) return i;
                return -1;
            }
        }

        public int CompletedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < steps.Count; i++) if (steps[i].completed) n++;
                return n;
            }
        }

        // ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (persistProgress) LoadProgress();
            if (playOnStart) StartTutorial();
        }

        /// <summary>เริ่ม/กลับมาตรวจ tutorial</summary>
        public void StartTutorial()
        {
            _running = true;
            ResetSlideAccumulators();
            NotifyActiveStepIfChanged();
        }

        /// <summary>หยุดตรวจ (เช่นตอนผู้เล่นกดข้าม)</summary>
        public void StopTutorial() => _running = false;

        private void Update()
        {
            if (!_running || steps.Count == 0) return;

            if (sequential)
            {
                int i = ActiveStepIndex;
                if (i < 0) return; // เสร็จหมดแล้ว
                if (CheckStep(steps[i])) MarkCompleted(i);
            }
            else
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i].completed) continue;
                    if (CheckStep(steps[i])) MarkCompleted(i);
                }
            }

            NotifyActiveStepIfChanged();
        }

        // ── เงื่อนไขการทำสำเร็จของแต่ละชนิด ──
        private bool CheckStep(TutorialStep step)
        {
            switch (step.type)
            {
                case TutorialStepType.SlideLeftRight:
                    return CheckSlideLeftRight();

                // เพิ่มเงื่อนไขข้ออื่นได้ที่นี่ในอนาคต:
                // case TutorialStepType.ZoomInOut: return ...;
                // case TutorialStepType.SwitchFloor: return ...;

                case TutorialStepType.Custom:
                default:
                    return false; // Custom → ต้องเรียก CompleteStep() เอง
            }
        }

        /// <summary>
        /// ตรวจ "เลื่อนซ้าย–เลื่อนขวา": ผู้เล่นต้องหมุนกล้องไปทางซ้าย "และ" ทางขวา จนสะสมครบ threshold ทั้งสองทาง
        /// (เกมนี้หมุนกล้องด้วยคลิกขวาค้างลากเมาส์แนวนอน)
        /// </summary>
        private bool CheckSlideLeftRight()
        {
            float dx = 0f;

            if (countRightMouseDrag && Input.GetMouseButton(1))
                dx += Input.GetAxis("Mouse X");

            if (countHorizontalAxis)
                dx += Input.GetAxis("Horizontal") * Time.deltaTime * 60f; // สเกลให้ใกล้เคียงเมาส์

            if (dx < 0f) _slideLeftAmount += -dx;
            else if (dx > 0f) _slideRightAmount += dx;

            return _slideLeftAmount >= slideThreshold && _slideRightAmount >= slideThreshold;
        }

        private void ResetSlideAccumulators()
        {
            _slideLeftAmount = 0f;
            _slideRightAmount = 0f;
        }

        // ── ทำเครื่องหมายว่าเสร็จ ──

        /// <summary>บังคับทำข้อให้สำเร็จจากภายนอก (ใช้กับ Custom หรือปุ่ม Skip)</summary>
        public void CompleteStep(int index)
        {
            if (index < 0 || index >= steps.Count || steps[index].completed) return;
            MarkCompleted(index);
        }

        /// <summary>ทำสำเร็จตาม id</summary>
        public void CompleteStep(string id)
        {
            for (int i = 0; i < steps.Count; i++)
                if (steps[i].id == id) { CompleteStep(i); return; }
        }

        private void MarkCompleted(int index)
        {
            steps[index].completed = true;
            steps[index].onCompleted?.Invoke();
            OnStepCompleted?.Invoke(index);

            if (persistProgress) SaveProgress(index);

            Debug.Log($"<color=lime>[Tutorial]</color> ✔ ข้อ {index} '{steps[index].title}' สำเร็จ!");

            // เตรียมตัวจับข้อถัดไป
            ResetSlideAccumulators();

            if (ActiveStepIndex < 0)
            {
                OnAllCompleted?.Invoke();
                Debug.Log("<color=lime>[Tutorial]</color> ★ ครบทุกข้อแล้ว!");
            }

            NotifyActiveStepIfChanged();
        }

        private void NotifyActiveStepIfChanged()
        {
            int active = ActiveStepIndex;
            if (active != _lastActiveIndex)
            {
                _lastActiveIndex = active;
                OnActiveStepChanged?.Invoke(active);
            }
        }

        // ── รีเซ็ต / persistence ──

        /// <summary>ล้างความคืบหน้าทั้งหมด (เริ่มใหม่)</summary>
        public void ResetProgress()
        {
            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].completed = false;
                if (persistProgress) PlayerPrefs.DeleteKey(saveKeyPrefix + steps[i].id);
            }
            ResetSlideAccumulators();
            _lastActiveIndex = -2;
            NotifyActiveStepIfChanged();
        }

        private void SaveProgress(int index)
        {
            PlayerPrefs.SetInt(saveKeyPrefix + steps[index].id, 1);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            for (int i = 0; i < steps.Count; i++)
                if (PlayerPrefs.GetInt(saveKeyPrefix + steps[i].id, 0) == 1)
                    steps[i].completed = true;
        }
    }
}
