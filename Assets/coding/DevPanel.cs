using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace X
{
    /// <summary>
    /// 開發者面板（僅在 Editor 或啟用 DEV_MODE 時顯示）
    /// 功能：
    ///   1. 傳送至任意大場景 / 房間
    ///   2. 強制拿取任意道具（加入背包）
    ///
    /// 使用方式：
    ///   - 掛到任一 GameObject 上
    ///   - Inspector 填入 RoomUIManager、InventoryUI、ItemDatabase 的引用
    ///   - tmpFont 欄位拖入「匯文明朝體 SDF.asset」
    ///   - 按下快捷鍵（預設 F12）開啟／關閉面板
    /// </summary>
    public class DevPanel : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        //  Inspector 設定
        // ─────────────────────────────────────────────

        [Header("快捷鍵設定")]
        [Tooltip("開啟/關閉開發者面板的按鍵")]
        public KeyCode toggleKey = KeyCode.F12;

        [Header("字體設定")]
        [Tooltip("拖入 匯文明朝體 SDF.asset（位於 Assets/字體/）")]
        public TMP_FontAsset tmpFont;

        [Header("引用（可留空，腳本將自動尋找）")]
        public RoomUIManager roomUIManager;
        public InventoryUI inventoryUI;
        public ItemDatabase itemDatabase;

        [Header("傳送設定（選填：使用 DirectRoomTeleporter 做黑幕轉場）")]
        [Tooltip("若指定，傳送時會觸發黑幕過場；留空則直接切換")]
        public DirectRoomTeleporter teleporter;

        [Header("面板 UI（若已預先建好請拖入；留空則動態生成）")]
        public GameObject panelRoot;

        [Header("面板尺寸（動態生成時有效）")]
        [Tooltip("面板寬度（像素）")]
        public float panelWidth = 380f;
        [Tooltip("面板高度（像素）")]
        public float panelHeight = 500f;
        [Tooltip("整體縮放倍率（1 = 原始大小）")]
        [Range(0.5f, 3f)]
        public float panelScale = 1f;

        // ─────────────────────────────────────────────
        //  私有狀態
        // ─────────────────────────────────────────────

        private bool _isOpen = false;

        // 動態生成的 UI 元素
        private GameObject _dynamicRoot;
        private bool _uiBuilt = false;
        private Canvas _createdCanvas;

        // 傳送欄位（TMP InputField）
        private TMP_InputField _sceneIdInput;
        private TMP_InputField _roomIndexInput;

        // 道具欄位
        private TMP_InputField  _itemIdInput;
        private TMP_Dropdown    _itemDropdown;
        private TextMeshProUGUI _feedbackText;

        // ─────────────────────────────────────────────
        //  Unity 生命週期
        // ─────────────────────────────────────────────

        void Awake()
        {
            if (roomUIManager == null) roomUIManager = FindFirstObjectByType<RoomUIManager>();
            if (inventoryUI   == null) inventoryUI   = FindFirstObjectByType<InventoryUI>();
        }

        void Start()
        {
            BuildUI();
            SetPanelVisible(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                TogglePanel();

            // Inspector 縮放滑桿即時同步
            if (_dynamicRoot != null)
                _dynamicRoot.transform.localScale = Vector3.one * panelScale;
        }

        void OnDestroy()
        {
            if (_createdCanvas != null && _createdCanvas.gameObject != null)
            {
                Destroy(_createdCanvas.gameObject);
            }
        }

        // ─────────────────────────────────────────────
        //  公開方法
        // ─────────────────────────────────────────────

        public void TogglePanel() => SetPanelVisible(!_isOpen);

        public void SetPanelVisible(bool visible)
        {
            _isOpen = visible;
            var root = panelRoot != null ? panelRoot : _dynamicRoot;
            if (root != null) root.SetActive(visible);
            if (visible) RefreshItemDropdown();
        }

        // ─────────────────────────────────────────────
        //  傳送功能
        // ─────────────────────────────────────────────

        private void DoTeleport()
        {
            if (!int.TryParse(_sceneIdInput?.text, out int sceneId))
            { ShowFeedback("⚠ 場景 ID 格式錯誤"); return; }

            if (!int.TryParse(_roomIndexInput?.text, out int roomIndex))
            { ShowFeedback("⚠ 房間索引格式錯誤"); return; }

            if (teleporter != null)
            {
                teleporter.StartTeleport(sceneId, roomIndex);
                ShowFeedback($"✓ 傳送中 → 場景 {sceneId} 房間 {roomIndex}");
                return;
            }
            if (roomUIManager != null)
            {
                roomUIManager.TransitionToBigScene(sceneId, roomIndex);
                ShowFeedback($"✓ 直接切換 → 場景 {sceneId} 房間 {roomIndex}");
            }
            else
            {
                ShowFeedback("⚠ 找不到 RoomUIManager");
            }
        }

        // ─────────────────────────────────────────────
        //  拿取道具功能
        // ─────────────────────────────────────────────

        private void DoPickupItem()
        {
            if (inventoryUI == null) { ShowFeedback("⚠ 找不到 InventoryUI"); return; }

            ItemData data = GetSelectedItemData();
            if (data == null) { ShowFeedback("⚠ 找不到指定道具"); return; }

            bool ok = inventoryUI.AddItemToSlot(data);
            if (ok)
            {
                if (PickupNotificationUI.Instance != null)
                    PickupNotificationUI.Instance.ShowNotification(data);
                ShowFeedback($"✓ 已取得：{data.itemName} (id={data.id})");
            }
            else
            {
                ShowFeedback($"⚠ 背包已滿，無法放入：{data.itemName}");
            }
        }

        private ItemData GetSelectedItemData()
        {
            if (_itemDropdown != null && itemDatabase != null && itemDatabase.items != null)
            {
                int sel = _itemDropdown.value;
                if (sel >= 0 && sel < itemDatabase.items.Count)
                    return itemDatabase.items[sel];
            }
            if (_itemIdInput != null && itemDatabase != null && itemDatabase.items != null)
            {
                if (int.TryParse(_itemIdInput.text, out int id))
                    return itemDatabase.items.Find(x => x.id == id);
            }
            return null;
        }

        // ─────────────────────────────────────────────
        //  Dropdown 更新
        // ─────────────────────────────────────────────

        private void RefreshItemDropdown()
        {
            if (_itemDropdown == null || itemDatabase == null || itemDatabase.items == null) return;
            _itemDropdown.ClearOptions();
            var opts = new List<TMP_Dropdown.OptionData>();
            foreach (var item in itemDatabase.items)
                opts.Add(new TMP_Dropdown.OptionData($"[{item.id}] {item.itemName}"));
            _itemDropdown.AddOptions(opts);
        }

        // ─────────────────────────────────────────────
        //  回饋文字
        // ─────────────────────────────────────────────

        private void ShowFeedback(string msg)
        {
            if (_feedbackText == null) return;
            _feedbackText.text = msg;
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 3f);
        }

        private void ClearFeedback()
        {
            if (_feedbackText != null) _feedbackText.text = string.Empty;
        }

        // ─────────────────────────────────────────────
        //  動態 UI 建構
        // ─────────────────────────────────────────────

        private void BuildUI()
        {
            if (panelRoot != null) { _uiBuilt = true; return; }

            // 總是建立專屬的 Canvas，避免掛載到其他可能被關閉（SetActive(false)）或銷毀的 Canvas 上
            var cgo = new GameObject("DevPanelCanvas");
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // 確保在所有 UI 之上顯示
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
            _createdCanvas = canvas;

            // ── 根面板 ──────────────────────────────
            _dynamicRoot = new GameObject("DevPanel_Root");
            _dynamicRoot.transform.SetParent(canvas.transform, false);

            var rt = _dynamicRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(10f, 0f);
            rt.sizeDelta = new Vector2(panelWidth, panelHeight);
            _dynamicRoot.transform.localScale = Vector3.one * panelScale;

            var bg = _dynamicRoot.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            _dynamicRoot.transform.SetAsLastSibling();

            // ── 垂直排列 ────────────────────────────
            var layout = _dynamicRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6f;
            layout.childControlHeight    = false;
            layout.childControlWidth     = true;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            // ── 內容 ────────────────────────────────
            AddLabel(_dynamicRoot, "🛠  開發者面板  [F12]", 18, Color.cyan, 30f);
            AddSeparator(_dynamicRoot);

            AddLabel(_dynamicRoot, "▶ 傳送至房間", 14, new Color(0.8f, 1f, 0.8f), 24f);
            _sceneIdInput   = AddInputRow(_dynamicRoot, "大場景 ID :", "0");
            _roomIndexInput = AddInputRow(_dynamicRoot, "房間索引 :", "0");
            AddButton(_dynamicRoot, "傳 送", new Color(0.2f, 0.6f, 1f), DoTeleport);

            AddSeparator(_dynamicRoot);

            AddLabel(_dynamicRoot, "▶ 拿取道具", 14, new Color(1f, 1f, 0.6f), 24f);
            if (itemDatabase != null && itemDatabase.items != null && itemDatabase.items.Count > 0)
            {
                _itemDropdown = AddDropdown(_dynamicRoot);
                RefreshItemDropdown();
            }
            else
            {
                _itemIdInput = AddInputRow(_dynamicRoot, "道具 ID :", "1");
            }
            AddButton(_dynamicRoot, "拿 取", new Color(1f, 0.7f, 0.1f), DoPickupItem);

            AddSeparator(_dynamicRoot);
            _feedbackText = AddLabel(_dynamicRoot, string.Empty, 12, new Color(0.7f, 1f, 0.7f), 22f);

            _uiBuilt = true;
        }

        // ─────────────────────────────────────────────
        //  TMP 工具方法
        // ─────────────────────────────────────────────

        /// <summary>建立一個 TextMeshProUGUI 標籤並回傳，供外部儲存（如 _feedbackText）</summary>
        private TextMeshProUGUI AddLabel(GameObject parent, string text, float fontSize, Color color, float height)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, height);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (tmpFont != null) tmp.font = tmpFont;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth   = 1f;

            return tmp;
        }

        private void AddSeparator(GameObject parent)
        {
            var go  = new GameObject("Sep");
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.15f);
            // Image 會自動建立 RectTransform，預設高度 100 → 必須明確設為 2
            var rt  = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 2f);
            var le  = go.AddComponent<LayoutElement>();
            le.preferredHeight = 2f;
            le.minHeight       = 2f;
            le.flexibleWidth   = 1f;
        }

        /// <summary>建立一排「標籤 + TMP_InputField」並回傳 InputField</summary>
        private TMP_InputField AddInputRow(GameObject parent, string label, string defaultVal)
        {
            var row = new GameObject("Row_" + label);
            row.transform.SetParent(parent.transform, false);

            var hg = row.AddComponent<HorizontalLayoutGroup>();
            hg.spacing              = 4f;
            hg.childControlHeight   = false;
            hg.childControlWidth    = true;
            hg.childForceExpandWidth  = false;
            hg.childForceExpandHeight = false;

            // HorizontalLayoutGroup 已自動附加 RectTransform
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0, 32f);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 32f;
            rowLe.flexibleWidth   = 1f;

            // ── 左側標籤 ──
            var lgo = new GameObject("lbl");
            lgo.transform.SetParent(row.transform, false);
            var lRt  = lgo.AddComponent<RectTransform>();
            lRt.sizeDelta = new Vector2(96f, 32f);
            var lTmp = lgo.AddComponent<TextMeshProUGUI>();
            lTmp.text      = label;
            lTmp.fontSize  = 13f;
            lTmp.color     = Color.white;
            lTmp.alignment = TextAlignmentOptions.MidlineRight;
            if (tmpFont != null) lTmp.font = tmpFont;
            var lLe = lgo.AddComponent<LayoutElement>();
            lLe.preferredWidth = 96f;
            lLe.flexibleWidth  = 0f;

            // ── TMP_InputField ──
            // 結構：InputField (Image) > Text Area > Text / Placeholder
            var igo = new GameObject("InputField");
            igo.transform.SetParent(row.transform, false);
            var iBg = igo.AddComponent<Image>();
            iBg.color = new Color(0.13f, 0.13f, 0.22f, 1f);
            var iRt = igo.GetComponent<RectTransform>();
            iRt.sizeDelta = new Vector2(0, 32f);
            var iLe = igo.AddComponent<LayoutElement>();
            iLe.flexibleWidth   = 1f;
            iLe.preferredHeight = 32f;

            // Text Area
            var area = new GameObject("Text Area");
            area.transform.SetParent(igo.transform, false);
            var areaRt = area.AddComponent<RectTransform>();
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = new Vector2(6, 2);
            areaRt.offsetMax = new Vector2(-6, -2);
            area.AddComponent<RectMask2D>();

            // Placeholder
            var ph    = new GameObject("Placeholder");
            ph.transform.SetParent(area.transform, false);
            var phRt  = ph.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = Vector2.zero;
            phRt.offsetMax = Vector2.zero;
            var phTmp = ph.AddComponent<TextMeshProUGUI>();
            phTmp.text      = defaultVal;
            phTmp.fontSize  = 13f;
            phTmp.color     = new Color(1f, 1f, 1f, 0.3f);
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (tmpFont != null) phTmp.font = tmpFont;

            // Text
            var tgo  = new GameObject("Text");
            tgo.transform.SetParent(area.transform, false);
            var tRt  = tgo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var tTmp = tgo.AddComponent<TextMeshProUGUI>();
            tTmp.fontSize  = 13f;
            tTmp.color     = Color.white;
            tTmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (tmpFont != null) tTmp.font = tmpFont;

            var field = igo.AddComponent<TMP_InputField>();
            field.textViewport   = areaRt;
            field.textComponent  = tTmp;
            field.placeholder    = phTmp;
            field.text           = defaultVal;
            field.contentType    = TMP_InputField.ContentType.IntegerNumber;
            field.targetGraphic  = iBg;

            return field;
        }

        /// <summary>建立 TMP_Dropdown 並回傳</summary>
        private TMP_Dropdown AddDropdown(GameObject parent)
        {
            var go = new GameObject("ItemDropdown");
            go.transform.SetParent(parent.transform, false);

            var bg  = go.AddComponent<Image>();
            bg.color = new Color(0.13f, 0.13f, 0.22f, 1f);

            var le  = go.AddComponent<LayoutElement>();
            le.preferredHeight = 34f;
            le.flexibleWidth   = 1f;

            // Label
            var lgo  = new GameObject("Label");
            lgo.transform.SetParent(go.transform, false);
            var lRt  = lgo.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(8, 2);
            lRt.offsetMax = new Vector2(-28, -2);
            var lTmp = lgo.AddComponent<TextMeshProUGUI>();
            lTmp.fontSize  = 13f;
            lTmp.color     = Color.white;
            lTmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (tmpFont != null) lTmp.font = tmpFont;

            // Arrow label
            var ago  = new GameObject("Arrow");
            ago.transform.SetParent(go.transform, false);
            var aRt  = ago.AddComponent<RectTransform>();
            aRt.anchorMin       = new Vector2(1f, 0.5f);
            aRt.anchorMax       = new Vector2(1f, 0.5f);
            aRt.pivot           = new Vector2(1f, 0.5f);
            aRt.anchoredPosition = new Vector2(-4, 0);
            aRt.sizeDelta       = new Vector2(20, 20);
            var aTmp = ago.AddComponent<TextMeshProUGUI>();
            aTmp.text      = "▼";
            aTmp.fontSize  = 12f;
            aTmp.color     = Color.white;
            aTmp.alignment = TextAlignmentOptions.Center;
            if (tmpFont != null) aTmp.font = tmpFont;

            var dropdown = go.AddComponent<TMP_Dropdown>();
            dropdown.captionText = lTmp;
            dropdown.targetGraphic = bg;

            // ── Template ──────────────────────────
            var template = new GameObject("Template");
            template.transform.SetParent(go.transform, false);
            var tmImg = template.AddComponent<Image>();
            tmImg.color = new Color(0.1f, 0.1f, 0.2f, 1f);
            var tmRt = template.GetComponent<RectTransform>();
            tmRt.anchorMin       = new Vector2(0, 0);
            tmRt.anchorMax       = new Vector2(1, 0);
            tmRt.pivot           = new Vector2(0.5f, 1f);
            tmRt.anchoredPosition = Vector2.zero;
            tmRt.sizeDelta       = new Vector2(0, 150);

            var sr = template.AddComponent<ScrollRect>();
            sr.horizontal = false;

            // Viewport
            var vp    = new GameObject("Viewport");
            vp.transform.SetParent(template.transform, false);
            vp.AddComponent<Image>(); // mask needs graphic
            var vpMask = vp.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;
            var vpRt  = vp.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;

            // Content
            var content   = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, 28);

            // Item template
            var item   = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            var itemToggle = item.AddComponent<Toggle>();
            var itemBg     = item.AddComponent<Image>();
            itemBg.color = new Color(0.18f, 0.18f, 0.32f, 1f);
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 0.5f);
            itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.sizeDelta = new Vector2(0, 28);

            // Item Checkmark (Toggle 需要)
            var checkmark = new GameObject("Item Checkmark");
            checkmark.transform.SetParent(item.transform, false);
            var ckImg = checkmark.AddComponent<Image>();
            ckImg.color = new Color(0.3f, 0.8f, 0.4f, 1f);
            var ckRt  = checkmark.GetComponent<RectTransform>();
            ckRt.anchorMin       = new Vector2(0, 0.5f);
            ckRt.anchorMax       = new Vector2(0, 0.5f);
            ckRt.anchoredPosition = new Vector2(10, 0);
            ckRt.sizeDelta       = new Vector2(10, 10);
            itemToggle.graphic   = ckImg;

            // Item Label
            var ilgo  = new GameObject("Item Label");
            ilgo.transform.SetParent(item.transform, false);
            var ilRt  = ilgo.AddComponent<RectTransform>();
            ilRt.anchorMin = Vector2.zero;
            ilRt.anchorMax = Vector2.one;
            ilRt.offsetMin = new Vector2(20, 0);
            ilRt.offsetMax = new Vector2(-4, 0);
            var ilTmp = ilgo.AddComponent<TextMeshProUGUI>();
            ilTmp.fontSize  = 12f;
            ilTmp.color     = Color.white;
            ilTmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (tmpFont != null) ilTmp.font = tmpFont;

            sr.content        = contentRt;
            sr.viewport       = vpRt;
            dropdown.template = tmRt;
            dropdown.itemText = ilTmp;

            template.SetActive(false);

            return dropdown;
        }

        private void AddButton(GameObject parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go  = new GameObject("Btn_" + label);
            go.transform.SetParent(parent.transform, false);

            var img = go.AddComponent<Image>();
            img.color = color;

            var le  = go.AddComponent<LayoutElement>();
            le.preferredHeight = 36f;
            le.flexibleWidth   = 1f;

            // TMP 文字
            var tgo  = new GameObject("Text");
            tgo.transform.SetParent(go.transform, false);
            var tRt  = tgo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var tTmp = tgo.AddComponent<TextMeshProUGUI>();
            tTmp.text      = label;
            tTmp.fontSize  = 15f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color     = Color.white;
            tTmp.alignment = TextAlignmentOptions.Center;
            if (tmpFont != null) tTmp.font = tmpFont;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.highlightedColor = new Color(
                Mathf.Min(color.r + 0.2f, 1f),
                Mathf.Min(color.g + 0.2f, 1f),
                Mathf.Min(color.b + 0.2f, 1f));
            colors.pressedColor = new Color(
                Mathf.Max(color.r - 0.15f, 0f),
                Mathf.Max(color.g - 0.15f, 0f),
                Mathf.Max(color.b - 0.15f, 0f));
            btn.colors = colors;
        }
    }
}
