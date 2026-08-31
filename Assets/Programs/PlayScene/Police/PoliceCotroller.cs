using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 音声命令に応じて警備員を動かし、お客さんを捕まえるクラス
/// </summary>
public class PoliceController : MonoBehaviour
{
    [SerializeField] private VoiceRecognizer voiceRecognizer;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CaptureConfirmUI confirmUI;

    [Header("各コーナーの移動先")]
    [SerializeField] private Transform fishCorner;
    [SerializeField] private Transform vegetableCorner;
    [SerializeField] private Transform snackCorner;
    [SerializeField] private Transform frozenFoodCorner;
    [SerializeField] private Transform drinkCorner;
    [SerializeField] private Transform preparedFoodCorner;
    [SerializeField] private Transform meatCorner;
    [SerializeField] private Transform breadCorner;
    [Header("捕獲設定")]
    [SerializeField] private float catchDistance = 4.0f;


    [Header("泥棒捕獲用警備員")]
    [SerializeField]
    private ArrestPoliceController arrestPolice;

    [Header("警備員モデル")]
    [SerializeField]
    private GameObject idlePolice;

    [SerializeField]
    private GameObject walkPolice;
    // 移動後に使う命令
    private VoiceCommand pendingCommand;

    // コーナーへ移動中かどうか
    private bool isMovingToCorner;

    // 確認中のお客さん
    private Customer confirmCustomer;

    // 「はい」「いいえ」の返事を待っているか
    private bool isWaitingForConfirmation;

    [SerializeField]
    private PlaySceneManager playSceneManager;

    private void Start()
    {
        ShowIdle();
        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnCommandRecognized += ExecuteCommand;

            // 「はい」「いいえ」の認識結果を受け取る
            voiceRecognizer.OnConfirmationRecognized += ConfirmCapture;
        }
        else
        {
            Debug.LogError("VoiceRecognizerが設定されていません");
        }
    }

    private void Update()
    {
        if (playSceneManager != null &&
    !playSceneManager.IsGameStarted())
        {
            return;
        }
        if (agent == null)
        {
            return;
        }

        if (isMovingToCorner &&
    !agent.pathPending &&
    agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            isMovingToCorner = false;

            // 到着したので完全停止
            agent.isStopped = true;
            agent.ResetPath();

            ShowIdle();

            Debug.Log("コーナーに到着しました");

            if (pendingCommand != null &&
                pendingCommand.isCaptureCommand)
            {
                Debug.Log("お客さんを探します");

                TryCatchCustomer(pendingCommand);
            }
            else
            {
                Debug.Log(
                    "捕獲命令がないため、移動だけで終了します"
                );
            }
        }
    }

    /// <summary>
    /// VoiceRecognizerから通常の音声命令を受け取る
    /// </summary>
    private void ExecuteCommand(VoiceCommand command)
    {
        if (command == null)
        {
            return;
        }
        // ========================================
        // 停止命令
        // ========================================
        if (command.isStopCommand)
        {
            StopPolice();
            return;
        }
        // 捕獲確認中は新しい通常命令を受け付けない
        if (isWaitingForConfirmation)
        {
            Debug.Log(
                "現在捕獲確認中です。「はい」か「いいえ」と話してください"
            );

            return;
        }

        if (!command.isCaptureCommand)
        {
            Debug.Log(
                "「捕まえろ」「確保」などの捕獲命令はなし"
            );
        }

        pendingCommand = command;

        // コーナーが指定されている場合
        if (command.corner != CornerType.None)
        {
            Transform destination =
                GetCornerTransform(command.corner);

            if (destination == null)
            {
                Debug.LogError(
                    "移動先が設定されていません"
                );

                return;
            }

            MoveTo(destination);

            return;
        }

        // コーナー指定なしで捕獲命令がある場合
        if (command.isCaptureCommand)
        {
            TryCatchCustomer(command);
        }
    }

    /// <summary>
    /// 警備員をその場で停止する
    /// </summary>
    private void StopPolice()
    {
        if (agent == null)
        {
            return;
        }

        // 移動を完全停止
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // 「コーナーへ移動中」状態を解除
        isMovingToCorner = false;

        // 保留している命令も解除
        pendingCommand = null;

        // 歩きモデルを消してIdleを表示
        ShowIdle();

        Debug.Log("警備員を停止しました");
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
    /// CornerTypeに対応するTransformを取得する
    /// </summary>
    private Transform GetCornerTransform(CornerType corner)
    {
        switch (corner)
        {
            case CornerType.Fish:
                return fishCorner;

            case CornerType.Vegetable:
                return vegetableCorner;

            case CornerType.Snack:
                return snackCorner;

            case CornerType.FrozenFood:
                return frozenFoodCorner;

            case CornerType.Drink:
                return drinkCorner;

            case CornerType.PreparedFood:
                return preparedFoodCorner;

            case CornerType.Meat:
                return meatCorner;
            case CornerType.Bread:
                return breadCorner;
        }

        return null;
    }

    /// <summary>
    /// 指定された場所へ警備員を移動させる
    /// </summary>
    private void MoveTo(Transform destination)
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            Debug.LogError(
                "警備員のNavMeshAgentを確認してください"
            );

            return;
        }
        ShowWalk();

        // StopPoliceで停止していた場合に再開
        agent.isStopped = false;

        agent.SetDestination(
            destination.position
        );
        isMovingToCorner = true;

        Debug.Log(
            destination.name +
            "へ向かいます"
        );
    }

    /// <summary>
    /// 近くにいるお客さんから
    /// 条件に合う一番近い人を探す
    /// </summary>
    private void TryCatchCustomer(
        VoiceCommand command)
    {
        Collider[] hitColliders =
            Physics.OverlapSphere(
                transform.position,
                catchDistance
            );

        Customer nearestCustomer = null;

        float nearestDistance =
            float.MaxValue;

        HashSet<Customer> checkedCustomers =
            new HashSet<Customer>();

        foreach (Collider hitCollider
                 in hitColliders)
        {
            Customer customer =
                hitCollider.GetComponentInParent<Customer>();

            if (customer == null ||
                customer.IsCaught ||
                checkedCustomers.Contains(customer))
            {
                continue;
            }

            checkedCustomers.Add(
                customer
            );

            // 指定された特徴に一致するか
            if (!customer.Matches(command))
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    customer.transform.position
                );

            if (distance < nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestCustomer =
                    customer;
            }
        }

        // お客さんが見つからなかった
        if (nearestCustomer == null)
        {
            if (command.HasNoFeature())
            {
                Debug.Log(
                    "近くに捕まえられるお客さんがいません"
                );
            }
            else
            {
                Debug.Log(
                    "指定された特徴のお客さんが近くにいません"
                );
            }

            return;
        }

        // 確認UIが設定されていない
        if (confirmUI == null)
        {
            Debug.LogError(
                "CaptureConfirmUIが設定されていません"
            );

            return;
        }

        // 捕獲候補のお客さんを保存
        confirmCustomer =
            nearestCustomer;

        // 「はい」「いいえ」の返事待ちにする
        isWaitingForConfirmation =
            true;

        // 確認画面を表示
        confirmUI.Show(
            confirmCustomer
        );

        Debug.Log(
            confirmCustomer.name +
            "を捕まえますか？ 「はい」か「いいえ」と話してください"
        );
    }

    /// <summary>
    /// 「はい」「いいえ」の音声認識結果を受け取る
    /// </summary>
    private void ConfirmCapture(
        bool isYes)
    {
        // 確認待ち状態ではない場合
        if (!isWaitingForConfirmation)
        {
            return;
        }

        // お客さんが存在しない場合
        if (confirmCustomer == null)
        {
            Debug.LogWarning(
                "確認対象のお客さんが存在しません"
            );

            isWaitingForConfirmation =
                false;

            confirmUI?.Hide();

            return;
        }

        if (isYes)
        {
            Debug.Log(
                "捕獲を決定しました"
            );

            CatchCustomer(
                confirmCustomer
            );
        }
        else
        {
            Debug.Log(
                confirmCustomer.name +
                "の捕獲をキャンセルしました"
            );
        }

        // 確認画面を閉じる
        if (confirmUI != null)
        {
            confirmUI.Hide();
        }

        // 確認状態を解除
        confirmCustomer = null;

        isWaitingForConfirmation =
            false;
    }

    /// <summary>
    /// 実際にお客さんを捕まえる
    /// </summary>
    private void CatchCustomer(Customer customer)
    {
        if (customer == null)
        {
            return;
        }

        if (customer.IsThief)
        {
            customer.Catch();

            arrestPolice.StartArrest(
                customer
            );
        }
        else
        {
            customer.Catch();
        }
    }
    
    private void OnDestroy()
    {
        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnCommandRecognized
                -= ExecuteCommand;

            voiceRecognizer.OnConfirmationRecognized
                -= ConfirmCapture;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            catchDistance
        );
    }
}