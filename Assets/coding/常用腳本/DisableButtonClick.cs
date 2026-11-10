using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛到 Button 物件上，保留 Transition 動畫（顏色變化）但清除所有 OnClick 事件。
/// 包含 Inspector 上設定的 Persistent Listeners 以及程式碼動態加入的 Listeners。
/// </summary>
[RequireComponent(typeof(Button))]
public class DisableButtonClick : MonoBehaviour
{
    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
    }

    void Start()
    {
        // 完全取代 onClick 事件，同時清除 Persistent Listeners（Inspector 設定）
        // 以及動態加入的 Runtime Listeners
        _button.onClick = new Button.ButtonClickedEvent();
    }
}
