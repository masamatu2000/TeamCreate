using UnityEngine;
using UnityEngine.AI;

//アニメーション
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
   [Header("移動設定")]
[SerializeField] private float moveRadius = 5.0f;
[SerializeField] private float minWaitTime = 2.0f;
[SerializeField] private float maxWaitTime = 5.0f;

[Header("NavMesh検索設定")]
[SerializeField] private float navMeshSampleDistance = 1.0f;
[SerializeField] private int positionSearchCount = 30;

    [Header("移動速度")]
    [SerializeField] private float normalSpeed = 2.0f;
    [SerializeField] private float thiefSpeed = 3.5f;

    [Header("各コーナー")]
    [SerializeField] private Transform fishCorner;
    [SerializeField] private Transform vegetableCorner;
    [SerializeField] private Transform snackCorner;
    [SerializeField] private Transform frozenFoodCorner;
    [SerializeField] private Transform drinkCorner;
    [SerializeField] private Transform preparedFoodCorner;
    [SerializeField] private Transform meatCorner;
    [SerializeField] private Transform breadCorner;
    [Header("泥棒設定")]
    [SerializeField] private Transform police;
    [SerializeField] private float escapeDistance = 8.0f;

    [Tooltip("泥棒が逃げた後、そのコーナー内で動く範囲")]
    [SerializeField] private float thiefMoveRadius = 2.0f;

    [Header("お客さん情報")]
    [SerializeField] private CustomerColor clothesColor;

    [SerializeField] private bool wearsHat;
    [SerializeField] private bool wearsGlasses;
    [SerializeField] private bool hasBag;
    [SerializeField] private bool isThief;

    [Header("アニメーション")]
    [SerializeField] private Animator animator;

    [Tooltip("待機中に特殊アニメーションを再生する確率")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float actionAnimationChance = 0.6f;

    // 現在のアニメーション
    private CustomerAnimationState currentAnimationState;

    // 待機中に特殊アニメーションを選択済みか
    private bool hasSelectedWaitAnimation;

    public bool IsThief => isThief;
    public bool IsCaught { get; private set; }

    private NavMeshAgent agent;

    [SerializeField]
    private PlaySceneManager playSceneManager;

    // 現在いるコーナー
    private Transform currentCorner;

    // 待機時間
    private float waitTimer;
    private bool isWaiting;

    // 泥棒がすでに逃げたか
    private bool hasEscaped;

    // コーナー一覧
    private Transform[] corners;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Animatorを取得
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // 各コーナーを配列にまとめる
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

    void Start()
    {
        if (playSceneManager == null)
        {
            playSceneManager =
                FindFirstObjectByType<PlaySceneManager>();
        }
        // ゲーム開始時にランダムなコーナーへ配置
        PlaceAtRandomCorner();

    // 泥棒・一般客で速度を設定
    SetMoveSpeed();

    if (isThief)
    {
        // 泥棒は配置されたコーナー周辺だけを動く
        SetDestinationAroundCurrentCorner();
    }
    else
    {
        // 一般客も最初は配置されたコーナー周辺を動く
        SetDestinationAroundCorner(
            currentCorner,
            moveRadius
        );
    }
}

/// <summary>
/// ゲーム開始時にランダムなコーナーへ配置する
/// </summary>
private void PlaceAtRandomCorner()
{
    if (corners == null || corners.Length == 0)
    {
        Debug.LogWarning(
            $"{gameObject.name}：コーナーが設定されていません"
        );

        return;
    }

    Transform selectedCorner = null;

    // 有効なコーナーを探す
    for (int i = 0; i < 20; i++)
    {
        int randomIndex =
            Random.Range(0, corners.Length);

        if (corners[randomIndex] != null)
        {
            selectedCorner = corners[randomIndex];
            break;
        }
    }

    if (selectedCorner == null)
    {
        Debug.LogWarning(
            $"{gameObject.name}：有効なコーナーがありません"
        );

        return;
    }

    currentCorner = selectedCorner;

    // =====================================
    // NavMesh上の安全な位置を何回か探す
    // =====================================

    for (int i = 0; i < positionSearchCount; i++)
    {
        Vector2 randomCircle =
            Random.insideUnitCircle * moveRadius;

        Vector3 randomPosition =
            currentCorner.position +
            new Vector3(
                randomCircle.x,
                0.0f,
                randomCircle.y
            );

        NavMeshHit hit;

        // ★検索距離を小さくする
        // 棚の反対側のNavMeshなどを拾いにくくする
        if (NavMesh.SamplePosition(
            randomPosition,
            out hit,
            navMeshSampleDistance,
            NavMesh.AllAreas))
        {
            if (agent.Warp(hit.position))
            {
                Debug.Log(
                    $"{gameObject.name} を " +
                    $"{currentCorner.name} に配置しました。" +
                    $"位置：{hit.position}"
                );

                return;
            }
        }
    }

    // =====================================
    // ランダム位置が全部失敗した場合
    // コーナー中心付近を探す
    // =====================================

    NavMeshHit centerHit;

    if (NavMesh.SamplePosition(
        currentCorner.position,
        out centerHit,
        navMeshSampleDistance,
        NavMesh.AllAreas))
    {
        agent.Warp(centerHit.position);

        Debug.LogWarning(
            $"{gameObject.name}：ランダム位置が見つからなかったため、" +
            $"{currentCorner.name} の中心付近に配置しました"
        );
    }
    else
    {
        Debug.LogError(
            $"{gameObject.name}：NavMesh上に配置できませんでした！"
        );
    }
}
    void Update()
    {

        // アニメーション更新
        UpdateAnimation();
        // ========================================
        // カウントダウン中は何もしない
        // ========================================
        if (playSceneManager != null &&
            !playSceneManager.IsGameStarted())
        {
            if (agent != null &&
                agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            return;
        }

        // ゲーム開始後は移動可能にする
        if (agent != null &&
            agent.isOnNavMesh &&
            !IsCaught)
        {
            agent.isStopped = false;
        }

        if (IsCaught)
        {
            return;
        }

        // ========================================
        // 泥棒の場合、警備員が近づいているか確認
        // ========================================

        if (isThief && !hasEscaped)
        {
            CheckPoliceDistance();
        }

        // ========================================
        // 通常の移動処理
        // ========================================

        if (agent.pathPending)
        {
            return;
        }

        // 目的地に到着
        if (!agent.pathPending &&
        agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            if (!isWaiting)
            {
                StartWaiting();
            }

            Wait();
        }
    }

    /// <summary>
    /// お客さんの現在の行動から
    /// アニメーションを決定する
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null ||
            agent == null)
        {
            return;
        }

        if (IsCaught)
        {
            SetAnimation(
                CustomerAnimationState.ArrestedWalk
            );

            return;
        }

        if (playSceneManager != null &&
            !playSceneManager.IsGameStarted())
        {
            SetAnimation(
                CustomerAnimationState.Idle
            );

            return;
        }

        // ========================================
        // コーナー到着後はPick系を優先
        // ========================================
        if (isWaiting)
        {
            return;
        }

        // ========================================
        // 移動中
        // ========================================
        bool isMoving =
            agent.isOnNavMesh &&
            !agent.isStopped &&
            agent.velocity.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            if (isThief && hasEscaped)
            {
                SetAnimation(
                    CustomerAnimationState.FastWalk
                );
            }
            else
            {
                SetAnimation(
                    CustomerAnimationState.Walk
                );
            }

            return;
        }

        SetAnimation(
            CustomerAnimationState.Idle
        );
    }

    /// <summary>
    /// Animatorのアニメーション状態を変更する
    /// </summary>
    private void SetAnimation(
        CustomerAnimationState state)
    {
        if (animator == null)
        {
            return;
        }

        // 同じアニメーションなら
        // 毎フレーム再設定しない
        if (currentAnimationState == state)
        {
            return;
        }

        currentAnimationState = state;

        animator.SetInteger(
            "AnimationState",
            (int)state
        );
    }

    /// <summary>
    /// 泥棒・一般客で移動速度を変更する
    /// </summary>
    private void SetMoveSpeed()
    {
        if (isThief)
        {
            agent.speed = thiefSpeed;
        }
        else
        {
            agent.speed = normalSpeed;
        }
    }

    private void StartWaiting()
    {
        isWaiting = true;

        // 到着したので一旦停止
        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        waitTimer =
            Random.Range(
                minWaitTime,
                maxWaitTime
            );

        SelectWaitAnimation();
    }

    /// <summary>
    /// コーナー到着時の商品取得アニメーションを選ぶ
    /// </summary>
    private void SelectWaitAnimation()
    {
        int randomAction = Random.Range(0, 2);

        switch (randomAction)
        {
            // しゃがんで商品を取る
            case 0:
                SetAnimation(
                    CustomerAnimationState.CrouchPick
                );
                break;

            // 普通に商品を取る
            case 1:
                SetAnimation(
                    CustomerAnimationState.TakeItem
                );
                break;
        }
    }

    /// <summary>
    /// 一定時間待機する
    /// </summary>
    private void Wait()
    {
        waitTimer -= Time.deltaTime;

        if (waitTimer > 0.0f)
        {
            return;
        }

        isWaiting = false;

        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (isThief)
        {
            SetDestinationAroundCurrentCorner();
        }
        else
        {
            MoveToRandomCorner();
        }
    }

    /// <summary>
    /// 一般客をランダムなコーナーへ移動させる
    /// </summary>
    private void MoveToRandomCorner()
    {
        if (corners == null || corners.Length == 0)
        {
            return;
        }

        // 現在とは違うコーナーを選ぶ
        Transform nextCorner = GetRandomDifferentCorner();

        if (nextCorner == null)
        {
            return;
        }

        currentCorner = nextCorner;

        // コーナーの中心ぴったりではなく
        // 少しランダムな場所へ向かわせる
        SetDestinationAroundCorner(
            currentCorner,
            moveRadius
        );
    }

    /// <summary>
    /// 現在とは違うランダムなコーナーを取得する
    /// </summary>
    private Transform GetRandomDifferentCorner()
    {
        // 使用できるコーナーが何個あるか確認
        int validCornerCount = 0;

        foreach (Transform corner in corners)
        {
            if (corner != null)
            {
                validCornerCount++;
            }
        }

        if (validCornerCount == 0)
        {
            return null;
        }

        // 1個しかなければそのコーナー
        if (validCornerCount == 1)
        {
            foreach (Transform corner in corners)
            {
                if (corner != null)
                {
                    return corner;
                }
            }
        }

        // 現在と違うコーナーが出るまで選ぶ
        for (int i = 0; i < 20; i++)
        {
            int randomIndex =
                Random.Range(0, corners.Length);

            Transform selectedCorner =
                corners[randomIndex];

            if (selectedCorner == null)
            {
                continue;
            }

            if (selectedCorner == currentCorner)
            {
                continue;
            }

            return selectedCorner;
        }

        return currentCorner;
    }

    /// <summary>
    /// 指定したコーナー周辺の
    /// NavMesh上にある安全な場所へ移動する
    /// </summary>
    private void SetDestinationAroundCorner(
        Transform corner,
        float radius)
    {
        if (corner == null)
        {
            return;
        }

        if (agent == null)
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

        for (int i = 0; i < positionSearchCount; i++)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle * radius;

            Vector3 randomPosition =
                corner.position +
                new Vector3(
                    randomCircle.x,
                    0.0f,
                    randomCircle.y
                );

            NavMeshHit hit;

            // 候補地点のすぐ近くに
            // NavMeshが存在する場合のみ使用
            if (!NavMesh.SamplePosition(
                randomPosition,
                out hit,
                navMeshSampleDistance,
                NavMesh.AllAreas))
            {
                continue;
            }

            // =====================================
            // 本当にそこまで移動できるか調べる
            // =====================================

            NavMeshPath path =
                new NavMeshPath();

            if (agent.CalculatePath(
                hit.position,
                path))
            {
                // 完全な経路がある場合だけ採用
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
            $"{corner.name}周辺に安全な移動先が見つかりませんでした"
        );
    }

    /// <summary>
    /// 現在のコーナー周辺へ移動する
    /// 主に泥棒用
    /// </summary>
    private void SetDestinationAroundCurrentCorner()
    {
        if (currentCorner == null)
        {
            return;
        }

        SetDestinationAroundCorner(
            currentCorner,
            thiefMoveRadius
        );
    }

    /// <summary>
    /// 警備員との距離を調べる
    /// </summary>
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

        // 警備員が近づいた
        if (distance <= escapeDistance)
        {
            EscapeFromPolice();
        }
    }

    /// <summary>
    /// 泥棒が警備員から逃げる
    /// </summary>
    private void EscapeFromPolice()
    {
        // すでに逃げていたら何もしない
        if (hasEscaped)
        {
            return;
        }

        hasEscaped = true;
        isWaiting = false;

        Transform escapeCorner =
            GetFarthestCornerFromPolice();

        if (escapeCorner == null)
        {
            return;
        }

        currentCorner = escapeCorner;

        Debug.Log(
            $"{gameObject.name} が警備員から逃げました！ " +
            $"逃走先：{escapeCorner.name}"
        );

        // 逃げるときだけコーナーへ直接向かう
        SetDestinationAroundCorner(
            currentCorner,
            thiefMoveRadius
        );
    }

    /// <summary>
    /// 警備員から一番遠いコーナーを探す
    /// </summary>
    private Transform GetFarthestCornerFromPolice()
    {
        if (police == null)
        {
            return GetRandomDifferentCorner();
        }

        Transform farthestCorner = null;
        float farthestDistance = -1.0f;

        foreach (Transform corner in corners)
        {
            if (corner == null)
            {
                continue;
            }

            // 今いるコーナーには逃げない
            if (corner == currentCorner)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    police.position,
                    corner.position
                );

            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                farthestCorner = corner;
            }
        }

        return farthestCorner;
    }

    /// <summary>
    /// 現在位置から最も近いコーナーを調べる
    /// </summary>
    private Transform FindNearestCorner()
    {
        Transform nearestCorner = null;
        float nearestDistance = float.MaxValue;

        foreach (Transform corner in corners)
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

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCorner = corner;
            }
        }

        return nearestCorner;
    }

    /// <summary>
    /// 泥棒かどうかを設定する
    /// </summary>
    public void SetThief(bool value)
    {
        isThief = value;

        // Start後に変更されても
        // 速度が切り替わるようにする
        if (agent != null)
        {
            SetMoveSpeed();
        }

        Debug.Log(
            $"{gameObject.name} 泥棒設定：{isThief}"
        );
    }

    /// <summary>
    /// 音声命令の特徴と一致しているか
    /// </summary>
    public bool Matches(VoiceCommand command)
    {
        if (command.clothesColor != CustomerColor.None &&
            clothesColor != command.clothesColor)
        {
            return false;
        }

        if (command.requiresHat && !wearsHat)
        {
            return false;
        }

        if (command.requiresGlasses && !wearsGlasses)
        {
            return false;
        }

        if (command.requiresBag && !hasBag)
        {
            return false;
        }

        return true;
    }

   /// <summary>
/// お客さんを捕まえる
/// </summary>
public void Catch()
{
    // すでに捕まっている泥棒なら何もしない
    if (IsCaught)
    {
        return;
    }

    // =====================================
    // 泥棒だった場合
    // =====================================
    if (IsThief)
    {
        IsCaught = true;

            SetAnimation(
            CustomerAnimationState.ArrestedWalk
        );

           
            Debug.Log($"{gameObject.name} は泥棒でした！確保成功！");
            playSceneManager.Caught();
            playSceneManager.ThiefCaught();
            // 泥棒だけ消す
            //gameObject.SetActive(false);
            return;
        }

    // =====================================
    // 一般客だった場合
    // =====================================
    else
    {
        Debug.Log($"{gameObject.name} は一般客です！誤認逮捕！");
            // 一般客を誤認逮捕
            playSceneManager.AddComplaint();
            playSceneManager.Caught();
            // 制限時間を減らす
            playSceneManager.AddTimePenalty();
            // 一般客なので消さない
            // gameObject.SetActive(false) は実行しない
        }
}
}