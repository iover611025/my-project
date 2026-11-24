using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛到 Button 物件上，保留 Transition 動畫（顏色變化）但在點擊後才清除所有 OnClick 事件。
/// 點擊當幀所有 Listeners（含 Inspector Persistent 與動態 Runtime）正常執行，
/// 下一幀再統一清除，避免重複點擊觸發。
/// </summary>
[RequireComponent(typeof(Button))]
public class DisableButtonClick : MonoBehaviour
{
    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
    }

    void OnEnable()
    {
        // 每次物件啟用時，註冊哨兵 Listener
        _button.onClick.AddListener(OnButtonClicked);
    }

    void OnDisable()
    {
        // 物件關閉時移除哨兵，避免重複累積
        _button.onClick.RemoveListener(OnButtonClicked);
    }

    void OnButtonClicked()
    {
        // 點擊當幀讓所有 Listeners 正常執行，下一幀再清除
        StartCoroutine(ClearNextFrame());
    }

    IEnumerator ClearNextFrame()
    {
        yield return null;
        // 完全取代 onClick 事件，同時清除 Persistent Listeners（Inspector 設定）
        // 以及動態加入的 Runtime Listeners
        _button.onClick = new Button.ButtonClickedEvent();
    }
}
