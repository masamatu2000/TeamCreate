using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TutorialCustomer : MonoBehaviour
{
    [Header("Customer")]
    [SerializeField]
    private NavMeshAgent agent;

    [SerializeField]
    private Animator animator;


    [Header("Thief")]
    [SerializeField]
    private bool isThief = false;


    private Transform targetCorner;

    private bool isArrested = false;

    // 泥棒行動を何度も開始しないため
    private bool thiefActionStarted = false;


    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }


    public void SetThief(bool value)
    {
        isThief = value;
    }


    public bool IsThief()
    {
        return isThief;
    }


    public void SetCorner(Transform corner)
    {
        if (corner == null)
        {
            return;
        }

        targetCorner = corner;

        MoveToCorner();
    }


    private void MoveToCorner()
    {
        if (agent == null ||
            targetCorner == null)
        {
            return;
        }

        agent.isStopped = false;

        agent.SetDestination(
            targetCorner.position
        );
    }


    private void Update()
    {
        if (isArrested)
        {
            return;
        }

        if (agent == null)
        {
            return;
        }


        // ==========================================
        // 移動中
        // ==========================================

        bool isMoving =
            agent.hasPath &&
            agent.remainingDistance >
            agent.stoppingDistance;


        if (isMoving)
        {
            if (animator != null)
            {
                animator.SetBool(
                    "Walk",
                    true
                );
            }

            return;
        }


        // ==========================================
        // 到着
        // ==========================================

        if (animator != null)
        {
            animator.SetBool(
                "Walk",
                false
            );
        }


        // ==========================================
        // 泥棒なら怪しい行動開始
        // ==========================================

        if (isThief &&
            !thiefActionStarted)
        {
            thiefActionStarted = true;

            StartCoroutine(
                ThiefActionRoutine()
            );
        }
    }


    // =========================================================
    // 泥棒専用アニメーション
    // =========================================================

    private IEnumerator ThiefActionRoutine()
    {
        if (animator == null)
        {
            yield break;
        }


        // 少し普通に立つ
        yield return new WaitForSeconds(
            1.0f
        );


        // ------------------------------------------
        // きょろきょろ
        // ------------------------------------------

        animator.SetTrigger(
            "LookAround"
        );

        yield return new WaitForSeconds(
            2.0f
        );


        // ------------------------------------------
        // 少し待つ
        // ------------------------------------------

        yield return new WaitForSeconds(
            0.5f
        );


        // ------------------------------------------
        // しゃがむ / 商品を取る
        // ------------------------------------------

        animator.SetTrigger(
            "CrouchPick"
        );

        yield return new WaitForSeconds(
            2.0f
        );


        // ------------------------------------------
        // 再び怪しい行動を繰り返す
        // ------------------------------------------

        thiefActionStarted = false;
    }


    public void Arrest()
    {
        isArrested = true;

        StopAllCoroutines();

        if (agent != null)
        {
            agent.isStopped = true;
        }

        if (animator != null)
        {
            animator.SetBool(
                "Walk",
                false
            );
        }

        Debug.Log(
            $"{gameObject.name} を確保しました"
        );
    }
}