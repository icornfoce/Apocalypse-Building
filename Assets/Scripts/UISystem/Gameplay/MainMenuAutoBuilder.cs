using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulation.Building;
using Simulation.Data;
using Simulation.Mission;   // MissionManager + DisasterData
using Simulation.Physics;   // SimulationManager

namespace Simulation.UI
{
    /// <summary>
    /// สร้างตึกอัตโนมัติสำหรับฉาก gameplay ที่ใช้เป็น "พื้นหลังเมนู":
    ///   • สุ่มงบประมาณ (minBudget..maxBudget) แล้วสร้างตึกให้อยู่ในงบจริง
    ///   • สุ่มจำนวนชั้น (minFloors..maxFloors) สร้างแบบไม่ลอยฟ้า (วางจากล่างขึ้นบน + ต่อ joint กับตัวรองรับ)
    ///   • ไม่วางตัวละคร
    ///   • เมื่อสร้างเสร็จ "กดเริ่มเอง" (StartSimulation)
    ///   • สุ่มภัยพิบัติ 1–3 อย่าง ไม่ซ้ำชนิด และทยอยเกิด (ไม่ติดกัน)
    ///
    /// วิธีใช้: วางสคริปต์นี้บน GameObject ในซีน gameplay (เช่น sandbox) ที่มี BuildingSystem/SimulationManager/MissionManager
    /// แล้วลาก StructureData (เสา/พื้น), MaterialData (ไม้/คอนกรีต/เหล็ก) และ DisasterData มาใส่ใน Inspector
    /// หมายเหตุ: prefab เสาควรสูงเท่ากับ heightStep ของ BuildingSystem เพื่อให้ซ้อนชั้นต่อกันพอดี
    /// </summary>
    public class MainMenuAutoBuilder : MonoBehaviour
    {
        [Header("References (เว้นว่างได้ ถ้าจะใช้ .Instance)")]
        [SerializeField] private BuildingSystem buildingSystem;

        [Header("Structure Data")]
        [Tooltip("เสา — โครงหลักของตึก (จำเป็น)")]
        [SerializeField] private StructureData pillarData;
        [Tooltip("พื้น/เพดาน — วางบนหัวเสาแต่ละชั้น (เว้นว่างได้ = สร้างเฉพาะเสา)")]
        [SerializeField] private StructureData floorData;

        [Header("Materials (สุ่มเลือกภายในงบ — เว้นว่าง = ใช้ default ของ structure)")]
        [SerializeField] private MaterialData[] materials;

        [Header("Disasters (ลาก DisasterData asset — สุ่มไม่ซ้ำชนิด)")]
        [SerializeField] private DisasterData[] disasters;

        [Header("Budget (สุ่ม)")]
        [SerializeField] private int minBudget = 5000;
        [SerializeField] private int maxBudget = 50000;

        [Header("Floors (สุ่ม)")]
        [SerializeField] private int minFloors = 1;
        [SerializeField] private int maxFloors = 10;

        [Header("Footprint ฐานตึก (จำนวนช่อง สุ่ม)")]
        [SerializeField] private int minFootprint = 2;
        [SerializeField] private int maxFootprint = 4;

        [Header("Disasters: จำนวน + จังหวะเวลา")]
        [SerializeField] private int minDisasters = 1;
        [SerializeField] private int maxDisasters = 3;
        [Tooltip("หน่วงก่อนภัยพิบัติแรก (วินาที) หลังเริ่มเล่น")]
        [SerializeField] private float firstDisasterDelay = 4f;
        [Tooltip("ช่วงเวลาห่างระหว่างภัยพิบัติ (สุ่มในช่วงนี้) — ทำให้ไม่ติดกัน")]
        [SerializeField] private Vector2 disasterGap = new Vector2(5f, 9f);

        [Header("Timing")]
        [Tooltip("หน่วงก่อนเริ่มสร้าง (รอ scene พร้อม)")]
        [SerializeField] private float startDelay = 0.5f;
        [Tooltip("หน่วงระหว่างวางแต่ละชิ้น — >0 จะเห็นตึกค่อย ๆ ขึ้น (และช่วยให้ physics อัปเดตตัวรองรับทัน)")]
        [SerializeField] private float placeStepDelay = 0.04f;
        [Tooltip("หน่วงหลังสร้างเสร็จก่อนกดเริ่มเล่น")]
        [SerializeField] private float startSimDelay = 1f;

        private void Start()
        {
            StartCoroutine(RunRoutine());
        }

        private IEnumerator RunRoutine()
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            BuildingSystem bs = buildingSystem != null ? buildingSystem : BuildingSystem.Instance;
            if (bs == null)
            {
                Debug.LogError("[MainMenuAutoBuilder] ไม่พบ BuildingSystem ในซีนนี้");
                yield break;
            }
            if (pillarData == null || pillarData.prefab == null)
            {
                Debug.LogError("[MainMenuAutoBuilder] ยังไม่ได้ตั้งค่า pillarData (เสา)");
                yield break;
            }

            // 1) สุ่มงบ แล้วเซ็ตเข้า BuildingSystem
            int budget = Random.Range(minBudget, maxBudget + 1);
            bs.SetBudget(budget);

            // 2) สุ่มจำนวนชั้น + ขนาดฐาน (จำกัดให้อยู่ในขอบเขต grid)
            int floors = Random.Range(Mathf.Max(1, minFloors), Mathf.Max(minFloors, maxFloors) + 1);

            int maxW = Mathf.Max(1, Mathf.Min(maxFootprint, bs.GridColumns));
            int maxD = Mathf.Max(1, Mathf.Min(maxFootprint, bs.GridRows));
            int w = Random.Range(Mathf.Clamp(minFootprint, 1, maxW), maxW + 1);
            int d = Random.Range(Mathf.Clamp(minFootprint, 1, maxD), maxD + 1);

            // จัดฐานให้อยู่กึ่งกลาง grid
            int col0 = (bs.GridColumns - w) / 2;
            int row0 = (bs.GridRows - d) / 2;

            // 3) สร้างจากชั้นล่างขึ้นบน (TryAutoPlace จะหยุดเองเมื่องบหมด/ไม่มีตัวรองรับ)
            yield return StartCoroutine(BuildTower(bs, floors, col0, row0, w, d));

            // 4) กดเริ่มเล่นเอง
            if (startSimDelay > 0f) yield return new WaitForSeconds(startSimDelay);
            if (SimulationManager.Instance != null && !SimulationManager.Instance.IsSimulating)
                SimulationManager.Instance.StartSimulation();

            // 5) สุ่มภัยพิบัติ ไม่ซ้ำชนิด + ทยอยเกิด
            yield return StartCoroutine(TriggerStaggeredDisasters());
        }

        private IEnumerator BuildTower(BuildingSystem bs, int floors, int col0, int row0, int w, int d)
        {
            for (int f = 1; f <= floors; f++)
            {
                bool placedAnyThisFloor = false;

                // เสาทุกช่องของฐาน
                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < d; j++)
                    {
                        MaterialData mat = PickMaterialWithinBudget(bs, pillarData);
                        Vector3 pos = bs.GridCellToWorld(col0 + i, row0 + j, f, pillarData);
                        if (bs.TryAutoPlace(pillarData, mat, pos, 0f))
                        {
                            placedAnyThisFloor = true;
                            if (placeStepDelay > 0f) yield return new WaitForSeconds(placeStepDelay);
                        }
                    }
                }

                // พื้น/เพดานวางบนหัวเสา (= ฐานของชั้นถัดไป) ถ้ามี floorData
                if (floorData != null && floorData.prefab != null)
                {
                    for (int i = 0; i < w; i++)
                    {
                        for (int j = 0; j < d; j++)
                        {
                            MaterialData mat = PickMaterialWithinBudget(bs, floorData);
                            Vector3 pos = bs.GridCellToWorld(col0 + i, row0 + j, f + 1, floorData);
                            if (bs.TryAutoPlace(floorData, mat, pos, 0f))
                            {
                                if (placeStepDelay > 0f) yield return new WaitForSeconds(placeStepDelay);
                            }
                        }
                    }
                }

                // ชั้นนี้วางอะไรไม่ได้เลย (งบหมด/ไม่มีตัวรองรับ) → หยุด
                if (!placedAnyThisFloor) yield break;
            }
        }

        /// <summary>
        /// สุ่มเลือกวัสดุที่ "จ่ายไหว" ภายในงบที่เหลือ; ถ้าไม่มีตัวไหนจ่ายไหว คืนตัวที่ถูกที่สุด
        /// (แล้ว TryAutoPlace จะคืน false เองเพื่อหยุดการสร้าง)
        /// </summary>
        private MaterialData PickMaterialWithinBudget(BuildingSystem bs, StructureData data)
        {
            if (materials == null || materials.Length == 0) return null;

            List<MaterialData> affordable = new List<MaterialData>();
            MaterialData cheapest = null;
            float cheapestCost = float.MaxValue;

            foreach (var m in materials)
            {
                if (m == null) continue;
                float cost = bs.GetEffectivePrice(data.basePrice + m.priceModifier);
                if (cost < cheapestCost) { cheapestCost = cost; cheapest = m; }
                if (cost <= bs.CurrentBudget) affordable.Add(m);
            }

            if (affordable.Count > 0) return affordable[Random.Range(0, affordable.Count)];
            return cheapest;
        }

        private IEnumerator TriggerStaggeredDisasters()
        {
            if (disasters == null || disasters.Length == 0) yield break;
            if (MissionManager.Instance == null) yield break;

            // รวมเฉพาะตัวที่ไม่ null และไม่ซ้ำ reference
            List<DisasterData> pool = new List<DisasterData>();
            foreach (var disaster in disasters)
                if (disaster != null && !pool.Contains(disaster)) pool.Add(disaster);
            if (pool.Count == 0) yield break;

            // สุ่มสับลำดับ (Fisher–Yates) แล้วหยิบ count ตัวแรก = ไม่ซ้ำชนิด
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int count = Mathf.Clamp(Random.Range(minDisasters, maxDisasters + 1), 1, pool.Count);

            if (firstDisasterDelay > 0f) yield return new WaitForSeconds(firstDisasterDelay);

            for (int i = 0; i < count; i++)
            {
                // หยุดถ้า simulation ถูกปิดไปแล้ว (เช่น ออกจากเมนู)
                if (SimulationManager.Instance == null || !SimulationManager.Instance.IsSimulating) yield break;

                MissionManager.Instance.TriggerDisasterDirectly(pool[i]);

                if (i < count - 1)
                {
                    float gap = Random.Range(disasterGap.x, disasterGap.y); // เว้นจังหวะ → ไม่ติดกัน
                    yield return new WaitForSeconds(gap);
                }
            }
        }
    }
}
