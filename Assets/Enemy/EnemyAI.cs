using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI; // NavMeshAgent���g�p

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack, Stunned, Dead, Support }

    [Header("��{AI�ݒ�")]
    public EnemyState currentState = EnemyState.Patrol;
    public float sightRange = 10f; // �v���C���[�𔭌����鋗��
    public float attackRange = 2f; // �v���C���[���U�����鋗��
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float rotationSpeed = 720f;
    public LayerMask playerLayer; // �v���C���[�̃��C���[
    public LayerMask obstacleLayer; // ��Q���̃��C���[

    [Header("�p�g���[���ݒ�")]
    public Transform[] patrolPoints;
    private int currentPatrolPointIndex;
    public float patrolPointTolerance = 0.5f;

    [Header("�U���ݒ�")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime;
    public int attackDamage = 10;
    public float attackAnimDuration = 1f; // �U���A�j���[�V�����̒����i���j

    [Header("���m�\��")]
    public float soundDetectionRadius = 10f;
    public LayerMask footprintLayer; // ���Ղ̃��C���[
    public float footprintDetectionRange = 5f;

    [Header("����x��AI")]
    public bool isBoss = false;
    [System.Serializable]
    public class BossPhase
    {
        public float healthThreshold = 0.5f; // �̗�50%�ňڍs
        public List<string> phaseSpecificAttackTriggers; // ���̃t�F�[�Y�Ŏg���U���A�j���[�V�����̃g���K�[���Ȃ�
        public float phaseSpecificMoveSpeedMultiplier = 1.2f; // �t�F�[�Y���̈ړ����x�{��
        // ���̑��A�t�F�[�Y�ŗL�̃X�L����s�����W�b�N�ւ̎Q��
    }
    public List<BossPhase> bossPhases;
    private int currentBossPhaseIndex = 0;

    public bool canCooperate = false; // �A�g�U���\��
    public float cooperativeAttackRange = 8f; // �A�g�U�����s���͈�
    public bool canUseEnvironment = false; // �����p�\��
    public string explosiveBarrelTag = "ExplosiveBarrel";
    public float environmentalInteractionRange = 10f;

    public bool canPredictDodge = false; // �\������\��
    public float predictionDodgeChance = 0.3f; // �\���������m��
    public float playerAttackPredictionTime = 0.5f; // �v���C���[���U�����͂��Ă���q�b�g����܂ł̗\������

    private NavMeshAgent navMeshAgent;
    private Transform player;
    private Animator animator;
    private EnemyHealth enemyHealth; // �G��Health�R���|�[�l���g

    // �A�j���[�^�[�n�b�V��
    private readonly int anim_MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private readonly int anim_AttackTriggerHash = Animator.StringToHash("Attack");
    private readonly int anim_StunnedBoolHash = Animator.StringToHash("IsStunned"); // �X�^����ԗp

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        player = GameObject.FindWithTag("Player")?.transform; // �v���C���[���^�O�Ō���
        if (player == null) Debug.LogWarning("Player (Tag: Player) not found in scene.", this);
    }

    void Start()
    {
        if (patrolPoints.Length > 0)
        {
            SetPatrolDestination();
        }
        else
        {
            currentState = EnemyState.Chase; // �p�g���[���|�C���g���Ȃ���΍ŏ�����ǐ�
        }
    }

    void Update()
    {
        if (enemyHealth.currentHealth <= 0)
        {
            currentState = EnemyState.Dead;
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrolState();
                break;
            case EnemyState.Chase:
                HandleChaseState();
                break;
            case EnemyState.Attack:
                HandleAttackState();
                break;
            case EnemyState.Stunned:
                HandleStunnedState();
                break;
            case EnemyState.Dead:
                HandleDeadState();
                break;
            case EnemyState.Support:
                // HandleSupportState(); // �A�g�s����AI���W�b�N
                break;
        }

        // �A�j���[�^�[�̈ړ����x�ݒ�
        animator.SetFloat(anim_MoveSpeedHash, navMeshAgent.velocity.magnitude / navMeshAgent.speed);
    }

    void HandlePatrolState()
    {
        navMeshAgent.speed = patrolSpeed;
        if (player != null && Vector3.Distance(transform.position, player.position) <= sightRange)
        {
            // �v���C���[�𔭌�
            currentState = EnemyState.Chase;
            Debug.Log("�v���C���[�����I�ǐՊJ�n");
            return;
        }

        if (navMeshAgent.remainingDistance < patrolPointTolerance && !navMeshAgent.pathPending)
        {
            // ���̃p�g���[���|�C���g��
            currentPatrolPointIndex = (currentPatrolPointIndex + 1) % patrolPoints.Length;
            SetPatrolDestination();
        }
    }

    void SetPatrolDestination()
    {
        if (patrolPoints.Length > 0)
        {
            navMeshAgent.SetDestination(patrolPoints[currentPatrolPointIndex].position);
        }
    }

    void HandleChaseState()
    {
        navMeshAgent.speed = chaseSpeed;
        if (player == null)
        {
            currentState = EnemyState.Patrol; // �v���C���[��������Ȃ��ꍇ�̓p�g���[���ɖ߂�
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
            navMeshAgent.isStopped = true;
            Debug.Log("�U���͈͓��I�U���J�n");
            return;
        }

        // �v���C���[�̕���������
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // �v���C���[��ǐ�
        navMeshAgent.SetDestination(player.position);

        // ���E�͈͊O�ɏo����ǐՂ���߂�
        if (distanceToPlayer > sightRange * 1.2f) // �����L�߂ɐݒ肵�A������������߂�
        {
            Debug.Log("�v���C���[�����������B");
            CheckForFootprints(); // ���Ղ��`�F�b�N
            // �ǐՂ���߂ăp�g���[���ɖ߂�i�܂��͒T���X�e�[�g�ցj
            // currentState = EnemyState.Patrol;
            // SetPatrolDestination();
        }

        // �����p�`�F�b�N
        if (canUseEnvironment)
        {
            CheckForEnvironmentalThreats();
        }
    }

    void HandleAttackState()
    {
        navMeshAgent.isStopped = true; // �U�����͒�~
        if (player == null || enemyHealth.currentHealth <= 0)
        {
            currentState = EnemyState.Patrol; // �v���C���[�����Ȃ��Ȃ�����A���������񂾂�p�g���[���ɖ߂�
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            currentState = EnemyState.Chase; // �U���͈͊O�ɏo����ǐՂ�
            navMeshAgent.isStopped = false;
            Debug.Log("�U���͈͊O�ցB�ǐՍĊJ");
            return;
        }

        // �v���C���[�̕���������
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // �U�����s
            animator.SetTrigger(anim_AttackTriggerHash);
            StartCoroutine(PerformAttack(attackAnimDuration));
            lastAttackTime = Time.time;
        }

        // �\������̃��W�b�N�������ŌĂяo��
        if (canPredictDodge)
        {
            CheckForPlayerAttackPrediction();
        }

        // �A�g�U���̃��W�b�N
        if (canCooperate)
        {
            CheckForCooperativeAttack();
        }
    }

    IEnumerator PerformAttack(float duration)
    {
        // �U���A�j���[�V�����̓r���Ń_���[�W����
        yield return new WaitForSeconds(duration * 0.5f);

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange + 0.5f) // �U�����͂����ŏI�`�F�b�N
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    void HandleStunnedState()
    {
        navMeshAgent.isStopped = true;
        animator.SetBool(anim_StunnedBoolHash, true);
        // �X�^�����͉����s�����Ȃ�
    }

    public void SetStunned(bool stunned)
    {
        bool wasStunned = currentState == EnemyState.Stunned;
        if (stunned)
        {
            currentState = EnemyState.Stunned;
            navMeshAgent.isStopped = true;
        }
        else if (wasStunned) // �X�^�����������ꂽ�ꍇ�̂ݏ�Ԃ�߂�
        {
            currentState = EnemyState.Chase; // �X�^��������͒ǐՏ�Ԃɖ߂�
            navMeshAgent.isStopped = false;
        }
        animator.SetBool(anim_StunnedBoolHash, stunned);
    }

    void HandleDeadState()
    {
        navMeshAgent.isStopped = true;
        // ���S�A�j���[�V�����Ȃǂ��Đ������̂ŁAAI�͒�~������
    }

    // --- ����x��AI�̋�̓I�Ȏ��� ---

    // �t�F�[�Y�ڍs�`�F�b�N (EnemyHealth����Ă΂�邱�Ƃ�z��)
    public void CheckBossPhase()
    {
        if (!isBoss || bossPhases.Count == 0) return;

        // ���̃t�F�[�Y�̗̑�臒l�ɒB���Ă��邩
        if (currentBossPhaseIndex < bossPhases.Count &&
            (float)enemyHealth.currentHealth / enemyHealth.maxHealth <= bossPhases[currentBossPhaseIndex].healthThreshold)
        {
            EnterBossPhase(currentBossPhaseIndex);
            currentBossPhaseIndex++;
        }
    }

    void EnterBossPhase(int phaseIndex)
    {
        Debug.Log($"�{�X���t�F�[�Y {phaseIndex + 1} �Ɉڍs�I");
        // �ړ����x�ύX
        navMeshAgent.speed *= bossPhases[phaseIndex].phaseSpecificMoveSpeedMultiplier;

        // �U���p�^�[����؂�ւ� (�����ł̓A�j���[�V�����̃g���K�[����z��)
        foreach (string attackTrigger in bossPhases[phaseIndex].phaseSpecificAttackTriggers)
        {
            Debug.Log($"�V�����U���p�^�[��: {attackTrigger}");
            // �A�j���[�^�[�̓���̍U���g���K�[��L���ɂ���Ȃǂ̃��W�b�N
        }

        // UIManager�ȂǂɃt�F�[�Y�ڍs��ʒm
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"�{�X���t�F�[�Y {phaseIndex + 1} �Ɉڍs�I", 3f);
    }

    void CheckForCooperativeAttack()
    {
        // ���͂̓G��T���A�A�g�U��������
        Collider[] nearbyEntities = Physics.OverlapSphere(transform.position, cooperativeAttackRange, playerLayer); // �v���C���[���^�[�Q�b�g�Ƃ��邪�A�G��AI���l��
        // ���ۂɂ�EnemyAI�����I�u�W�F�N�g��T��
        foreach (var entityCol in nearbyEntities)
        {
            if (entityCol.transform != this.transform && entityCol.CompareTag("Enemy"))
            {
                // ��: �v���C���[�����݌����ɂ���A�܂��̓o�t��������
                // �ȈՎ����Ƃ��āA�����_���Ȋm���ŘA�g�U���Ɉڍs
                if (Random.value < 0.1f) // 10%�̊m���ŘA�g
                {
                    // StartCoroutine(PerformCooperativeAction(entityCol.GetComponent<EnemyAI>()));
                    Debug.Log($"{this.name} �� {entityCol.name} ���A�g�s���������I");
                }
                break; // �ŏ��̓G��������Ώ\��
            }
        }
    }

    void CheckForEnvironmentalThreats()
    {
        Collider[] hitObjects = Physics.OverlapSphere(transform.position, environmentalInteractionRange, obstacleLayer);
        foreach (var objCol in hitObjects)
        {
            if (objCol.CompareTag(explosiveBarrelTag))
            {
                // �������Ƀv���C���[���߂��ꍇ�A������U�����郍�W�b�N
                if (player != null && Vector3.Distance(player.position, objCol.transform.position) < 5f)
                {
                    Debug.Log($"{this.name} ����������_���I");
                    navMeshAgent.SetDestination(objCol.transform.position);
                    // ���������U�������p�̃A�j���[�V������s�����J�n
                    // StartCoroutine(AttackEnvironmentalObject(objCol.gameObject));
                    break;
                }
            }
        }
    }

    void CheckForPlayerAttackPrediction()
    {
        // ���̎���: �v���C���[���U���A�j���[�V�������J�n�������Ƃ�PlayerController����󂯎�邱�Ƃ�z��
        // PlayerController playerController = player.GetComponent<PlayerController>(); // ���Ɏ擾
        // if (playerController != null && playerController.IsAttacking() && Random.value < predictionDodgeChance)
        // {
        //     // �v���C���[�̍U����������t�����ɉ��
        //     Vector3 dodgeDirection = (transform.position - player.position).normalized;
        //     // navMeshAgent.Move(dodgeDirection * dodgeSpeed * Time.deltaTime);
        //     Debug.Log($"{this.name} ���\����������I");
        //     // ����A�j���[�V�������g���K�[
        // }
    }

    void CheckForFootprints()
    {
        Collider[] footprints = Physics.OverlapSphere(transform.position, footprintDetectionRange, footprintLayer);
        if (footprints.Length > 0)
        {
            // �ł��߂����ՂɌ�����
            Transform nearestFootprint = null;
            float minDistance = float.MaxValue;
            foreach (var fp in footprints)
            {
                float dist = Vector3.Distance(transform.position, fp.transform.position);
                if (dist < minDistance) { minDistance = dist; nearestFootprint = fp.transform; }
            }
            if (nearestFootprint != null)
            {
                navMeshAgent.SetDestination(nearestFootprint.position);
                currentState = EnemyState.Chase; // ���Ղ���������ǐՏ�ԂɈڍs
                Debug.Log($"���Ղ𔭌��I�ǐՒ�...");
            }
        }
        else // ���Ղ�����������p�g���[���ɖ߂�
        {
            currentState = EnemyState.Patrol;
            SetPatrolDestination();
            Debug.Log("���Ղ��������A�p�g���[���ɖ߂�B");
        }
    }

    // --- Sensing Utilities ---
    void OnDrawGizmosSelected()
    {
        // ���E�͈͂�Editor�ŕ`��
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // �U���͈͂�Editor�ŕ`��
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // �A�g�U���͈͂�Editor�ŕ`��
        if (canCooperate)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, cooperativeAttackRange);
        }

        // �����p�͈͂�Editor�ŕ`��
        if (canUseEnvironment)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, environmentalInteractionRange);
        }

        // ���̌��m�͈͂�Editor�ŕ`��
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, soundDetectionRadius);

        // ���Ղ̌��m�͈͂�Editor�ŕ`��
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, footprintDetectionRange);
    }
}