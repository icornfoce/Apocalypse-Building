using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Simulation.UI
{
    [RequireComponent(typeof(Collider))]
    public class MainMenu3DButton : MonoBehaviour
    {
        [Header("Scale Settings (Hover)")]
        [SerializeField] private float hoverScaleMultiplier = 1.18f;
        [SerializeField] private float scaleDuration = 0.2f;

        [Header("Idle Float (ลอยตัวให้ดูมีชีวิต)")]
        [SerializeField] private bool useIdleFloat = true;
        [SerializeField] private float floatAmplitude = 0.12f;
        [SerializeField] private float floatSpeed = 1.6f;

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

        private Vector3 _originalLocalPosition;
        private Quaternion _originalLocalRotation;
        private Vector3 _originalScale;
        private UnityEngine.Camera _mainCamera;

        private bool _entranceDone;
        private float _phase; // desync per button for idle float

        private void Awake()
        {
            _originalLocalPosition = transform.localPosition;
            _originalLocalRotation = transform.localRotation;
            _originalScale = transform.localScale;
            _mainCamera = UnityEngine.Camera.main;
            _phase = transform.GetSiblingIndex() * 0.7f;
        }

        private void OnEnable()
        {
            PlayEntranceAnimation();
        }

        private void OnDisable()
        {
            transform.DOKill();
            _entranceDone = false;
            transform.localPosition = _originalLocalPosition;
            transform.localRotation = _originalLocalRotation;
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
            _entranceDone = false;

            // Start to the left of the original local position
            Vector3 startPos = _originalLocalPosition - new Vector3(slideDistance, 0f, 0f);
            transform.localPosition = startPos;
            transform.localScale = _originalScale;

            transform.DOLocalMove(_originalLocalPosition, slideDuration)
                .SetDelay(delay)
                .SetEase(slideEase)
                .OnComplete(() => _entranceDone = true);
        }

        private void Update()
        {
            RotateTowardsMouse();
            ApplyIdleFloat();
        }

        private void ApplyIdleFloat()
        {
            if (!useIdleFloat || !_entranceDone) return;
            float off = Mathf.Sin((Time.time + _phase) * floatSpeed) * floatAmplitude;
            Vector3 p = _originalLocalPosition;
            p.y += off;
            transform.localPosition = p;
        }

        private void OnMouseEnter()
        {
            transform.DOKill(true);
            transform.DOScale(_originalScale * hoverScaleMultiplier, scaleDuration).SetEase(Ease.OutBack);
        }

        private void OnMouseExit()
        {
            transform.DOKill(true);
            transform.DOScale(_originalScale, scaleDuration).SetEase(Ease.OutQuad);
        }

        private void OnMouseDown()
        {
            transform.DOPunchScale(Vector3.one * -0.12f, 0.18f, 10, 1f);
            onClick?.Invoke();
        }

        private void RotateTowardsMouse()
        {
            if (_mainCamera == null) _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null) return;

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
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

                    Quaternion baseWorldRotation = transform.parent != null
                        ? transform.parent.rotation * _originalLocalRotation
                        : _originalLocalRotation;

                    Quaternion relativeRot = Quaternion.Inverse(baseWorldRotation) * targetRotation;
                    Vector3 relativeEuler = relativeRot.eulerAngles;

                    float relX = relativeEuler.x > 180f ? relativeEuler.x - 360f : relativeEuler.x;
                    float relY = relativeEuler.y > 180f ? relativeEuler.y - 360f : relativeEuler.y;

                    relX = Mathf.Clamp(relX, -maxTiltAngle, maxTiltAngle);
                    relY = Mathf.Clamp(relY, -maxTiltAngle, 0f);

                    targetRotation = baseWorldRotation * Quaternion.Euler(relX, relY, 0f);

                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
                }
            }
        }
    }
}
