
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
/// お客さん1人分の処理
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

    [Header("棚が多いコーナー設定")]

    [Tooltip("お菓子・飲料コーナーで、行動後に同じコーナー内の別棚へ移動する確率")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float stayInLargeCornerRate = 0.6f;

    // 現在向かっているActionPoint
    private Transform currentActionPoint;
    // ========================================
    // 不審行動確率
    // ========================================

    [Header("不審行動確率")]

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

    [Tooltip("泥棒が逃げた後、そのコーナー内で動く範囲")]
    [SerializeField]
    private float thiefMoveRadius = 2.0f;


    // ========================================
    // お客さん情報
    // ========================================

    [Header("お客さん情報")]

    [SerializeField]
    private CustomerColor clothesColor;

    [SerializeField]
    private bool wearsHat;

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


    // ========================================
    // その他
    // ========================================

    [SerializeField]
    private PlaySceneManager playSceneManager;


    // ========================================
    // 公開情報
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

    private CustomerAnimationState currentAnimationState =
        (CustomerAnimationState)(-1);


    // ========================================
    // Awake
    // ========================================

    private void Awake()
    {
        agent =
            GetComponent<NavMeshAgent>();

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
                FindFirstObjectByType<PlaySceneManager>();
        }


        PlaceAtRandomCorner();


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
        // カウントダウン中なら停止
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


        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;
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
        // ゲーム開始瞬間
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
        // キョロキョロ中
        // ========================================

        if (isLookingAround)
        {
            StopAgent();

            UpdateAnimation();

            return;
        }


        // ========================================
        // 商品を見る・取る
        // ========================================

        if (isWaiting)
        {
            StopAgent();

            Wait();

            UpdateAnimation();

            return;
        }


        // ========================================
        // 通常移動
        // ========================================

        ResumeAgent();


        // ========================================
        // 泥棒が警備員から逃げる
        // ========================================

        if (isThief &&
            !hasEscaped)
        {
            CheckPoliceDistance();
        }


        // ========================================
        // 経路計算中
        // ========================================

        if (agent.pathPending)
        {
            UpdateAnimation();

            return;
        }


        // ========================================
        // 目的地到着判定
        // ========================================

        if (agent.hasPath &&
            agent.remainingDistance <=
            agent.stoppingDistance + 0.3f)
        {
            StartWaiting();

            UpdateAnimation();

            return;
        }


        UpdateAnimation();
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
        // 商品取得中
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
        // キョロキョロ中
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
        // 通常状態
        // ========================================

        SetAnimation(
            CustomerAnimationState.Idle
        );


        float speed =
            0.0f;


        if (agent.isOnNavMesh &&
            !agent.isStopped)
        {
            speed =
                agent.velocity.magnitude;
        }


        animator.SetFloat(
            "Speed",
            speed
        );
    }


    // ========================================
    // 特殊アニメーション切り替え
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
    // 通常速度設定
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
    // 目的地到着
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


        // ========================================
        // 一般客 10%
        // 泥棒   30%
        // ========================================

        float suspiciousRate =
            isThief
                ? thiefSuspiciousRate
                : normalCustomerSuspiciousRate;


        bool doSuspiciousAction =
            Random.value <
            suspiciousRate;


        // ========================================
        // 通常行動
        // ========================================

        if (!doSuspiciousAction)
        {
            StartNormalAction();

            return;
        }


        // ========================================
        // 不審行動
        // ========================================

        StartSuspiciousAction();
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
        //    $"{gameObject.name}：" +
        //    "通常行動 → 商品を見る"
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
            // しゃがんで漁る
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
                //    $"{gameObject.name}：" +
                //    "不審行動 → しゃがんで漁る"
                //);

                break;


            // ========================================
            // キョロキョロ
            // ========================================

            case 1:

                isLookingAround =
                    true;


                StartCoroutine(
                    LookAroundBeforeMove()
                );


                //Debug.Log(
                //    $"{gameObject.name}：" +
                //    "不審行動 → キョロキョロ"
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
                //    $"{gameObject.name}：" +
                //    "不審行動 → 早歩き"
                //);

                break;
        }
    }


    // ========================================
    // 商品取得中
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
    // キョロキョロ
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
        // 次の場所を設定
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
            CustomerAnimationState.Idle
        );


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
        // 棚が多いコーナーの場合
        //
        // お菓子 / 飲料
        // ========================================

        bool isLargeCorner =
            currentCorner == snackCorner ||
            currentCorner == drinkCorner;


        if (isLargeCorner)
        {
            // ========================================
            // 一定確率で
            // 同じコーナーの別棚を見る
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
    // 別コーナーへ
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
    // 現在とは違うコーナー
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
        
        if (corner == null ||
            agent == null)
        {
            return;
        }


        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning(
                $"{gameObject.name} がNavMesh上にいません"
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
        //else
        //{
        //    //Debug.LogWarning(
        //    //    $"{corner.name} にActionPointが見つかりません"
        //    //);
        //}


        // ========================================
        // ActionPointがない場合は従来方式
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
    // コーナー内の停止ポイントを取得
    // 前回と違うActionPointを優先する
    // ========================================

    private Transform GetRandomActionPoint(
        Transform corner)
    {
       
        if (corner == null)
        {
            return null;
        }

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

            // ActionPointが複数ある場合は
            // 前回と同じ場所を除外
            if (child == currentActionPoint)
            {
                continue;
            }
          
            actionPoints.Add(child);
        }

        // 前回と違う場所がなかった場合
        // 同じ場所でもいいので再取得
        if (actionPoints.Count == 0)
        {
            foreach (Transform child in children)
            {
                if (child == corner)
                {
                    continue;
                }

                if (child.name.Contains("ActionPoint"))
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

        return currentActionPoint;
    }


    // ========================================
    // 泥棒
    // 現在コーナー内
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
    // 泥棒逃走
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
    // 最寄りコーナー
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
    // 特徴一致
    // ========================================

    public bool Matches(
        VoiceCommand command)
    {
        if (command.clothesColor !=
                CustomerColor.None &&
            clothesColor !=
                command.clothesColor)
        {
            return false;
        }


        if (command.requiresHat &&
            !wearsHat)
        {
            return false;
        }


        if (command.requiresGlasses &&
            !wearsGlasses)
        {
            return false;
        }


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
}

