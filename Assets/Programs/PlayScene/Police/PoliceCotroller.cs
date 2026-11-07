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

    [Header("捕獲結果UI")]
    [SerializeField]
    private CustomerPreview customerPreview;
    [SerializeField]
    private GameObject walkPolice;
    [Header("捕獲対象マーカー")]
    [SerializeField]
    private Transform targetMarker;

    [SerializeField]
    private float markerHeight = 2.0f;

    [SerializeField]
    private Camera markerCamera;
    // 移動後に使う命令
    private VoiceCommand pendingCommand;
    // 現在警備員がいるコーナー
    private CornerType currentCorner =
        CornerType.None;
    // コーナーへ移動中かどうか
    private bool isMovingToCorner;

    [SerializeField]
    private PlaySceneManager playSceneManager;

    private void Start()
    {
        ShowIdle();
        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnCommandRecognized += ExecuteCommand;
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

            agent.isStopped = true;
            agent.ResetPath();

            ShowIdle();

            // ========================================
            // 警備員の現在コーナーを記録
            // ========================================
            if (pendingCommand != null &&
                pendingCommand.corner != CornerType.None)
            {
                currentCorner =
                    pendingCommand.corner;
            }

            Debug.Log(
                "コーナーに到着しました：" +
                currentCorner
            );

            if (pendingCommand != null &&
                pendingCommand.isCaptureCommand)
            {
                Debug.Log("お客さんを探します");

                TryCatchCustomer(
                    pendingCommand
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
    /// 捕獲範囲内から、条件に合う一番近いお客さんを捕まえる
    /// </summary>
    private void TryCatchCustomer(VoiceCommand command)
    {
        Customer nearestCustomer = FindNearestCustomer(command);

        if (nearestCustomer == null)
        {
            if (command.HasNoFeature())
            {
                Debug.Log("近くに捕まえられるお客さんがいません");
            }
            else
            {
                Debug.Log("近くに指定された特徴のお客さんがいません");
            }

            return;
        }

        Debug.Log(nearestCustomer.name + "を捕獲します");
        CatchCustomer(nearestCustomer);
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

        // 捕獲結果を表示
        if (customerPreview != null)
        {
            customerPreview.Show(
                customer,
                customer.IsThief
            );
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
    /// <summary>
    /// 捕獲範囲内にいる、一番近いお客さんを取得する。
    /// 命令が指定されていれば、特徴も確認する。
    /// </summary>
    private Customer FindNearestCustomer(VoiceCommand command = null)
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            catchDistance
        );

        Customer nearestCustomer = null;

        float nearestDistanceSquared = float.PositiveInfinity;
        float catchDistanceSquared = catchDistance * catchDistance;

        HashSet<Customer> checkedCustomers = new HashSet<Customer>();

        foreach (Collider hitCollider in hitColliders)
        {
            Customer customer =
                hitCollider.GetComponentInParent<Customer>();

            if (customer == null ||
                !customer.isActiveAndEnabled ||
                customer.IsCaught)
            {
                continue;
            }

            // 同じお客さんに複数のColliderがあっても、一度だけ調べる
            if (!checkedCustomers.Add(customer))
            {
                continue;
            }

            // 捕獲命令がある場合は、指定された特徴も確認する
            if (command != null && !customer.Matches(command))
            {
                continue;
            }

            float distanceSquared =
                (customer.transform.position - transform.position).sqrMagnitude;

            // Colliderだけが範囲に入っている場合もあるため、
            // お客さんの座標でも距離を確認する
            if (distanceSquared > catchDistanceSquared)
            {
                continue;
            }

            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestCustomer = customer;
            }
        }

        return nearestCustomer;
    }

    /// <summary>
    /// 一番近いお客さんの頭上にマーカーを表示する
    /// </summary>
    private void LateUpdate()
    {
        if (targetMarker == null)
        {
            return;
        }

        // ゲーム開始前は非表示
        if (playSceneManager != null &&
            !playSceneManager.IsGameStarted())
        {
            targetMarker.gameObject.SetActive(false);
            return;
        }

        Customer target = FindNearestCustomer();

        // 捕獲できるお客さんがいなければ非表示
        if (target == null)
        {
            targetMarker.gameObject.SetActive(false);
            return;
        }

        targetMarker.position =
            target.transform.position + Vector3.up * markerHeight;

        // 三角形をカメラの方向に向ける
        Camera viewCamera =
            markerCamera != null ? markerCamera : Camera.main;

        if (viewCamera != null)
        {
            targetMarker.rotation = viewCamera.transform.rotation;
        }

        targetMarker.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (targetMarker != null)
        {
            targetMarker.gameObject.SetActive(false);
        }
    }
}
