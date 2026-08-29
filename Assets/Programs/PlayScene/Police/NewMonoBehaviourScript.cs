using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// “D–_‚ğ˜As‚·‚éê—pŒx”õˆõ
/// •’i‚Í”ñ•\¦B
/// “D–_Šm’è‚ÉoŒ»‚µA“D–_‚Ì‚Æ‚±‚ë‚ÖŒü‚©‚¢A
/// •ß‚Ü‚¦‚½‚ ‚ÆoŒ»’n“_‚Ü‚Å˜A‚ê‚Ä–ß‚éB
/// </summary>
public class ArrestPoliceController : MonoBehaviour
{
    [Header("ˆÚ“®")]
    [SerializeField]
    private NavMeshAgent agent;

    [Header("ƒQ[ƒ€ŠÇ—")]
    [SerializeField]
    private PlaySceneManager playSceneManager;

    [Header("˜As‚Ì‚¨‹q‚³‚ñ‚ÌˆÊ’u")]
    [SerializeField]
    private Vector3 customerCarryOffset =
        new Vector3(1.0f, 0.0f, 0.0f);
    [Header("Œx”õˆõƒ‚ƒfƒ‹")]
    [SerializeField]
    private GameObject idlePolice;

    [SerializeField]
    private GameObject walkPolice;

    // •ßŠl‘ÎÛ‚Ì“D–_
    private Customer targetCustomer;

    // ‚±‚ÌŒx”õˆõ‚ªÅ‰‚É‚¢‚½ˆÊ’u
    private Vector3 startPosition;

    private Quaternion startRotation;


    // ó‘Ô
    private bool isMovingToCustomer = false;

    private bool isReturning = false;


    private void Awake()
    {
        if (agent == null)
        {
            agent =
                GetComponent<NavMeshAgent>();
        }
        walkPolice.SetActive(false);
        idlePolice.SetActive(false);
        // Œx”õˆõ‚ÌoŒ»’n“_‚ğ•Û‘¶
        startPosition =
            transform.position;

        startRotation =
            transform.rotation;
    }


    private void Update()
    {
        if (agent == null)
        {
            return;
        }


        // =========================================
        // “D–_‚Ì‚Æ‚±‚ë‚ÖˆÚ“®’†
        // =========================================

        if (isMovingToCustomer)
        {
            if (targetCustomer == null)
            {
                CancelArrest();

                return;
            }

            if (!agent.pathPending &&
                agent.remainingDistance <=
                agent.stoppingDistance + 0.3f)
            {
                ArrivedAtCustomer();
            }

            return;
        }


        // =========================================
        // “D–_‚ğ˜A‚ê‚Ä–ß‚Á‚Ä‚¢‚é
        // =========================================

        if (isReturning)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <=
                agent.stoppingDistance + 0.3f)
            {
                FinishArrest();
            }
        }
    }

    private void ShowIdle()
    {
        if (idlePolice != null)
        {
            idlePolice.SetActive(true);
        }

        if (walkPolice != null)
        {
            walkPolice.SetActive(false);
        }
    }

    private void ShowWalk()
    {
        if (idlePolice != null)
        {
            idlePolice.SetActive(false);
        }

        if (walkPolice != null)
        {
            walkPolice.SetActive(true);
        }
    }


    /// <summary>
    /// “D–_‚Ì˜AsŠJn
    /// </summary>
    public void StartArrest(
        Customer customer)
    {
        if (customer == null)
        {
            return;
        }


        targetCustomer =
            customer;


        // ”O‚Ì‚½‚ß–ˆ‰ñŠJnˆÊ’u‚Ö–ß‚·
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(startPosition);
        }
        else
        {
            transform.position = startPosition;
        }

        transform.rotation =
            startRotation;


        gameObject.SetActive(true);


        if (agent == null)
        {
            agent =
                GetComponent<NavMeshAgent>();
        }


        if (agent == null ||
            !agent.isOnNavMesh)
        {
            Debug.LogError(
                "•ßŠl—pŒx”õˆõ‚ªNavMeshã‚É‚¢‚Ü‚¹‚ñ"
            );

            return;
        }


        isMovingToCustomer =
            true;

        isReturning =
            false;

        ShowWalk();
        agent.SetDestination(
            targetCustomer.transform.position
        );


        Debug.Log(
            "•ßŠl—pŒx”õˆõ‚ª“D–_‚Ì‚Æ‚±‚ë‚ÖŒü‚©‚¢‚Ü‚·"
        );
    }


    /// <summary>
    /// “D–_‚Ì‚Æ‚±‚ë‚Ö“’…
    /// </summary>
    private void ArrivedAtCustomer()
    {
        isMovingToCustomer =
            false;


        if (targetCustomer == null)
        {
            CancelArrest();

            return;
        }


        // “D–_‚ÌNavMeshAgent‚ğ~‚ß‚é
        NavMeshAgent customerAgent =
            targetCustomer.GetComponent<NavMeshAgent>();

        if (customerAgent != null)
        {
            customerAgent.ResetPath();

            customerAgent.enabled =
                false;
        }


        // Œx”õˆõ‚É‚Â‚¢‚Ä‚­‚é‚æ‚¤‚É‚·‚é
        targetCustomer.transform.SetParent(
            transform
        );


        targetCustomer.transform.localPosition =
            customerCarryOffset;


        Debug.Log(
            "“D–_‚ğŠm•Û‚µ‚Ü‚µ‚½BoŒ»’n“_‚Ö–ß‚è‚Ü‚·"
        );
        ShowWalk();

        // Å‰‚ÌˆÊ’u‚Ö–ß‚é
        agent.SetDestination(
            startPosition
        );


        isReturning =
            true;
    }


    /// <summary>
    /// oŒ»’n“_‚Ü‚Å–ß‚Á‚½
    /// </summary>
    private void FinishArrest()
    {
        isReturning =
            false;


        if (targetCustomer != null)
        {
            // “X“àl”‚ğŒ¸‚ç‚·
            if (playSceneManager != null)
            {
                playSceneManager.CustomerExited();
            }


            // “D–_‚ğÁ‚·
            targetCustomer.gameObject.SetActive(
                false
            );


            targetCustomer =
                null;
        }


        // Œx”õˆõ‚ğŠJn’n“_‚É–ß‚·
        transform.position =
            startPosition;

        transform.rotation =
            startRotation;


        Debug.Log(
            "“D–_‚Ì˜AsŠ®—¹"
        );

        ShowIdle();
        // ‚Ü‚½‘Ò‹@ó‘Ô‚Ö
        gameObject.SetActive(
            false
        );
    }


    /// <summary>
    /// “r’†‚Å•ßŠl‚Å‚«‚È‚­‚È‚Á‚½ê‡
    /// </summary>
    private void CancelArrest()
    {
        isMovingToCustomer =
            false;

        isReturning =
            false;

        targetCustomer =
            null;


        transform.position =
            startPosition;

        transform.rotation =
            startRotation;


        gameObject.SetActive(
            false
        );
    }
}