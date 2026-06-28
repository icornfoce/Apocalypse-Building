using UnityEngine;
using System.Collections.Generic;
using Simulation.Building;
using Simulation.Data;

namespace Simulation.Physics
{
    /// <summary>
    /// Physics-based structural stress system.
    /// 
    /// Reads currentForce and currentTorque from a Joint each FixedUpdate,
    /// converts them into a combined stress value, deducts HP proportionally,
    /// recovers HP when stress is relieved, and breaks the structural connection
    /// (destroying the Joint + disabling all Colliders) when HP reaches zero.
    /// 
    /// Attach this component to any GameObject that has:
    ///   - A Rigidbody (isKinematic = false, useGravity = true)
    ///   - A Joint (FixedJoint or ConfigurableJoint) connecting it to the grid/neighbour
    ///   - One or more Colliders
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class StructuralStress : MonoBehaviour
    {
        // ────────────────────────────────────────────────────────────────
        // Serialized Settings
        // ────────────────────────────────────────────────────────────────

        [Header("HP Settings")]
        [Tooltip("Maximum structural hit points. Pulled from StructureData.baseHP if a StructureUnit is present.")]
        [SerializeField] private float baseHP = 100f;

        [Header("Force → Damage Conversion")]
        [Tooltip("Forces below this magnitude cause zero damage (Newtons).")]
        [SerializeField] private float forceThreshold = 100f;

        [Tooltip("Multiplier: damage = (forceMagnitude - threshold) * forceDamageMultiplier * dt")]
        [SerializeField] private float forceDamageMultiplier = 0.1f;

        [Header("Torque → Damage Conversion")]
        [Tooltip("Torques below this magnitude cause zero damage (N·m).")]
        [SerializeField] private float torqueThreshold = 100f;

        [Tooltip("Multiplier: damage = (torqueMagnitude - threshold) * torqueDamageMultiplier * dt")]
        [SerializeField] private float torqueDamageMultiplier = 0.05f;

        [Header("HP Recovery")]
        [Tooltip("HP recovered per second while total stress is below thresholds.")]
        [SerializeField] private float recoveryRate = 10f;

        [Tooltip("Time (seconds) of continuous low-stress before recovery begins.")]
        [SerializeField] private float recoveryCooldown = 0.5f;

        [Header("Impact → Damage Conversion")]
        [Tooltip("Impulse below this magnitude cause zero damage (N·s).")]
        [SerializeField] private float impactDamageThreshold = 50f;
        [SerializeField] private float impactDamageMultiplier = 0.05f;

        [Tooltip("Impulse จากการชนที่จะทำให้ข้อต่อ 'หลุด' (Detach) ทันที — ยิ่งสูงข้อต่อยิ่งแข็ง ไม่หลุดจากการชนเบาๆ")]
        [SerializeField] private float collisionDetachImpulse = 30f;
        [Tooltip("Impulse จากการชนที่จะทำให้ 'พัง' (Break) ทันที")]
        [SerializeField] private float collisionBreakImpulse = 80f;

        [Header("Physical Limits")]
        [SerializeField] private float maxCompression = 1000f;
        [SerializeField] private float maxTension = 1000f;
        [Tooltip("How many times its own weight a structure can support before stress damage begins. " +
                 "E.g. 10 means a floor can hold 10× its own mass worth of structures above it.")]
        [SerializeField] private float supportedLoadMultiplier = 10f;

        [Header("Visual Feedback")]
        [Tooltip("If assigned, these material colors lerp based on structural health. If empty, it will auto-find all renderers in children.")]
        [SerializeField] private Renderer[] stressRenderers;
        
        [SerializeField] [Range(0f, 1f)] private float stressVisualIntensity = 1.0f;

        [Header("Global Settings")]
        public static bool ShowHPVisualsGlobal = true;

        // ── ตำแหน่งจุดที่โครงสร้าง "พัง" ระหว่างด่านนี้ ──
        // ใช้โดยสกิล Engineer เพื่อโชว์ VFX สรุปความเสียหายตรงจุดที่พังตอนจบด่าน
        public static readonly List<Vector3> RecentBreakPositions = new List<Vector3>();

        /// <summary>ล้างประวัติจุดพัง (เรียกตอนเริ่มด่าน/เริ่มจำลองใหม่)</summary>
        public static void ClearBreakHistory() => RecentBreakPositions.Clear();

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // ────────────────────────────────────────────────────────────────
        // Runtime State
        // ────────────────────────────────────────────────────────────────

        private float _currentHP;
        private Joint _joint;
        private Rigidbody _rb;
        private Collider[] _colliders;
        private bool _isBroken;
        private bool _isDetached;
        private int _originalLayer;

        // กันอาการ "ลอย": จับครั้งเดียวตอนเริ่มจำลองว่าชิ้นนี้เป็น "ตัวยึดกับพื้น/โลก" จริงไหม
        // ground anchor = มี Joint ที่ connectedBody เป็น null ตั้งแต่เริ่ม (ก่อนมีอะไรถูกทำลาย)
        private bool _hadGroundAnchor;
        private bool _anchorChecked;

        // Recovery cooldown timer — counts how long stress has been below thresholds
        private float _lowStressTimer;

        // ── กันตัวละครเดินชนแล้วทำให้โครงสร้าง "พัง" ──
        // ตัวละคร (NPC/คน/ซอมบี้) เป็น Rigidbody แบบ Kinematic ที่ขับด้วย NavMeshAgent
        // เวลาเดินเบียดกำแพง/ประตู มันจะ "ดัน" Rigidbody ของโครงสร้าง → Joint รับแรงพุ่งสูงผิดปกติ
        // แล้ว FixedUpdate ด้านล่างจะตีความว่ารับแรงเกินขีดจำกัด → Break() ทันที
        // OnCollisionEnter เดิมกันเฉพาะ "อิมพัลส์" จากตัวละครไว้แล้ว แต่เส้นทาง "แรงที่ Joint" ยังรั่วอยู่
        // จึงจับเวลาไว้ว่าเพิ่งมีตัวละครมาสัมผัส แล้ว "งด" คิดดาเมจจากแรง/ทอร์กในช่วงสั้นๆ นั้น
        // (ของที่ "ตกหล่น/ปลิว" มาทับไม่ใช่ NavMeshAgent จึงยังสร้างความเสียหาย/ทับคนได้ตามปกติ)
        private float _characterContactTimer;
        private const float CHARACTER_CONTACT_GRACE = 0.3f;

        // Settlement timer — skip damage for a short time after init to let physics settle
        private float _settlementTimer;
        private const float SETTLEMENT_DURATION = 0.5f;

        // cached original values for restart
        private float _originalBaseHP;
        private float _originalMaxCompression;
        private float _originalMaxTension;

        // Cached original material colors
        private Color[] _cachedOriginalColors;
        private bool _hasStressRenderers;

        // Zero-bounce material for debris
        private static PhysicsMaterial _zeroBounceMaterial;

        // ────────────────────────────────────────────────────────────────
        // Public Read-Only Properties
        // ────────────────────────────────────────────────────────────────

        /// <summary>Current structural HP (0 = broken).</summary>
        public float CurrentHP => _currentHP;

        /// <summary>Maximum HP this part started with.</summary>
        public float BaseHP => baseHP;

        /// <summary>Normalised health ratio [0 … 1].</summary>
        public float HealthRatio => baseHP > 0f ? Mathf.Clamp01(_currentHP / baseHP) : 0f;

        /// <summary>True after the joint has been destroyed and colliders disabled.</summary>
        public bool IsBroken => _isBroken;

        /// <summary>True if the structure has been detached from its joints but not fully broken yet.</summary>
        public bool IsDetached => _isDetached;

        /// <summary>Last computed combined force magnitude on the joint (N).</summary>
        public float LastForceMagnitude { get; private set; }

        /// <summary>Last computed combined torque magnitude on the joint (N·m).</summary>
        public float LastTorqueMagnitude { get; private set; }

        // ────────────────────────────────────────────────────────────────
        // Events
        // ────────────────────────────────────────────────────────────────

        /// <summary>Fired the instant the structure breaks. Passes this component.</summary>
        public event System.Action<StructuralStress> OnBreak;

        /// <summary>Fired every FixedUpdate with (currentHP, healthRatio).</summary>
        public event System.Action<float, float> OnHealthChanged;

        // ────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>();
            _joint = GetComponent<Joint>();

            // Auto-find renderers if not set manually
            if (stressRenderers == null || stressRenderers.Length == 0)
            {
                stressRenderers = GetComponentsInChildren<Renderer>();
            }
            
            // Cache stress renderers
            _hasStressRenderers = stressRenderers != null && stressRenderers.Length > 0;
            if (_hasStressRenderers)
            {
                _cachedOriginalColors = new Color[stressRenderers.Length];
                for (int i = 0; i < stressRenderers.Length; i++)
                {
                    if (stressRenderers[i] != null)
                        _cachedOriginalColors[i] = stressRenderers[i].material.color;
                }
            }
        }

        private void Start()
        {
            if (_joint == null) _joint = GetComponent<Joint>();
            
            // หมายเหตุ: ตอนเพิ่งวาง ชิ้นอาจ "ยังไม่มี Joint" ได้ตามปกติ (ยังไม่ผูก/ไม่มีตัวค้ำใต้พอดี)
            // และ Joint จะถูกสร้างใหม่ทั้งหมดตอนเริ่มจำลอง (RefreshAllJoints) อยู่แล้ว
            // จึงเตือนเฉพาะตอนเปิด debug เพื่อไม่ให้ Console รก
            if (_joint == null && showDebugLogs)
            {
                Debug.LogWarning($"[StructuralStress] No Joint found on '{name}'. " +
                                 "Attach a FixedJoint or ConfigurableJoint for stress simulation.", this);
            }

            // Automatically initialize from StructureUnit if available and not already initialized
            if (_originalBaseHP == 0f)
            {
                var unit = GetComponent<StructureUnit>();
                if (unit != null && unit.Data != null)
                {
                    float maxHP = unit.Data.baseHP * (unit.CurrentMaterial != null ? unit.CurrentMaterial.hpMultiplier : 1f);
                    float compLimit = unit.Data.baseMaxCompression * (unit.CurrentMaterial != null ? unit.CurrentMaterial.compressionMultiplier : 1f);
                    float tenLimit = unit.Data.baseMaxTension * (unit.CurrentMaterial != null ? unit.CurrentMaterial.tensionMultiplier : 1f);
                    InitializeStress(maxHP, compLimit, tenLimit);

                    if (_rb != null)
                    {
                        _rb.mass = unit.Data.baseMass * (unit.CurrentMaterial != null ? unit.CurrentMaterial.massMultiplier : 1f);
                    }
                }
            }
        }

        /// <summary>
        /// All physics stress logic runs in FixedUpdate to stay synchronised with
        /// Unity's physics solver (where joint forces are computed).
        /// </summary>
        private void FixedUpdate()
        {
            if (_isBroken) return;

            // ── Settlement Period: ข้ามการคำนวณ damage ช่วง Physics settle ──
            if (_settlementTimer > 0f)
            {
                _settlementTimer -= Time.fixedDeltaTime;
                return;
            }

            // นับถอยหลังช่วง "ตัวละครเพิ่งมาเบียด" (ตั้งค่าจาก OnCollisionEnter/Stay)
            if (_characterContactTimer > 0f) _characterContactTimer -= Time.fixedDeltaTime;

            // จับครั้งเดียวหลัง settle (ก่อนมีอะไรถูกทำลาย): ชิ้นนี้เป็น ground anchor หรือไม่
            if (!_anchorChecked) CaptureAnchorState();

            // ── ตรวจ "การสูญเสียตัวค้ำ" แล้วลองหาที่ยึดใหม่ก่อนปล่อยร่วง ──
            // (เช่น พื้น/ตัวค้ำถูกทำลาย → joint หายไป หรือ connectedBody กลายเป็น null)
            if (!_isDetached && SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating)
            {
                if (_joint == null) _joint = GetComponent<Joint>();

                bool lostSupport;
                if (_joint == null)
                {
                    lostSupport = true; // joint ถูกทำลายไปเลย
                }
                else if (!_hadGroundAnchor)
                {
                    // ไม่ใช่ ground anchor: ถ้าทุก joint ไม่เหลือ connectedBody ที่ยังมีชีวิต = ตัวค้ำถูกทำลาย
                    bool hasLiveSupport = false;
                    foreach (var sj in GetComponents<Joint>())
                        if (sj != null && sj.connectedBody != null) { hasLiveSupport = true; break; }
                    lostSupport = !hasLiveSupport;
                }
                else
                {
                    lostSupport = false; // เป็น ground anchor ของแท้ → ไม่ต้องร่วง
                }

                if (lostSupport)
                {
                    // ลองหาจุดยึดใหม่จาก "พื้น/ground/เพื่อนบ้านที่ยังเหลือ" ก่อนจะปล่อยร่วง
                    bool reattached = Simulation.Building.BuildingSystem.Instance != null
                                      && Simulation.Building.BuildingSystem.Instance.TryReattachJoint(gameObject);
                    if (reattached)
                    {
                        _joint = GetComponent<Joint>();
                        CaptureAnchorState(); // อัปเดตสถานะ anchor ใหม่ (อาจกลายเป็น ground anchor)
                    }
                    else
                    {
                        Detach(); // ไม่มีที่ยึดเหลือจริงๆ → ปล่อยร่วงตามฟิสิกส์
                        return;
                    }
                }
            }

            if (_joint == null) return; // ไม่ได้ simulate และไม่มี joint → ข้าม

            // ── 1. Read forces from the joint ─────────────────────────
            float forceMag = 0f;
            float torqueMag = 0f;

            if (_joint != null)
            {
                forceMag = _joint.currentForce.magnitude;
                torqueMag = _joint.currentTorque.magnitude;
            }

            // ── Subtract expected structural load ────────────────────
            // Joint.currentForce includes the gravitational reaction force of
            // this piece AND everything it supports above (walls on floor, etc).
            // We subtract (own mass × gravity × multiplier) so that normal
            // building loads (up to N× own weight) cause zero damage.
            // Only forces that EXCEED this expected load cause stress.
            float restingWeight = _rb != null ? _rb.mass * UnityEngine.Physics.gravity.magnitude : 0f;
            float expectedLoad = restingWeight * supportedLoadMultiplier;
            forceMag = Mathf.Max(0f, forceMag - expectedLoad);

            LastForceMagnitude  = forceMag;
            LastTorqueMagnitude = torqueMag;

            // ── 2. Compute per-frame damage ───────────────────────────
            float dt = Time.fixedDeltaTime;

            float forceDamage  = 0f;
            float torqueDamage = 0f;

            // Use the lower of compression/tension as a base threshold for damage if needed,
            // or directly calculate damage based on reaching the limits.
            float currentLimit = maxTension; 
            // Simple heuristic for compression vs tension: 
            // If we have a joint, we could check axial force, but for now we use the magnitude
            // and compare against tension (common for most bridge materials).
            
            // ถ้าตัวละครเพิ่งมาเดินเบียด/ดัน ให้ "งด" คิดดาเมจจากแรงและทอร์กในเฟรมนี้
            // (กันกำแพง/ประตูพังเพราะโดน NPC เดินชน — แรงที่เห็นมาจากการดันของตัวละคร ไม่ใช่ภาระโครงสร้างจริง)
            // ประตูถือว่า "มีคนดันอยู่" เสมอ → งดคิดดาเมจจากแรง joint (คนเดินชนประตูจะไม่ทำให้พัง)
            bool characterPushing = _characterContactTimer > 0f || IsDoor();

            if (!characterPushing && forceMag > forceThreshold)
            {
                forceDamage = (forceMag - forceThreshold) * forceDamageMultiplier * dt;

                // ถ้าแรงกระทำเกินขีดจำกัดสูงสุดของวัสดุ (maxTension) ให้ข้อต่อหักทันที (Instant Snap)
                if (forceMag > currentLimit)
                {
                    _currentHP = 0f;
                    Break();
                    return;
                }
            }

            if (!characterPushing && torqueMag > torqueThreshold)
            {
                torqueDamage = (torqueMag - torqueThreshold) * torqueDamageMultiplier * dt;
            }

            float totalDamage = forceDamage + torqueDamage;

            // ── 3. Apply damage or recovery ───────────────────────────
            if (totalDamage > 0f)
            {
                _currentHP -= totalDamage;
                _lowStressTimer = 0f; // reset recovery cooldown

                if (showDebugLogs)
                {
                    Debug.Log($"[Stress] {name}  F={forceMag:F1}N  T={torqueMag:F1}N·m  " +
                              $"dmg={totalDamage:F2}  HP={_currentHP:F1}/{baseHP}", this);
                }

                // ── 4. Break check ────────────────────────────────────
                if (_currentHP <= 0f)
                {
                    _currentHP = 0f;
                    Break();
                    return;
                }
            }
            else
            {
                // No damage this tick — count towards recovery
                _lowStressTimer += dt;

                if (_lowStressTimer >= recoveryCooldown && _currentHP < baseHP)
                {
                    _currentHP = Mathf.Min(_currentHP + recoveryRate * dt, baseHP);
                }
            }

            // ── 5. Visual feedback ────────────────────────────────────
            UpdateVisualStress();

            // ── 6. Notify listeners ───────────────────────────────────
            OnHealthChanged?.Invoke(_currentHP, HealthRatio);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // ข้ามการตรวจจับในช่วง settle เพื่อป้องกันโครงสร้างถล่มตั้งแต่เริ่มจำลอง
            if (_settlementTimer > 0f) return;

            // ── ข้ามการชนจาก "ตัวละคร" (NPC / คน / ซอมบี้) ──
            // เดินชนประตู/กำแพงต้องไม่ทำให้ Joint หลุดหรือพัง (ตัวละครทุกตัวมี NavMeshAgent, โครงสร้างไม่มี)
            if (collision.collider != null &&
                collision.collider.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null)
            {
                // กันแรง "ดัน" จากการเดินชนไปทำให้ Joint พังใน FixedUpdate ด้วย
                _characterContactTimer = CHARACTER_CONTACT_GRACE;
                return;
            }

            float impact = collision.impulse.magnitude;

            var unit = GetComponent<Building.StructureUnit>();
            bool isGadget = unit != null && unit.Data != null && unit.Data.isGadget;

            // ── 0. ชิ้นส่วนที่หลุดออก (Detached) หรือ Gadget: กระแทกอะไรก็ตามนิดเดียว → พังทันที ไม่เด้ง ──
            if ((_isDetached || isGadget) && !_isBroken && impact > 0.5f)
            {
                _currentHP = 0f;
                Break();
                return;
            }

            // ── 0.5 โครงสร้างที่ยังเกาะอยู่: ถ้าชน/กระแทกแค่นิดเดียว (impact > 2f) ให้ Joint หลุด (Detach) ทันที ──
            if (!_isDetached && !_isBroken && impact > collisionDetachImpulse)
            {
                if (showDebugLogs)
                    Debug.Log($"[Stress] {name} joint snapped (detached) due to collision impact: {impact:F1}");

                Detach();

                // ถ้าชนแรงขึ้นมาหน่อย (impact > 10f) ให้พัง (Break) ไปเลย
                if (impact > collisionBreakImpulse)
                {
                    _currentHP = 0f;
                    Break();
                }
                return;
            }

            // ── 1. Impact Damage: If THIS structure is stable, but something hits it hard ──
            if (!_isBroken && impact > impactDamageThreshold) 
            {
                // Convert impulse to direct HP damage
                float damage = (impact - impactDamageThreshold) * impactDamageMultiplier;
                if (damage > 1f)
                {
                    ApplyExternalDamage(damage);
                    
                    if (showDebugLogs)
                        Debug.Log($"[Stress] {name} took {damage:F1} impact damage from {collision.gameObject.name} (Impulse: {impact:F1})");
                }
            }

            // ── 2. Visual/Camera Effects for broken pieces ──
            if (_isBroken && impact > 20f)
            {
                Building.BuildingSystem.Instance?.TriggerCameraShake(
                    Mathf.Clamp(impact * 0.01f, 0.3f, 2f));

                if (unit != null && unit.CurrentMaterial != null)
                {
                    ContactPoint contact = collision.GetContact(0);

                    if (unit.CurrentMaterial.breakVFX != null)
                        Instantiate(unit.CurrentMaterial.breakVFX, contact.point, Quaternion.identity, GetVFXContainer());

                    if (unit.CurrentMaterial.breakSound != null)
                        AudioSource.PlayClipAtPoint(unit.CurrentMaterial.breakSound, contact.point);
                }
                return;
            }

            // Generic camera shake for heavy impacts
            if (impact > 100f)
            {
                Building.BuildingSystem.Instance?.TriggerCameraShake(Mathf.Clamp(impact * 0.005f, 0.2f, 1.5f));
            }
        }

        /// <summary>
        /// ตัวละครยัง "เบียด/ดัน" โครงสร้างค้างอยู่ → ต่ออายุช่วงงดคิดดาเมจจากแรงเรื่อยๆ
        /// (เช่น NPC ยืนจ่อประตูที่กำลังเปิด หรือเดินดันกำแพงต่อเนื่อง)
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            if (_isBroken) return;
            if (collision.collider != null &&
                collision.collider.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null)
            {
                _characterContactTimer = CHARACTER_CONTACT_GRACE;
            }
        }

        /// <summary>
        /// แจ้งว่ามี "ตัวละคร" (คน/NPC) อยู่/เดินใกล้โครงสร้างนี้ → ต่ออายุช่วงงดคิดดาเมจจากแรง (joint/collision)
        /// เรียกจากภายนอก (เช่น DoorController) เพื่อกันประตู/กำแพงพังตอนคนเดินชน
        /// โดยใช้การตรวจจับที่เชื่อถือได้ ไม่ต้องพึ่ง collision event ที่อาจหลุดเฟรม
        /// </summary>
        public void NotifyCharacterContact()
        {
            _characterContactTimer = CHARACTER_CONTACT_GRACE;
        }

        // ประตูเป็นองค์ประกอบ "ใช้งาน" (เปิด/ปิด) ไม่ใช่ตัวรับโหลดโครงสร้าง
        // จึงไม่ควรพังจากแรงที่ joint (เช่น คนเดินดัน) — แต่ยังพังจากภัยพิบัติ/เสียตัวค้ำได้
        private int _isDoorState = -1; // -1=ยังไม่รู้, 0=ไม่ใช่, 1=ประตู
        private bool IsDoor()
        {
            if (_isDoorState < 0)
            {
                var u = GetComponent<Building.StructureUnit>();
                _isDoorState = (u != null && u.Data != null && u.Data.structureType == StructureType.Door) ? 1 : 0;
            }
            return _isDoorState == 1;
        }



        // ────────────────────────────────────────────────────────────────
        // Breaking Logic
        // ────────────────────────────────────────────────────────────────

        // ────────────────────────────────────────────────────────────────
        // Breaking Logic
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Disconnects the structure from the grid/neighbours and enables physics,
        /// but does NOT mark it as "broken" or zero out HP. This allows pieces
        /// to fall and bounce while still being "alive".
        /// </summary>
        private void Detach()
        {
            if (_isDetached) return;
            _isDetached = true;

            // 1. Destroy ALL joints บนตัวนี้
            Joint[] allJoints = GetComponents<Joint>();
            foreach (var j in allJoints) Destroy(j);
            _joint = null;

            // 2. ลบ Joint ของโครงสร้างอื่นที่อ้างถึง Rigidbody ตัวนี้ด้วย
            //    ป้องกันปัญหา "ดึงกลับ" ที่ทำให้ชิ้นส่วนเด้งแปลกๆ
            if (_rb != null)
            {
                Joint[] sceneJoints = FindObjectsByType<Joint>(FindObjectsSortMode.None);
                foreach (var j in sceneJoints)
                {
                    if (j != null && j.connectedBody == _rb)
                        Destroy(j);
                }
            }

            // 3. Enable physics collisions and ensure NOT triggers
            RestorePhysicsCollisions();

            if (_zeroBounceMaterial == null)
            {
                _zeroBounceMaterial = new PhysicsMaterial("DebrisZeroBounce");
                _zeroBounceMaterial.bounciness = 0f;
                _zeroBounceMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
                _zeroBounceMaterial.staticFriction = 0.6f;
                _zeroBounceMaterial.dynamicFriction = 0.6f;
            }

            if (_colliders != null)
            {
                StructureUnit unit = GetComponent<StructureUnit>();
                bool isDoor = unit != null && unit.Data != null && unit.Data.structureType == StructureType.Door;

                foreach (var col in _colliders)
                {
                    if (col != null)
                    {
                        if (!isDoor) col.isTrigger = false;
                        col.material = _zeroBounceMaterial;
                    }
                }
            }

            // 4. Move to Default layer (0) so it collides with the Structure layer
            _originalLayer = gameObject.layer;
            SetLayerRecursive(gameObject, 0);

            // 5. Make Rigidbody dynamic
            if (_rb != null)
            {
                foreach (var meshCol in GetComponentsInChildren<MeshCollider>())
                {
                    meshCol.convex = true;
                }

                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                _rb.WakeUp();

                // (เอา random torque ออก เพื่อไม่ให้ชิ้นส่วนกระเด็นหมุนตอนหลุด — ปล่อยให้ตกตามฟิสิกส์)
            }
        }

        /// <summary>
        /// Immediately breaks this structural element:
        ///  1. Detaches it (if not already)
        ///  2. Marks as broken and zero out HP
        ///  3. Plays break VFX / SFX
        ///  4. Deactivates after a delay (5s)
        /// </summary>
        private void Break()
        {
            if (_isBroken) return;
            _isBroken = true;
            _currentHP = 0f;

            // บันทึกตำแหน่งจุดพัง เพื่อให้สกิล Engineer โชว์ VFX สรุปความเสียหายตอนจบด่าน
            RecentBreakPositions.Add(transform.position);

            // ทำลาย Joint ทั้งหมดเพื่อให้โครงสร้างด้านบนตรวจเจอว่าตัวค้ำหายไป (Cascading Failure)
            Joint[] allJoints = GetComponents<Joint>();
            foreach (var j in allJoints) Destroy(j);
            _joint = null;

            // ลบ Joint ของโครงสร้างอื่นที่อ้างถึง Rigidbody ตัวนี้
            // เพื่อให้ตัวที่ต่ออยู่ Detach → ร่วง → กระแทก → Break → cascade ต่อไปเรื่อยๆ
            if (_rb != null)
            {
                Joint[] sceneJoints = FindObjectsByType<Joint>(FindObjectsSortMode.None);
                foreach (var j in sceneJoints)
                {
                    if (j != null && j.connectedBody == _rb)
                        Destroy(j);
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[StructuralStress] *** BREAK *** {name}", this);
            }

            // Trigger camera shake and sound
            var unit = GetComponent<Building.StructureUnit>();
            float shakeIntensity = 2.0f;
            if (unit != null && unit.Data != null)
            {
                shakeIntensity = unit.Data.breakShakeIntensity;
                if (unit.Data.breakSFX != null)
                    AudioSource.PlayClipAtPoint(unit.Data.breakSFX, transform.position);
            }
            
            Building.BuildingSystem.Instance?.TriggerCameraShake(shakeIntensity);

            // Spawn break VFX แทน Prefab ที่หายไป
            if (unit != null)
            {
                if (unit.CurrentMaterial != null)
                {
                    if (unit.CurrentMaterial.breakSound != null)
                        AudioSource.PlayClipAtPoint(unit.CurrentMaterial.breakSound, transform.position);

                    if (unit.CurrentMaterial.breakVFX != null)
                        Instantiate(unit.CurrentMaterial.breakVFX, transform.position, Quaternion.identity, GetVFXContainer());
                }

                if (unit.Data != null && unit.Data.breakVFX != null)
                {
                    Instantiate(unit.Data.breakVFX, transform.position, transform.rotation, GetVFXContainer());
                }
            }

            // Fire event
            OnBreak?.Invoke(this);

            // คำนวณชั้นสูงสุดใหม่ เพราะอาจจะมีบางชั้นพังลงไป
            Building.BuildingSystem.Instance?.RecalculateMaxFloor();

            // ── ซ่อน Prefab ทันที — ไม่ต้องรอให้ร่วงก่อน ──
            foreach (var col in _colliders)
            {
                if (col != null) col.enabled = false;
            }
            gameObject.SetActive(false);

            // ── REBUILD NAVMESH ──
            // เมื่อโครงสร้างพัง (กำแพงถล่ม/พื้นทะลุ) ให้คำนวณ NavMesh ใหม่เพื่อให้ AI หาทางไปต่อได้
            if (SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating)
            {
                SimulationManager.Instance.RebuildNavMesh();
            }
        }

        private Transform GetVFXContainer()
        {
            var go = GameObject.Find("BreakVFXContainer_Runtime");
            if (go == null)
            {
                go = new GameObject("BreakVFXContainer_Runtime");
            }
            return go.transform;
        }

        /// <summary>
        /// คืน physics collision กับ Structure อื่นทั้งหมดในฉาก
        /// เพื่อให้ชิ้นส่วนที่พังแล้วชนกับของอื่นได้ ไม่ทะลุกัน
        /// </summary>
        private void RestorePhysicsCollisions()
        {
            Collider[] myColliders = GetComponentsInChildren<Collider>(true);
            if (myColliders.Length == 0) return;

            // IgnoreOverlappingCollisions ใน BuildingSystem สั่ง IgnoreCollision(true) กับ
            // Collider ของ Structure ทุกตัวที่วางแล้ว — ต้อง restore ทั้งหมดจึงจะชนได้
            // ใช้ FindObjectsByType<Collider> เพื่อหา Collider ทุกตัวในฉาก (รวม inactive)
            Collider[] allSceneColliders = FindObjectsByType<Collider>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var otherCol in allSceneColliders)
            {
                if (otherCol == null) continue;
                // ข้าม Collider ที่เป็นของตัวเอง
                if (otherCol.transform.root == transform.root) continue;

                foreach (var myCol in myColliders)
                {
                    if (myCol != null)
                        UnityEngine.Physics.IgnoreCollision(myCol, otherCol, false);
                }
            }
        }

        /// <summary>
        /// ปิด Collider + ซ่อน GameObject หลังจาก delay
        /// ใช้แทน SetActive(false) ทันที เพื่อให้เห็นชิ้นส่วนร่วงลงพื้น
        /// </summary>
        private System.Collections.IEnumerator DeactivateAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // ปิด Collider ก่อนซ่อน (cleanup)
            foreach (var col in _colliders)
            {
                if (col != null) col.enabled = false;
            }

            gameObject.SetActive(false);

            // อัพเดท NavMesh อีกครั้งหลังจากซากหายไปแล้ว เพื่อให้ NPC เดินผ่านได้
            if (SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating)
            {
                SimulationManager.Instance.RebuildNavMesh();
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Visual Stress Indicator
        // ────────────────────────────────────────────────────────────────

        public void RefreshVisual()
        {
            UpdateVisualStress();
        }

        private void UpdateVisualStress()
        {
            if (!_hasStressRenderers || stressRenderers == null) return;

            // ถ้าชิ้นส่วนนี้กำลังถูก Highlight (เลือกอยู่) ไม่ต้องอัปเดตสีทับ
            var unit = GetComponent<Simulation.Building.StructureUnit>();
            if (unit != null && unit.IsHighlighted) return;

            // ใช้ _originalBaseHP ในการคำนวณสี เพื่อให้เห็นความเสียหายถาวรจากซอมบี้
            float health = _originalBaseHP > 0 ? Mathf.Clamp01(_currentHP / _originalBaseHP) : 0f;

            // 100% (1.0) -> ใช้สีวัสดุดั้งเดิม (ใส)
            // 50%  (0.5) -> สีส้ม
            // 0%   (0.0) -> สีแดง
            if (!ShowHPVisualsGlobal)
            {
                for (int i = 0; i < stressRenderers.Length; i++)
                {
                    if (stressRenderers[i] != null)
                        stressRenderers[i].material.color = _cachedOriginalColors[i];
                }
                return;
            }

            for (int i = 0; i < stressRenderers.Length; i++)
            {
                if (stressRenderers[i] == null) continue;

                Color original = _cachedOriginalColors[i];
                Color resultColor;

                if (health >= 0.5f)
                {
                    float t = (1.0f - health) / 0.5f; 
                    resultColor = Color.Lerp(original, new Color(1f, 0.5f, 0f), t);
                }
                else
                {
                    float t = (0.5f - health) / 0.5f; 
                    resultColor = Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, t);
                }

                stressRenderers[i].material.color = Color.Lerp(original, resultColor, stressVisualIntensity);
            }
        }

        /// <summary>
        /// คำสั่งสำหรับเปิด/ปิด การแสดงผลสี HP ทั้งหมด (ใช้ควบคุมจาก UI)
        /// </summary>
        public static void SetVisualStatus(bool isActive)
        {
            ShowHPVisualsGlobal = isActive;
            
            // อัปเดตสีของทุกชิ้นทันทีเพื่อให้เห็นผล
            StructuralStress[] allStress = FindObjectsByType<StructuralStress>(FindObjectsSortMode.None);
            foreach (var s in allStress) s.UpdateVisualStress();
        }

        // ────────────────────────────────────────────────────────────────
        // Public API — External Damage / Reset
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Apply direct damage from an external source (e.g. explosions, disasters).
        /// </summary>
        public void ApplyExternalDamage(float amount)
        {
            if (_isBroken || amount <= 0f) return;

            _currentHP -= amount;
            _lowStressTimer = 0f;

            if (_currentHP <= 0f)
            {
                _currentHP = 0f;
                Break();
            }
        }

        /// <summary>
        /// Apply damage that also reduces the maximum HP (permanent damage).
        /// Used by zombies to make structures permanently weaker.
        /// </summary>
        public void ApplyMaxHPDamage(float amount)
        {
            if (_isBroken || amount <= 0f) return;

            // คำนวณสัดส่วนความเสียหายเทียบกับ Max HP เดิมเพื่อลดค่าขีดจำกัดทางกายภาพด้วย
            float damageRatio = (baseHP > 0) ? (amount / baseHP) : 1f;

            // ลดทั้ง Max HP และ Current HP
            baseHP = Mathf.Max(0f, baseHP - amount);
            _currentHP = Mathf.Min(_currentHP, baseHP);
            
            // ลดขีดจำกัดแรงกดและแรงดึง (ทำให้โครงสร้างรับน้ำหนักได้น้อยลงถาวร)
            maxCompression = Mathf.Max(0f, maxCompression * (1f - damageRatio));
            maxTension = Mathf.Max(0f, maxTension * (1f - damageRatio));

            _lowStressTimer = 0f;

            if (baseHP <= 0f || _currentHP <= 0f)
            {
                _currentHP = 0f;
                Break();
            }
        }

        public void InitializeStress(float maxHP, float compression = 1000f, float tension = 1000f)
        {
            baseHP = maxHP;
            _currentHP = maxHP;
            maxCompression = compression;
            maxTension = tension;

            // Cache original values for restart/rewind
            _originalBaseHP = maxHP;
            _originalMaxCompression = maxCompression;
            _originalMaxTension = maxTension;

            _settlementTimer = SETTLEMENT_DURATION;
            _anchorChecked = false;

            // บันทึกสีดั้งเดิมใหม่ทุกครั้งที่มีการ Initialize (เพื่อให้ครอบคลุมชิ้นส่วนที่เพิ่มทีหลังอย่าง DoorFrame)
            stressRenderers = GetComponentsInChildren<Renderer>();

            if (stressRenderers != null && stressRenderers.Length > 0)
            {
                _cachedOriginalColors = new Color[stressRenderers.Length];
                for (int i = 0; i < stressRenderers.Length; i++)
                {
                    if (stressRenderers[i] != null)
                        _cachedOriginalColors[i] = stressRenderers[i].material.color;
                }
                _hasStressRenderers = true;
            }

            UpdateVisualStress();
        }

        /// <summary>
        /// Fully reset HP to baseHP (e.g. after a repair action).
        /// Does nothing if already broken.
        /// </summary>
        public void RepairFull()
        {
            if (_isBroken) return;
            _currentHP = baseHP;
            UpdateVisualStress();
        }

        /// <summary>
        /// Completely resets this component to its initial state.
        /// Used by SimulationManager to rewind after simulation stops.
        /// </summary>
        public void ResetFull()
        {
            // หยุด Coroutine ที่ค้างอยู่ (เช่น DeactivateAfterDelay) เมื่อ Rewind
            StopAllCoroutines();

            _isBroken = false;
            _isDetached = false;

            // Restore original structural properties
            baseHP = _originalBaseHP;
            _currentHP = _originalBaseHP;
            maxCompression = _originalMaxCompression;
            maxTension = _originalMaxTension;

            _lowStressTimer = 0f;
            _settlementTimer = SETTLEMENT_DURATION;
            _anchorChecked = false;
            LastForceMagnitude = 0f;
            LastTorqueMagnitude = 0f;

            // Restore original layer
            if (_originalLayer != 0)
                SetLayerRecursive(gameObject, _originalLayer);

            // Re-cache colliders (in case array was stale)
            _colliders = GetComponentsInChildren<Collider>();

            // Re-enable all colliders
            foreach (var col in _colliders)
            {
                if (col != null) col.enabled = true;
            }

            // Re-find joint (will be re-created externally by SimulationManager)
            _joint = GetComponent<Joint>();

            UpdateVisualStress();
        }

        /// <summary>
        /// ตรวจว่าชิ้นนี้วางอยู่บน "พื้น/สภาพแวดล้อม" (ไม่ใช่โครงสร้างอื่น) โดยตรงหรือไม่
        /// ใช้ตัดสินว่าเมื่อ Joint หลุดหมดแล้วควรปล่อยร่วง (ไม่ใช่พื้น) หรือยืนนิ่ง (เป็นพื้น)
        /// </summary>
        private void CaptureAnchorState()
        {
            _anchorChecked = true;
            _hadGroundAnchor = false;

            // ground anchor = มี Joint ที่ connectedBody เป็น null อยู่แล้วตั้งแต่เริ่มจำลอง
            // (สร้างโดย AttachJoint ตอนวางบนพื้น/ground) — ชิ้นพวกนี้ยึดกับโลกโดยตั้งใจ ไม่ต้องสั่งร่วง
            // ส่วนชิ้นที่ยึดกับโครงสร้างอื่น ถ้าตัวค้ำถูกทำลาย connectedBody จะกลายเป็น null ทีหลัง → จึงสั่งร่วงได้
            Joint[] joints = GetComponents<Joint>();
            foreach (var j in joints)
            {
                if (j != null && j.connectedBody == null) { _hadGroundAnchor = true; return; }
            }
        }

        private void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

#if UNITY_EDITOR
        // ────────────────────────────────────────────────────────────────
        // Editor Gizmos — visualise stress state in Scene view
        // ────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // Draw a small sphere coloured by health
            Gizmos.color = Color.Lerp(Color.red, Color.green, HealthRatio);
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Draw force vector
            if (_joint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, _joint.currentForce.normalized * 0.5f);
            }
        }
#endif
    }
}
