using UnityEngine;
using System.Collections.Generic;
using Simulation.Mission;
using Simulation.Physics;

namespace Simulation.Building
{
    /// <summary>
    /// Balloon Launcher — ค้นหา Balloon Zombie บินได้ในระยะ หันไปหา และยิงเพื่อให้ร่วงลงมา
    /// </summary>
    public class BalloonLauncher : MonoBehaviour
    {
        [Header("Settings")]
        public float shootRange = 20f;
        public float shootCooldown = 1.5f;
        
        [Header("Rotation")]
        [Tooltip("ส่วนหัวที่ใช้หมุนหันหาเป้าหมาย (ถ้าไม่มีจะหมุนทั้งตัว)")]
        public Transform pivotTransform;
        public float rotationSpeed = 5f;

        [Header("Visuals & Audio")]
        [Tooltip("จุดที่กระสุนออกจากปากกระบอก (ถ้าไม่มีจะกะเอาจากตำแหน่งกลางบน)")]
        public Transform muzzlePoint;
        public GameObject muzzleFlashPrefab;
        public AudioClip shootSound;
        public Color beamColor = new Color(1f, 0.3f, 0f, 0.8f);

        private float _shootTimer;

        private void Start()
        {
            if (pivotTransform == null)
            {
                // ลองหาชิ้นส่วนชื่อ Pivot หรือ Head ในลูก
                pivotTransform = transform.Find("Pivot");
                if (pivotTransform == null) pivotTransform = transform.Find("Head");
                if (pivotTransform == null) pivotTransform = transform;
            }

            if (muzzlePoint == null)
            {
                muzzlePoint = transform.Find("Muzzle");
                if (muzzlePoint == null && pivotTransform != null) muzzlePoint = pivotTransform.Find("Muzzle");
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
                // หันส่วนยิงไปหาเป้าหมาย
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

        private void AimAt(Vector3 targetPosition)
        {
            if (pivotTransform == null) return;

            // หันไปหาเป้าหมาย (เน้นแกน Y และ X เล็กน้อย)
            Vector3 direction = (targetPosition - pivotTransform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            
            // หมุนแบบ Smooth
            pivotTransform.rotation = Quaternion.Slerp(pivotTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }

        private void Shoot(BalloonZombieAI target)
        {
            Vector3 startPos = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 1.2f;
            Vector3 targetPos = target.transform.position + Vector3.up * 0.5f; // เล็งตรงตัว (หรือตัวบอลลูน)

            // 1. Muzzle flash
            if (muzzleFlashPrefab != null)
            {
                Instantiate(muzzleFlashPrefab, startPos, Quaternion.LookRotation(targetPos - startPos));
            }

            // 2. Play Sound
            if (shootSound != null)
            {
                AudioSource.PlayClipAtPoint(shootSound, startPos);
            }
            else
            {
                // Play a default high-pitched beep/pop if no sound is assigned
                var tempGO = new GameObject("TempAudio");
                tempGO.transform.position = startPos;
                var source = tempGO.AddComponent<AudioSource>();
                source.spatialBlend = 1.0f;
                source.volume = 0.5f;
                source.PlayOneShot(CreateDefaultShootClip());
                Destroy(tempGO, 1.0f);
            }

            // 3. Create laser tracer / beam visual
            CreateTracerBeam(startPos, targetPos);

            // 4. Pop balloon
            target.PopBalloon();

            Debug.Log($"<color=cyan>[BalloonLauncher]</color> Shot at Balloon Zombie: {target.name}");
        }

        private void CreateTracerBeam(Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject("LaserTracer");
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            
            line.startWidth = 0.15f;
            line.endWidth = 0.05f;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            
            // สร้าง Material สีส้ม/เหลืองแบบเรืองแสง
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null) lineShader = Shader.Find("Legacy Shaders/Particles/Additive");
            Material mat = new Material(lineShader != null ? lineShader : Shader.Find("Hidden/Internal-Colored"));
            mat.color = beamColor;
            line.material = mat;

            // ทำลายตัวเองใน 0.15 วินาที โดยให้ค่อยๆ หรี่จาง
            StartCoroutine(FadeAndDestroyLine(lineObj, line));
        }

        private System.Collections.IEnumerator FadeAndDestroyLine(GameObject obj, LineRenderer line)
        {
            float duration = 0.15f;
            float elapsed = 0f;
            Color originalColor = beamColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pct = elapsed / duration;
                Color nextColor = originalColor;
                nextColor.a = Mathf.Lerp(originalColor.a, 0f, pct);
                if (line != null)
                {
                    line.material.color = nextColor;
                    line.startWidth = Mathf.Lerp(0.15f, 0f, pct);
                }
                yield return null;
            }

            Destroy(obj);
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
}
