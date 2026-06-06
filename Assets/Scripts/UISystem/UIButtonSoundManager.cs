using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    public class UIButtonSoundManager : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("The sound effect to play when any UI button is clicked.")]
        public AudioClip clickSound;
        
        private AudioSource audioSource;

        private void Awake()
        {
            // Create an AudioSource to play the sound
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // Make it 2D sound
        }

        private void Start()
        {
            AttachSoundToAllButtons();
        }

        /// <summary>
        /// Finds all buttons in the active scene and attaches the click sound event to them.
        /// </summary>
        public void AttachSoundToAllButtons()
        {
            // Find all Button components in the scene (including inactive ones)
            Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

            foreach (Button btn in allButtons)
            {
                // Ensure it's part of the scene, not a prefab in the project
                if (btn.gameObject.scene.IsValid())
                {
                    // Remove first to avoid duplicate listeners if called multiple times
                    btn.onClick.RemoveListener(PlayClickSound);
                    btn.onClick.AddListener(PlayClickSound);
                }
            }
        }

        /// <summary>
        /// Plays the configured click sound.
        /// </summary>
        public void PlayClickSound()
        {
            if (clickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            else
            {
                Debug.LogWarning("UIButtonSoundManager: Click sound is not assigned.");
            }
        }
    }
}
