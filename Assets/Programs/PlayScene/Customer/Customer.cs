using UnityEngine;
using UnityEngine.AI;

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

        // 各コーナーを配列にまとめる
        corners = new Transform[]
        {
            fishCorner,
            vegetableCorner,
            snackCorner,
            frozenFoodCorner,
            drinkCorner,
            preparedFoodCorner,
            meatCorner
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
        Debug.LogWarning($"{gameObject.name}：コーナーが設定されていません");
        return;
    }

    // 使用できるコーナーだけ探す
    Transform selectedCorner = null;

    for (int i = 0; i < 20; i++)
    {
        int randomIndex = Random.Range(0, corners.Length);

        if (corners[randomIndex] != null)
        {
            selectedCorner = corners[randomIndex];
            break;
        }
    }

    if (selectedCorner == null)
    {
        Debug.LogWarning($"{gameObject.name}：有効なコーナーがありません");
        return;
    }

    currentCorner = selectedCorner;

    // コーナー周辺のランダムな位置
    Vector3 randomPosition =
        currentCorner.position +
        Random.insideUnitSphere * moveRadius;

    randomPosition.y = currentCorner.position.y;

    // NavMesh上の位置を探す
    if (NavMesh.SamplePosition(
        randomPosition,
        out NavMeshHit hit,
        moveRadius,
        NavMesh.AllAreas))
    {
        // NavMeshAgentで移動するのではなく、
        // ゲーム開始時なのでその場へ瞬間移動
        agent.Warp(hit.position);

        Debug.Log(
            $"{gameObject.name} を " +
            $"{currentCorner.name} に配置しました"
        );
    }
    else
    {
        // ランダム位置が見つからなかったら
        // コーナーの中心付近を探す
        if (NavMesh.SamplePosition(
            currentCorner.position,
            out hit,
            moveRadius,
            NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }
}
    void Update()
    {
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
        if (!agent.hasPath ||
            agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                StartWaiting();
            }

            Wait();
        }
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

    /// <summary>
    /// 待機開始
    /// </summary>
    private void StartWaiting()
    {
        isWaiting = true;

        waitTimer =
            Random.Range(minWaitTime, maxWaitTime);
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

        if (isThief)
        {
            // 泥棒は現在いるコーナーから
            // あまり離れない
            SetDestinationAroundCurrentCorner();
        }
        else
        {
            // 一般客はいろいろなコーナーへ行く
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
    /// 指定したコーナー周辺へ移動する
    /// </summary>
    private void SetDestinationAroundCorner(
        Transform corner,
        float radius)
    {
        if (corner == null)
        {
            return;
        }

        Vector3 randomPosition =
            corner.position +
            Random.insideUnitSphere * radius;

        randomPosition.y = corner.position.y;

        // NavMesh上の移動可能な場所を探す
        if (NavMesh.SamplePosition(
            randomPosition,
            out NavMeshHit hit,
            radius,
            NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // ランダム地点が見つからなかった場合
            // コーナー中心を探す
            if (NavMesh.SamplePosition(
                corner.position,
                out hit,
                radius,
                NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
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

        // 移動を停止
        if (agent != null)
        {
            agent.isStopped = true;
        }

        Debug.Log($"{gameObject.name} は泥棒でした！確保成功！");
            playSceneManager.Caught();
            playSceneManager.ThiefCaught();
            // 泥棒だけ消す
            gameObject.SetActive(false);
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