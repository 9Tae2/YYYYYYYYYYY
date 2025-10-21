using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    [Header("Boss Settings")]
    public float detectionRange = 10f;
    public float maxHealth = 100f;
    public float lowHealthThreshold = 0.3f; // 30%

    [Header("Detection Settings")]
    [Tooltip("�÷��̾� ���� ����")]
    public float playerDetectionRadius = 10f;
    [Tooltip("���� ������ �ð������� ǥ������ ����")]
    public bool showDetectionRange = true;

    [Header("Attack Settings")]
    public float basicAttackInterval = 2f;
    public float areaAttackInterval = 8f;
    public float lowHealthAreaAttackInterval = 4f;
    public float basicAttackRange = 2f;
    public float basicAttackDamage = 20f;
    public float areaAttackDamage = 30f;

    [Header("Area Attack Settings")]
    [Tooltip("���� ��ų�� �ּ� ����")]
    public float areaAttackMinRange = 3f;
    [Tooltip("���� ��ų�� �ִ� ����")]
    public float areaAttackMaxRange = 8f;
    [Tooltip("���� ��ų ������ �ð������� ǥ������ ����")]
    public bool showAreaAttackRange = true;
    [Tooltip("���� ��ų�� ����� ��ġ (0=���� �߽�, 1=�÷��̾� ��ġ, 0.5=�߰�����)")]
    [Range(0f, 1f)]
    public float areaAttackTargetBlend = 0.5f;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float returnSpeed = 2f;

    [Header("Territory Settings")]
    [Tooltip("������ �ʱ� ��ġ (����θ� ���� ��ġ ���)")]
    public Transform initialPositionTransform;
    [Tooltip("�ʱ� ��ġ�� �߽����� �� Ȱ�� ���� �ݰ�")]
    public float territoryRadius = 15f;
    [Tooltip("������ �ð������� ǥ������ ����")]
    public bool showTerritoryRange = true;

    [Header("Particle Effects")]
    [Tooltip("���� ���� ��� ��ƼŬ ������")]
    public GameObject areaAttackWarningParticle;
    [Tooltip("���� ���� ���� ��ƼŬ ������")]
    public GameObject areaAttackExplosionParticle;

    // Components
    private Animator animator;
    private NavMeshAgent navAgent;
    private Transform player;
    private Vector3 initialPosition;

    // State Management
    public enum BossState
    {
        Idle,
        FirstEncounter,
        Combat,
        Returning,
        Dead
    }

    private BossState currentState = BossState.Idle;
    public float currentHealth; // public���� ����
    private bool hasEncounteredPlayer = false;
    private bool isLowHealth = false;

    // Timers
    private float basicAttackTimer;
    private float areaAttackTimer;
    private bool isAttacking = false;
    private bool isDamaged = false;

    // Area Attack Variables
    private float currentAreaAttackRange;
    private Vector3 currentAreaAttackCenter;

    // Animation State Hashes (���� ����ȭ)
    private readonly int idleHash = Animator.StringToHash("Idle");
    private readonly int angryHash = Animator.StringToHash("Angry");
    private readonly int walkHash = Animator.StringToHash("Walk");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int areaAttackHash = Animator.StringToHash("AreaAttack");
    private readonly int areaAttackReadyHash = Animator.StringToHash("AreaAttackReady");
    private readonly int damageHash = Animator.StringToHash("Damage");
    private readonly int dieHash = Animator.StringToHash("Die");

    void Start()
    {
        // ������Ʈ �ʱ�ȭ
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Rigidbody ���� (�ִ� ���)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // NavMeshAgent�� �浹 ����
            rb.useGravity = false;
        }

        // NavMeshAgent ���� (�߿�!)
        if (navAgent != null)
        {
            navAgent.updateRotation = false; // ���� ȸ�� ����
            navAgent.updatePosition = true;
            navAgent.speed = moveSpeed;
            navAgent.angularSpeed = 0f; // �ڵ� ȸ�� ��Ȱ��ȭ
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = basicAttackRange * 0.5f; // ���� ������ ���ݿ��� ����
            navAgent.autoBraking = true;
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        // �ʱ� ����
        if (initialPositionTransform != null)
        {
            initialPosition = initialPositionTransform.position;
        }
        else
        {
            initialPosition = transform.position;
        }

        currentHealth = maxHealth;

        // ���� ���� ����ȭ (���� ȣȯ��)
        detectionRange = playerDetectionRadius;

        // �ʱ� ���� ����
        SetState(BossState.Idle);

        if (player == null)
        {
            Debug.LogWarning("Player not found! Make sure Player has 'Player' tag.");
        }
    }

    void Update()
    {
        if (currentState == BossState.Dead || player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float distanceFromInitialPosition = Vector3.Distance(player.position, initialPosition); // �ʱ���ġ ����!

        UpdateState(distanceToPlayer, distanceFromInitialPosition);
        UpdateTimers();
        CheckLowHealth();
    }

    void UpdateState(float distanceToPlayer, float distanceFromInitialPosition)
    {
        switch (currentState)
        {
            case BossState.Idle:
                HandleIdleState(distanceFromInitialPosition);
                break;

            case BossState.FirstEncounter:
                HandleFirstEncounterState();
                break;

            case BossState.Combat:
                HandleCombatState(distanceToPlayer, distanceFromInitialPosition);
                break;

            case BossState.Returning:
                HandleReturningState(distanceFromInitialPosition);
                break;
        }
    }

    void HandleIdleState(float distanceFromInitialPosition)
    {
        if (distanceFromInitialPosition <= territoryRadius)
        {
            SetState(BossState.FirstEncounter);
        }
    }

    void HandleFirstEncounterState()
    {
        // �÷��̾ �ٶ󺸱�
        LookAtPlayer();

        // ���� �ִϸ��̼��� �������� Ȯ��
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Angry"))
        {
            SetState(BossState.Combat);
            hasEncounteredPlayer = true;
        }
    }

    void HandleCombatState(float distanceToPlayer, float distanceFromInitialPosition)
    {
        // �÷��̾ ������ ������� Ȯ��!
        if (distanceFromInitialPosition > territoryRadius)
        {
            SetState(BossState.Returning);
            return;
        }

        if (!isAttacking && !isDamaged)
        {
            // �÷��̾� ����
            if (navAgent.enabled)
            {
                navAgent.SetDestination(player.position);
                navAgent.isStopped = false;
            }

            // �ڿ������� ȸ�� (�̵� �߿��� �÷��̾ �ٶ�)
            LookAtPlayer();

            // �̵� �ִϸ��̼�
            if (navAgent.velocity.magnitude > 0.1f)
            {
                PlayAnimation(walkHash);
            }
            else
            {
                PlayAnimation(idleHash);
            }

            // ���� ����
            if (distanceToPlayer <= basicAttackRange && basicAttackTimer >= basicAttackInterval)
            {
                StartCoroutine(PerformBasicAttack());
            }
            else
            {
                float areaInterval = isLowHealth ? lowHealthAreaAttackInterval : areaAttackInterval;
                if (areaAttackTimer >= areaInterval)
                {
                    StartCoroutine(PerformAreaAttack());
                }
            }
        }
    }

    void HandleReturningState(float distanceFromInitialPosition)
    {
        // �÷��̾ �ٽ� ���� ������ ���Դ��� Ȯ��
        if (distanceFromInitialPosition <= territoryRadius)
        {
            SetState(BossState.FirstEncounter);
            return;
        }

        // ���� �ڽ��� �ʱ� ��ġ������ �Ÿ� ���
        float bossDistanceToInitial = Vector3.Distance(transform.position, initialPosition);

        // �ʱ� ��ġ�� �����ߴ��� Ȯ��
        if (bossDistanceToInitial <= 1.5f)
        {
            // �ʱ� ��ġ ���� - Idle ���·� ��ȯ
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
            navAgent.isStopped = true;

            // ��Ȯ�� ��ġ�� ȸ������ ����
            transform.position = initialPosition;
            transform.rotation = Quaternion.identity;

            SetState(BossState.Idle);
            hasEncounteredPlayer = false;

            Debug.Log("Boss returned to initial position and entered Idle state");
        }
        else
        {
            // ���� ���ư��� �� - ��� �ʱ� ��ġ�� �̵�
            navAgent.speed = returnSpeed;
            navAgent.isStopped = false;

            // �������� ���������� ���� (NavMesh ���� ����)
            if (!navAgent.pathPending && navAgent.remainingDistance < 0.1f)
            {
                navAgent.SetDestination(initialPosition);
            }

            // Walk �ִϸ��̼�
            PlayAnimation(walkHash);

            Debug.Log($"Boss returning to initial position. Distance: {bossDistanceToInitial:F1}");
        }
    }

    void SetState(BossState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case BossState.Idle:
                navAgent.speed = moveSpeed;
                navAgent.isStopped = true; // Idle ���¿����� ������ ����
                PlayAnimation(idleHash);
                ResetTimers();
                Debug.Log("Boss entered Idle state");
                break;

            case BossState.FirstEncounter:
                navAgent.ResetPath();
                PlayAnimation(angryHash);
                break;

            case BossState.Combat:
                navAgent.speed = moveSpeed;
                ResetTimers();
                break;

            case BossState.Returning:
                navAgent.speed = returnSpeed;
                navAgent.isStopped = false;
                // �ʱ� ��ġ�� �̵� ����
                if (navAgent.destination != initialPosition)
                {
                    navAgent.SetDestination(initialPosition);
                }
                Debug.Log("Boss is returning to initial position");
                break;

            case BossState.Dead:
                navAgent.enabled = false;
                PlayAnimation(dieHash);
                break;
        }
    }

    IEnumerator PerformBasicAttack()
    {
        isAttacking = true;
        navAgent.ResetPath();

        LookAtPlayer();
        PlayAnimation(attackHash);

        // ���� �ִϸ��̼� ���
        yield return new WaitForSeconds(0.5f);

        // ���� �Ÿ� ��Ȯ�� (������ �������� �� ����)
        float currentDistance = Vector3.Distance(transform.position, player.position);
        Debug.Log($"Basic Attack - Distance: {currentDistance:F1}, Range: {basicAttackRange}");

        // ������ ���� (�÷��̾ ���� ���� �ִ� ���)
        if (currentDistance <= basicAttackRange)
        {
            // PlayerHealth�� ������ ���, ������ ����� �α׸�
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(basicAttackDamage);
                Debug.Log($"Basic Attack HIT! Damage: {basicAttackDamage}");
            }
            else
            {
                Debug.Log($"Player hit by basic attack! Damage: {basicAttackDamage}");
            }
        }
        else
        {
            Debug.Log($"Basic Attack MISSED! Distance: {currentDistance:F1} > Range: {basicAttackRange}");
        }

        // ���� �� ���
        yield return new WaitForSeconds(0.5f);

        basicAttackTimer = 0f;
        isAttacking = false;
    }

    IEnumerator PerformAreaAttack()
    {
        isAttacking = true;
        navAgent.ResetPath();

        // ���� ������ ��ġ ����
        SetRandomAreaAttackParameters();

        // ��ƼŬ ����Ʈ�� ���� (���� �� ����)
        if (areaAttackWarningParticle != null)
        {
            Vector3 particlePos = currentAreaAttackCenter + Vector3.up * 0.1f;
            GameObject warningFX = Instantiate(areaAttackWarningParticle, particlePos, Quaternion.identity);

            // ��ƼŬ ũ�⸦ ���� ���� ������ �°� ����
            warningFX.transform.localScale = Vector3.one * (currentAreaAttackRange / 3f);

            Destroy(warningFX, 1.5f); // 1.5�� �� �ڵ� ����
        }

        LookAtPlayer();

        // �غ� �ִϸ��̼�
        PlayAnimation(areaAttackReadyHash);
        yield return new WaitForSeconds(1f);

        // ���� ���� �ִϸ��̼�
        PlayAnimation(areaAttackHash);
        yield return new WaitForSeconds(0.5f);

        // ������ ���� �� ������ ����
        ApplyAreaDamage();

        // ���� ��ƼŬ ����Ʈ ���� (ũ�� ����)
        if (areaAttackExplosionParticle != null)
        {
            Vector3 particlePos = currentAreaAttackCenter + Vector3.up * 0.1f;
            GameObject explosionFX = Instantiate(areaAttackExplosionParticle, particlePos, Quaternion.identity);

            // ��ƼŬ ũ�⸦ ���� ���� ������ �°� ����
            explosionFX.transform.localScale = Vector3.one * (currentAreaAttackRange / 3f);

            Destroy(explosionFX, 3f); // 3�� �� �ڵ� ����
        }

        // ���� �� ���
        yield return new WaitForSeconds(1f);

        areaAttackTimer = 0f;
        isAttacking = false;
    }

    void SetRandomAreaAttackParameters()
    {
        // ���� ���� ����
        currentAreaAttackRange = Random.Range(areaAttackMinRange, areaAttackMaxRange);

        // ���� �߽��� ���� (������ �÷��̾� ������ ���� ����Ʈ)
        Vector3 bossPosition = transform.position;
        Vector3 playerPosition = player.position;
        currentAreaAttackCenter = Vector3.Lerp(bossPosition, playerPosition, areaAttackTargetBlend);

        // �ణ�� ������ �߰� (�ɼ�)
        Vector3 randomOffset = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        );
        currentAreaAttackCenter += randomOffset;

        Debug.Log($"Area Attack - Range: {currentAreaAttackRange:F1}, Center: {currentAreaAttackCenter}");
    }

    void ApplyAreaDamage()
    {
        // �÷��̾ ���� ���� ���� ���� �ִ��� Ȯ��
        float distanceToAttackCenter = Vector3.Distance(player.position, currentAreaAttackCenter);

        if (distanceToAttackCenter <= currentAreaAttackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(areaAttackDamage);
                Debug.Log($"Player hit by area attack! Distance: {distanceToAttackCenter:F1}");
            }
            else
            {
                Debug.Log($"Player hit by area attack! Damage: {areaAttackDamage}, Distance: {distanceToAttackCenter:F1}");
            }
        }
        else
        {
            Debug.Log($"Player avoided area attack. Distance: {distanceToAttackCenter:F1}");
        }
    }

    public void TakeDamage(float damage)
    {
        if (currentState == BossState.Dead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Boss took {damage} damage! Current health: {currentHealth}/{maxHealth}");

        // �ǰ� �ִϸ��̼�
        if (!isAttacking)
        {
            StartCoroutine(PlayDamageAnimation());
        }

        // ü���� 0 ���ϰ� �Ǹ� ���
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            SetState(BossState.Dead);
        }
    }

    IEnumerator PlayDamageAnimation()
    {
        isDamaged = true;
        navAgent.ResetPath();

        PlayAnimation(damageHash);
        yield return new WaitForSeconds(0.5f);

        isDamaged = false;
    }

    void UpdateTimers()
    {
        if (currentState == BossState.Combat && !isAttacking)
        {
            basicAttackTimer += Time.deltaTime;
            areaAttackTimer += Time.deltaTime;
        }
    }

    void CheckLowHealth()
    {
        isLowHealth = (currentHealth / maxHealth) <= lowHealthThreshold;
    }

    void ResetTimers()
    {
        basicAttackTimer = 0f;
        areaAttackTimer = 0f;
    }

    void LookAtPlayer()
    {
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0; // Y�� ȸ�� ����

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }
        }
    }

    void PlayAnimation(int animationHash)
    {
        if (animator != null)
        {
            animator.Play(animationHash);
        }
    }

    // �ܺο��� ���� ���� Ȯ�ο�
    public BossState GetCurrentState()
    {
        return currentState;
    }

    // ����׿� �����
    void OnDrawGizmosSelected()
    {
        Vector3 centerPos = Application.isPlaying ? initialPosition :
            (initialPositionTransform != null ? initialPositionTransform.position : transform.position);

        // �÷��̾� ���� ���� (���� �߽�)
        if (showDetectionRange)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);
        }

        // ���� ���� (�ʱ� ��ġ �߽�)
        if (showTerritoryRange)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // ����Ÿ (������)
            Gizmos.DrawWireSphere(centerPos, territoryRadius);

            // ���� �߽��� ǥ��
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(centerPos, Vector3.one * 1.5f);
        }

        // �⺻ ���� ����
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, basicAttackRange);

        // ���� ���� ���� �̸����� (�ּ�/�ִ�)
        if (showAreaAttackRange)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.3f); // ������ �Ķ�
            Gizmos.DrawWireSphere(transform.position, areaAttackMinRange);

            Gizmos.color = new Color(0f, 0f, 1f, 0.1f); // �� ������ �Ķ�
            Gizmos.DrawWireSphere(transform.position, areaAttackMaxRange);
        }

        // ���� ���� ���� ���� (���� ���� ���� ��)
        if (Application.isPlaying && currentAreaAttackRange > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentAreaAttackCenter, currentAreaAttackRange);

            // ���� �߽��� ǥ��
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(currentAreaAttackCenter, Vector3.one * 0.5f);
        }

        // ������ ��ġ ǥ��
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(centerPos, Vector3.one);

        // �ʱ� ��ġ������ ���ἱ (���� ���� ��)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, centerPos);
        }
    }

    // ��Ÿ�ӿ��� ���� Ȯ�ο� (�ɼ�)
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        // ���� ���� ǥ��
        switch (currentState)
        {
            case BossState.Idle:
                Gizmos.color = Color.white;
                break;
            case BossState.FirstEncounter:
                Gizmos.color = new Color(1f, 0.5f, 0f); // ��������
                break;
            case BossState.Combat:
                Gizmos.color = Color.red;
                break;
            case BossState.Returning:
                Gizmos.color = Color.blue;
                break;
            case BossState.Dead:
                Gizmos.color = Color.black;
                break;
        }

        // ���� �Ӹ� ���� ���� ǥ��
        Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);
    }
}