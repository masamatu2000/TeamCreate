
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// ========================================
// �A�j���[�V�������
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
/// ���q����1�l���̏���
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Customer : MonoBehaviour
{
    // ========================================
    // �ړ��ݒ�
    // ========================================

    [Header("�ړ��ݒ�")]

    [SerializeField]
    private float moveRadius = 5.0f;

    [SerializeField]
    private float minWaitTime = 2.0f;

    [SerializeField]
    private float maxWaitTime = 5.0f;

    private Rigidbody rb;


    // ========================================
    // NavMesh�����ݒ�
    // ========================================

    [Header("NavMesh�����ݒ�")]

    [SerializeField]
    private float navMeshSampleDistance = 1.0f;

    [SerializeField]
    private int positionSearchCount = 30;


    // ========================================
    // �ړ����x
    // ========================================

    [Header("�ړ����x")]

    [SerializeField]
    private float normalSpeed = 2.0f;

    [SerializeField]
    private float thiefSpeed = 3.5f;

    [SerializeField]
    private float suspiciousFastWalkSpeed = 3.2f;

    [SerializeField]
    private float suspiciousFastWalkTime = 2.5f;

    [Header("お客さん同士の重なり防止")]

    [SerializeField]
    private float minimumAgentRadius = 0.4f;

    [SerializeField]
    [Range(0, 99)]
    private int avoidancePriorityMin = 20;

    [SerializeField]
    [Range(0, 99)]
    private int avoidancePriorityMax = 80;

    [Header("�I�������R�[�i�[�ݒ�")]

    [Tooltip("���َq�E�����R�[�i�[�ŁA�s����ɓ����R�[�i�[���̕ʒI�ֈړ�����m��")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float stayInLargeCornerRate = 0.6f;

    // ���݌������Ă���ActionPoint
    private Transform currentActionPoint;

    private Transform reservedActionPoint;

    private static readonly HashSet<Transform> reservedActionPoints =
        new HashSet<Transform>();
    // ========================================
    // �s�R�s���m��
    // ========================================

    [Header("�s�R�s���m��")]

    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float normalCustomerSuspiciousRate = 0.1f;

    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float thiefSuspiciousRate = 0.3f;


    // ========================================
    // �e�R�[�i�[
    // ========================================

    [Header("�e�R�[�i�[")]

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
    // �D�_�ݒ�
    // ========================================

    [Header("�D�_�ݒ�")]

    [SerializeField]
    private Transform police;

    [SerializeField]
    private float escapeDistance = 8.0f;

    [Tooltip("�D�_����������A���̃R�[�i�[���œ����͈�")]
    [SerializeField]
    private float thiefMoveRadius = 2.0f;


    // ========================================
    // ���q������
    // ========================================

    [Header("���q������")]

    [SerializeField]
    private CustomerColor clothesColor;

    [SerializeField]
    private bool wearsHat;
    [SerializeField]
    private CustomerColor hatColor = CustomerColor.None;
    [SerializeField]
    private bool wearsGlasses;

    [SerializeField]
    private bool hasBag;

    [SerializeField]
    private bool isThief;


    // ========================================
    // �A�j���[�V����
    // ========================================

    [Header("�A�j���[�V����")]

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private float idleBeforeActionTime = 0.15f;

    [SerializeField]
    private float maxCornerActionTime = 10.0f;

    [Header("移動停止からの復旧")]

    [SerializeField]
    private float stuckTimeLimit = 3.0f;

    [SerializeField]
    private float stuckMoveThreshold = 0.03f;


    // ========================================
    // ���̑�
    // ========================================

    [SerializeField]
    private PlaySceneManager playSceneManager;


    // ========================================
    // ���J���
    // ========================================

    public bool IsThief => isThief;

    public bool IsCaught { get; private set; }


    // ========================================
    // �����ϐ�
    // ========================================

    private NavMeshAgent agent;

    private Transform currentCorner;

    private Transform[] corners;

    private float waitTimer;

    private bool isWaiting;

    private bool isLookingAround;

    private bool isSuspiciousFastWalking;

    private bool hasEscaped;

    private bool wasGameStarted;

    private bool hasMoveDestination;

    private Vector3 moveDestination;

    private Vector3 lastMoveCheckPosition;

    private float stuckTimer;

    private CustomerAnimationState currentAnimationState =
        (CustomerAnimationState)(-1);


    // ========================================
    // Awake
    // ========================================

    private void Awake()
    {
        agent =
            GetComponent<NavMeshAgent>();

        agent.radius =
            Mathf.Max(
                agent.radius,
                minimumAgentRadius
            );

        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        int minimumPriority =
            Mathf.Min(
                avoidancePriorityMin,
                avoidancePriorityMax
            );

        int maximumPriority =
            Mathf.Max(
                avoidancePriorityMin,
                avoidancePriorityMax
            );

        agent.avoidancePriority =
            Random.Range(
                minimumPriority,
                maximumPriority + 1
            );

        rb =
            GetComponent<Rigidbody>();


        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }


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

        if (playSceneManager == null)
        {
            playSceneManager =
                FindAnyObjectByType<PlaySceneManager>();
        }


        PlaceAtRandomCorner();

        lastMoveCheckPosition =
            transform.position;


        SetMoveSpeed();


        // ========================================
        // �ŏ��̖ړI�n
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
        // �J�E���g�_�E�����Ȃ��~
        // ========================================

        if (playSceneManager != null &&
            !playSceneManager.IsGameStarted())
        {
            StopAgent();
        }


        wasGameStarted =
            playSceneManager != null &&
            playSceneManager.IsGameStarted();


        SetAnimation(
            CustomerAnimationState.Idle
        );
    }


    // ========================================
    // Rigidbody�Œ�
    // ========================================

    private void FreezeCustomer()
    {
        if (rb == null)
        {
            return;
        }


        rb.constraints =
            RigidbodyConstraints.FreezePosition |
            RigidbodyConstraints.FreezeRotation;



    }


    private void UnfreezeCustomer()
    {
        if (rb == null)
        {
            return;
        }


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


        bool isGameStarted =
            playSceneManager == null ||
            playSceneManager.IsGameStarted();


        // ========================================
        // �Q�[���J�n�O
        // ========================================

        if (!isGameStarted)
        {
            StopAgent();

            FreezeCustomer();

            UpdateAnimation();

            wasGameStarted =
                false;

            return;
        }


        // ========================================
        // �Q�[���J�n�u��
        // ========================================

        if (!wasGameStarted)
        {
            wasGameStarted =
                true;


            UnfreezeCustomer();


            if (!IsCaught &&
                !isWaiting &&
                !isLookingAround)
            {
                ResumeAgent();
            }
        }


        // ========================================
        // �ߊl�ς�
        // ========================================

        if (IsCaught)
        {
            UpdateAnimation();

            return;
        }


        // ========================================
        // �L�����L������
        // ========================================

        if (isLookingAround)
        {
            StopAgent();

            UpdateAnimation();

            return;
        }


        // ========================================
        // ���i������E���
        // ========================================

        if (isWaiting)
        {
            StopAgent();

            UpdateAnimation();

            return;
        }


        // ========================================
        // �ʏ�ړ�
        // ========================================

        ResumeAgent();


        // ========================================
        // �D�_���x�������瓦����
        // ========================================

        if (isThief &&
            !hasEscaped)
        {
            CheckPoliceDistance();
        }


        // ========================================
        // �o�H�v�Z��
        // ========================================

        if (agent.pathPending)
        {
            UpdateAnimation();

            return;
        }


        // ========================================
        // 棚や他のお客さんに引っかかった場合の復旧
        // ========================================

        if (CheckAndRecoverFromStuck())
        {
            UpdateAnimation();

            return;
        }


        // ========================================
        // �ړI�n��������
        // ========================================

        Vector3 destinationOffset =
            moveDestination - transform.position;

        destinationOffset.y =
            0.0f;

        float arrivalDistance =
            agent.stoppingDistance + 0.3f;

        if (hasMoveDestination &&
            destinationOffset.sqrMagnitude <=
                arrivalDistance * arrivalDistance &&
            (!agent.hasPath ||
             agent.velocity.sqrMagnitude <= 0.01f))
        {
            hasMoveDestination =
                false;

            StartWaiting();

            UpdateAnimation();

            return;
        }


        UpdateAnimation();
    }


    // ========================================
    // 移動中に一定時間ほとんど進まなかった場合、
    // 別の目的地を設定して復旧する
    // ========================================

    private bool CheckAndRecoverFromStuck()
    {
        if (agent == null ||
            !agent.isOnNavMesh ||
            agent.isStopped ||
            agent.pathPending)
        {
            stuckTimer = 0.0f;
            lastMoveCheckPosition = transform.position;

            return false;
        }

        // 目的地の設定自体に失敗した場合も、
        // 少し待ってから別の目的地を探し直す。
        if (!hasMoveDestination)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer < stuckTimeLimit)
            {
                return false;
            }

            stuckTimer = 0.0f;

            if (isThief)
            {
                SetDestinationAroundCurrentCorner();
            }
            else
            {
                MoveToRandomCorner();
            }

            SetAnimation(
                CustomerAnimationState.Walk
            );

            ResumeAgent();

            return true;
        }

        Vector3 movedOffset =
            transform.position - lastMoveCheckPosition;

        movedOffset.y =
            0.0f;

        float moveThresholdSqr =
            stuckMoveThreshold * stuckMoveThreshold;

        if (movedOffset.sqrMagnitude <= moveThresholdSqr &&
            agent.velocity.sqrMagnitude <= 0.01f)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0.0f;
        }

        lastMoveCheckPosition =
            transform.position;

        if (stuckTimer < stuckTimeLimit)
        {
            return false;
        }

        stuckTimer = 0.0f;
        hasMoveDestination = false;

        agent.ResetPath();

        if (isThief)
        {
            SetDestinationAroundCurrentCorner();
        }
        else
        {
            MoveToRandomCorner();
        }

        SetAnimation(
            CustomerAnimationState.Walk
        );

        ResumeAgent();

        return true;
    }


    // ========================================
    // NavMeshAgent��~
    // ========================================

    private void StopAgent()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }


        agent.isStopped =
            true;

        agent.velocity =
            Vector3.zero;
    }


    // ========================================
    // NavMeshAgent�ĊJ
    // ========================================

    private void ResumeAgent()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }


        agent.isStopped =
            false;
    }


    // ========================================
    // �A�j���[�V�����X�V
    // ========================================

    private void UpdateAnimation()
    {
        if (animator == null ||
            agent == null)
        {
            return;
        }


        // ========================================
        // �ߊl�ς�
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
        // �Q�[���J�n�O
        // ========================================

        if (playSceneManager != null &&
            !playSceneManager.IsGameStarted())
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );

            return;
        }


        // ========================================
        // ���i�擾��
        // ========================================

        if (isWaiting)
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );

            return;
        }


        // ========================================
        // �L�����L������
        // ========================================

        if (isLookingAround)
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );

            return;
        }


        // ========================================
        // �ʏ���
        // ========================================

        // ゲーム開始後、行動中でなければ必ずWalk。
        // NavMeshAgentの速度が一瞬0になってもIdleへ戻さない。
        animator.SetFloat(
            "Speed",
            1.0f
        );

        SetAnimation(
            CustomerAnimationState.Walk
        );
    }


    // ========================================
    // ����A�j���[�V�����؂�ւ�
    // ========================================

    private void SetAnimation(
        CustomerAnimationState state)
    {
        if (animator == null)
        {
            return;
        }


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
    // �ʏ푬�x�ݒ�
    // ========================================

    private void SetMoveSpeed()
    {
        if (agent == null)
        {
            return;
        }


        if (isSuspiciousFastWalking)
        {
            agent.speed =
                suspiciousFastWalkSpeed;

            return;
        }


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
    // �ړI�n����
    // ========================================

    private void StartWaiting()
    {
        if (isWaiting ||
            isLookingAround ||
            isSuspiciousFastWalking ||
            IsCaught)
        {
            return;
        }


        StopAgent();

        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        isWaiting =
            true;

        isLookingAround =
            false;

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );
        }


        // ========================================
        // ��ʋq 10%
        // �D�_   30%
        // ========================================

        CustomerAnimationState actionState =
            CustomerAnimationState.TakeItem;

        bool playIdleFirst =
            isThief;

        // 一般客はWalkとPickだけを使用する。
        // 泥棒だけは従来の不審行動を残す。
        if (isThief)
        {
            SetAnimation(
                CustomerAnimationState.Idle
            );

            if (Random.value < thiefSuspiciousRate)
            {
                actionState =
                    Random.value < 0.5f
                        ? CustomerAnimationState.CrouchPick
                        : CustomerAnimationState.LookAround;
            }
        }

        StartCoroutine(
            PlayCornerAction(
                actionState,
                playIdleFirst
            )
        );
    }


    // ========================================
    // Idleからコーナー行動を再生し、
    // モーションが終わってから次の移動を始める
    // ========================================

    private IEnumerator PlayCornerAction(
        CustomerAnimationState actionState,
        bool playIdleFirst)
    {
        if (playIdleFirst)
        {
            yield return new WaitForSeconds(
                idleBeforeActionTime
            );
        }
        else
        {
            yield return null;
        }

        if (IsCaught)
        {
            yield break;
        }

        int idleStateHash =
            animator != null
                ? animator.GetCurrentAnimatorStateInfo(0).fullPathHash
                : 0;

        SetAnimation(actionState);

        bool actionStarted =
            false;

        int actionStateHash =
            0;

        float elapsedTime =
            0.0f;

        // AnimatorがIdleから選択したモーションへ
        // 切り替わるまで待つ。
        while (animator != null &&
               elapsedTime < 1.0f)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(0);

            if (!animator.IsInTransition(0) &&
                stateInfo.fullPathHash != idleStateHash)
            {
                actionStarted =
                    true;

                actionStateHash =
                    stateInfo.fullPathHash;

                break;
            }

            elapsedTime +=
                Time.deltaTime;

            yield return null;
        }

        elapsedTime =
            0.0f;

        if (actionStarted)
        {
            // 選択したモーションが1周終わるまで待つ。
            while (animator != null &&
                   elapsedTime < maxCornerActionTime)
            {
                AnimatorStateInfo stateInfo =
                    animator.GetCurrentAnimatorStateInfo(0);

                bool animationFinished =
                    !animator.IsInTransition(0) &&
                    stateInfo.fullPathHash == actionStateHash &&
                    stateInfo.normalizedTime >= 1.0f;

                if (animationFinished)
                {
                    break;
                }

                elapsedTime +=
                    Time.deltaTime;

                yield return null;
            }
        }
        else
        {
            // 遷移設定に問題があっても永久停止しないための予備待機。
            yield return new WaitForSeconds(
                Mathf.Max(0.5f, minWaitTime)
            );
        }

        if (IsCaught)
        {
            yield break;
        }

        // Pickの姿勢のまま移動しないように、
        // 先にAnimatorをWalkへ切り替える。
        SetAnimation(
            CustomerAnimationState.Walk
        );

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                1.0f
            );

            animator.CrossFade(
                "Base Layer.Walk",
                0.1f,
                0
            );

            float walkTransitionTimer =
                0.0f;

            // Animatorが実際にWalkへ入るまで、
            // NavMeshAgentは停止したまま待つ。
            while (walkTransitionTimer < 1.0f)
            {
                AnimatorStateInfo stateInfo =
                    animator.GetCurrentAnimatorStateInfo(0);

                if (!animator.IsInTransition(0) &&
                    stateInfo.IsName("Base Layer.Walk"))
                {
                    break;
                }

                walkTransitionTimer +=
                    Time.deltaTime;

                yield return null;
            }
        }

        isWaiting =
            false;

        MoveAfterWaiting();
    }


    // ========================================
    // �ʏ�s��
    // ========================================

    private void StartNormalAction()
    {
        isWaiting =
            true;


        waitTimer =
            Random.Range(
                minWaitTime,
                maxWaitTime
            );


        SetAnimation(
            CustomerAnimationState.TakeItem
        );


        //Debug.Log(
        //    $"{gameObject.name}�F" +
        //    "�ʏ�s�� �� ���i������"
        //);
    }


    // ========================================
    // �s�R�s��
    // ========================================

    private void StartSuspiciousAction()
    {
        int randomAction =
            Random.Range(
                0,
                3
            );


        switch (randomAction)
        {
            // ========================================
            // ���Ⴊ��ŋ���
            // ========================================

            case 0:

                isWaiting =
                    true;


                waitTimer =
                    Random.Range(
                        minWaitTime,
                        maxWaitTime
                    );


                SetAnimation(
                    CustomerAnimationState.CrouchPick
                );


                //Debug.Log(
                //    $"{gameObject.name}�F" +
                //    "�s�R�s�� �� ���Ⴊ��ŋ���"
                //);

                break;


            // ========================================
            // �L�����L����
            // ========================================

            case 1:

                isLookingAround =
                    true;


                StartCoroutine(
                    LookAroundBeforeMove()
                );


                //Debug.Log(
                //    $"{gameObject.name}�F" +
                //    "�s�R�s�� �� �L�����L����"
                //);

                break;


            // ========================================
            // ������
            // ========================================

            case 2:

                StartCoroutine(
                    SuspiciousFastWalk()
                );


                //Debug.Log(
                //    $"{gameObject.name}�F" +
                //    "�s�R�s�� �� ������"
                //);

                break;
        }
    }


    // ========================================
    // ���i�擾��
    // ========================================

    private void Wait()
    {
        waitTimer -=
            Time.deltaTime;


        if (waitTimer >
            0.0f)
        {
            return;
        }


        isWaiting =
            false;


        MoveAfterWaiting();
    }


    // ========================================
    // �L�����L����
    // ========================================

    private IEnumerator LookAroundBeforeMove()
    {
        StopAgent();


        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                0.0f
            );
        }


        SetAnimation(
            CustomerAnimationState.LookAround
        );


        yield return
            new WaitForSeconds(
                2.0f
            );


        isLookingAround =
            false;


        MoveAfterWaiting();
    }


    // ========================================
    // �s�R�ȑ�����
    // ========================================

    private IEnumerator SuspiciousFastWalk()
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            yield break;
        }


        isSuspiciousFastWalking =
            true;


        agent.speed =
            suspiciousFastWalkSpeed;


        // ========================================
        // ���̏ꏊ��ݒ�
        // ========================================

        if (isThief)
        {
            SetDestinationAroundCurrentCorner();
        }
        else
        {
            MoveToRandomCorner();
        }


        ResumeAgent();


        yield return
            new WaitForSeconds(
                suspiciousFastWalkTime
            );


        isSuspiciousFastWalking =
            false;


        SetMoveSpeed();
    }


    // ========================================
    // �s���I����
    // ========================================

    // ========================================
    // �s���I����
    // ========================================

    private void MoveAfterWaiting()
    {
        if (IsCaught)
        {
            return;
        }

        isWaiting = false;
        isLookingAround = false;

        SetAnimation(
            CustomerAnimationState.Walk
        );

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                1.0f
            );
        }


        // ========================================
        // �D�_
        // ========================================

        if (isThief)
        {
            SetDestinationAroundCurrentCorner();

            ResumeAgent();

            return;
        }


        // ========================================
        // �I�������R�[�i�[�̏ꍇ
        //
        // ���َq / ����
        // ========================================

        bool isLargeCorner =
            currentCorner == snackCorner ||
            currentCorner == drinkCorner;


        if (isLargeCorner)
        {
            // ========================================
            // ���m����
            // �����R�[�i�[�̕ʒI������
            // ========================================

            if (Random.value <
                stayInLargeCornerRate)
            {
                SetDestinationAroundCorner(
                    currentCorner,
                    moveRadius
                );

                ResumeAgent();

                return;
            }
        }


        // ========================================
        // �ʂ̃R�[�i�[�ֈړ�
        // ========================================

        MoveToRandomCorner();

        ResumeAgent();
    }


    // ========================================
    // �Q�[���J�n���̏����z�u
    // ========================================

    private void PlaceAtRandomCorner()
    {
        if (corners == null ||
            corners.Length == 0)
        {
            Debug.LogWarning(
                $"{gameObject.name}�F" +
                "�R�[�i�[���ݒ肳��Ă��܂���"
            );

            return;
        }


        bool[] checkedCorners =
            new bool[corners.Length];


        int checkedCount =
            0;


        while (checkedCount <
            corners.Length)
        {
            int randomIndex =
                Random.Range(
                    0,
                    corners.Length
                );


            if (checkedCorners[randomIndex])
            {
                continue;
            }


            checkedCorners[randomIndex] =
                true;


            checkedCount++;


            Transform selectedCorner =
                corners[randomIndex];


            if (selectedCorner == null)
            {
                continue;
            }


            currentCorner =
                selectedCorner;


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
                    if (agent.Warp(
                        hit.position))
                    {
                        return;
                    }
                }
            }


            NavMeshHit centerHit;


            if (NavMesh.SamplePosition(
                currentCorner.position,
                out centerHit,
                navMeshSampleDistance,
                NavMesh.AllAreas))
            {
                if (agent.Warp(
                    centerHit.position))
                {
                    return;
                }
            }
        }


        Debug.LogError(
            $"{gameObject.name}�F" +
            "���ׂẴR�[�i�[�Ŕz�u�Ɏ��s���܂���"
        );
    }


    // ========================================
    // ��ʋq
    // �ʃR�[�i�[��
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
    // ���݂Ƃ͈Ⴄ�R�[�i�[
    // ========================================

    private Transform GetRandomDifferentCorner()
    {
        int validCornerCount =
            0;


        foreach (Transform corner
                 in corners)
        {
            if (corner != null)
            {
                validCornerCount++;
            }
        }


        if (validCornerCount ==
            0)
        {
            return null;
        }


        if (validCornerCount ==
            1)
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


        return currentCorner;
    }


    // ========================================
    // �w��R�[�i�[�ֈړ�
    // ========================================

    private void SetDestinationAroundCorner(
        Transform corner,
        float radius)
    {

        ReleaseActionPointReservation();

        hasMoveDestination =
            false;

        if (corner == null ||
            agent == null)
        {
            return;
        }


        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning(
                $"{gameObject.name} ��NavMesh��ɂ��܂���"
            );

            return;
        }

        // ========================================
        // ActionPoint��D��
        // ========================================

        Transform actionPoint =
            GetRandomActionPoint(
                corner
            );

        if (actionPoint != null)
        {



            NavMeshHit actionHit;


            if (NavMesh.SamplePosition(
                actionPoint.position,
                out actionHit,
                3.0f,
                NavMesh.AllAreas))
            {



                NavMeshPath actionPath =
                    new NavMeshPath();


                if (agent.CalculatePath(
                    actionHit.position,
                    actionPath))
                {
                    if (actionPath.status ==
                        NavMeshPathStatus.PathComplete)
                    {



                        agent.SetDestination(
                            actionHit.position
                        );

                        moveDestination =
                            actionHit.position;

                        hasMoveDestination =
                            true;

                        stuckTimer =
                            0.0f;

                        lastMoveCheckPosition =
                            transform.position;

                        return;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"{actionPoint.name} �܂ł̌o�H���s���S�ł�"
                        );
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    $"{actionPoint.name} �̋߂���NavMesh������܂���"
                );
            }
        }

        // ActionPointまでの経路が作れなかった場合は予約を解放し、
        // コーナー周辺のランダム地点を探す。
        ReleaseActionPointReservation();
        //else
        //{
        //    //Debug.LogWarning(
        //    //    $"{corner.name} ��ActionPoint��������܂���"
        //    //);
        //}


        // ========================================
        // ActionPoint���Ȃ��ꍇ�͏]������
        // ========================================

        for (int i = 0;
             i < positionSearchCount;
             i++)
        {
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


            if (!NavMesh.SamplePosition(
                randomPosition,
                out hit,
                navMeshSampleDistance,
                NavMesh.AllAreas))
            {
                continue;
            }


            NavMeshPath path =
                new NavMeshPath();


            if (agent.CalculatePath(
                hit.position,
                path))
            {
                if (path.status ==
                    NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(
                        hit.position
                    );

                    moveDestination =
                        hit.position;

                    hasMoveDestination =
                        true;

                    stuckTimer =
                        0.0f;

                    lastMoveCheckPosition =
                        transform.position;

                    return;
                }
            }
        }


        Debug.LogWarning(
            $"{gameObject.name}�F" +
            $"{corner.name}���ӂɈړ��悪������܂���ł���"
        );
    }


    // ========================================
    // ActionPoint�擾
    // ========================================

    // ========================================
    // �R�[�i�[���̒�~�|�C���g���擾
    // �O��ƈႤActionPoint��D�悷��
    // ========================================

    private Transform GetRandomActionPoint(
        Transform corner)
    {

        if (corner == null)
        {
            return null;
        }

        reservedActionPoints.RemoveWhere(
            point => point == null
        );

        Transform[] children =
            corner.GetComponentsInChildren<Transform>();

        List<Transform> actionPoints =
            new List<Transform>();

        foreach (Transform child in children)
        {
            if (child == corner)
            {
                continue;
            }

            if (!child.name.Contains("ActionPoint"))
            {
                continue;
            }

            // ActionPoint����������ꍇ��
            // �O��Ɠ����ꏊ�����O
            if (child == currentActionPoint)
            {
                continue;
            }

            // ほかのお客さんが使用中の場所は選ばない。
            if (reservedActionPoints.Contains(child))
            {
                continue;
            }

            actionPoints.Add(child);
        }

        // �O��ƈႤ�ꏊ���Ȃ������ꍇ
        // �����ꏊ�ł������̂ōĎ擾
        if (actionPoints.Count == 0)
        {
            foreach (Transform child in children)
            {
                if (child == corner)
                {
                    continue;
                }

                if (child.name.Contains("ActionPoint") &&
                    !reservedActionPoints.Contains(child))
                {
                    actionPoints.Add(child);
                }
            }
        }

        if (actionPoints.Count == 0)
        {
            return null;
        }

        int randomIndex =
            Random.Range(
                0,
                actionPoints.Count
            );

        currentActionPoint =
            actionPoints[randomIndex];

        reservedActionPoint =
            currentActionPoint;

        reservedActionPoints.Add(
            reservedActionPoint
        );

        return currentActionPoint;
    }


    // ========================================
    // 使用中のActionPoint予約を解放
    // ========================================

    private void ReleaseActionPointReservation()
    {
        if (reservedActionPoint == null)
        {
            return;
        }

        reservedActionPoints.Remove(
            reservedActionPoint
        );

        reservedActionPoint =
            null;
    }


    private void OnDisable()
    {
        ReleaseActionPointReservation();
    }


    // ========================================
    // �D�_
    // ���݃R�[�i�[��
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
    // �x�����Ƃ̋���
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


        if (distance <=
            escapeDistance)
        {
            EscapeFromPolice();
        }
    }


    // ========================================
    // �D�_����
    // ========================================

    private void EscapeFromPolice()
    {
        if (hasEscaped)
        {
            return;
        }


        hasEscaped =
            true;


        isWaiting =
            false;

        isLookingAround =
            false;

        isSuspiciousFastWalking =
            false;


        StopAllCoroutines();


        SetMoveSpeed();


        Transform escapeCorner =
            GetFarthestCornerFromPolice();


        if (escapeCorner == null)
        {
            return;
        }


        currentCorner =
            escapeCorner;


        SetAnimation(
            CustomerAnimationState.Idle
        );


        SetDestinationAroundCorner(
            currentCorner,
            thiefMoveRadius
        );


        ResumeAgent();
    }


    // ========================================
    // �x��������ł������R�[�i�[
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
    // �Ŋ��R�[�i�[
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
    // �D�_�ݒ�
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
            $"{gameObject.name} �D�_�ݒ�F{isThief}"
        );
    }


    // ========================================
    // ������v
    // ========================================

    public bool Matches(
     VoiceCommand command)
    {
        // ========================================
        // ���̐F
        // ========================================

        if (command.clothesColor !=
                CustomerColor.None &&
            clothesColor !=
                command.clothesColor)
        {
            return false;
        }


        // ========================================
        // �X�q
        // ========================================

        if (command.requiresHat)
        {
            // �X�q�����Ԃ��Ă��Ȃ�
            if (!wearsHat)
            {
                return false;
            }

            // �X�q�̐F�܂Ŏw�肳��Ă���
            if (command.hatColor !=
                    CustomerColor.None &&
                hatColor !=
                    command.hatColor)
            {
                return false;
            }
        }


        // ========================================
        // ���K�l
        // ========================================

        if (command.requiresGlasses &&
            !wearsGlasses)
        {
            return false;
        }


        // ========================================
        // �o�b�O
        // ========================================

        if (command.requiresBag &&
            !hasBag)
        {
            return false;
        }


        return true;
    }


    // ========================================
    // �ߊl
    // ========================================

    public void Catch()
    {
        if (IsCaught)
        {
            return;
        }


        // ========================================
        // �D�_
        // ========================================

        if (IsThief)
        {
            IsCaught =
                true;

            ReleaseActionPointReservation();


            isWaiting =
                false;

            isLookingAround =
                false;

            isSuspiciousFastWalking =
                false;


            StopAllCoroutines();


            StopAgent();


            SetAnimation(
                CustomerAnimationState.ArrestedWalk
            );


            Debug.Log(
                $"{gameObject.name} �͓D�_�ł����I�m�ې����I"
            );


            if (playSceneManager != null)
            {
                playSceneManager.Caught();

                playSceneManager.ThiefCaught();
            }


            return;
        }


        // ========================================
        // ��ʋq
        // ========================================

        Debug.Log(
            $"{gameObject.name} �͈�ʋq�ł��I��F�ߕ߁I"
        );


        if (playSceneManager != null)
        {
            playSceneManager.AddComplaint();

            playSceneManager.Caught();

            playSceneManager.AddTimePenalty();
        }
    }
    public CornerType GetCurrentCornerType()
    {
        if (currentCorner == fishCorner)
        {
            return CornerType.Fish;
        }

        if (currentCorner == vegetableCorner)
        {
            return CornerType.Vegetable;
        }

        if (currentCorner == snackCorner)
        {
            return CornerType.Snack;
        }

        if (currentCorner == frozenFoodCorner)
        {
            return CornerType.FrozenFood;
        }

        if (currentCorner == drinkCorner)
        {
            return CornerType.Drink;
        }

        if (currentCorner == preparedFoodCorner)
        {
            return CornerType.PreparedFood;
        }

        if (currentCorner == meatCorner)
        {
            return CornerType.Meat;
        }

        if (currentCorner == breadCorner)
        {
            return CornerType.Bread;
        }

        return CornerType.None;
    }
}
