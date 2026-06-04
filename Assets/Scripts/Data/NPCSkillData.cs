using UnityEngine;

namespace Simulation.Data
{
    /// <summary>
    /// ประเภทสกิลของ NPC ทั้ง 6 ตัว
    /// </summary>
    public enum NPCSkillType
    {
        /// <summary>วิศวกร — แสดงแรงเค้นเป็นแถบสี</summary>
        Engineer,
        /// <summary>ช่างก่อสร้าง — ซ่อมโครงสร้างที่เสียหาย</summary>
        Builder,
        /// <summary>นักเศรษฐศาสตร์ — ลดราคาก่อสร้าง 10% (Auto)</summary>
        Economist,
        /// <summary>สถาปนิก — บัฟออร่าสะท้อนดาเมจ 20% (เมื่อตรงเงื่อนไข)</summary>
        Architect,
        /// <summary>นักการเมือง — ยกเลิกภาษี (Auto)</summary>
        Politician,
        /// <summary>ทหาร — เรียกทหารมายิงซอมบี้</summary>
        Commander
    }

    /// <summary>
    /// ScriptableObject เก็บข้อมูล NPC แต่ละตัว (สเตตัส, สกิล, VFX, SFX)
    /// สร้างจาก Create > Simulation > NPC Skill Data
    /// </summary>
    [CreateAssetMenu(fileName = "New NPC Skill Data", menuName = "Simulation/NPC Skill Data")]
    public class NPCSkillData : ScriptableObject
    {
        [Header("General Info")]
        [Tooltip("ชื่อ NPC")]
        public string npcName;
        [TextArea(1, 3)]
        [Tooltip("คำอธิบายตัวละครและสกิล")]
        public string description;
        [Tooltip("ไอคอนสำหรับ UI")]
        public Sprite icon;
        [Tooltip("ราคาสำหรับวางตัวละคร NPC ตัวนี้")]
        public int placementPrice = 100;

        [Header("Stats")]
        [Tooltip("เลือดสูงสุด")]
        public float maxHealth = 100f;
        [Tooltip("ความเร็วเดิน")]
        public float moveSpeed = 3f;

        [Header("Skill")]
        [Tooltip("ประเภทสกิลของ NPC")]
        public NPCSkillType skillType;
        [Tooltip("คูลดาวน์สกิล (วินาที, 0 = ใช้ได้ทันที)")]
        public float skillCooldown = 0f;
        [Tooltip("ระยะเวลาสกิล (สำหรับสกิลที่มี Duration, 0 = ไม่มี)")]
        public float skillDuration = 0f;

        [Header("Skill - Builder")]
        [Tooltip("จำนวน HP ที่ซ่อมต่อวินาที")]
        public float repairPerSecond = 20f;

        [Header("Skill - Commander")]
        [Tooltip("ดาเมจต่อซอมบี้ต่อรอบยิง")]
        public float soldierDamage = 25f;
        [Tooltip("ระยะยิงของทหาร")]
        public float soldierRange = 15f;
        [Tooltip("ความถี่ยิง (วินาทีต่อรอบ)")]
        public float soldierFireRate = 0.5f;
        [Tooltip("จำนวนทหาร")]
        public int soldierCount = 3;

        [Header("Skill - Architect Conditions")]
        [Tooltip("เปิดเงื่อนไข: ห้ามมีเสาเกินจำนวนนี้ (0 = ปิดเงื่อนไข)")]
        public int maxPillarCondition = 0;
        [Tooltip("เปิดเงื่อนไข: ห้ามพื้นที่เกิน (ตารางเมตร, 0 = ปิด)")]
        public int maxAreaCondition = 0;
        [Tooltip("เปิดเงื่อนไข: ต้องมีอย่างน้อยกี่ชั้น (0 = ปิด)")]
        public int minFloorCondition = 0;

        [Header("Assets — Prefab")]
        [Tooltip("Prefab ตัว NPC (ที่มี PersonAI หรือ NPCController)")]
        public GameObject prefab;

        [Header("VFX")]
        [Tooltip("VFX ตอนเลือก NPC (วงแหวนที่พื้น/ออร่า)")]
        public GameObject selectionVFX;
        [Tooltip("VFX ตอนใช้สกิล")]
        public GameObject skillActivateVFX;
        [Tooltip("VFX ตอนสกิลทำงาน (เช่น ค้อนซ่อม, ออร่าบัฟ)")]
        public GameObject skillEffectVFX;
        [Tooltip("VFX ตอนตาย")]
        public GameObject deathVFX;

        [Header("SFX")]
        [Tooltip("เสียงตอนเลือก NPC")]
        public AudioClip selectionSFX;
        [Tooltip("เสียงตอนใช้สกิล")]
        public AudioClip skillActivateSFX;
        [Tooltip("เสียงตอนสกิลทำงาน")]
        public AudioClip skillEffectSFX;
        [Tooltip("เสียงตอนเดิน")]
        public AudioClip walkSFX;
        [Tooltip("เสียงตอนตาย")]
        public AudioClip deathSFX;
    }
}
