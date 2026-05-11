using UnityEngine;
using System.Collections.Generic;

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
            Debug.Log("<color=red>🧟 Zombie Apocalypse Started!</color>");

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
                SpawnZombieOfType(data.zombiePrefab, "Zombie", typeof(ZombieAI));
                _normalSpawned++;
            }

            // Digger Zombie
            int diggerCount = Mathf.Min(_diggerPerWave, data.diggerZombieCount - _diggerSpawned);
            for (int i = 0; i < diggerCount; i++)
            {
                SpawnZombieOfType(data.diggerZombiePrefab, "DiggerZombie", typeof(DiggerZombieAI));
                _diggerSpawned++;
            }

            // Balloon Zombie
            int balloonCount = Mathf.Min(_balloonPerWave, data.balloonZombieCount - _balloonSpawned);
            for (int i = 0; i < balloonCount; i++)
            {
                SpawnZombieOfType(data.balloonZombiePrefab, "BalloonZombie", typeof(BalloonZombieAI));
                _balloonSpawned++;
            }

            int total = normalCount + diggerCount + balloonCount;
            Debug.Log($"<color=red>🧟 Wave {_currentWave}/{_totalWaves}: {normalCount} normal, {diggerCount} digger, {balloonCount} balloon</color>");
        }

        private void SpawnZombieOfType(GameObject prefab, string typeName, System.Type aiType)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[ZombieDisaster] {typeName} prefab is null! Skipping.");
                return;
            }

            // หาตำแหน่งสุ่มรอบๆ centerOffset
            Vector2 randomCircle = Random.insideUnitCircle * (data.radius > 0 ? data.radius : 20f);
            Vector3 spawnPos = data.centerOffset + new Vector3(randomCircle.x, 0, randomCircle.y);

            // เช็ค NavMesh (สำหรับ Zombie ธรรมดา)
            Vector3 finalPos = spawnPos;
            if (aiType == typeof(ZombieAI))
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    finalPos = hit.position;
                }
            }

            GameObject zombie = Object.Instantiate(prefab, finalPos, Quaternion.identity);
            zombie.name = $"{typeName}_{_currentWave}_{Random.Range(0, 1000)}";

            // เพิ่ม AI component ถ้า Prefab ไม่ได้ติดมา
            if (zombie.GetComponent(aiType) == null)
            {
                zombie.AddComponent(aiType);
            }
        }
    }
}
