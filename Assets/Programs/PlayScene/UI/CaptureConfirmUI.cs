using UnityEngine;

/// <summary>
/// 捕獲確認画面を管理するクラス
/// </summary>
public class CaptureConfirmUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private CustomerPreview customerPreview;

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
            Debug.LogError("確認対象のお客さんがnullです");
            return;
        }

        // お客さんの3Dプレビューを表示
        if (customerPreview != null)
        {
            customerPreview.Show(customer);
        }

        // 確認画面を表示
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

        if (customerPreview != null)
        {
            customerPreview.Clear();
        }
    }
}