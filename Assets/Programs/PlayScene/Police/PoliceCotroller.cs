using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 音声認識の結果に応じて警備員を移動・捕獲させるクラス
/// </summary>
public class PoliceController : MonoBehaviour
{
    [SerializeField]
    private VoiceRecognizer voiceRecognizer;

    [SerializeField]
    private NavMeshAgent agent;

    [Header("各コーナーの移動先")]
    [SerializeField] private Transform fishCorner;
    [SerializeField] private Transform vegetableCorner;
    [SerializeField] private Transform snackCorner;
    [SerializeField] private Transform frozenFoodCorner;
    [SerializeField] private Transform drinkCorner;
    [SerializeField] private Transform preparedFoodCorner;
    [SerializeField] private Transform meatCorner;

    [Header("捕獲設定")]
    [SerializeField]
    private float catchDistance = 3.0f;

    // 音声コマンドと移動先の対応表
    private Dictionary<string, Transform> destinations;

    // 捕獲命令として扱う言葉
    private HashSet<string> catchCommands;

    // 捕獲済みかどうか
    private bool isCatching;

    private void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        InitializeMoveCommands();
        InitializeCatchCommands();

        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnCommandRecognized += ExecuteCommand;
        }
        else
        {
            Debug.LogError("VoiceRecognizerが設定されていません");
        }
    }

    /// <summary>
    /// 移動コマンドを登録する
    /// </summary>
    private void InitializeMoveCommands()
    {
        destinations = new Dictionary<string, Transform>()
        {
            // 鮮魚
            { "鮮魚コーナー", fishCorner },
            { "さかなコーナー", fishCorner },

            // 野菜
            { "野菜コーナー", vegetableCorner },
            { "青果コーナー", vegetableCorner },

            // お菓子
            { "お菓子コーナー", snackCorner },
            { "菓子コーナー", snackCorner },

            // 冷凍食品
            { "冷凍食品コーナー", frozenFoodCorner },
            { "冷凍コーナー", frozenFoodCorner },

            // 飲料
            { "飲料コーナー", drinkCorner },
            { "飲み物コーナー", drinkCorner },
            { "ドリンクコーナー", drinkCorner },
            { "ドリンク売り場", drinkCorner },
            { "ジュース売り場", drinkCorner },

            // 惣菜
            { "惣菜コーナー", preparedFoodCorner },
            { "おかずコーナー", preparedFoodCorner },
            { "お弁当コーナー", preparedFoodCorner },

            // 精肉
            { "精肉コーナー", meatCorner },
            { "肉コーナー", meatCorner }
        };
    }

    /// <summary>
    /// 捕獲コマンドを登録する
    /// VoiceRecognizerに登録した言葉と同じものを設定する
    /// </summary>
    private void InitializeCatchCommands()
    {
        catchCommands = new HashSet<string>()
        {
            "ほかく",
            "つかまえろ",
            "とらえろ",
            "確保",
            "逮捕",
            "行け",
        };
    }

    /// <summary>
    /// VoiceRecognizerから受け取ったコマンドを振り分ける
    /// </summary>
    private void ExecuteCommand(string command)
    {
        Debug.Log("受け取った音声コマンド：" + command);

        // 移動コマンドか確認する
        if (destinations.TryGetValue(command, out Transform destination))
        {
            MoveTo(destination);
            return;
        }

        // 捕獲コマンドか確認する
        if (catchCommands.Contains(command))
        {
            ExecuteCatchCommand();
            return;
        }

        Debug.Log("対応していない指示です：" + command);
    }

    /// <summary>
    /// 指定されたコーナーへ移動する
    /// </summary>
    private void MoveTo(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogError("移動先が設定されていません");
            return;
        }

        if (agent == null)
        {
            Debug.LogError("NavMeshAgentが設定されていません");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("警備員がNavMesh上にいません");
            return;
        }

        agent.SetDestination(destination.position);

        Debug.Log(destination.name + "へ向かいます");
    }

    /// <summary>
    /// 捕獲音声を受け取ったときに呼ばれる関数
    /// </summary>
    private void ExecuteCatchCommand()
    {
        if (isCatching)
        {
            Debug.Log("現在、捕獲処理中です");
            return;
        }

        Debug.Log("捕獲命令を受け取りました");

        Customer targetCustomer = FindNearestCustomer();

        if (targetCustomer == null)
        {
            Debug.Log("捕獲できる距離にお客さんがいません");
            return;
        }

        CatchCustomer(targetCustomer);
    }

    /// <summary>
    /// 捕獲範囲内にいる最も近いお客さんを探す
    /// </summary>
    private Customer FindNearestCustomer()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            catchDistance
        );

        Customer nearestCustomer = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hitCollider in colliders)
        {
            // Colliderが子オブジェクトに付いている場合も考慮
            Customer customer =
                hitCollider.GetComponentInParent<Customer>();

            if (customer == null)
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

        return nearestCustomer;
    }

    /// <summary>
    /// お客さんを捕獲する
    /// </summary>
    private void CatchCustomer(Customer customer)
    {
        if (customer == null)
        {
            return;
        }

        isCatching = true;

        // 捕獲中は警備員を停止させる
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        Debug.Log(customer.name + "を捕獲しました");

        if (customer.IsThief)
        {
            Debug.Log("泥棒を捕まえました！");
            customer.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("一般のお客さんを誤って捕まえました！");
        }

        isCatching = false;
    }

    private void OnDestroy()
    {
        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnCommandRecognized -= ExecuteCommand;
        }
    }

    /// <summary>
    /// Sceneビューに捕獲範囲を表示する
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            catchDistance
        );
    }
}