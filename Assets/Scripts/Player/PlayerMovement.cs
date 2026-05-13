using UnityEngine;

namespace Simulation.Player
{
    /// <summary>
    /// PlayerMovement - ระบบเดินพื้นฐานด้วย WASD
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("ความเร็วในการเดิน")]
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("ความเร็วในการหมุนตัว")]
        [SerializeField] private float rotationSpeed = 10f;
        [Tooltip("แรงโน้มถ่วง")]
        [SerializeField] private float gravity = -9.81f;

        private CharacterController _controller;
        private Vector3 _velocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // รับค่าจาก WASD หรือ Arrow Keys
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // คำนวณทิศทาง (World Space)
            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

            if (direction.magnitude >= 0.1f)
            {
                // ขยับตัวละคร
                _controller.Move(direction * moveSpeed * Time.deltaTime);

                // หมุนตัวละครไปยังทิศทางที่เดิน
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // จัดการเรื่องแรงโน้มถ่วง (เพื่อให้ตัวละครติดพื้น)
            ApplyGravity();
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
    }
}
