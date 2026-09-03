using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// ========================================
// アニメーション状態
//
// Idle / Walk / FastWalk は
// AnimatorのSpeedパラメーターで切り替える。
//
// LookAround / CrouchPick / TakeItem / ArrestedWalk は
// AnimationStateで切り替える。
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

    // 現在いるコーナー
    private Transform currentCorner;

    // コーナー一覧
    private Transform[] corners;

    // 商品取得中の待ち時間
    private float waitTimer;

    // 商品取得中
    private bool isWaiting;

    // キョロキョロ中
    private bool isLookingAround;

    // 泥棒が警備員から逃げ始めたか
    private bool hasEscaped;

    // 前フレームまでゲーム開始済みだったか
    private bool wasGameStarted;

    // 現在設定している特殊アニメーション
    private CustomerAnimationState currentAnimationState =
        (CustomerAnimationState)(-1);


    // ========================================
    // Awake
    // ========================================

    private void Awake()
    {
        // NavMeshAgent取得
        agent =
            GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        // AnimatorがInspectorで設定されていなければ
        // 子オブジェクトから自動取得
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        // コーナーを配列にまとめる
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
        // PlaySceneManager取得
        if (playSceneManager == null)
        {
            playSceneManager =
                FindFirstObjectByType<PlaySceneManager>();
        }

        // ゲーム開始時に
        // ランダムなコーナー周辺へ配置
        PlaceAtRandomCorner();
        Debug.Log(
    $"{gameObject.name} Start配置後 " +
    $"TransformY={transform.position.y}, " +
    $"AgentNextY={agent.nextPosition.y}, " +
    $"isOnNavMesh={agent.isOnNavMesh}"
);
        if (animator != null)
        {
            Debug.Log(
                $"{gameObject.name} Animator側 " +
                $"LocalY={animator.transform.localPosition.y}, " +
                $"WorldY={animator.transform.position.y}"
            );
        }
        // 移動速度を設定
        SetMoveSpeed();


        // ========================================
        // 最初の目的地を設定
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


        // 現在ゲーム開始済みか保存
        wasGameStarted =
            playSceneManager != null &&
            playSceneManager.IsGameStarted();


        // 最初は通常状態
        SetAnimation(
            CustomerAnimationState.Idle
        );
    }

    private void FreezeCustomer()
    {
        if (rb == null)
        {
            return;
        }

        // 位置と回転を完全固定
        rb.constraints =
            RigidbodyConstraints.FreezePosition |
            RigidbodyConstraints.FreezeRotation;

        // 念のため速度も0
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    private void UnfreezeCustomer()
    {
        if (rb == null)
        {
            return;
        }

        // 回転だけ固定したまま
        // Positionは自由にしてNavMeshAgentで動けるようにする
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


        // ========================================
        // ゲーム開始判定
        // ========================================

        bool isGameStarted =
            playSceneManager == null ||
            playSceneManager.IsGameStarted();


        // ========================================
        // カウントダウン中
        // ========================================

        if (!isGameStarted)
        {
            // お客さんを停止
            StopAgent();
            // カウントダウン中は完全固定
            FreezeCustomer();
            // Speedを0にする
            UpdateAnimation();

            wasGameStarted = false;

            return;
        }


        // ========================================
        // ゲーム開始した瞬間
        // ========================================

        if (!wasGameStarted)
        {
            wasGameStarted = true;
            // 座標固定を解除
            UnfreezeCustomer();
            // 特殊行動中でなければ移動再開
            if (!IsCaught &&
                !isWaiting &&
                !isLookingAround)
            {
                ResumeAgent();
            }
        }


        // ========================================
        // 捕まっている場合
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
        // 商品取得中
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
        // 泥棒の場合
        // 警備員が近づいているか確認
        // ========================================

        if (isThief &&
            !hasEscaped)
        {
            CheckPoliceDistance();
        }


        // ========================================
        // 経路計算中なら待つ
        // ========================================

        if (agent.pathPending)
        {
            UpdateAnimation();

            return;
        }


        // ========================================
        // 目的地へ到着したか確認
        // ========================================

        if (agent.hasPath &&
            agent.remainingDistance <=
            agent.stoppingDistance + 0.5f)
        {
            StartWaiting();

            UpdateAnimation();

            return;
        }


        // ========================================
        // 通常時のアニメーション更新
        // ========================================

        UpdateAnimation();
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            return;
        }

        Debug.Log(
            $"{gameObject.name} LateUpdate " +
            $"CustomerY={transform.position.y}, " +
            $"AnimatorY={animator.transform.position.y}, " +
            $"AnimatorLocalY={animator.transform.localPosition.y}"
        );
    }
    // ========================================
    // NavMeshAgentを停止
    // ========================================

    private void StopAgent()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }

        // 移動停止
        agent.isStopped = true;

        // 残っている速度も0にする
        agent.velocity =
            Vector3.zero;
    }


    // ========================================
    // NavMeshAgentの移動再開
    // ========================================

    private void ResumeAgent()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }

        agent.isStopped = false;
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
        // 捕まった泥棒
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
            // カウントダウン中なのでSpeedは0
            animator.SetFloat(
                "Speed",
                0.0f
            );

            // Idleの沈み検証のため、
            // 現在はここではAnimationStateを変更しない
            //
            // SetAnimation(
            //     CustomerAnimationState.Idle
            // );

            return;
        }


        // ========================================
        // 商品取得中
        // ========================================

        if (isWaiting)
        {
            // 移動アニメーションを出さない
            animator.SetFloat(
                "Speed",
                0.0f
            );

            // AnimationStateは
            // CrouchPick または TakeItem のまま
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

            // AnimationStateは
            // LookAroundのまま
            return;
        }


        // ========================================
        // 通常状態
        // ========================================

        SetAnimation(
            CustomerAnimationState.Idle
        );


        // ========================================
        // 実際の移動速度をAnimatorへ送る
        //
        // Speed = 0      → Idle
        // Speed > 0.1    → Walk
        // Speed > 2.8    → FastWalk
        // ========================================

        float speed = 0.0f;

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

        // 同じ状態なら再設定しない
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
    // 移動速度設定
    // ========================================

    private void SetMoveSpeed()
    {
        if (agent == null)
        {
            return;
        }

        // 泥棒が逃げ始めた場合だけ早くする
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
    // 商品取得開始
    // ========================================

    private void StartWaiting()
    {
        // すでに特殊行動中なら開始しない
        if (isWaiting ||
            isLookingAround ||
            IsCaught)
        {
            return;
        }

        isWaiting = true;

        // 到着したので停止
        StopAgent();

        // 商品を見る時間
        waitTimer =
            Random.Range(
                minWaitTime,
                maxWaitTime
            );

        // 商品取得アニメーションを選ぶ
        SelectWaitAnimation();
    }


    // ========================================
    // 商品取得アニメーションをランダム選択
    // ========================================

    private void SelectWaitAnimation()
    {
        int randomAction =
            Random.Range(0, 2);

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


    // ========================================
    // 商品取得中の待機処理
    // ========================================

    private void Wait()
    {
        waitTimer -=
            Time.deltaTime;

        // まだ時間が残っている
        if (waitTimer > 0.0f)
        {
            return;
        }


        // 商品取得終了
        isWaiting = false;


        // ========================================
        // 30%の確率でキョロキョロする
        // ========================================

        if (Random.value < 0.3f)
        {
            isLookingAround = true;

            StartCoroutine(
                LookAroundBeforeMove()
            );

            return;
        }


        // キョロキョロしない場合は
        // そのまま次の場所へ移動
        MoveAfterWaiting();
    }


    // ========================================
    // キョロキョロ
    // ========================================

    private IEnumerator LookAroundBeforeMove()
    {
        // キョロキョロ中は完全停止
        StopAgent();

        animator.SetFloat(
            "Speed",
            0.0f
        );

        SetAnimation(
            CustomerAnimationState.LookAround
        );


        // 2秒間キョロキョロ
        yield return
            new WaitForSeconds(2.0f);


        // キョロキョロ終了
        isLookingAround = false;


        // 次の場所へ移動
        MoveAfterWaiting();
    }


    // ========================================
    // 商品取得・キョロキョロ後の移動
    // ========================================

    private void MoveAfterWaiting()
    {
        if (IsCaught)
        {
            return;
        }

        isWaiting = false;
        isLookingAround = false;


        // 特殊アニメーション終了
        SetAnimation(
            CustomerAnimationState.Idle
        );


        // ========================================
        // 次の目的地を設定
        // ========================================

        if (isThief)
        {
            SetDestinationAroundCurrentCorner();
        }
        else
        {
            MoveToRandomCorner();
        }


        // 移動再開
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


        Transform selectedCorner =
            null;


        // ========================================
        // 有効なコーナーをランダム選択
        // ========================================

        for (int i = 0;
             i < 20;
             i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    corners.Length
                );

            if (corners[randomIndex] != null)
            {
                selectedCorner =
                    corners[randomIndex];

                break;
            }
        }


        if (selectedCorner == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}：" +
                "有効なコーナーがありません"
            );

            return;
        }


        currentCorner =
            selectedCorner;


        // ========================================
        // コーナー周辺のNavMesh上から
        // 初期位置をランダムに探す
        // ========================================

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
                // NavMesh上へ瞬間移動
                if (agent.Warp(
                    hit.position))
                {
                    return;
                }
            }
        }


        // ========================================
        // ランダム位置が見つからなかった場合
        // コーナー中心付近へ配置
        // ========================================

        NavMeshHit centerHit;


        if (NavMesh.SamplePosition(
            currentCorner.position,
            out centerHit,
            navMeshSampleDistance,
            NavMesh.AllAreas))
        {
            agent.Warp(
                centerHit.position
            );
        }
        else
        {
            Debug.LogError(
                $"{gameObject.name}：" +
                "NavMesh上に配置できませんでした"
            );
        }
    }


    // ========================================
    // 一般客
    // 別のランダムなコーナーへ移動
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
    // 現在とは違うコーナーをランダム取得
    // ========================================

    private Transform GetRandomDifferentCorner()
    {
        int validCornerCount = 0;


        foreach (Transform corner
                 in corners)
        {
            if (corner != null)
            {
                validCornerCount++;
            }
        }


        // コーナーなし
        if (validCornerCount == 0)
        {
            return null;
        }


        // 1個しかない場合
        if (validCornerCount == 1)
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


        // 現在とは違うコーナーを探す
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


        // 見つからなければ現在のコーナー
        return currentCorner;
    }


    // ========================================
    // 指定コーナー周辺の安全な場所へ移動
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


        for (int i = 0;
             i < positionSearchCount;
             i++)
        {
            // コーナー周辺のランダム位置
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


            // ランダム位置付近にNavMeshがあるか確認
            if (!NavMesh.SamplePosition(
                randomPosition,
                out hit,
                navMeshSampleDistance,
                NavMesh.AllAreas))
            {
                continue;
            }


            // 実際にそこまで移動できるか確認
            NavMeshPath path =
                new NavMeshPath();


            if (agent.CalculatePath(
                hit.position,
                path))
            {
                // 完全な経路がある場合のみ採用
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
            $"{corner.name}周辺に" +
            "安全な移動先が見つかりませんでした"
        );
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
    // 警備員との距離確認
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


        // 警備員が近づいたら逃走
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
        // すでに逃げている
        if (hasEscaped)
        {
            return;
        }


        hasEscaped = true;

        isWaiting = false;
        isLookingAround = false;


        // 逃走速度へ変更
        SetMoveSpeed();


        // 警備員から最も遠いコーナーを探す
        Transform escapeCorner =
            GetFarthestCornerFromPolice();


        if (escapeCorner == null)
        {
            return;
        }


        currentCorner =
            escapeCorner;


        Debug.Log(
            $"{gameObject.name} が警備員から逃げました！ " +
            $"逃走先：{escapeCorner.name}"
        );


        // 特殊アニメーション解除
        SetAnimation(
            CustomerAnimationState.Idle
        );


        // 逃走先を設定
        SetDestinationAroundCorner(
            currentCorner,
            thiefMoveRadius
        );


        ResumeAgent();
    }


    // ========================================
    // 警備員から最も遠いコーナーを取得
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


            // 現在いるコーナーは除外
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
    // 現在位置から最も近いコーナー
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
    // 泥棒かどうか設定
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
    // 音声命令の特徴と一致しているか確認
    // ========================================

    public bool Matches(
        VoiceCommand command)
    {
        // 服の色
        if (command.clothesColor !=
                CustomerColor.None &&
            clothesColor !=
                command.clothesColor)
        {
            return false;
        }


        // 帽子
        if (command.requiresHat &&
            !wearsHat)
        {
            return false;
        }


        // 眼鏡
        if (command.requiresGlasses &&
            !wearsGlasses)
        {
            return false;
        }


        // 鞄
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
        // すでに捕獲済み
        if (IsCaught)
        {
            return;
        }


        // ========================================
        // 泥棒だった場合
        // ========================================

        if (IsThief)
        {
            IsCaught = true;

            isWaiting = false;
            isLookingAround = false;


            // キョロキョロなどのCoroutineを停止
            StopAllCoroutines();


            // 移動停止
            StopAgent();


            // 捕獲後の歩行アニメーション
            SetAnimation(
                CustomerAnimationState.ArrestedWalk
            );


            Debug.Log(
                $"{gameObject.name} は泥棒でした！確保成功！"
            );


            playSceneManager.Caught();

            playSceneManager.ThiefCaught();


            return;
        }


        // ========================================
        // 一般客だった場合
        // ========================================

        Debug.Log(
            $"{gameObject.name} は一般客です！誤認逮捕！"
        );


        // クレーム追加
        playSceneManager.AddComplaint();


        // 捕獲処理
        playSceneManager.Caught();


        // 制限時間ペナルティ
        playSceneManager.AddTimePenalty();
    }
}