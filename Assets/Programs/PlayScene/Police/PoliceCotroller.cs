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

    [Header("捕獲設定")]
    [SerializeField] private float catchDistance = 4.0f;

    private VoiceCommand pendingCommand;
    private bool isMovingToCorner;

    private void Start()
    {
        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnCommandRecognized += ExecuteCommand;
        }
    }

    private void Update()
    {
        if (agent.remainingDistance <= agent.stoppingDistance + 0.2f&&isMovingToCorner)
        {
            isMovingToCorner = false;

            Debug.Log("コーナーに到着しました");

            if (pendingCommand.isCaptureCommand)
            {
                Debug.Log("お客さんを探します");
                TryCatchCustomer(pendingCommand);
            }
            else
            {
                Debug.Log("捕獲命令がないため、移動だけで終了します");
            }
        }
    }

    /// <summary>
    /// VoiceRecognizerから命令を受け取る
    /// </summary>
    private void ExecuteCommand(VoiceCommand command)
    {
        if (!command.isCaptureCommand)
        {
            Debug.Log("「捕まえろ」「確保」などの捕獲命令はなし");
        }

        pendingCommand = command;

        // コーナーが言われた場合は、まずそこへ移動
        if (command.corner != CornerType.None)
        {
            Transform destination = GetCornerTransform(command.corner);

            if (destination == null)
            {
                Debug.LogError("移動先が設定されていません");
                return;
            }

            MoveTo(destination);
            return;
        }

        // コーナーが言われなかった場合は、今いる場所の近くを探す
        Debug.Log("コーナー指定なし：現在地付近のお客さんを探します");
        if(command.isCaptureCommand)
            TryCatchCustomer(command);
    }

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
        }

        return null;
    }

    private void MoveTo(Transform destination)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogError("警備員のNavMeshAgentを確認してください");
            return;
        }

        agent.SetDestination(destination.position);
        isMovingToCorner = true;

        Debug.Log(destination.name + "へ向かいます");
    }

    /// <summary>
    /// 近くにいるお客さんから、条件に合う一番近い人を探す
    /// 特徴がない場合は、近くのお客さんをそのまま捕まえる
    /// </summary>
    private void TryCatchCustomer(VoiceCommand command)
    {
        Collider[] hitColliders =
            Physics.OverlapSphere(transform.position, catchDistance);

        Customer nearestCustomer = null;
        float nearestDistance = float.MaxValue;

        HashSet<Customer> checkedCustomers = new HashSet<Customer>();

        foreach (Collider hitCollider in hitColliders)
        {
            Customer customer =
                hitCollider.GetComponentInParent<Customer>();

            if (customer == null ||
                customer.IsCaught ||
                checkedCustomers.Contains(customer))
            {
                continue;
            }

            checkedCustomers.Add(customer);

            // 色・帽子などが指定されていれば、その条件に合う人だけを候補にする
            if (!customer.Matches(command))
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                customer.transform.position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCustomer = customer;
            }
        }

        if (nearestCustomer == null)
        {
            if (command.HasNoFeature())
            {
                Debug.Log("近くに捕まえられるお客さんがいません");
            }
            else
            {
                Debug.Log("指定された特徴のお客さんが近くにいません");
            }

            return;
        }

        CatchCustomer(nearestCustomer);
    }

    private void CatchCustomer(Customer customer)
    {
        Debug.Log(
            customer.name +
            (customer.IsThief ? " を捕まえた！泥棒です" : " を捕まえた！一般客です")
        );

        customer.Catch();
    }

    private void OnDestroy()
    {
        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnCommandRecognized -= ExecuteCommand;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}