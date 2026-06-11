using UnityEngine;

namespace Simulation.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float deceleration = 16f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Camera Settings")]
        [SerializeField] private Transform targetCamera;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 12f, -8f);
        [SerializeField] private float cameraLerpSpeed = 5f;

        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameterName = "Speed";

        [Header("VFX Settings")]
        [SerializeField] private ParticleSystem moveVfx;

        [Header("SFX Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private float stepInterval = 0.35f;

        private CharacterController _controller;
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private float _currentSpeed;
        private float _stepTimer;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (targetCamera == null && UnityEngine.Camera.main != null)
            {
                targetCamera = UnityEngine.Camera.main.transform;
            }
        }

        private void Update()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

            if (inputDirection.magnitude >= 0.1f)
            {
                _moveDirection = inputDirection;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, maxSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, deceleration * Time.deltaTime);
            }

            if (_currentSpeed > 0.01f)
            {
                _controller.Move(_moveDirection * _currentSpeed * Time.deltaTime);

                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            ApplyGravity();
            ApplyCameraFollow();
            ApplyEffects();
        }

        private void ApplyGravity()
        {
            if (_controller.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void ApplyCameraFollow()
        {
            if (targetCamera == null)
            {
                if (UnityEngine.Camera.main != null)
                {
                    targetCamera = UnityEngine.Camera.main.transform;
                }
                else
                {
                    return;
                }
            }

            var cameraController = targetCamera.GetComponent<Simulation.Camera.CameraController>();
            if (cameraController != null)
            {
                // เลื่อนจุดหมุน (PivotPoint) ของกล้องตามตัวผู้เล่นแบบนุ่มนวล
                cameraController.PivotPoint = Vector3.Lerp(cameraController.PivotPoint, transform.position, cameraLerpSpeed * Time.deltaTime);
            }
            else
            {
                // เลื่อนตำแหน่งกล้องตรงๆ ถ้าไม่มี CameraController
                Vector3 targetPosition = transform.position + cameraOffset;
                targetCamera.position = Vector3.Lerp(targetCamera.position, targetPosition, cameraLerpSpeed * Time.deltaTime);
            }
        }

        private void ApplyEffects()
        {
            if (animator != null)
            {
                animator.SetFloat(speedParameterName, _currentSpeed);
            }

            if (moveVfx != null)
            {
                var emission = moveVfx.emission;
                emission.enabled = _currentSpeed > 0.1f && _controller.isGrounded;
            }

            if (_currentSpeed > 0.1f && _controller.isGrounded)
            {
                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                    PlayFootstepSound();
                    _stepTimer = stepInterval;
                }
            }
            else
            {
                _stepTimer = 0f;
            }
        }

        private void PlayFootstepSound()
        {
            if (audioSource != null && footstepSounds != null && footstepSounds.Length > 0)
            {
                int index = Random.Range(0, footstepSounds.Length);
                audioSource.PlayOneShot(footstepSounds[index]);
            }
        }
    }
}
