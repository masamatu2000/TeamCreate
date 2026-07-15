using UnityEngine;
using UnityEngine.AI;

public class PoliceController : MonoBehaviour
{
    [SerializeField]
    private VoiceRecognizer voiceRecognizer;

    [SerializeField]
    private NavMeshAgent agent;

    [Header("移動先")]
    [SerializeField]
    private Transform fishCorner;

    [SerializeField]
    private Transform meatCorner;

    [SerializeField]
    private Transform vegetableCorner;

    private void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        // VoiceRecognizerから認識結果を受け取る
        voiceRecognizer.OnCommandRecognized += ExecuteCommand;
    }

    private void ExecuteCommand(string command)
    {
        switch (command)
        {
            case "魚コーナー":
                MoveTo(fishCorner);
                break;

            case "肉コーナー":
                MoveTo(meatCorner);
                break;

            case "野菜コーナー":
                MoveTo(vegetableCorner);
                break;

            default:
                Debug.Log("対応していない指示です：" + command);
                break;
        }
    }

    private void MoveTo(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogError("移動先が設定されていません");
            return;
        }

        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogError("警備員がNavMesh上にいません");
            return;
        }

        agent.SetDestination(destination.position);

        Debug.Log(destination.name + "へ向かいます");
    }

    private void OnDestroy()
    {
        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnCommandRecognized -= ExecuteCommand;
        }
    }
}