using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 音声認識の結果に応じて警備員を移動させるクラス
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

    // 音声コマンドと移動先を対応させるDictionary
    private Dictionary<string, Transform> destinations;

    private void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
        //下には、音声コマンドと移動先を対応させるDictionaryを初期化するコードを追加してください。
        //例えば野菜コーナーのほかに「青果コーナー」も同じ移動先に対応させる場合は、以下のように記述します。
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

            // 惣菜
            { "惣菜コーナー", preparedFoodCorner },
            { "おかずコーナー", preparedFoodCorner },

            // 精肉
            { "精肉コーナー", meatCorner },
            { "肉コーナー", meatCorner }
        };

        if (voiceRecognizer != null)
        {
            // VoiceRecognizerから認識結果を受け取る
            voiceRecognizer.OnCommandRecognized += ExecuteCommand;
        }
        else
        {
            Debug.LogError(
                "VoiceRecognizerが設定されていません"
            );
        }
    }

    /// <summary>
    /// 認識した言葉に対応する移動先を調べる
    /// </summary>
    private void ExecuteCommand(string command)
    {
        Debug.Log("受け取った音声コマンド：" + command);

        if (destinations.TryGetValue(
            command,
            out Transform destination))
        {
            MoveTo(destination);
        }
        else
        {
            Debug.Log(
                "対応していない指示です：" + command
            );
        }
    }

    /// <summary>
    /// 指定されたコーナーへ移動する
    /// </summary>
    private void MoveTo(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogError(
                "移動先が設定されていません"
            );

            return;
        }

        if (agent == null)
        {
            Debug.LogError(
                "NavMeshAgentが設定されていません"
            );

            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "警備員がNavMesh上にいません"
            );

            return;
        }

        agent.SetDestination(destination.position);

        Debug.Log(
            destination.name + "へ向かいます"
        );
    }

    private void OnDestroy()
    {
        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnCommandRecognized -= ExecuteCommand;
        }
    }
}