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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isOpen)
            {
                // 檢查持有物品
                Sprite heldIcon = inventoryUI != null && inventoryUI.heldItemImage != null ? inventoryUI.heldItemImage.sprite : null;
                if (heldIcon == null)
                {
                    Debug.Log("請先握持正確的道具！");
                    return;
                }
                var itemData = itemDatabase.items.Find(x => x.icon == heldIcon);
                if (itemData != null && itemData.id == requiredItemId)
                {
                    doorToggle.OnClick(); // 開門
                    isOpen = true;
                    Debug.Log("門已開啟！");
                }
                else
                {
                    Debug.Log("握持的不是正確的道具，無法開門！");
                }
            }
            else
            {
                // 門已開啟，僅允許空手切換場景
                Sprite heldIcon = inventoryUI != null && inventoryUI.heldItemImage != null ? inventoryUI.heldItemImage.sprite : null;
                if (heldIcon == null)
                {
                    StartCoroutine(FadeAndSwitchPanel());
                }
            }
        }

        private IEnumerator FadeAndSwitchPanel()
        {
            if (blackFadeImage == null) yield break;

            // 淡入
            yield return StartCoroutine(FadeBlack(0f, 1f, 0.5f));
            // 切換Panel
            if (currentPanel != null) currentPanel.SetActive(false);
            if (nextPanel != null) nextPanel.SetActive(true);
            // 停留1.5秒
            yield return new WaitForSeconds(1.5f);
            // 淡出
            yield return StartCoroutine(FadeBlack(1f, 0f, 0.5f));
        }

        private IEnumerator FadeBlack(float from, float to, float duration)
        {
            float t = 0f;
            Color c = blackFadeImage.color;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(from, to, t / duration);
                blackFadeImage.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }
            blackFadeImage.color = new Color(c.r, c.g, c.b, to);
        }
    }
}