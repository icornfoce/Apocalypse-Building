using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Simulation.UI
{
    [RequireComponent(typeof(Collider))]
    public class MainMenu3DButton : MonoBehaviour
    {
        [Header("Scale Settings (Hover)")]
        [SerializeField] private float hoverScaleMultiplier = 1.2f;
        [SerializeField] private float scaleDuration = 0.2f;

        [Header("Color Settings (Optional)")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = Color.cyan;
        [SerializeField] private float colorTransitionDuration = 0.2f;
        [SerializeField] private bool useColorTransition = true;

        [Header("Rotation Settings (Tilt to Mouse)")]
        [Tooltip("Max angle (degrees) the cube can tilt towards the mouse.")]
        [SerializeField] private float maxTiltAngle = 15f;
        [Tooltip("Smoothness of the rotation follow.")]
        [SerializeField] private float rotationSmoothSpeed = 10f;
        [SerializeField] private bool invertRotation = true;

        [Header("Entrance Animation")]
        [SerializeField] private float slideDistance = 5f;
        [SerializeField] private float slideDuration = 0.8f;
        [SerializeField] private float slideDelay = 0f;
        [SerializeField] private Ease slideEase = Ease.OutBack;

        [Header("Click Events")]
        public UnityEvent onClick;

        private Renderer _renderer;
        private Material _material;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale;
        private UnityEngine.Camera _mainCamera;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null && useColorTransition)
            {
                // Create an instance of the material to avoid modifying the asset shared material
                _material = _renderer.material;
                _material.color = normalColor;
            }

            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            _originalScale = transform.localScale;
            _mainCamera = UnityEngine.Camera.main;
        }

        private void OnEnable()
        {
            PlayEntranceAnimation();
        }

        private void OnDisable()
        {
            // Reset state
            transform.DOKill();
            if (_material != null)
            {
                _material.DOKill();
                _material.color = normalColor;
            }
            transform.position = _originalPosition;
            transform.rotation = _originalRotation;
            transform.localScale = _originalScale;
        }

        /// <summary>
        /// Plays the entrance animation sliding from left to right (original position).
        /// </summary>
        public void PlayEntranceAnimation()
        {
            PlayEntranceAnimation(slideDelay);
        }

        /// <summary>
        /// Plays the entrance animation sliding from left to right with a custom delay.
        /// </summary>
        public void PlayEntranceAnimation(float delay)
        {
            transform.DOKill();
            
            // Set starting position to the left of the original position
            Vector3 startPos = _originalPosition - new Vector3(slideDistance, 0f, 0f);
            transform.position = startPos;
            transform.localScale = _originalScale; // Reset scale during entrance
            
            // Slide to the original position
            transform.DOMove(_originalPosition, slideDuration)
                .SetDelay(delay)
                .SetEase(slideEase);
        }

        private void Update()
        {
            // Always rotate towards the mouse cursor
            RotateTowardsMouse();
        }

        private void OnMouseEnter()
        {
            // Scale up on hover
            transform.DOKill(true);
            transform.DOScale(_originalScale * hoverScaleMultiplier, scaleDuration);

            // Change color smoothly if enabled
            if (useColorTransition && _material != null)
            {
                _material.DOColor(hoverColor, colorTransitionDuration);
            }
        }

        private void OnMouseExit()
        {
            // Scale back down
            transform.DOKill(true);
            transform.DOScale(_originalScale, scaleDuration);

            // Return color to normal if enabled
            if (useColorTransition && _material != null)
            {
                _material.DOColor(normalColor, colorTransitionDuration);
            }
        }

        private void OnMouseDown()
        {
            // Mini punch scale effect on click based on current scale
            transform.DOPunchScale(Vector3.one * -0.1f, 0.15f, 10, 1f);

            onClick?.Invoke();
        }

        private void RotateTowardsMouse()
        {
            if (_mainCamera == null) _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null) return;

            // Create a plane facing the camera, positioned at the button's center
            Plane plane = new Plane(-_mainCamera.transform.forward, transform.position);
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out float enterDistance))
            {
                Vector3 mouseWorldPoint = ray.GetPoint(enterDistance);
                Vector3 direction = invertRotation 
                    ? (transform.position - mouseWorldPoint) 
                    : (mouseWorldPoint - transform.position);

                if (direction != Vector3.zero)
                {
                    // Calculate the look rotation facing the mouse point
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

                    // Calculate relative rotation to original rotation
                    Quaternion relativeRot = Quaternion.Inverse(_originalRotation) * targetRotation;
                    Vector3 relativeEuler = relativeRot.eulerAngles;

                    // Normalize Euler angles to -180 to 180 range
                    float relX = relativeEuler.x > 180f ? relativeEuler.x - 360f : relativeEuler.x;
                    float relY = relativeEuler.y > 180f ? relativeEuler.y - 360f : relativeEuler.y;

                    // Clamp angles: Allow only left turn (-maxTiltAngle to 0)
                    relX = Mathf.Clamp(relX, -maxTiltAngle, maxTiltAngle);
                    relY = Mathf.Clamp(relY, -maxTiltAngle, 0f);

                    // Reconstruct target rotation relative to the original rotation
                    targetRotation = _originalRotation * Quaternion.Euler(relX, relY, 0f);

                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
                }
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
