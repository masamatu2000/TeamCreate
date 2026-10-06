using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 捕獲したお客さんを一時的に表示する
/// </summary>
public class CustomerPreview : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField]
    private Transform previewPoint;

    [SerializeField]
    private GameObject previewPanel;

    [SerializeField]
    private TMP_Text resultText;

    [SerializeField]
    private float displayTime = 2.5f;
    [SerializeField]
    private RenderTexture previewTexture;

    private GameObject previewObject;

    private Coroutine previewCoroutine;
    private void Start()
    {
        if (previewPanel != null)
        {
            previewPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 指定したお客さんを表示
    /// </summary>
    public void Show(
     Customer customer,
     bool isThief
 )
    {
        Debug.Log("CustomerPreview.Show が呼ばれました！");

        if (customer == null)
        {
            Debug.LogError("customer が null です！");
            return;
        }

        if (previewPanel == null)
        {
            Debug.LogError("previewPanel が設定されていません！");
            return;
        }

        if (customer == null)
        {
            return;
        }

        if (previewCoroutine != null)
        {
            StopCoroutine(
                previewCoroutine
            );

            Clear();

            if (previewPanel != null)
            {
                previewPanel.SetActive(false);
            }
        }

        previewCoroutine =
            StartCoroutine(
                ShowRoutine(
                    customer,
                    isThief
                )
            );
    }


    private IEnumerator ShowRoutine(
     Customer customer,
     bool isThief
         )
    {

        Clear();

        // 捕まえた時だけ表示
        if (previewPanel != null)
        {
            previewPanel.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text =
                isThief
                ? "泥棒だった！"
                : "お客さんだった…";
        }

        previewObject =
            Instantiate(
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


        Customer previewCustomer =
            previewObject.GetComponent<Customer>();

        if (previewCustomer != null)
        {
            previewCustomer.enabled = false;
        }


        NavMeshAgent previewAgent =
            previewObject.GetComponent<NavMeshAgent>();

        if (previewAgent != null)
        {
            previewAgent.enabled = false;
        }


        Rigidbody previewRigidbody =
            previewObject.GetComponent<Rigidbody>();

        if (previewRigidbody != null)
        {
            previewRigidbody.linearVelocity =
                Vector3.zero;

            previewRigidbody.angularVelocity =
                Vector3.zero;

            previewRigidbody.useGravity =
                false;

            previewRigidbody.isKinematic =
                true;
        }


        Animator[] animators =
            previewObject.GetComponentsInChildren<Animator>(
                true
            );

        foreach (Animator animator in animators)
        {
            animator.applyRootMotion = false;
        }


        // 数秒待つ
        yield return new WaitForSeconds(
            displayTime
        );


        // Customerコピー削除
        Clear();


        // UIも消す
        if (previewPanel != null)
        {
            previewPanel.SetActive(false);
        }

        previewCoroutine = null;
    } 


    /// <summary>
    /// 確認用Customer削除
    /// </summary>
    public void Clear()
    {
        if (previewObject != null)
        {
            Destroy(
                previewObject
            );

            previewObject =
                null;
        }
    }
}