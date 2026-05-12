using UnityEngine;
using System.Collections.Generic;
using Simulation.Character;

namespace Simulation.Building
{
    /// <summary>
    /// กับดักหนาม — ทำดาเมจใส่ซอมบี้ (หรือใครก็ตาม) ที่มาเหยียบ
    /// ใช้ per-target cooldown เพื่อไม่ให้ตี target เดียวกันถี่เกินไป
    /// </summary>
    public class SpikeTrap : MonoBehaviour
    {
        public float damage = 25f;
        public float damageInterval = 1f;
        public float personDamageRatio = 0.1f;

        // เก็บ cooldown ต่อ target แต่ละตัว
        private Dictionary<int, float> _cooldowns = new Dictionary<int, float>();

        private void Update()
        {
            // อัปเดต cooldowns
            List<int> expired = null;
            var keys = new List<int>(_cooldowns.Keys);
            foreach (var key in keys)
            {
                _cooldowns[key] -= Time.deltaTime;
                if (_cooldowns[key] <= -5f) // ลบ entry ที่หมดอายุนานแล้ว
                {
                    if (expired == null) expired = new List<int>();
                    expired.Add(key);
                }
            }
            if (expired != null)
            {
                foreach (var key in expired) _cooldowns.Remove(key);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            int id = other.gameObject.GetInstanceID();

            // เช็ค cooldown
            if (_cooldowns.TryGetValue(id, out float remaining) && remaining > 0f)
                return;

            // ตั้ง cooldown ใหม่
            _cooldowns[id] = damageInterval;

            // เช็คว่าเป็น ZombieAI ประเภทต่างๆ หรือไม่
            var zombie = other.GetComponentInParent<Simulation.Mission.ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage);
                return;
            }

            var balloonZombie = other.GetComponentInParent<Simulation.Mission.BalloonZombieAI>();
            if (balloonZombie != null)
            {
                balloonZombie.TakeDamage(damage);
                return;
            }

            var diggerZombie = other.GetComponentInParent<Simulation.Mission.DiggerZombieAI>();
            if (diggerZombie != null)
            {
                diggerZombie.TakeDamage(damage);
                return;
            }

            // ถ้าเป็นคนปกติ ดาเมจน้อยลง
            var person = other.GetComponentInParent<PersonAI>();
            if (person != null)
            {
                person.TakeDamage(damage * personDamageRatio);
            }
        }
    }
}
