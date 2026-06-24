using UnityEngine;

namespace Simulation.Core
{
    /// <summary>
    /// ระบบเซฟความคืบหน้า (ดาวที่ได้ + ด่านที่ปลดล็อก) แบบถาวรด้วย PlayerPrefs
    /// PlayerPrefs จะคงอยู่หลังปิดเกม/ในเกมที่ build แล้ว ดังนั้นข้อมูลดาวจะไม่หาย
    ///
    /// ใช้ "ชื่อด่าน" (MissionData.missionName) เป็นรหัสประจำด่าน (levelId)
    /// </summary>
    public static class ProgressSave
    {
        private const string StarsPrefix  = "NSC_stars_";
        private const string UnlockPrefix = "NSC_unlock_";

        // ────────────────────────────── Stars ──────────────────────────────

        /// <summary>ดาวที่ได้ของด่านนี้ (0–3)</summary>
        public static int GetStars(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return 0;
            return PlayerPrefs.GetInt(StarsPrefix + levelId, 0);
        }

        /// <summary>บันทึกดาว — เก็บเฉพาะค่าที่ "ดีที่สุด" (ไม่ลดลงถ้าเล่นซ้ำได้น้อยกว่าเดิม)</summary>
        public static void SetStars(string levelId, int stars)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            stars = Mathf.Clamp(stars, 0, 3);
            if (stars > GetStars(levelId))
            {
                PlayerPrefs.SetInt(StarsPrefix + levelId, stars);
                PlayerPrefs.Save();
            }
        }

        // ───────────────────────────── Unlock ──────────────────────────────

        /// <summary>ด่านนี้ถูกปลดล็อกแล้วหรือยัง (ค่าเริ่มต้น = ล็อก)</summary>
        public static bool IsUnlocked(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return false;
            return PlayerPrefs.GetInt(UnlockPrefix + levelId, 0) == 1;
        }

        /// <summary>ตั้งสถานะปลดล็อกของด่าน</summary>
        public static void SetUnlocked(string levelId, bool unlocked)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            PlayerPrefs.SetInt(UnlockPrefix + levelId, unlocked ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ───────────────────────────── Utility ─────────────────────────────

        /// <summary>ล้างความคืบหน้าของด่านหนึ่ง (ดาว + ปลดล็อก) — ใช้ตอนทดสอบ</summary>
        public static void ResetLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            PlayerPrefs.DeleteKey(StarsPrefix + levelId);
            PlayerPrefs.DeleteKey(UnlockPrefix + levelId);
            PlayerPrefs.Save();
        }
    }
}
