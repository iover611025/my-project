using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace X
{
    public class RequireHeldItemToOpen : MonoBehaviour, IPointerClickHandler
    {
        public int requiredItemId; // 需要握持的道具ID
        public InventoryUI inventoryUI; // 拖入InventoryUI
        public ToggleUIObject doorToggle; // 門的開關腳本
        public ItemDatabase itemDatabase;

        [Header("場景切換用")]
        public GameObject currentPanel; // 目前UI場景
        public GameObject nextPanel;    // 要切換的UI場景
        public Image blackFadeImage;    // 黑幕Image（全螢幕，預設透明）

        private bool isOpen = false;
        private bool _isSwitching = false; // 防止重複切換

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[RequireHeldItemToOpen] Click received. isOpen={isOpen}");

            if (!isOpen)
            {
                if (itemDatabase == null)
                {
                    Debug.LogWarning("[RequireHeldItemToOpen] itemDatabase 未指派！");
                    return;
                }
                if (doorToggle == null)
                {
                    Debug.LogWarning("[RequireHeldItemToOpen] doorToggle 未指派！");
                    return;
                }

                // 檢查持有物品（更健壯的空手判斷）
                Sprite heldIcon = null;
                if (inventoryUI != null && inventoryUI.heldItemImage != null)
                    heldIcon = inventoryUI.heldItemImage.sprite;

                bool emptyHand = IsEmptyHand(inventoryUI);

                Debug.Log($"[RequireHeldItemToOpen] heldIcon={(heldIcon != null ? heldIcon.name : "null")}, emptyHand={emptyHand}");

                if (emptyHand)
                {
                    Debug.Log("[RequireHeldItemToOpen] 請先握持正確的道具！");
                    return;
                }

                var itemData = itemDatabase.items.Find(x => x.icon == heldIcon);
                if (itemData != null && itemData.id == requiredItemId)
                {
                    doorToggle.OnClick(); // 開門
                    isOpen = true;
                    Debug.Log("[RequireHeldItemToOpen] 門已開啟！");
                }
                else
                {
                    Debug.Log("[RequireHeldItemToOpen] 握持的不是正確的道具，無法開門！");
                }
            }
            else
            {
                // 門已開啟，僅允許空手切換場景
                Debug.Log("[RequireHeldItemToOpen] 門已開啟，檢查是否空手以切換場景");
                if (_isSwitching)
                {
                    Debug.Log("[RequireHeldItemToOpen] 已在切換中，忽略點擊");
                    return;
                }

                if (IsEmptyHand(inventoryUI))
                {
                    if (blackFadeImage == null)
                    {
                        Debug.LogWarning("[RequireHeldItemToOpen] blackFadeImage 未指派，無過場效果但仍會切換 Panel");
                        // 直接切換
                        if (currentPanel != null) currentPanel.SetActive(false);
                        if (nextPanel != null) nextPanel.SetActive(true);
                        return;
                    }

                    Debug.Log("[RequireHeldItemToOpen] 空手，開始過場切換");
                    StartCoroutine(FadeAndSwitchPanel());
                }
                else
                {
                    Debug.Log("[RequireHeldItemToOpen] 仍持有物品，無法切換場景");
                }
            }
        }

        // 更保守的空手判斷：image null / sprite null / image disabled / alpha ~0
        private bool IsEmptyHand(InventoryUI inv)
        {
            if (inv == null) return true;
            var img = inv.heldItemImage;
            if (img == null) return true;
            if (!img.enabled) return true;
            if (img.sprite == null) return true;
            // 若 UI 使用透明來隱藏握持物，判斷 alpha
            if (img.color.a <= 0.01f) return true;
            return false;
        }

        private IEnumerator FadeAndSwitchPanel()
        {
            if (_isSwitching) yield break;
            _isSwitching = true;

            if (blackFadeImage == null)
            {
                _isSwitching = false;
                yield break;
            }

            // 確保黑幕可見並從 alpha=0 開始
            blackFadeImage.gameObject.SetActive(true);
            Color col = blackFadeImage.color;
            blackFadeImage.color = new Color(col.r, col.g, col.b, 0f);

            // 淡入（使用 unscaled time）
            yield return StartCoroutine(FadeBlack(0f, 1f, 0.5f));

            // 切換Panel（null 檢查）
            if (currentPanel != null) currentPanel.SetActive(false);
            if (nextPanel != null) nextPanel.SetActive(true);

            // 停留1.5秒（實際時間）
            yield return new WaitForSecondsRealtime(1.5f);

            // 淡出
            yield return StartCoroutine(FadeBlack(1f, 0f, 0.5f));

            _isSwitching = false;
        }

        private IEnumerator FadeBlack(float from, float to, float duration)
        {
            if (blackFadeImage == null) yield break;

            float t = 0f;
            // capture base color
            Color baseColor = new Color(blackFadeImage.color.r, blackFadeImage.color.g, blackFadeImage.color.b, 0f);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                blackFadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            blackFadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
        }
    }
}