using UnityEngine;
using UnityEngine.EventSystems;

namespace X
{
    public class HeldItemSlot : MonoBehaviour, IPointerClickHandler
    {
        public InventoryUI inventoryUI;

        public void OnPointerClick(PointerEventData eventData)
        {
            // 呼叫 InventoryUI 的 OnHeldAreaClick（包含分配邏輯與診斷），
            // 避免直接 ClearHeldItem 導致握持物件消失而未嘗試分配。
            if (inventoryUI != null)
            {
                inventoryUI.OnHeldAreaClick();
            }
            else
            {
                // fallback：若未指派 InventoryUI，仍保留原本清除行為避免 null 例外
                var inv = GetComponentInParent<InventoryUI>();
                if (inv != null)
                    inv.OnHeldAreaClick();
            }
        }
    }
}