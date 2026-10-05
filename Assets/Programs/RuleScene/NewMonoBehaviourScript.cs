using UnityEngine;
using UnityEngine.AI;

public class TutorialPoliceController : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent agent;

    [Header("Police Models")]
    [SerializeField]
    private GameObject idleModel;

    [SerializeField]
    private GameObject walkModel;


    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }


    private void Start()
    {
        SetIdle();
    }


    public void MoveTo(Vector3 position)
    {
        if (agent == null)
        {
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(position);
    }


    private void Update()
    {
        if (agent == null)
        {
            return;
        }

        bool isMoving =
            agent.velocity.sqrMagnitude > 0.1f;

        if (isMoving)
        {
            SetWalk();
        }
        else
        {
            SetIdle();
        }
    }


    private void SetIdle()
    {
        if (idleModel != null)
        {
            idleModel.SetActive(true);
        }

        if (walkModel != null)
        {
            walkModel.SetActive(false);
        }
    }


    private void SetWalk()
    {
        if (idleModel != null)
        {
            idleModel.SetActive(false);
        }

        if (walkModel != null)
        {
            walkModel.SetActive(true);
        }
    }
}