using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// お客さん1人分の処理
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Customer : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveRadius = 10.0f;
    [SerializeField] private float minWaitTime = 2.0f;
    [SerializeField] private float maxWaitTime = 5.0f;

    [Header("お客さん情報")]
    
    [SerializeField] private CustomerColor clothesColor;

    [SerializeField] private bool wearsHat;
    [SerializeField] private bool wearsGlasses;
    [SerializeField] private bool hasBag;
    [SerializeField] private bool isThief;

    public bool IsThief => isThief;
   

    
    public bool IsCaught { get; private set; }
    private NavMeshAgent agent;
    private Vector3 startPosition;

    private float waitTimer;
    private bool isWaiting;

   

     void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        startPosition = transform.position;
    }

     void Start()
    {
        SetNextDestination();
    }

     void Update()
    {
        // 移動中なら何もしない
        if (agent.pathPending)
        {
            return;
        }

        // 目的地に到着したか
        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                StartWaiting();
            }

            Wait();
        }
    }

    /// <summary>
    /// 待機開始
    /// </summary>
    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    /// <summary>
    /// 一定時間待機する
    /// </summary>
    private void Wait()
    {
        waitTimer -= Time.deltaTime;

        if (waitTimer <= 0.0f)
        {
            isWaiting = false;
            SetNextDestination();
        }
    }

    /// <summary>
    /// 次の目的地を決める
    /// </summary>
    private void SetNextDestination()
    {
        Vector3 randomPosition =
            startPosition + Random.insideUnitSphere * moveRadius;

        randomPosition.y = transform.position.y;

        // ランダムな位置の近くにNavMeshがあるか調べる
        if (NavMesh.SamplePosition(
            randomPosition,
            out NavMeshHit hit,
            moveRadius,
            NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// 泥棒かどうかを設定する
    /// </summary>
    public void SetThief(bool value)
    {
        isThief = value;

        Debug.Log($"{gameObject.name} 泥棒設定：{isThief}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    public void Catch()
    {
        if (IsCaught)
        {
            return;
        }

        IsCaught = true;

        // NavMeshAgentを使っているなら止める
        UnityEngine.AI.NavMeshAgent customerAgent =
            GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (customerAgent != null)
        {
            customerAgent.isStopped = true;
        }

        gameObject.SetActive(false);
    }
}