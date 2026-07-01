using UnityEngine;
using System;
using System.Collections.Generic;

namespace Simulation.UI
{
    /// <summary>
    /// GameSettings - ศูนย์กลางจัดการค่า Settings ทั้งหมดของเกม
    /// บันทึก/โหลดผ่าน PlayerPrefs และ Apply ค่าจริงไปยังระบบต่างๆ
    /// </summary>
    public static class GameSettings
    {
        // ─────────────────────────────────────────────
        //  Events
        // ─────────────────────────────────────────────

        /// <summary>
        /// แจ้งเตือนเมื่อค่า Settings ใดๆ เปลี่ยนแปลง
        /// </summary>
        public static event Action OnSettingsChanged;

        // ─────────────────────────────────────────────
        //  PlayerPrefs Keys
        // ─────────────────────────────────────────────

        private const string KEY_LANGUAGE = "Settings_Language";
        private const string KEY_RESOLUTION_INDEX = "Settings_ResolutionIndex";
        private const string KEY_VSYNC = "Settings_VSync";
        private const string KEY_FRAMERATE = "Settings_Framerate";
        private const string KEY_FULLSCREEN = "Settings_Fullscreen";
        private const string KEY_UI_SCALE = "Settings_UIScale";
        private const string KEY_SHOW_TUTORIALS = "Settings_ShowTutorials";
        private const string KEY_TOOLTIPS = "Settings_Tooltips";
        private const string KEY_SHOW_NODE_BUILDING_GRID = "Settings_ShowNodeBuildingGrid";
        private const string KEY_CAMERA_SENSITIVITY = "Settings_CameraSensitivity";
        private const string KEY_CAMERA_SHAKE = "Settings_CameraShake";
        private const string KEY_OCCLUSION_TRANSPARENCY = "Settings_OcclusionTransparency";

        private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";
        private const string KEY_MUSIC_VOLUME = "Settings_MusicVolume";
        private const string KEY_AMBIENCE_VOLUME = "Settings_AmbienceVolume";
        private const string KEY_UI_VOLUME = "Settings_UIVolume";
        private const string KEY_SFX_VOLUME = "Settings_SFXVolume";
        private const string KEY_PHYSICS_VOLUME = "Settings_PhysicsVolume";
        private const string KEY_BLOCK_VOLUME = "Settings_BlockVolume";

        // ─────────────────────────────────────────────
        //  Default Values — General
        // ─────────────────────────────────────────────

        public const int DEFAULT_LANGUAGE = 0; // 0 = Thai
        public const int DEFAULT_RESOLUTION_INDEX = -1; // -1 = ใช้ความละเอียดจอปัจจุบัน
        public const bool DEFAULT_VSYNC = true;
        public const int DEFAULT_FRAMERATE = 60;
        public const bool DEFAULT_FULLSCREEN = true;
        public const float DEFAULT_UI_SCALE = 1.0f;
        public const bool DEFAULT_SHOW_TUTORIALS = true;
        public const bool DEFAULT_TOOLTIPS = true;
        public const bool DEFAULT_SHOW_NODE_BUILDING_GRID = true;
        public const float DEFAULT_CAMERA_SENSITIVITY = 1.0f;
        public const bool DEFAULT_CAMERA_SHAKE = true;
        public const float DEFAULT_OCCLUSION_TRANSPARENCY = 0.3f;

        // ─────────────────────────────────────────────
        //  Default Values — Sound
        // ─────────────────────────────────────────────

        public const float DEFAULT_MASTER_VOLUME = 1.0f;
        public const float DEFAULT_MUSIC_VOLUME = 0.5f;
        public const float DEFAULT_AMBIENCE_VOLUME = 0.5f;
        public const float DEFAULT_UI_VOLUME = 0.5f;
        public const float DEFAULT_SFX_VOLUME = 0.5f;
        public const float DEFAULT_PHYSICS_VOLUME = 0.5f;
        public const float DEFAULT_BLOCK_VOLUME = 0.5f;

        // ─────────────────────────────────────────────
        //  Framerate Options
        // ─────────────────────────────────────────────

        /// <summary>
        /// ตัวเลือก Framerate ที่มีให้เลือก (-1 = Unlimited)
        /// </summary>
        public static readonly int[] FramerateOptions = { 30, 60, 120, 144, -1 };

        /// <summary>
        /// ชื่อแสดงของตัวเลือก Framerate
        /// </summary>
        public static readonly string[] FramerateLabels = { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "Unlimited" };

        // ─────────────────────────────────────────────
        //  Language Options
        // ─────────────────────────────────────────────

        public static readonly string[] LanguageOptions = { "Thai", "English", "Japanese", "Chinese" };

        // ─────────────────────────────────────────────
        //  Resolution Cache
        // ─────────────────────────────────────────────

        private static Resolution[] _cachedResolutions;

        /// <summary>
        /// ดึงรายชื่อ Resolution ที่จอรองรับ (กรองตัวซ้ำ)
        /// </summary>
        public static Resolution[] GetAvailableResolutions()
        {
            if (_cachedResolutions == null)
            {
                var resList = new List<Resolution>();
                var seen = new HashSet<string>();
                foreach (var res in Screen.resolutions)
                {
                    string key = $"{res.width}x{res.height}";
                    if (!seen.Contains(key))
                    {
                        seen.Add(key);
                        resList.Add(res);
                    }
                }
                _cachedResolutions = resList.ToArray();
            }
            return _cachedResolutions;
        }

        /// <summary>
        /// สร้างชื่อแสดงสำหรับ Resolution dropdown
        /// </summary>
        public static string[] GetResolutionLabels()
        {
            var resolutions = GetAvailableResolutions();
            string[] labels = new string[resolutions.Length];
            for (int i = 0; i < resolutions.Length; i++)
            {
                labels[i] = $"{resolutions[i].width} x {resolutions[i].height}";
            }
            return labels;
        }

        // ═════════════════════════════════════════════
        //  LOAD — โหลดค่าจาก PlayerPrefs
        // ═════════════════════════════════════════════

        // ── General ──

        public static int LoadLanguage()
        {
            return PlayerPrefs.GetInt(KEY_LANGUAGE, DEFAULT_LANGUAGE);
        }

        public static int LoadResolutionIndex()
        {
            return PlayerPrefs.GetInt(KEY_RESOLUTION_INDEX, DEFAULT_RESOLUTION_INDEX);
        }

        public static bool LoadVSync()
        {
            return PlayerPrefs.GetInt(KEY_VSYNC, DEFAULT_VSYNC ? 1 : 0) == 1;
        }

        public static int LoadFramerate()
        {
            return PlayerPrefs.GetInt(KEY_FRAMERATE, DEFAULT_FRAMERATE);
        }

        public static bool LoadFullscreen()
        {
            return PlayerPrefs.GetInt(KEY_FULLSCREEN, DEFAULT_FULLSCREEN ? 1 : 0) == 1;
        }

        public static float LoadUIScale()
        {
            return PlayerPrefs.GetFloat(KEY_UI_SCALE, DEFAULT_UI_SCALE);
        }

        public static bool LoadShowTutorials()
        {
            return PlayerPrefs.GetInt(KEY_SHOW_TUTORIALS, DEFAULT_SHOW_TUTORIALS ? 1 : 0) == 1;
        }

        public static bool LoadTooltips()
        {
            return PlayerPrefs.GetInt(KEY_TOOLTIPS, DEFAULT_TOOLTIPS ? 1 : 0) == 1;
        }

        public static bool LoadShowNodeBuildingGrid()
        {
            return PlayerPrefs.GetInt(KEY_SHOW_NODE_BUILDING_GRID, DEFAULT_SHOW_NODE_BUILDING_GRID ? 1 : 0) == 1;
        }

        public static float LoadCameraSensitivity()
        {
            return PlayerPrefs.GetFloat(KEY_CAMERA_SENSITIVITY, DEFAULT_CAMERA_SENSITIVITY);
        }

        public static bool LoadCameraShake()
        {
            return PlayerPrefs.GetInt(KEY_CAMERA_SHAKE, DEFAULT_CAMERA_SHAKE ? 1 : 0) == 1;
        }

        public static float LoadOcclusionTransparency()
        {
            return PlayerPrefs.GetFloat(KEY_OCCLUSION_TRANSPARENCY, DEFAULT_OCCLUSION_TRANSPARENCY);
        }

        // ── Sound ──

        public static float LoadMasterVolume()
        {
            return PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, DEFAULT_MASTER_VOLUME);
        }

        public static float LoadMusicVolume()
        {
            return PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, DEFAULT_MUSIC_VOLUME);
        }

        public static float LoadAmbienceVolume()
        {
            return PlayerPrefs.GetFloat(KEY_AMBIENCE_VOLUME, DEFAULT_AMBIENCE_VOLUME);
        }

        public static float LoadUIVolume()
        {
            return PlayerPrefs.GetFloat(KEY_UI_VOLUME, DEFAULT_UI_VOLUME);
        }

        public static float LoadSFXVolume()
        {
            return PlayerPrefs.GetFloat(KEY_SFX_VOLUME, DEFAULT_SFX_VOLUME);
        }

        public static float LoadPhysicsVolume()
        {
            return PlayerPrefs.GetFloat(KEY_PHYSICS_VOLUME, DEFAULT_PHYSICS_VOLUME);
        }

        public static float LoadBlockVolume()
        {
            return PlayerPrefs.GetFloat(KEY_BLOCK_VOLUME, DEFAULT_BLOCK_VOLUME);
        }

        // ═════════════════════════════════════════════
        //  SAVE — บันทึกค่าลง PlayerPrefs
        // ═════════════════════════════════════════════

        // ── General ──

        public static void SaveLanguage(int value)
        {
            PlayerPrefs.SetInt(KEY_LANGUAGE, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveResolutionIndex(int value)
        {
            PlayerPrefs.SetInt(KEY_RESOLUTION_INDEX, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveVSync(bool value)
        {
            PlayerPrefs.SetInt(KEY_VSYNC, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveFramerate(int value)
        {
            PlayerPrefs.SetInt(KEY_FRAMERATE, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveFullscreen(bool value)
        {
            PlayerPrefs.SetInt(KEY_FULLSCREEN, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveUIScale(float value)
        {
            PlayerPrefs.SetFloat(KEY_UI_SCALE, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveShowTutorials(bool value)
        {
            PlayerPrefs.SetInt(KEY_SHOW_TUTORIALS, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveTooltips(bool value)
        {
            PlayerPrefs.SetInt(KEY_TOOLTIPS, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveShowNodeBuildingGrid(bool value)
        {
            PlayerPrefs.SetInt(KEY_SHOW_NODE_BUILDING_GRID, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveCameraSensitivity(float value)
        {
            PlayerPrefs.SetFloat(KEY_CAMERA_SENSITIVITY, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveCameraShake(bool value)
        {
            PlayerPrefs.SetInt(KEY_CAMERA_SHAKE, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveOcclusionTransparency(float value)
        {
            PlayerPrefs.SetFloat(KEY_OCCLUSION_TRANSPARENCY, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        // ── Sound ──

        public static void SaveMasterVolume(float value)
        {
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveAmbienceVolume(float value)
        {
            PlayerPrefs.SetFloat(KEY_AMBIENCE_VOLUME, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveUIVolume(float value)
        {
            PlayerPrefs.SetFloat(KEY_UI_VOLUME, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveSFXVolume(float value)
        {
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SavePhysicsVolume(float value)
        {
            PlayerPrefs.SetFloat(KEY_PHYSICS_VOLUME, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public static void SaveBlockVolume(float value)
        {
            PlayerPrefs.SetFloat(KEY_BLOCK_VOLUME, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        // ═════════════════════════════════════════════
        //  APPLY — ใช้ค่า Settings จริงกับระบบ
        // ═════════════════════════════════════════════

        /// <summary>
        /// Apply การตั้งค่า Resolution + Fullscreen
        /// </summary>
        public static void ApplyResolution(int resolutionIndex, bool fullscreen)
        {
            var resolutions = GetAvailableResolutions();
            if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
            {
                var res = resolutions[resolutionIndex];
                Screen.SetResolution(res.width, res.height, fullscreen);
            }
            else
            {
                // ใช้ค่าปัจจุบัน
                Screen.fullScreen = fullscreen;
            }
        }

        /// <summary>
        /// Apply การตั้งค่า VSync
        /// </summary>
        public static void ApplyVSync(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }

        /// <summary>
        /// Apply การตั้งค่า Framerate
        /// </summary>
        public static void ApplyFramerate(int framerate)
        {
            Application.targetFrameRate = framerate;
        }

        /// <summary>
        /// Apply การตั้งค่า Fullscreen
        /// </summary>
        public static void ApplyFullscreen(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }

        /// <summary>
        /// Apply volume ไปยัง BGMManager (Music Volume * Master Volume)
        /// </summary>
        public static void ApplyMusicVolume()
        {
            float master = LoadMasterVolume();
            float music = LoadMusicVolume();
            if (AudioSystem.BGMManager.Instance != null)
            {
                AudioSystem.BGMManager.Instance.BGMVolume = music * master;
            }
        }

        /// <summary>
        /// Apply volume ไปยัง ClickSound/SFX (SFX Volume * Master Volume)
        /// </summary>
        public static void ApplySFXVolume()
        {
            float master = LoadMasterVolume();
            float sfx = LoadSFXVolume();
            if (AudioSystem.ClickSound.Instance != null)
            {
                AudioSystem.ClickSound.Instance.SFXVolume = sfx * master;
            }
        }

        /// <summary>
        /// Apply ค่า Settings ทั้งหมดไปยังระบบจริง
        /// </summary>
        public static void ApplyAllSettings()
        {
            // General
            ApplyResolution(LoadResolutionIndex(), LoadFullscreen());
            ApplyVSync(LoadVSync());
            ApplyFramerate(LoadFramerate());

            // Sound
            ApplyMusicVolume();
            ApplySFXVolume();
        }

        // ═════════════════════════════════════════════
        //  RESET — รีเซ็ตค่ากลับเป็น Default
        // ═════════════════════════════════════════════

        // ── Individual Reset — General ──

        public static void ResetLanguage()
        {
            SaveLanguage(DEFAULT_LANGUAGE);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetResolution()
        {
            SaveResolutionIndex(DEFAULT_RESOLUTION_INDEX);
            ApplyResolution(DEFAULT_RESOLUTION_INDEX, LoadFullscreen());
            OnSettingsChanged?.Invoke();
        }

        public static void ResetVSync()
        {
            SaveVSync(DEFAULT_VSYNC);
            ApplyVSync(DEFAULT_VSYNC);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetFramerate()
        {
            SaveFramerate(DEFAULT_FRAMERATE);
            ApplyFramerate(DEFAULT_FRAMERATE);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetFullscreen()
        {
            SaveFullscreen(DEFAULT_FULLSCREEN);
            ApplyFullscreen(DEFAULT_FULLSCREEN);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetUIScale()
        {
            SaveUIScale(DEFAULT_UI_SCALE);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetShowTutorials()
        {
            SaveShowTutorials(DEFAULT_SHOW_TUTORIALS);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetTooltips()
        {
            SaveTooltips(DEFAULT_TOOLTIPS);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetShowNodeBuildingGrid()
        {
            SaveShowNodeBuildingGrid(DEFAULT_SHOW_NODE_BUILDING_GRID);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetCameraSensitivity()
        {
            SaveCameraSensitivity(DEFAULT_CAMERA_SENSITIVITY);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetCameraShake()
        {
            SaveCameraShake(DEFAULT_CAMERA_SHAKE);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetOcclusionTransparency()
        {
            SaveOcclusionTransparency(DEFAULT_OCCLUSION_TRANSPARENCY);
            OnSettingsChanged?.Invoke();
        }

        // ── Individual Reset — Sound ──

        public static void ResetMasterVolume()
        {
            SaveMasterVolume(DEFAULT_MASTER_VOLUME);
            ApplyMusicVolume();
            ApplySFXVolume();
            OnSettingsChanged?.Invoke();
        }

        public static void ResetMusicVolume()
        {
            SaveMusicVolume(DEFAULT_MUSIC_VOLUME);
            ApplyMusicVolume();
            OnSettingsChanged?.Invoke();
        }

        public static void ResetAmbienceVolume()
        {
            SaveAmbienceVolume(DEFAULT_AMBIENCE_VOLUME);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetUIVolume()
        {
            SaveUIVolume(DEFAULT_UI_VOLUME);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetSFXVolume()
        {
            SaveSFXVolume(DEFAULT_SFX_VOLUME);
            ApplySFXVolume();
            OnSettingsChanged?.Invoke();
        }

        public static void ResetPhysicsVolume()
        {
            SavePhysicsVolume(DEFAULT_PHYSICS_VOLUME);
            OnSettingsChanged?.Invoke();
        }

        public static void ResetBlockVolume()
        {
            SaveBlockVolume(DEFAULT_BLOCK_VOLUME);
            OnSettingsChanged?.Invoke();
        }

        // ── Batch Reset ──

        /// <summary>
        /// รีเซ็ตค่า General Settings ทั้งหมดกลับเป็นค่าเริ่มต้น
        /// </summary>
        public static void ResetGeneralToDefaults()
        {
            SaveLanguage(DEFAULT_LANGUAGE);
            SaveResolutionIndex(DEFAULT_RESOLUTION_INDEX);
            SaveVSync(DEFAULT_VSYNC);
            SaveFramerate(DEFAULT_FRAMERATE);
            SaveFullscreen(DEFAULT_FULLSCREEN);
            SaveUIScale(DEFAULT_UI_SCALE);
            SaveShowTutorials(DEFAULT_SHOW_TUTORIALS);
            SaveTooltips(DEFAULT_TOOLTIPS);
            SaveShowNodeBuildingGrid(DEFAULT_SHOW_NODE_BUILDING_GRID);
            SaveCameraSensitivity(DEFAULT_CAMERA_SENSITIVITY);
            SaveCameraShake(DEFAULT_CAMERA_SHAKE);
            SaveOcclusionTransparency(DEFAULT_OCCLUSION_TRANSPARENCY);

            // Apply ค่า General ที่เปลี่ยนไป
            ApplyResolution(DEFAULT_RESOLUTION_INDEX, DEFAULT_FULLSCREEN);
            ApplyVSync(DEFAULT_VSYNC);
            ApplyFramerate(DEFAULT_FRAMERATE);

            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// รีเซ็ตค่า Sound Settings ทั้งหมดกลับเป็นค่าเริ่มต้น
        /// </summary>
        public static void ResetSoundToDefaults()
        {
            SaveMasterVolume(DEFAULT_MASTER_VOLUME);
            SaveMusicVolume(DEFAULT_MUSIC_VOLUME);
            SaveAmbienceVolume(DEFAULT_AMBIENCE_VOLUME);
            SaveUIVolume(DEFAULT_UI_VOLUME);
            SaveSFXVolume(DEFAULT_SFX_VOLUME);
            SavePhysicsVolume(DEFAULT_PHYSICS_VOLUME);
            SaveBlockVolume(DEFAULT_BLOCK_VOLUME);

            // Apply ค่า Sound ที่เปลี่ยนไป
            ApplyMusicVolume();
            ApplySFXVolume();

            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// รีเซ็ตค่า Settings ทั้งหมดกลับเป็นค่าเริ่มต้น
        /// </summary>
        public static void ResetAllToDefaults()
        {
            ResetGeneralToDefaults();
            ResetSoundToDefaults();
        }
    }
}
