using TMPro;
using UnityEngine;

/// <summary>
/// 捕獲確認画面を管理するクラス
/// </summary>
public class CaptureConfirmUI : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private TMP_Text messageText;

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// 捕獲確認画面を表示する
    /// </summary>
    public void Show(Customer customer)
    {
        if (customer == null)
        {
            Debug.LogError(
                "確認対象のお客さんがnullです"
            );

            return;
        }

        if (messageText != null)
        {
            messageText.text =
                customer.name +
                "\nを捕まえますか？" +
                "\n\n「はい」または「いいえ」と話してください";
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    /// <summary>
    /// 確認画面を閉じる
    /// </summary>
    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}