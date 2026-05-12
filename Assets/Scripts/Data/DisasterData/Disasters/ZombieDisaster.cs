using UnityEngine;
using System.Collections.Generic;
using Simulation.Building;

namespace Simulation.Mission
{
    /// <summary>
    /// Zombie Disaster — Spawn ซอมบี้ทุกประเภทตามจำนวนที่กำหนด เป็น wave
    /// รองรับ: Zombie ธรรมดา, DiggerZombie, BalloonZombie
    /// </summary>
    public class ZombieDisaster : DisasterBase
    {
        private float _waveTimer;
        private int _currentWave;
        private int _totalWaves;

        // จำนวนต่อ wave ของแต่ละประเภท
        private int _normalPerWave;
        private int _diggerPerWave;
        private int _balloonPerWave;

        // จำนวนที่ spawn ไปแล้ว
        private int _normalSpawned;
        private int _diggerSpawned;
        private int _balloonSpawned;

        public ZombieDisaster(DisasterData data, MonoBehaviour runner) : base(data, runner) { }

        protected override void OnStart()
        {
            Debug.Log("<color=red> Zombie Apocalypse Started!</color>");

            _totalWaves = Mathf.Max(1, data.zombieWaveCount);
            _normalPerWave = Mathf.Max(0, Mathf.CeilToInt((float)data.zombieSpawnCount / _totalWaves));
            _diggerPerWave = Mathf.Max(0, Mathf.CeilToInt((float)data.diggerZombieCount / _totalWaves));
            _balloonPerWave = Mathf.Max(0, Mathf.CeilToInt((float)data.balloonZombieCount / _totalWaves));
            _currentWave = 0;
            _waveTimer = 0f;
            _normalSpawned = 0;
            _diggerSpawned = 0;
            _balloonSpawned = 0;

            // Spawn wave แรกทันที
            SpawnWave();
        }

        protected override void OnUpdate(float dt)
        {
            if (_currentWave >= _totalWaves) return;

            _waveTimer += dt;

            float interval = data.duration / Mathf.Max(1, _totalWaves);
            if (_waveTimer >= interval)
            {
                _waveTimer = 0f;
                SpawnWave();
            }
        }

        private void SpawnWave()
        {
            if (_currentWave >= _totalWaves) return;
            _currentWave++;

            // Zombie ธรรมดา
            int normalCount = Mathf.Min(_normalPerWave, data.zombieSpawnCount - _normalSpawned);
            for (int i = 0; i < normalCount; i++)
            {
                _normalSpawned++;
                SpawnZombieOfType(data.zombiePrefab, $"zombie{_normalSpawned}", typeof(ZombieAI));
            }

            // Digger Zombie
            int diggerCount = Mathf.Min(_diggerPerWave, data.diggerZombieCount - _diggerSpawned);
            for (int i = 0; i < diggerCount; i++)
            {
                _diggerSpawned++;
                SpawnZombieOfType(data.diggerZombiePrefab, $"DiggerZombie{_diggerSpawned}", typeof(DiggerZombieAI));
            }

            // Balloon Zombie
            int balloonCount = Mathf.Min(_balloonPerWave, data.balloonZombieCount - _balloonSpawned);
            for (int i = 0; i < balloonCount; i++)
            {
                _balloonSpawned++;
                SpawnZombieOfType(data.balloonZombiePrefab, $"BalloonZombie{_balloonSpawned}", typeof(BalloonZombieAI));
            }

            int total = normalCount + diggerCount + balloonCount;
            Debug.Log($"<color=red> Wave {_currentWave}/{_totalWaves}: {normalCount} normal, {diggerCount} digger, {balloonCount} balloon</color>");
        }

        private void SpawnZombieOfType(GameObject prefab, string typeName, System.Type aiType)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[ZombieDisaster] {typeName} prefab is null! Skipping.");
                return;
            }

            Vector3 finalPos = data.centerOffset;
            bool foundPos = false;
            
            // ใช้ LayerMask "Ground" (อ้างอิงจาก BuildingSystem)
            int groundMask = LayerMask.GetMask("Ground");
            if (groundMask == 0) groundMask = LayerMask.GetMask("Default"); // Fallback ถ้าไม่มี Layer Ground

            // พยายามสุ่มหาตำแหน่งที่เหมาะสม (นอก Grid และอยู่บน Ground)
            for (int i = 0; i < 30; i++)
            {
                // สุ่มตำแหน่งในวงกลมรอบจุดศูนย์กลางภัยพิบัติ
                Vector2 randomCircle = Random.insideUnitCircle * (data.radius > 0 ? data.radius : 20f);
                Vector3 randomPos = data.centerOffset + new Vector3(randomCircle.x, 50f, randomCircle.y);

                // Raycast ลงมาหาพื้น Ground
                if (UnityEngine.Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 100f, groundMask))
                {
                    Vector3 hitPoint = hit.point;

                    // ตรวจสอบว่าอยู่นอกเขต Grid (พื้นที่ก่อสร้าง) หรือไม่
                    if (BuildingSystem.Instance != null)
                    {
                        float gridSize = BuildingSystem.Instance.GetGridSize;
                        float limitX = (BuildingSystem.Instance.GridColumns * gridSize) * 0.5f;
                        float limitZ = (BuildingSystem.Instance.GridRows * gridSize) * 0.5f;

                        // ถ้าจุดที่ Raycast โดน อยู่ในขอบเขต Grid ให้ข้ามไปสุ่มใหม่
                        if (hitPoint.x > -limitX && hitPoint.x < limitX &&
                            hitPoint.z > -limitZ && hitPoint.z < limitZ)
                        {
                            continue;
                        }
                    }

                    finalPos = hitPoint;
                    foundPos = true;
                    break;
                }
            }

            // ถ้าสุ่มไม่สำเร็จใน 30 รอบ (อาจจะเพราะรัศมีแคบไปหรือ Grid ใหญ่มาก) ให้ขยับออกไปไกลขึ้นเป็น fallback
            if (!foundPos)
            {
                Vector2 fallbackCircle = Random.insideUnitCircle.normalized * (data.radius > 0 ? data.radius * 1.5f : 30f);
                Vector3 fallbackPos = data.centerOffset + new Vector3(fallbackCircle.x, 0, fallbackCircle.y);
                
                if (UnityEngine.Physics.Raycast(fallbackPos + Vector3.up * 50f, Vector3.down, out RaycastHit fallbackHit, 100f, groundMask))
                {
                    finalPos = fallbackHit.point;
                }
                else
                {
                    finalPos = fallbackPos;
                }
            }

            // เช็ค NavMesh (สำหรับ Zombie ธรรมดา) เพื่อความแม่นยำในการเดิน
            if (aiType == typeof(ZombieAI))
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(finalPos, out UnityEngine.AI.NavMeshHit navHit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    finalPos = navHit.position;
                }
            }

            GameObject zombie = Object.Instantiate(prefab, finalPos, Quaternion.identity);
            zombie.name = typeName;

            // เพิ่ม AI component ถ้า Prefab ไม่ได้ติดมา
            if (zombie.GetComponent(aiType) == null)
            {
                zombie.AddComponent(aiType);
            }
        }
    }
}
