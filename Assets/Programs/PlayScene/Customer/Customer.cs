
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// ========================================
// アニメーション状態
// ========================================
public enum CustomerAnimationState
{
    Idle = 0,
    Walk = 1,
    FastWalk = 2,
    LookAround = 3,
    CrouchPick = 4,
    TakeItem = 5,
    ArrestedWalk = 6
}

/// <summary>
/// お客さん1人分の動作を管理する
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Customer : MonoBehaviour
{
    // ========================================
    // 移動設定
    // ========================================

    [Header("移動設定")]

    [SerializeField]
    private float moveRadius = 5.0f;

    [SerializeField]
    private float minWaitTime = 2.0f;

    [SerializeField]
    private float maxWaitTime = 5.0f;

    private Rigidbody rb;


    // ========================================
    // NavMesh検索設定
    // ========================================

    [Header("NavMesh検索設定")]

    [SerializeField]
    private float navMeshSampleDistance = 1.0f;

    [SerializeField]
    private int positionSearchCount = 30;


    // ========================================
    // 移動速度
    // ========================================

    [Header("移動速度")]

    [SerializeField]
    private float normalSpeed = 2.0f;

    [SerializeField]
    private float thiefSpeed = 3.5f;

    [SerializeField]
    private float suspiciousFastWalkSpeed = 3.2f;

    [SerializeField]
    private float suspiciousFastWalkTime = 2.5f;

    [Header("お客さん同士の重なり防止")]

    [SerializeField]
    private float minimumAgentRadius = 0.4f;

    [SerializeField]
    [Range(0, 99)]
    private int avoidancePriorityMin = 20;

    [SerializeField]
    [Range(0, 99)]
    private int avoidancePriorityMax = 80;

    [Header("大きなコーナーの移動設定")]

    [Tooltip("お菓子・飲料コーナーで、同じコーナー内の別の場所へ移動する確率")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float stayInLargeCornerRate = 0.6f;

    // 現在使用しているActionPoint
    private Transform currentActionPoint;

    private Transform reservedActionPoint;

    private static readonly HashSet<Transform> reservedActionPoints =
        new HashSet<Transform>();
    // ========================================
    // 不審行動の確率
    // ========================================

    [Header("不審行動の確率")]

    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float normalCustomerSuspiciousRate = 0.1f;

    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float thiefSuspiciousRate = 0.3f;


    // ========================================
    // 各コーナー
    // ========================================

    [Header("各コーナー")]

    [SerializeField]
    private Transform fishCorner;

    [SerializeField]
    private Transform vegetableCorner;

    [SerializeField]
    private Transform snackCorner;

    [SerializeField]
    private Transform frozenFoodCorner;

    [SerializeField]
    private Transform drinkCorner;

    [SerializeField]
    private Transform preparedFoodCorner;

    [SerializeField]
    private Transform meatCorner;

    [SerializeField]
    private Transform breadCorner;


    // ========================================
    // 泥棒設定
    // ========================================

    [Header("泥棒設定")]

    [SerializeField]
    private Transform police;

    [SerializeField]
    private float escapeDistance = 8.0f;

    [Tooltip("警備員が近づいたら、現在のコーナーから逃げる距離")]
    [SerializeField]
    private float thiefMoveRadius = 2.0f;


    // ========================================
    // お客さんの特徴
    // ========================================

    [Header("お客さんの特徴")]

    [SerializeField]
    private CustomerColor clothesColor;

    [SerializeField]
    private bool wearsHat;
    [SerializeField]
    private CustomerColor hatColor = CustomerColor.None;
    [SerializeField]
    private bool wearsGlasses;

    [SerializeField]
    private bool hasBag;

    [SerializeField]
    private bool isThief;


    // ========================================
    // アニメーション
    // ========================================

    [Header("アニメーション")]

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private float idleBeforeActionTime = 0.15f;

    [SerializeField]
    private float maxCornerActionTime = 10.0f;

    [Header("移動停止からの復旧")]

    [SerializeField]
    private float stuckTimeLimit = 3.0f;

    [SerializeField]
    private float stuckMoveThreshold = 0.03f;


    // ========================================
    // その他
    // ========================================

    [SerializeField]
    private PlaySceneManager playSceneManager;


    // ========================================
    // 公開プロパティ
    // ========================================

    public bool IsThief => isThief;

    public bool IsCaught { get; private set; }


    // ========================================
    // 内部変数
    // ========================================

    private NavMeshAgent agent;

    private Transform currentCorner;

    private Transform[] corners;

    private float waitTimer;

    private bool isWaiting;

    private bool isLookingAround;

    private bool isSuspiciousFastWalking;

    private bool hasEscaped;

    private bool wasGameStarted;

    private bool hasMoveDestination;

    private Vector3 moveDestination;

    private Vector3 lastMoveCheckPosition;

    private float stuckTimer;

    private CustomerAnimationState currentAnimationState =
        (CustomerAnimationState)(-1);


    // ========================================
    // Awake
    // ========================================

    private void Awake()
    {
        agent =
            GetComponent<NavMeshAgent>();

        agent.radius =
            Mathf.Max(
                agent.radius,
                minimumAgentRadius
            );

        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        int minimumPriority =
            Mathf.Min(
                avoidancePriorityMin,
                avoidancePriorityMax
            );

        int maximumPriority =
            Mathf.Max(
                avoidancePriorityMin,
                avoidancePriorityMax
            );

        agent.avoidancePriority =
            Random.Range(
                minimumPriority,
                maximumPriority + 1
            );

        rb =
            GetComponent<Rigidbody>();


        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }


        corners = new Transform[]
        {
            fishCorner,
            vegetableCorner,
            snackCorner,
            frozenFoodCorner,
            drinkCorner,
            preparedFoodCorner,
            meatCorner,
            breadCorner
        };
    }


    // ========================================
    // Start
    // ========================================

    private void Start()
    {

        if (playSceneManager == null)
        {
            playSceneManager =
                FindAnyObjectByType<PlaySceneManager>();
        }


        PlaceAtRandomCorner();

        lastMoveCheckPosition =
            transform.position;


        SetMoveSpeed();


        // ========================================
        // 最初の目的地
        // ========================================

        if (isThief)
        {
            SetDestinationAroundCurrentCorner();
        }
        else
        {
            SetDestinationAroundCorner(
                currentCorner,
                moveRadius
            );
        }


        // ========================================
        // ゲーム開始前は動かさない
        // ========================================

        if (playSceneManager != null &&
            !playSceneManager.IsGameStarted())
        {
            StopAgent();
        }


        wasGameStarted =
            playSceneManager != null &&
            playSceneManager.IsGameStarted();


        SetAnimation(
            CustomerAnimationState.Idle
        );
    }


    // ========================================
    // Rigidbody固定
    // ========================================

    private void FreezeCustomer()
    {
        if (rb == null)
        {
            return;
        }


        rb.constraints =
            RigidbodyConstraints.FreezePosition |
            RigidbodyConstraints.FreezeRotation;



    }


    private void UnfreezeCustomer()
    {
        if (rb == null)
        {
            return;
        }


        rb.constraints =
            RigidbodyConstraints.FreezeRotation;
    }


    // ========================================
    // Update
    // ========================================

    private void Update()
    {
        if (agent == null)
        {
            return;
        }


        bool isGameStarted =
            playSceneManager == null ||
            playSceneManager.IsGameStarted();


        // ========================================
        // ゲーム開始前
        // ========================================

        if (!isGameStarted)
        {
            StopAgent();

            FreezeCustomer();

            UpdateAnimation();

            wasGameStarted =
                false;

            return;
        }


        // ========================================
        // ゲーム開始直後
        // ========================================

        if (!wasGameStarted)
        {
            wasGameStarted =
                true;


            UnfreezeCustomer();


            if (!IsCaught &&
                !isWaiting &&
                !isLookingAround)
            {
                ResumeAgent();
            }
        }


        // ========================================
        // 捕獲済み
        // ========================================

        if (IsCaught)
        {
            UpdateAnimation();

            return;
        }


        // ========================================
        // 見回し中
        // ========================================

        if (isLookingAround)
        {
            StopAgent();

            UpdateAnimation();

            return;
        }


        // ========================================
        // 商品を取る・待機中
        // ========================================

        if (isWaiting)
        {
            StopAgent();

            UpdateAnimation();

            return;
        }


        // ========================================
        // 通常移動
        // ========================================

        ResumeAgent();


        // ========================================
        // 泥棒は警備員との距離を確認
        // ========================================

        if (isThief &&
            !hasEscaped)
        {
            CheckPoliceDistance();
        }


        // ========================================
        // 経路の計算中
        // ========================================

        if (agent.pathPending)
        {
            UpdateAnimation();

            return;
        }


        // ========================================
        // 棚や他のお客さんに引っかかった場合の復旧
        // ========================================

        if (CheckAndRecoverFromStuck())
        {
            UpdateAnimation();

            return;
        }


        // ========================================
        // 目的地への到着確認
        // ========================================

        Vector3 destinationOffset =
            moveDestination - transform.position;

        destinationOffset.y =
            0.0f;

        float arrivalDistance =
            agent.stoppingDistance + 0.3f;

        if (hasMoveDestination &&
            destinationOffset.sqrMagnitude <=
                arrivalDistance * arrivalDistance &&
            (!agent.hasPath ||
             agent.velocity.sqrMagnitude <= 0.01f))
        {
            hasMoveDestination =
                false;

            StartWaiting();

            UpdateAnimation();

            return;
        }


        UpdateAnimation();
    }


    // ========================================
    // 移動中に一定時間ほとんど進まなかった場合、
    // 別の目的地を設定して復旧する
    // ========================================

    private bool CheckAndRecoverFromStuck()
    {
        if (agent == null ||
            !agent.isOnNavMesh ||
            agent.isStopped ||
            agent.pathPending)
        {
            stuckTimer = 0.0f;
            lastMoveCheckPosition = transform.position;

            return false;
        }

        // 目的地の設定自体に失敗した場合も、
        // 少し待ってから別の目的地を探し直す。
        if (!hasMoveDestination)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer < stuckTimeLimit)
            {
                return false;
            }

            stuckTimer = 0.0f;

            if (isThief)
            {
                SetDestinationAroundCurrentCorner();
            }
            else
            {
                MoveToRandomCorner();
            }

            SetAnimation(
                CustomerAnimationState.Walk
            );

            ResumeAgent();

            return true;
        }

        Vector3 movedOffset =
            transform.position - lastMoveCheckPosition;

        movedOffset.y =
            0.0f;

        float moveThresholdSqr =
            stuckMoveThreshold * stuckMoveThreshold;

        if (movedOffset.sqrMagnitude <= moveThresholdSqr &&
            agent.velocity.sqrMagnitude <= 0.01f)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0.0f;
        }

        lastMoveCheckPosition =
            transform.position;

        if (stuckTimer < stuckTimeLimit)
        {
            return false;
        }

        stuckTimer = 0.0f;
        hasMoveDestination = false;

        agent.ResetPath();

        if (isThief)
        {
            SetDestinationAroundCurrentCorner();
        }
        else
        {
            MoveToRandomCorner();
        }

        SetAnimation(
            CustomerAnimationState.Walk
        );

        ResumeAgent();

        return true;
    }


    // ========================================
    // NavMeshAgent停止
    // ========================================

    private void StopAgent()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }


        agent.isStopped =
            true;

        agent.velocity =
            Vector3.zero;
    }


    // ========================================
    // NavMeshAgent再開
    // ========================================

    private void ResumeAgent()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }


        agent.isStopped =
            false;
    }


    // ========================================
    // アニメーション更新
    // ========================================

    private void UpdateAnimation()
    {
        if (animator == null ||
            agent == null)
        {
            return;
        }


        // ========================================
        // 捕獲済み
        // ========================================

        if (IsCaught)
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );


            SetAnimation(
                CustomerAnimationState.ArrestedWalk
            );

            return;
        }


        // ========================================
        // ゲーム開始前
        // ========================================

        if (playSceneManager != null &&
            !playSceneManager.IsGameStarted())
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );

            return;
        }


        // ========================================
        // 商品を取る動作
        // ========================================

        if (isWaiting)
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );

            return;
        }


        // ========================================
        // 見回し中
        // ========================================

        if (isLookingAround)
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );

            return;
        }


        // ========================================
        // 通常移動
        // ========================================

        // ゲーム開始後、行動中でなければ必ずWalk。
        // NavMeshAgentの速度が一瞬0になってもIdleへ戻さない。
        animator.SetFloat(
            "Speed",
            1.0f
        );

        SetAnimation(
            CustomerAnimationState.Walk
        );
    }


    // ========================================
    // 状態に応じたアニメーション切り替え
    // ========================================

    private void SetAnimation(
        CustomerAnimationState state)
    {
        if (animator == null)
        {
            return;
        }


        if (currentAnimationState ==
            state)
        {
            return;
        }


        currentAnimationState =
            state;


        animator.SetInteger(
     "AnimationState",
     (int)state
 );
    }


    // ========================================
    // 通常の移動速度設定
    // ========================================

    private void SetMoveSpeed()
    {
        if (agent == null)
        {
            return;
        }


        if (isSuspiciousFastWalking)
        {
            agent.speed =
                suspiciousFastWalkSpeed;

            return;
        }


        if (isThief &&
            hasEscaped)
        {
            agent.speed =
                thiefSpeed;
        }
        else
        {
            agent.speed =
                normalSpeed;
        }
    }


    // ========================================
    // 目的地の設定
    // ========================================

    private void StartWaiting()
    {
        if (isWaiting ||
            isLookingAround ||
            isSuspiciousFastWalking ||
            IsCaught)
        {
            return;
        }


        StopAgent();

        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        isWaiting =
            true;

        isLookingAround =
            false;

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );
        }


        // ========================================
        // 一般客 10%
        // 泥棒 30%
        // ========================================

        CustomerAnimationState actionState =
            CustomerAnimationState.TakeItem;

        bool playIdleFirst =
            isThief;

        // 一般客はWalkとPickだけを使用する。
        // 泥棒だけは従来の不審行動を残す。
        if (isThief)
        {
            SetAnimation(
                CustomerAnimationState.Idle
            );

            if (Random.value < thiefSuspiciousRate)
            {
                actionState =
                    Random.value < 0.5f
                        ? CustomerAnimationState.CrouchPick
                        : CustomerAnimationState.LookAround;
            }
        }

        StartCoroutine(
            PlayCornerAction(
                actionState,
                playIdleFirst
            )
        );
    }


    // ========================================
    // Idleからコーナー行動を再生し、
    // モーションが終わってから次の移動を始める
    // ========================================

    private IEnumerator PlayCornerAction(
        CustomerAnimationState actionState,
        bool playIdleFirst)
    {
        if (playIdleFirst)
        {
            yield return new WaitForSeconds(
                idleBeforeActionTime
            );
        }
        else
        {
            yield return null;
        }

        if (IsCaught)
        {
            yield break;
        }

        int idleStateHash =
            animator != null
                ? animator.GetCurrentAnimatorStateInfo(0).fullPathHash
                : 0;

        SetAnimation(actionState);

        bool actionStarted =
            false;

        int actionStateHash =
            0;

        float elapsedTime =
            0.0f;

        // AnimatorがIdleから選択したモーションへ
        // 切り替わるまで待つ。
        while (animator != null &&
               elapsedTime < 1.0f)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(0);

            if (!animator.IsInTransition(0) &&
                stateInfo.fullPathHash != idleStateHash)
            {
                actionStarted =
                    true;

                actionStateHash =
                    stateInfo.fullPathHash;

                break;
            }

            elapsedTime +=
                Time.deltaTime;

            yield return null;
        }

        elapsedTime =
            0.0f;

        if (actionStarted)
        {
            // 選択したモーションが1周終わるまで待つ。
            while (animator != null &&
                   elapsedTime < maxCornerActionTime)
            {
                AnimatorStateInfo stateInfo =
                    animator.GetCurrentAnimatorStateInfo(0);

                bool animationFinished =
                    !animator.IsInTransition(0) &&
                    stateInfo.fullPathHash == actionStateHash &&
                    stateInfo.normalizedTime >= 1.0f;

                if (animationFinished)
                {
                    break;
                }

                elapsedTime +=
                    Time.deltaTime;

                yield return null;
            }
        }
        else
        {
            // 遷移設定に問題があっても永久停止しないための予備待機。
            yield return new WaitForSeconds(
                Mathf.Max(0.5f, minWaitTime)
            );
        }

        if (IsCaught)
        {
            yield break;
        }

        // Pickの姿勢のまま移動しないように、
        // 先にAnimatorをWalkへ切り替える。
        SetAnimation(
            CustomerAnimationState.Walk
        );

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                1.0f
            );

            animator.CrossFade(
                "Base Layer.Walk",
                0.1f,
                0
            );

            float walkTransitionTimer =
                0.0f;

            // Animatorが実際にWalkへ入るまで、
            // NavMeshAgentは停止したまま待つ。
            while (walkTransitionTimer < 1.0f)
            {
                AnimatorStateInfo stateInfo =
                    animator.GetCurrentAnimatorStateInfo(0);

                if (!animator.IsInTransition(0) &&
                    stateInfo.IsName("Base Layer.Walk"))
                {
                    break;
                }

                walkTransitionTimer +=
                    Time.deltaTime;

                yield return null;
            }
        }

        isWaiting =
            false;

        MoveAfterWaiting();
    }


    // ========================================
    // 通常行動
    // ========================================

    private void StartNormalAction()
    {
        isWaiting =
            true;


        waitTimer =
            Random.Range(
                minWaitTime,
                maxWaitTime
            );


        SetAnimation(
            CustomerAnimationState.TakeItem
        );


        //Debug.Log(
        //    "{gameObject.name}：" +
        //    "通常行動：商品を取る"
        //);
    }


    // ========================================
    // 不審行動
    // ========================================

    private void StartSuspiciousAction()
    {
        int randomAction =
            Random.Range(
                0,
                3
            );


        switch (randomAction)
        {
            // ========================================
            // しゃがんで商品を取る
            // ========================================

            case 0:

                isWaiting =
                    true;


                waitTimer =
                    Random.Range(
                        minWaitTime,
                        maxWaitTime
                    );


                SetAnimation(
                    CustomerAnimationState.CrouchPick
                );


                //Debug.Log(
                //    "{gameObject.name}：" +
                //    "不審行動：しゃがんで商品を取る"
                //);

                break;


            // ========================================
            // 周囲を見回す
            // ========================================

            case 1:

                isLookingAround =
                    true;


                StartCoroutine(
                    LookAroundBeforeMove()
                );


                //Debug.Log(
                //    "{gameObject.name}：" +
                //    "不審行動：周囲を見回す"
                //);

                break;


            // ========================================
            // 早歩き
            // ========================================

            case 2:

                StartCoroutine(
                    SuspiciousFastWalk()
                );


                //Debug.Log(
                //    "{gameObject.name}：" +
                //    "不審行動：早歩き"
                //);

                break;
        }
    }


    // ========================================
    // 商品を取る動作中
    // ========================================

    private void Wait()
    {
        waitTimer -=
            Time.deltaTime;


        if (waitTimer >
            0.0f)
        {
            return;
        }


        isWaiting =
            false;


        MoveAfterWaiting();
    }


    // ========================================
    // 周囲を見回す
    // ========================================

    private IEnumerator LookAroundBeforeMove()
    {
        StopAgent();


        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );
        }


        SetAnimation(
            CustomerAnimationState.LookAround
        );


        yield return
            new WaitForSeconds(
                2.0f
            );


        isLookingAround =
            false;


        MoveAfterWaiting();
    }


    // ========================================
    // 不審な早歩き
    // ========================================

    private IEnumerator SuspiciousFastWalk()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            yield break;
        }


        isSuspiciousFastWalking =
            true;


        agent.speed =
            suspiciousFastWalkSpeed;


        // ========================================
        // 次の行き先を設定
        // ========================================

        if (isThief)
        {
            SetDestinationAroundCurrentCorner();
        }
        else
        {
            MoveToRandomCorner();
        }


        ResumeAgent();


        yield return
            new WaitForSeconds(
                suspiciousFastWalkTime
            );


        isSuspiciousFastWalking =
            false;


        SetMoveSpeed();
    }


    // ========================================
    // 行動終了後
    // ========================================

    // ========================================
    // 行動終了後
    // ========================================

    private void MoveAfterWaiting()
    {
        if (IsCaught)
        {
            return;
        }

        isWaiting = false;
        isLookingAround = false;

        SetAnimation(
            CustomerAnimationState.Walk
        );

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                1.0f
            );
        }


        // ========================================
        // 泥棒
        // ========================================

        if (isThief)
        {
            SetDestinationAroundCurrentCorner();

            ResumeAgent();

            return;
        }


        // ========================================
        // 大きなコーナーの場合
        //
        // お菓子・飲料
        // ========================================

        bool isLargeCorner =
            currentCorner == snackCorner ||
            currentCorner == drinkCorner;


        if (isLargeCorner)
        {
            // ========================================
            // 一定の確率で
            // 同じコーナーの別の場所に移動
            // ========================================

            if (Random.value <
                stayInLargeCornerRate)
            {
                SetDestinationAroundCorner(
                    currentCorner,
                    moveRadius
                );

                ResumeAgent();

                return;
            }
        }


        // ========================================
        // 別のコーナーへ移動
        // ========================================

        MoveToRandomCorner();

        ResumeAgent();
    }


    // ========================================
    // ゲーム開始時の初期配置
    // ========================================

    private void PlaceAtRandomCorner()
    {
        if (corners == null ||
            corners.Length == 0)
        {
            Debug.LogWarning(
                $"{gameObject.name}：" +
                "コーナーが設定されていません"
            );

            return;
        }


        bool[] checkedCorners =
            new bool[corners.Length];


        int checkedCount =
            0;


        while (checkedCount <
            corners.Length)
        {
            int randomIndex =
                Random.Range(
                    0,
                    corners.Length
                );


            if (checkedCorners[randomIndex])
            {
                continue;
            }


            checkedCorners[randomIndex] =
                true;


            checkedCount++;


            Transform selectedCorner =
                corners[randomIndex];


            if (selectedCorner == null)
            {
                continue;
            }


            currentCorner =
                selectedCorner;


            for (int i = 0;
                 i < positionSearchCount;
                 i++)
            {
                Vector2 randomCircle =
                    Random.insideUnitCircle *
                    moveRadius;


                Vector3 randomPosition =
                    currentCorner.position +
                    new Vector3(
                        randomCircle.x,
                        0.0f,
                        randomCircle.y
                    );


                NavMeshHit hit;


                if (NavMesh.SamplePosition(
                    randomPosition,
                    out hit,
                    navMeshSampleDistance,
                    NavMesh.AllAreas))
                {
                    if (agent.Warp(
                        hit.position))
                    {
                        return;
                    }
                }
            }


            NavMeshHit centerHit;


            if (NavMesh.SamplePosition(
                currentCorner.position,
                out centerHit,
                navMeshSampleDistance,
                NavMesh.AllAreas))
            {
                if (agent.Warp(
                    centerHit.position))
                {
                    return;
                }
            }
        }


        Debug.LogError(
            $"{gameObject.name}：" +
            "すべてのコーナーで配置に失敗しました"
        );
    }


    // ========================================
    // 一般客
    // 別のコーナーへ
    // ========================================

    private void MoveToRandomCorner()
    {
        if (corners == null ||
            corners.Length == 0)
        {
            return;
        }


        Transform nextCorner =
            GetRandomDifferentCorner();


        if (nextCorner == null)
        {
            return;
        }


        currentCorner =
            nextCorner;


        SetDestinationAroundCorner(
            currentCorner,
            moveRadius
        );
    }


    // ========================================
    // 現在とは異なるコーナー
    // ========================================

    private Transform GetRandomDifferentCorner()
    {
        int validCornerCount =
            0;


        foreach (Transform corner
                 in corners)
        {
            if (corner != null)
            {
                validCornerCount++;
            }
        }


        if (validCornerCount ==
            0)
        {
            return null;
        }


        if (validCornerCount ==
            1)
        {
            foreach (Transform corner
                     in corners)
            {
                if (corner != null)
                {
                    return corner;
                }
            }
        }


        for (int i = 0;
             i < 20;
             i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    corners.Length
                );


            Transform selectedCorner =
                corners[randomIndex];


            if (selectedCorner == null)
            {
                continue;
            }


            if (selectedCorner ==
                currentCorner)
            {
                continue;
            }


            return selectedCorner;
        }


        return currentCorner;
    }


    // ========================================
    // 指定コーナーへ移動
    // ========================================

    private void SetDestinationAroundCorner(
        Transform corner,
        float radius)
    {

        ReleaseActionPointReservation();

        hasMoveDestination =
            false;

        if (corner == null ||
            agent == null)
        {
            return;
        }


        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning(
                $"{gameObject.name} はNavMesh上にいません"
            );

            return;
        }

        // ========================================
        // ActionPointを優先
        // ========================================

        Transform actionPoint =
            GetRandomActionPoint(
                corner
            );

        if (actionPoint != null)
        {



            NavMeshHit actionHit;


            if (NavMesh.SamplePosition(
                actionPoint.position,
                out actionHit,
                3.0f,
                NavMesh.AllAreas))
            {



                NavMeshPath actionPath =
                    new NavMeshPath();


                if (agent.CalculatePath(
                    actionHit.position,
                    actionPath))
                {
                    if (actionPath.status ==
                        NavMeshPathStatus.PathComplete)
                    {



                        agent.SetDestination(
                            actionHit.position
                        );

                        moveDestination =
                            actionHit.position;

                        hasMoveDestination =
                            true;

                        stuckTimer =
                            0.0f;

                        lastMoveCheckPosition =
                            transform.position;

                        return;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"{actionPoint.name} までの経路が不完全です"
                        );
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    $"{actionPoint.name} の近くにNavMeshがありません"
                );
            }
        }

        // ActionPointまでの経路が作れなかった場合は予約を解放し、
        // コーナー周辺のランダム地点を探す。
        ReleaseActionPointReservation();
        //else
        //{
        //    //Debug.LogWarning(
        //    "{corner.name} にActionPointが見つかりません"
        //    //);
        //}


        // ========================================
        // ActionPointに行けない場合は周辺へ移動
        // ========================================

        for (int i = 0;
             i < positionSearchCount;
             i++)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle *
                radius;


            Vector3 randomPosition =
                corner.position +
                new Vector3(
                    randomCircle.x,
                    0.0f,
                    randomCircle.y
                );


            NavMeshHit hit;


            if (!NavMesh.SamplePosition(
                randomPosition,
                out hit,
                navMeshSampleDistance,
                NavMesh.AllAreas))
            {
                continue;
            }


            NavMeshPath path =
                new NavMeshPath();


            if (agent.CalculatePath(
                hit.position,
                path))
            {
                if (path.status ==
                    NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(
                        hit.position
                    );

                    moveDestination =
                        hit.position;

                    hasMoveDestination =
                        true;

                    stuckTimer =
                        0.0f;

                    lastMoveCheckPosition =
                        transform.position;

                    return;
                }
            }
        }


        Debug.LogWarning(
            $"{gameObject.name}：" +
            $"{corner.name}周辺に移動先が見つかりませんでした"
        );
    }


    // ========================================
    // ActionPoint取得
    // ========================================

    // ========================================
    // コーナー内の移動先を取得
    // 前回と異なるActionPointを優先
    // ========================================

    private Transform GetRandomActionPoint(
        Transform corner)
    {

        if (corner == null)
        {
            return null;
        }

        reservedActionPoints.RemoveWhere(
            point => point == null
        );

        Transform[] children =
            corner.GetComponentsInChildren<Transform>();

        List<Transform> actionPoints =
            new List<Transform>();

        foreach (Transform child in children)
        {
            if (child == corner)
            {
                continue;
            }

            if (!child.name.Contains("ActionPoint"))
            {
                continue;
            }

            // ActionPointが設定されている場合は
            // 前回と同じ場所を除外
            if (child == currentActionPoint)
            {
                continue;
            }

            // ほかのお客さんが使用中の場所は選ばない。
            if (reservedActionPoints.Contains(child))
            {
                continue;
            }

            actionPoints.Add(child);
        }

        // 前回と異なる場所がない場合は
        // 同じ場所も候補にして再取得
        if (actionPoints.Count == 0)
        {
            foreach (Transform child in children)
            {
                if (child == corner)
                {
                    continue;
                }

                if (child.name.Contains("ActionPoint") &&
                    !reservedActionPoints.Contains(child))
                {
                    actionPoints.Add(child);
                }
            }
        }

        if (actionPoints.Count == 0)
        {
            return null;
        }

        int randomIndex =
            Random.Range(
                0,
                actionPoints.Count
            );

        currentActionPoint =
            actionPoints[randomIndex];

        reservedActionPoint =
            currentActionPoint;

        reservedActionPoints.Add(
            reservedActionPoint
        );

        return currentActionPoint;
    }


    // ========================================
    // 使用中のActionPoint予約を解放
    // ========================================

    private void ReleaseActionPointReservation()
    {
        if (reservedActionPoint == null)
        {
            return;
        }

        reservedActionPoints.Remove(
            reservedActionPoint
        );

        reservedActionPoint =
            null;
    }


    private void OnDisable()
    {
        ReleaseActionPointReservation();
    }


    // ========================================
    // 泥棒
    // 現在のコーナー周辺へ移動
    // ========================================

    private void SetDestinationAroundCurrentCorner()
    {
        if (currentCorner == null)
        {
            return;
        }


        float radius =
            hasEscaped
                ? thiefMoveRadius
                : moveRadius;


        SetDestinationAroundCorner(
            currentCorner,
            radius
        );
    }


    // ========================================
    // 警備員との距離
    // ========================================

    private void CheckPoliceDistance()
    {
        if (police == null)
        {
            return;
        }


        float distance =
            Vector3.Distance(
                transform.position,
                police.position
            );


        if (distance <=
            escapeDistance)
        {
            EscapeFromPolice();
        }
    }


    // ========================================
    // 泥棒の逃走
    // ========================================

    private void EscapeFromPolice()
    {
        if (hasEscaped)
        {
            return;
        }


        hasEscaped =
            true;


        isWaiting =
            false;

        isLookingAround =
            false;

        isSuspiciousFastWalking =
            false;


        StopAllCoroutines();


        SetMoveSpeed();


        Transform escapeCorner =
            GetFarthestCornerFromPolice();


        if (escapeCorner == null)
        {
            return;
        }


        currentCorner =
            escapeCorner;


        SetAnimation(
            CustomerAnimationState.Idle
        );


        SetDestinationAroundCorner(
            currentCorner,
            thiefMoveRadius
        );


        ResumeAgent();
    }


    // ========================================
    // 警備員から最も遠いコーナー
    // ========================================

    private Transform GetFarthestCornerFromPolice()
    {
        if (police == null)
        {
            return
                GetRandomDifferentCorner();
        }


        Transform farthestCorner =
            null;


        float farthestDistance =
            -1.0f;


        foreach (Transform corner
                 in corners)
        {
            if (corner == null)
            {
                continue;
            }


            if (corner ==
                currentCorner)
            {
                continue;
            }


            float distance =
                Vector3.Distance(
                    police.position,
                    corner.position
                );


            if (distance >
                farthestDistance)
            {
                farthestDistance =
                    distance;

                farthestCorner =
                    corner;
            }
        }


        return farthestCorner;
    }


    // ========================================
    // 最も近いコーナー
    // ========================================

    private Transform FindNearestCorner()
    {
        Transform nearestCorner =
            null;


        float nearestDistance =
            float.MaxValue;


        foreach (Transform corner
                 in corners)
        {
            if (corner == null)
            {
                continue;
            }


            float distance =
                Vector3.Distance(
                    transform.position,
                    corner.position
                );


            if (distance <
                nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestCorner =
                    corner;
            }
        }


        return nearestCorner;
    }


    // ========================================
    // 泥棒設定
    // ========================================

    public void SetThief(
        bool value)
    {
        isThief =
            value;


        if (agent != null)
        {
            SetMoveSpeed();
        }


        Debug.Log(
            $"{gameObject.name} 泥棒設定：{isThief}"
        );
    }


    // ========================================
    // 音声命令との特徴照合
    // ========================================

    public bool Matches(
     VoiceCommand command)
    {
        // ========================================
        // 服の色
        // ========================================

        if (command.clothesColor !=
                CustomerColor.None &&
            clothesColor !=
                command.clothesColor)
        {
            return false;
        }


        // ========================================
        // 帽子
        // ========================================

        if (command.requiresHat)
        {
            // 帽子をかぶっていない
            if (!wearsHat)
            {
                return false;
            }

            // 帽子の色まで指定されている
            if (command.hatColor !=
                    CustomerColor.None &&
                hatColor !=
                    command.hatColor)
            {
                return false;
            }
        }


        // ========================================
        // 眼鏡
        // ========================================

        if (command.requiresGlasses &&
            !wearsGlasses)
        {
            return false;
        }


        // ========================================
        // バッグ
        // ========================================

        if (command.requiresBag &&
            !hasBag)
        {
            return false;
        }


        return true;
    }


    // ========================================
    // 捕獲
    // ========================================

    public void Catch()
    {
        if (IsCaught)
        {
            return;
        }


        // ========================================
        // 泥棒
        // ========================================

        if (IsThief)
        {
            IsCaught =
                true;

            ReleaseActionPointReservation();


            isWaiting =
                false;

            isLookingAround =
                false;

            isSuspiciousFastWalking =
                false;


            StopAllCoroutines();


            StopAgent();


            SetAnimation(
                CustomerAnimationState.ArrestedWalk
            );


            Debug.Log(
                $"{gameObject.name} は泥棒でした！確保成功！"
            );


            if (playSceneManager != null)
            {
                playSceneManager.Caught();

                playSceneManager.ThiefCaught();
            }


            return;
        }


        // ========================================
        // 一般客
        // ========================================

        Debug.Log(
            $"{gameObject.name} は一般客です！誤認逮捕！"
        );


        if (playSceneManager != null)
        {
            playSceneManager.AddComplaint();

            playSceneManager.Caught();

            playSceneManager.AddTimePenalty();
        }
    }
    public CornerType GetCurrentCornerType()
    {
        if (currentCorner == fishCorner)
        {
            return CornerType.Fish;
        }

        if (currentCorner == vegetableCorner)
        {
            return CornerType.Vegetable;
        }

        if (currentCorner == snackCorner)
        {
            return CornerType.Snack;
        }

        if (currentCorner == frozenFoodCorner)
        {
            return CornerType.FrozenFood;
        }

        if (currentCorner == drinkCorner)
        {
            return CornerType.Drink;
        }

        if (currentCorner == preparedFoodCorner)
        {
            return CornerType.PreparedFood;
        }

        if (currentCorner == meatCorner)
        {
            return CornerType.Meat;
        }

        if (currentCorner == breadCorner)
        {
            return CornerType.Bread;
        }

        return CornerType.None;
    }
}
