using UnityEngine;
using Simulation.Core;

namespace Simulation.UI
{
    /// <summary>
    /// SceneButton — ตัวช่วยเปลี่ยนซีนสำหรับปุ่ม UI
    ///
    /// วิธีใช้:
    ///   1) แปะคอมโพเนนต์นี้บน GameObject ของปุ่ม (Button)
    ///   2) เลือก action (และใส่ targetScene ถ้าเลือก LoadByName)
    ///   3) ที่ Button > OnClick() กด + แล้วลาก GameObject เดียวกันนี้เข้าไป เลือกเมธอด SceneButton.Go()
    ///
    /// หรือเรียกจากโค้ดอื่น: sceneButton.LoadScene("Lv.1");
    /// </summary>
    public class SceneButton : MonoBehaviour
    {
        public enum Action
        {
            LoadByName,     // โหลดซีนตามชื่อใน targetScene
            MainMenu,       // ไปเมนูหลัก
            LevelSelect,    // ไปหน้าเลือกด่าน
            ReloadCurrent,  // โหลดซีนปัจจุบันใหม่
            Quit            // ออกจากเกม
        }

        [Tooltip("การกระทำเมื่อกดปุ่ม")]
        [SerializeField] private Action action = Action.LoadByName;

        [Tooltip("ชื่อซีนปลายทาง (ใช้เมื่อ action = LoadByName) ต้องตรงกับ Build Settings")]
        [SerializeField] private string targetScene = "";

        /// <summary>ผูกเมธอดนี้กับ Button.onClick ใน Inspector</summary>
        public void Go()
        {
            var mgr = GameSceneManager.Instance; // สร้างให้อัตโนมัติถ้ายังไม่มี

            switch (action)
            {
                case Action.MainMenu:      mgr.LoadMainMenu();        break;
                case Action.LevelSelect:   mgr.LoadLevelSelect();     break;
                case Action.ReloadCurrent: mgr.ReloadCurrentScene();  break;
                case Action.Quit:          mgr.QuitGame();            break;
                default:                   mgr.LoadScene(targetScene); break;
            }
        }

        /// <summary>เรียกจากโค้ด/ปุ่มอื่นเพื่อโหลดซีนตามชื่อโดยตรง</summary>
        public void LoadScene(string sceneName)
        {
            GameSceneManager.Instance.LoadScene(sceneName);
        }
    }
}
