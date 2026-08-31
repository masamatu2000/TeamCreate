using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ïﬂälëŒè€ÇÃÇ®ãqÇ≥ÇÒÇämîFâÊñ ópÇ…ï\é¶Ç∑ÇÈ
/// </summary>
public class CustomerPreview : MonoBehaviour
{
    [SerializeField]
    private Transform previewPoint;

    private GameObject previewObject;

    /// <summary>
    /// éwíËÇµÇΩÇ®ãqÇ≥ÇÒÇämîFâÊñ Ç…ï\é¶Ç∑ÇÈ
    /// </summary>
    public void Show(Customer customer)
    {
        if (customer == null)
        {
            return;
        }

        Clear();

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

        // Customerí‚é~
        Customer previewCustomer =
            previewObject.GetComponent<Customer>();

        if (previewCustomer != null)
        {
            previewCustomer.enabled = false;
        }

        // NavMeshAgentí‚é~
        NavMeshAgent previewAgent =
            previewObject.GetComponent<NavMeshAgent>();

        if (previewAgent != null)
        {
            previewAgent.enabled = false;
        }

        // Rigidbodyí‚é~
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
    previewObject.GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            animator.applyRootMotion = false;
            animator.enabled = false;
        }
    }

    /// <summary>
    /// ämîFópÇÃÇ®ãqÇ≥ÇÒÇçÌèúÇ∑ÇÈ
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