using UnityEngine;
using System.Collections.Generic;
using Simulation.Mission;
using Simulation.Physics;

namespace Simulation.Building
{
    /// <summary>
    /// Balloon Launcher (ธนู) — ค้นหา Balloon Zombie ในระยะ, หันหา, และยิงลูกธนูเพื่อให้ร่วงลงมา
    /// Pivot แยก 2 แกน: HorizontalPivot (Y) หมุนซ้ายขวา, VerticalPivot (X) ก้มเงย
    /// </summary>
    public class BalloonLauncher : MonoBehaviour
    {
        [Header("Settings")]
        public float shootRange = 20f;
        public float shootCooldown = 1.5f;
        
        [Header("Rotation — Dual Pivot")]
        [Tooltip("Pivot แนวนอน: หมุนแกน Y (ซ้าย/ขวา) — Auto-find 'HorizontalPivot' ถ้าไม่ assign")]
        public Transform horizontalPivot;
        [Tooltip("Pivot แนวตั้ง: หมุนแกน Z (ก้ม/เงย) — Auto-find 'VerticalPivot' ถ้าไม่ assign")]
        public Transform verticalPivot;
        public float rotationSpeed = 5f;

        [Header("Arrow Projectile")]
        [Tooltip("Prefab ลูกธนู (ถ้าไม่มีจะสร้างแบบ Procedural)")]
        public GameObject arrowPrefab;
        [Tooltip("ความเร็วลูกธนู (m/s)")]
        public float arrowSpeed = 30f;

        [Header("Visuals & Audio")]
        [Tooltip("จุดที่ลูกธนูออก (ถ้าไม่มีจะกะจากตำแหน่งกลางบน)")]
        public Transform muzzlePoint;
        public GameObject muzzleFlashPrefab;
        public AudioClip shootSound;

        private float _shootTimer;

        private void Start()
        {
            // Auto-find HorizontalPivot
            if (horizontalPivot == null)
            {
                horizontalPivot = transform.Find("HorizontalPivot");
                if (horizontalPivot == null) horizontalPivot = transform.Find("Pivot");
                if (horizontalPivot == null) horizontalPivot = transform;
            }

            // Auto-find VerticalPivot (ลูกของ HorizontalPivot)
            if (verticalPivot == null)
            {
                verticalPivot = horizontalPivot.Find("VerticalPivot");
                if (verticalPivot == null) verticalPivot = horizontalPivot.Find("Pitch");
                // ถ้าไม่มี VerticalPivot แยก ก็ใช้ HorizontalPivot ตัวเดียวทำทั้ง 2 แกน
                if (verticalPivot == null) verticalPivot = horizontalPivot;
            }

            // Auto-find MuzzlePoint
            if (muzzlePoint == null)
            {
                muzzlePoint = transform.Find("Muzzle");
                if (muzzlePoint == null && verticalPivot != null) muzzlePoint = verticalPivot.Find("Muzzle");
            }
        }

        private void Update()
        {
            // ทำงานเฉพาะตอนจำลองการต่อสู้ (Simulation ทำงาน)
            if (SimulationManager.Instance == null || !SimulationManager.Instance.IsSimulating)
            {
                return;
            }

            _shootTimer -= Time.deltaTime;

            // ค้นหา Balloon Zombie ที่ยังไม่ร่วงและยังไม่ตาย
            BalloonZombieAI target = FindClosestTarget();

            if (target != null)
            {
                // หันส่วนยิงไปหาเป้าหมาย (แยก 2 แกน)
                AimAt(target.transform.position);

                // ถ้าระยะเวลา Cooldown หมดแล้ว ให้ยิง!
                if (_shootTimer <= 0f)
                {
                    Shoot(target);
                    _shootTimer = shootCooldown;
                }
            }
        }

        private BalloonZombieAI FindClosestTarget()
        {
            var zombies = Object.FindObjectsByType<BalloonZombieAI>(FindObjectsSortMode.None);
            BalloonZombieAI closest = null;
            float minDist = float.MaxValue;

            Vector3 myPos = transform.position;

            foreach (var zombie in zombies)
            {
                if (zombie == null || zombie.IsDead || zombie.HasLanded) continue;

                float dist = Vector3.Distance(myPos, zombie.transform.position);
                if (dist <= shootRange && dist < minDist)
                {
                    minDist = dist;
                    closest = zombie;
                }
            }

            return closest;
        }

        /// <summary>
        /// หันไปหาเป้าหมายด้วย Dual Pivot:
        /// 1. HorizontalPivot หมุนแกน Y (Yaw ซ้ายขวา)
        /// 2. VerticalPivot หมุนแกน X (Pitch ก้มเงย)
        /// </summary>
        private void AimAt(Vector3 targetPosition)
        {
            float smoothFactor = rotationSpeed * Time.deltaTime;

            // ── 1. Horizontal Pivot (Yaw — หมุนซ้ายขวา) ──
            if (horizontalPivot != null)
            {
                Vector3 directionH = targetPosition - horizontalPivot.position;
                directionH.y = 0f; // ล็อกแนวราบ ไม่ให้เอียง
                if (directionH.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotH = Quaternion.LookRotation(directionH.normalized);
                    horizontalPivot.rotation = Quaternion.Slerp(horizontalPivot.rotation, targetRotH, smoothFactor);
                }
            }

            // ── 2. Vertical Pivot (Pitch — ก้มเงย) ──
            if (verticalPivot != null)
            {
                Vector3 directionV = targetPosition - verticalPivot.position;
                if (directionV.sqrMagnitude > 0.001f)
                {
                    // คำนวณมุม Pitch (ก้ม/เงย) ในแกน Local Z ของ VerticalPivot
                    Vector3 localDir = horizontalPivot.InverseTransformDirection(directionV.normalized);
                    float pitch = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;

                    // จำกัดมุมก้มเงย (-80 ถึง +80)
                    pitch = Mathf.Clamp(pitch, -80f, 80f);

                    Quaternion targetRotV = Quaternion.Euler(0f, 0f, pitch);
                    verticalPivot.localRotation = Quaternion.Slerp(verticalPivot.localRotation, targetRotV, smoothFactor);
                }
            }
        }

        private void Shoot(BalloonZombieAI target)
        {
            Vector3 startPos = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 1.2f;
            Vector3 direction = (target.transform.position - startPos).normalized;

            // 1. Muzzle flash
            if (muzzleFlashPrefab != null)
            {
                Instantiate(muzzleFlashPrefab, startPos, Quaternion.LookRotation(direction));
            }

            // 2. Play Sound
            if (shootSound != null)
            {
                AudioSource.PlayClipAtPoint(shootSound, startPos);
            }
            else
            {
                // เล่นเสียงสังเคราะห์ถ้าไม่มีเสียงกำหนด
                var tempGO = new GameObject("TempAudio");
                tempGO.transform.position = startPos;
                var source = tempGO.AddComponent<AudioSource>();
                source.spatialBlend = 1.0f;
                source.volume = 0.5f;
                source.PlayOneShot(CreateDefaultShootClip());
                Destroy(tempGO, 1.0f);
            }

            // 3. สร้างลูกธนูยิงไปหาเป้าหมาย
            SpawnArrow(startPos, direction, target);

            Debug.Log($"<color=cyan>[BalloonLauncher]</color> Shot arrow at Balloon Zombie: {target.name}");
        }

        private void SpawnArrow(Vector3 startPos, Vector3 direction, BalloonZombieAI target)
        {
            GameObject arrowObj;
            if (arrowPrefab != null)
            {
                arrowObj = Instantiate(arrowPrefab, startPos, Quaternion.LookRotation(direction));
            }
            else
            {
                // สร้างลูกธนูแบบ Procedural
                arrowObj = CreateProceduralArrow(startPos, direction);
            }

            // ติด ArrowProjectile script เพื่อควบคุมการบิน
            ArrowProjectile projectile = arrowObj.AddComponent<ArrowProjectile>();
            projectile.Initialize(target, arrowSpeed);
        }

        /// <summary>
        /// สร้างลูกธนูจำลองจาก Primitive (Cylinder ลำตัว + ปลายแหลม)
        /// </summary>
        private GameObject CreateProceduralArrow(Vector3 position, Vector3 direction)
        {
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.position = position;
            arrow.transform.rotation = Quaternion.LookRotation(direction);

            // ลำตัวธนู (Cylinder ยาวผอม)
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(arrow.transform);
            shaft.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Cylinder ปกติยืนตั้ง → หมุนให้นอนไปข้างหน้า
            shaft.transform.localScale = new Vector3(0.04f, 0.3f, 0.04f);
            
            // ลบ Collider ของ Shaft
            var shaftCol = shaft.GetComponent<Collider>();
            if (shaftCol != null) DestroyImmediate(shaftCol);

            // ทำสีลำตัว
            var shaftRenderer = shaft.GetComponent<Renderer>();
            if (shaftRenderer != null)
            {
                Material shaftMat = new Material(Shader.Find("Standard"));
                shaftMat.color = new Color(0.55f, 0.35f, 0.15f); // สีไม้
                shaftRenderer.material = shaftMat;
            }

            // ปลายธนู (Cube เล็กเฉียงเป็นหัวลูกศร)
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "Tip";
            tip.transform.SetParent(arrow.transform);
            tip.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tip.transform.localScale = new Vector3(0.08f, 0.08f, 0.02f);

            // ลบ Collider ของ Tip
            var tipCol = tip.GetComponent<Collider>();
            if (tipCol != null) DestroyImmediate(tipCol);

            // ทำสีหัวลูกศร
            var tipRenderer = tip.GetComponent<Renderer>();
            if (tipRenderer != null)
            {
                Material tipMat = new Material(Shader.Find("Standard"));
                tipMat.color = new Color(0.4f, 0.4f, 0.45f); // สีเหล็ก
                tipMat.SetFloat("_Metallic", 0.8f);
                tipRenderer.material = tipMat;
            }

            return arrow;
        }

        private AudioClip CreateDefaultShootClip()
        {
            // สร้างเสียงป๊อปสังเคราะห์สั้นๆ (Beep sound) เพื่อให้ใช้งานได้เลยแบบไม่ต้องมีไฟล์เสียง
            int samplerate = 44100;
            float frequency = 800f;
            AudioClip myClip = AudioClip.Create("PopSound", samplerate, samplerate / 10, 1, false);
            float[] samples = new float[samplerate / 10];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samplerate;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * (1f - (float)i / samples.Length);
            }
            myClip.SetData(samples, 0);
            return myClip;
        }
    }

    /// <summary>
    /// ลูกธนูที่บินไปหาเป้าหมาย (BalloonZombieAI)
    /// เมื่อถึงหรือเป้าตาย → Pop Balloon + ทำลายตัวเอง
    /// </summary>
    public class ArrowProjectile : MonoBehaviour
    {
        private BalloonZombieAI _target;
        private float _speed;
        private float _lifetime;
        private const float MAX_LIFETIME = 5f;
        private const float HIT_DISTANCE = 1.0f;

        public void Initialize(BalloonZombieAI target, float speed)
        {
            _target = target;
            _speed = speed;
            _lifetime = 0f;
        }

        private void Update()
        {
            _lifetime += Time.deltaTime;

            // ถ้าเป้าตายหรือหายไป หรือเกินเวลา → ทำลาย
            if (_target == null || _lifetime > MAX_LIFETIME)
            {
                Destroy(gameObject);
                return;
            }

            // บินตรงไปหาเป้าหมาย
            Vector3 targetPos = _target.transform.position;
            Vector3 direction = (targetPos - transform.position).normalized;

            // หมุนหน้าไปทิศที่บิน
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // เคลื่อนที่
            transform.position += direction * _speed * Time.deltaTime;

            // เช็คว่าถึงเป้าหมายหรือยัง
            float dist = Vector3.Distance(transform.position, targetPos);
            if (dist <= HIT_DISTANCE)
            {
                // โดนแล้ว!
                if (_target != null && !_target.IsDead && !_target.HasLanded)
                {
                    _target.PopBalloon();
                }
                Destroy(gameObject);
            }
        }
    }
}
