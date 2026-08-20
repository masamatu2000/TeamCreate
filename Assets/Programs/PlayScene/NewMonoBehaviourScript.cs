using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 捕獲対象のお客さんを確認画面用に表示する
/// </summary>
public class CustomerPreview : MonoBehaviour
{
    [SerializeField]
    private Transform previewPoint;

    private GameObject previewObject;

    /// <summary>
    /// 指定したお客さんを確認画面に表示する
    /// </summary>
    public void Show(Customer customer)
    {
        if (customer == null)
        {
            return;
        }

        // 前回のお客さんを消す
        Clear();

        // お客さんを複製
        previewObject = Instantiate(
            customer.gameObject,
            previewPoint.position,
            previewPoint.rotation
        );

        previewObject.transform.SetParent(
            previewPoint
        );

        previewObject.transform.localPosition =
            Vector3.zero;

        previewObject.transform.localRotation =
            Quaternion.identity;

        // Customerの処理を止める
        Customer previewCustomer =
            previewObject.GetComponent<Customer>();

        if (previewCustomer != null)
        {
            previewCustomer.enabled = false;
        }

        // NavMeshAgentも止める
        NavMeshAgent previewAgent =
            previewObject.GetComponent<NavMeshAgent>();

        if (previewAgent != null)
        {
            previewAgent.enabled = false;
        }
    }

    /// <summary>
    /// 確認用のお客さんを削除する
    /// </summary>
    public void Clear()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }
}