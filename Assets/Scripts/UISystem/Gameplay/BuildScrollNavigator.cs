using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Simulation.UI
{
    /// <summary>
    /// เลื่อน ScrollRect ไปหาหมวดหมู่/Tag ที่กำหนด เพื่อให้ขึ้นมาอยู่แถวบนสุด
    /// </summary>
    public class BuildScrollNavigator : MonoBehaviour
    {
        [Header("Scroll Setup")]
        [Tooltip("ScrollRect ของหน้าต่างสิ่งก่อสร้าง")]
        [SerializeField] private ScrollRect scrollRect;
        
        [Tooltip("เป้าหมาย (Tag/GameObject) ที่ต้องการให้เลื่อนไปหา")]
        [SerializeField] private RectTransform targetTag;

        [Tooltip("Panel/ScrollRect to open before scrolling to the target.")]
        [SerializeField] private GameObject panelToOpen;

        [Header("Scroll Settings")]
        [Tooltip("ให้เลื่อนแบบนุ่มนวล (Smooth) หรือไม่")]
        [SerializeField] private bool smoothScroll = true;
        [Tooltip("ระยะเวลาในการเลื่อน (วินาที)")]
        [SerializeField] private float scrollDuration = 0.3f;
        [Tooltip("ระยะชดเชยการเลื่อนแนวตั้ง (บวกค่าเพื่อดันเนื้อหาขึ้น ลบค่าเพื่อดันลง)")]
        [SerializeField] private float yOffset = 0f;

        /// <summary>
        /// คำสั่งหลักสำหรับปุ่มหมวดหมู่: เลื่อนหน้าจอไปยัง Tag เป้าหมาย
        /// </summary>
        public void ScrollToTarget()
        {
            if (panelToOpen != null && !panelToOpen.activeSelf)
            {
                panelToOpen.SetActive(true);
            }

            if (scrollRect != null && !scrollRect.gameObject.activeSelf)
            {
                scrollRect.gameObject.SetActive(true);
            }

            if (scrollRect == null || scrollRect.content == null || targetTag == null)
            {
                Debug.LogWarning("[BuildScrollNavigator] ScrollRect, Content, or TargetTag is missing!");
                return;
            }

            // สั่งอัปเดต UI Layout ทันที เพื่อป้องกันค่าตำแหน่งคลาดเคลื่อน
            Canvas.ForceUpdateCanvases();

            // หาความสูงของขอบบนของ Content
            float contentTop = scrollRect.content.rect.yMax;
            
            // หาตำแหน่งของ TargetTag ในระบบพิกัดภายในของ Content
            Vector3 localTargetPos = scrollRect.content.InverseTransformPoint(targetTag.position);
            
            // คำนวณระยะห่างจากขอบบนสุดของ Content ลงมาถึงตัว Target
            float distanceToTop = contentTop - localTargetPos.y;

            // คำนวณช่วงพิกัดที่ ScrollRect สามารถเลื่อนขึ้นลงได้โดยไม่หลุดขอบ Content
            float minAnchoredY = 0f;
            float maxAnchoredY = Mathf.Max(0f, scrollRect.content.rect.height - scrollRect.viewport.rect.height);

            // จำกัดตำแหน่งไม่ให้เลื่อนเกินขอบเขตบน/ล่าง (บวกด้วย yOffset เพื่อชดเชยความสูงเพิ่มเติม)
            float targetAnchoredY = Mathf.Clamp(distanceToTop + yOffset, minAnchoredY, maxAnchoredY);

            // สั่งเลื่อนตำแหน่ง Y
            if (smoothScroll)
            {
                // หยุดอนิเมชันเดิม (ถ้ามี)
                scrollRect.content.DOKill();
                // เลื่อนพิกัด Y แบบนุ่มนวล
                scrollRect.content.DOAnchorPosY(targetAnchoredY, scrollDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true); // ทำงานได้แม้จะอยู่ในสภาวะหยุดเวลา
            }
            else
            {
                // เลื่อนทันที
                Vector2 targetPos = scrollRect.content.anchoredPosition;
                targetPos.y = targetAnchoredY;
                scrollRect.content.anchoredPosition = targetPos;
            }
        }
    }
}
