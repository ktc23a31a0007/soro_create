using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI; // NavMeshAgent���g�p���Ȃ��ꍇ�A�s�v

public class PlayerController : MonoBehaviour
{
    [Header("�ړ��ݒ�")]
    public float moveSpeed = 5f;
    public float sprintSpeedMultiplier = 1.5f;
    public float rotationSpeed = 720f;
    public CharacterController characterController; // �K�v�ɉ�����NavMeshAgent�ɕύX

    [Header("�X�^�~�i�ݒ�")]
    public float maxStamina = 100f;
    public float staminaRecoveryRate = 10f; // 1�b������̉񕜗�
    public float staminaRecoveryDelay = 1.5f; // �X�^�~�i�����̉񕜊J�n�܂ł̒x��
    public float dodgeStaminaCost = 15f; // ������̃X�^�~�i����
    public float heavyAttackStaminaCost = 20f; // ���U���X�^�~�i����
    public float staminaDepletedDuration = 2f; // �X�^�~�i�؂�f�o�t�̌p������
    public float staminaDepletedMoveSpeedPenalty = 0.5f; // �X�^�~�i�؂ꎞ�̈ړ����x�{��
    private float currentStamina;
    private float lastStaminaConsumeTime;
    private bool isStaminaDepleted = false;

    [Header("�U���ݒ�")]
    public float attackRange = 2f;
    public LayerMask enemyLayer; // �G�̃��C���[
    public Transform attackPoint; // �U������̌��_
    public float attackCooldown = 0.5f;
    private float lastAttackTime;

    [Header("�^�[�Q�e�B���O�ݒ�")]
    public float targetingRange = 15f;
    public GameObject targetIndicatorPrefab; // �^�[�Q�b�g�\���p�v���n�u
    private Transform currentTarget;
    private GameObject currentTargetIndicator;
    private List<Transform> availableTargets = new List<Transform>();
    private int currentTargetIndex = -1;

    [Header("�X�L���ݒ�")]
    // public List<SkillNode> learnedSkills; // SkillTreeData����擾
    // public int currentSkillPoints; // �X�L���|�C���g�Ǘ��p

    private Animator animator;
    private Camera mainCamera;
    private PlayerStats playerStats; // PlayerStats�ւ̎Q��

    // �A�j���[�^�[�n�b�V���i�p�t�H�[�}���X�œK���j
    private readonly int anim_MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private readonly int anim_AttackTriggerHash = Animator.StringToHash("Attack");
    private readonly int anim_DodgeTriggerHash = Animator.StringToHash("Dodge");
    private readonly int anim_HeavyAttackTriggerHash = Animator.StringToHash("HeavyAttack");

    void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        currentStamina = maxStamina;
        playerStats = GetComponent<PlayerStats>(); // PlayerStats�R���|�[�l���g���擾
        if (playerStats == null) Debug.LogError("PlayerStats�R���|�[�l���g��������܂���I", this);
    }

    void Update()
    {
        HandleMovement();
        HandleStamina();
        HandleAttack();
        HandleTargeting();

        // ��: Debug UI�p (UIManager������ꍇ�͕s�v)
        // Debug.Log($"HP: {GetComponent<PlayerHealth>().currentHealth}/{GetComponent<PlayerHealth>().maxHealth} | �X�^�~�i: {currentStamina}/{maxStamina}");
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = mainCamera.transform.right * horizontal + mainCamera.transform.forward * vertical;
        moveDirection.y = 0; // Y�������̈ړ��𖳎�

        float currentMoveSpeed = moveSpeed;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0;

        if (isSprinting && !isStaminaDepleted)
        {
            currentMoveSpeed *= sprintSpeedMultiplier;
            currentStamina -= Time.deltaTime * (staminaRecoveryRate * 0.5f); // �X�v�����g���̃X�^�~�i����
            lastStaminaConsumeTime = Time.time;
        }

        if (isStaminaDepleted)
        {
            currentMoveSpeed *= staminaDepletedMoveSpeedPenalty;
        }

        characterController.Move(moveDirection.normalized * currentMoveSpeed * Time.deltaTime);

        // �v���C���[�̌������ړ������ɍ��킹��
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }

        animator.SetFloat(anim_MoveSpeedHash, moveDirection.magnitude);

        // ����A�N�V����
        if (Input.GetKeyDown(KeyCode.Space) && currentStamina >= dodgeStaminaCost && !isStaminaDepleted)
        {
            StartCoroutine(PerformDodge(moveDirection));
        }
    }

    IEnumerator PerformDodge(Vector3 dodgeDirection)
    {
        currentStamina -= dodgeStaminaCost;
        lastStaminaConsumeTime = Time.time;
        if (UIManager.Instance != null) UIManager.Instance.UpdateStaminaBar(currentStamina, maxStamina);

        animator.SetTrigger(anim_DodgeTriggerHash);
        float dodgeSpeed = 10f; // ��𒆂̈ړ����x
        float dodgeDuration = 0.3f; // ����̌p������
        float startTime = Time.time;

        // �ꎞ�I�ɖ��G��Ԃɂ���Ȃ�
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.SetInvincible(true);

        while (Time.time < startTime + dodgeDuration)
        {
            characterController.Move(dodgeDirection.normalized * dodgeSpeed * Time.deltaTime);
            yield return null;
        }

        if (playerHealth != null) playerHealth.SetInvincible(false);
    }

    void HandleStamina()
    {
        if (Time.time - lastStaminaConsumeTime >= staminaRecoveryDelay && currentStamina < maxStamina)
        {
            currentStamina += staminaRecoveryRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        if (currentStamina <= 0 && !isStaminaDepleted)
        {
            StartCoroutine(ApplyStaminaDepletedDebuff());
        }
        else if (isStaminaDepleted && currentStamina > 0 && (Time.time - lastStaminaConsumeTime) > staminaDepletedDuration)
        {
            // �X�^�~�i���񕜂��A���f�o�t���Ԃ��o�߂��������
            isStaminaDepleted = false;
        }

        if (UIManager.Instance != null) UIManager.Instance.UpdateStaminaBar(currentStamina, maxStamina);
    }

    IEnumerator ApplyStaminaDepletedDebuff()
    {
        isStaminaDepleted = true;
        float originalMoveSpeed = moveSpeed;
        float originalSprintSpeedMultiplier = sprintSpeedMultiplier;

        moveSpeed *= staminaDepletedMoveSpeedPenalty;
        sprintSpeedMultiplier *= staminaDepletedMoveSpeedPenalty;
        Debug.Log("�X�^�~�i�؂�I�ړ����x���ቺ���܂����B");
        if (UIManager.Instance != null) UIManager.Instance.ShowDebuffIcon("StaminaDepleted");

        // �X�^�~�i�����ʉ񕜂���܂Ńf�o�t���p��
        while (currentStamina <= maxStamina * 0.2f || Time.time - lastStaminaConsumeTime < staminaDepletedDuration) // ��: 20%�񕜂���܂Ōp��
        {
            yield return null;
        }

        moveSpeed = originalMoveSpeed;
        sprintSpeedMultiplier = originalSprintSpeedMultiplier;
        isStaminaDepleted = false;
        Debug.Log("�X�^�~�i�؂�f�o�t�����B");
        if (UIManager.Instance != null) UIManager.Instance.HideDebuffIcon("StaminaDepleted");
    }

    void HandleAttack()
    {
        if (playerStats == null) return; // PlayerStats���Ȃ��ꍇ�͏������Ȃ�
        if (Time.time - lastAttackTime < attackCooldown) return;

        // �ʏ�U��
        if (Input.GetMouseButtonDown(0)) // ���N���b�N
        {
            animator.SetTrigger(anim_AttackTriggerHash);
            StartCoroutine(PerformAttack(playerStats.baseAttackDamage, playerStats.equippedWeaponElement));
            lastAttackTime = Time.time;
        }
        // ���U�� (�E�N���b�N�������Ń`���[�W�Ȃ�)
        else if (Input.GetMouseButtonDown(1) && currentStamina >= heavyAttackStaminaCost && !isStaminaDepleted) // �E�N���b�N
        {
            animator.SetTrigger(anim_HeavyAttackTriggerHash);
            currentStamina -= heavyAttackStaminaCost;
            lastStaminaConsumeTime = Time.time;
            if (UIManager.Instance != null) UIManager.Instance.UpdateStaminaBar(currentStamina, maxStamina);
            StartCoroutine(PerformAttack(playerStats.baseAttackDamage * 2, playerStats.equippedWeaponElement)); // ��: �_���[�W2�{
            lastAttackTime = Time.time;
        }
        else if (Input.GetMouseButtonDown(1) && currentStamina < heavyAttackStaminaCost && !isStaminaDepleted)
        {
            Debug.Log("�X�^�~�i�����肸���U���ł��܂���I");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�X�^�~�i�s���I", 2f);
        }
    }

    IEnumerator PerformAttack(int baseDamage, ElementType element)
    {
        yield return new WaitForSeconds(0.2f); // �A�j���[�V�����̃q�b�g�^�C�~���O�ɍ��킹��

        if (attackPoint == null)
        {
            Debug.LogError("Attack Point���ݒ肳��Ă��܂���I", this);
            yield break;
        }

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
        foreach (Collider enemyCollider in hitEnemies)
        {
            EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(baseDamage, enemyCollider, element); // �����ƃq�b�g�R���C�_�[���n��
            }
        }
    }

    void HandleTargeting()
    {
        if (Input.GetMouseButtonDown(2)) // �}�E�X�����{�^���Ń^�[�Q�e�B���O�؂�ւ�
        {
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                FindAndSetInitialTarget();
            }
            else
            {
                CycleTarget();
            }
        }

        // �^�[�Q�b�g���ݒ肳��Ă���ꍇ�A��Ƀ^�[�Q�b�g�̕�������
        if (currentTarget != null)
        {
            Vector3 lookDirection = currentTarget.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // �^�[�Q�b�g���L���łȂ��Ȃ�����N���A
        if (currentTarget != null && !currentTarget.gameObject.activeInHierarchy)
        {
            ClearTarget();
        }
    }

    void FindAndSetInitialTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, targetingRange, enemyLayer);
        availableTargets.Clear();
        foreach (var hitCollider in hitColliders)
        {
            // EnemyHealth�R���|�[�l���g�������݂̂̂��^�[�Q�b�g�Ƃ���
            if (hitCollider.GetComponent<EnemyHealth>() != null)
            {
                availableTargets.Add(hitCollider.transform);
            }
        }
        // �v���C���[����̋������߂����Ƀ\�[�g
        availableTargets.Sort((a, b) => Vector3.Distance(transform.position, a.position).CompareTo(Vector3.Distance(transform.position, b.position)));

        if (availableTargets.Count > 0)
        {
            currentTargetIndex = 0;
            SetTarget(availableTargets[currentTargetIndex]);
        }
        else
        {
            Debug.Log("�߂��Ƀ^�[�Q�b�g�ł���G�����܂���B");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�^�[�Q�b�g�Ȃ�", 1f);
        }
    }

    void CycleTarget()
    {
        if (availableTargets.Count <= 1) return;

        // ���݂̃^�[�Q�b�g�����X�g��������Ă����ꍇ�̃��t���b�V��
        availableTargets.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy || t.GetComponent<EnemyHealth>() == null);
        if (availableTargets.Count == 0)
        {
            ClearTarget();
            FindAndSetInitialTarget(); // �ēx�߂��̓G��T��
            return;
        }

        currentTargetIndex = (currentTargetIndex + 1) % availableTargets.Count;
        SetTarget(availableTargets[currentTargetIndex]);
    }

    void SetTarget(Transform newTarget)
    {
        if (currentTargetIndicator != null)
        {
            Destroy(currentTargetIndicator); // �����̃C���W�P�[�^�[��j��
        }
        currentTarget = newTarget;
        if (currentTarget != null && targetIndicatorPrefab != null)
        {
            currentTargetIndicator = Instantiate(targetIndicatorPrefab, currentTarget); // �^�[�Q�b�g�̎q�Ƃ��Đ���
            currentTargetIndicator.transform.localPosition = Vector3.zero; // �K�v�ɉ����ăI�t�Z�b�g����
            Debug.Log($"�^�[�Q�b�g: {currentTarget.name}");

            // ��_�����\���iEnemyHealth��WeakPoint������ꍇ�j
            EnemyHealth enemyHealth = currentTarget.GetComponent<EnemyHealth>();
            if (enemyHealth != null && enemyHealth.weakPoints.Count > 0)
            {
                enemyHealth.HighlightWeakPoints(true);
            }
        }
    }

    void ClearTarget()
    {
        if (currentTargetIndicator != null)
        {
            Destroy(currentTargetIndicator);
        }
        if (currentTarget != null)
        {
            // ��_��������
            EnemyHealth enemyHealth = currentTarget.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.HighlightWeakPoints(false);
            }
        }
        currentTarget = null;
        currentTargetIndicator = null;
        currentTargetIndex = -1;
        availableTargets.Clear();
        Debug.Log("�^�[�Q�b�g�����B");
    }

    // �v���C���[�̃X�^�~�i�c�ʂ��擾����O�����J���\�b�h (UI�Ȃǂɗ��p)
    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }

    // �Z�[�u/���[�h�p�X�^�~�i�ݒ�
    public void SetCurrentStamina(float stamina)
    {
        currentStamina = stamina;
        if (UIManager.Instance != null) UIManager.Instance.UpdateStaminaBar(currentStamina, maxStamina);
    }
}