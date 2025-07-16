using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI; // NavMeshAgentを使用しない場合、不要

public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 5f;
    public float sprintSpeedMultiplier = 1.5f;
    public float rotationSpeed = 720f;
    public CharacterController characterController; // 必要に応じてNavMeshAgentに変更

    [Header("スタミナ設定")]
    public float maxStamina = 100f;
    public float staminaRecoveryRate = 10f; // 1秒あたりの回復量
    public float staminaRecoveryDelay = 1.5f; // スタミナ消費後の回復開始までの遅延
    public float dodgeStaminaCost = 15f; // 回避時のスタミナ消費
    public float heavyAttackStaminaCost = 20f; // 強攻撃スタミナ消費
    public float staminaDepletedDuration = 2f; // スタミナ切れデバフの継続時間
    public float staminaDepletedMoveSpeedPenalty = 0.5f; // スタミナ切れ時の移動速度倍率
    private float currentStamina;
    private float lastStaminaConsumeTime;
    private bool isStaminaDepleted = false;

    [Header("攻撃設定")]
    public float attackRange = 2f;
    public LayerMask enemyLayer; // 敵のレイヤー
    public Transform attackPoint; // 攻撃判定の原点
    public float attackCooldown = 0.5f;
    private float lastAttackTime;

    [Header("ターゲティング設定")]
    public float targetingRange = 15f;
    public GameObject targetIndicatorPrefab; // ターゲット表示用プレハブ
    private Transform currentTarget;
    private GameObject currentTargetIndicator;
    private List<Transform> availableTargets = new List<Transform>();
    private int currentTargetIndex = -1;

    [Header("スキル設定")]
    // public List<SkillNode> learnedSkills; // SkillTreeDataから取得
    // public int currentSkillPoints; // スキルポイント管理用

    private Animator animator;
    private Camera mainCamera;
    private PlayerStats playerStats; // PlayerStatsへの参照

    // アニメーターハッシュ（パフォーマンス最適化）
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
        playerStats = GetComponent<PlayerStats>(); // PlayerStatsコンポーネントを取得
        if (playerStats == null) Debug.LogError("PlayerStatsコンポーネントが見つかりません！", this);
    }

    void Update()
    {
        HandleMovement();
        HandleStamina();
        HandleAttack();
        HandleTargeting();

        // 例: Debug UI用 (UIManagerがある場合は不要)
        // Debug.Log($"HP: {GetComponent<PlayerHealth>().currentHealth}/{GetComponent<PlayerHealth>().maxHealth} | スタミナ: {currentStamina}/{maxStamina}");
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = mainCamera.transform.right * horizontal + mainCamera.transform.forward * vertical;
        moveDirection.y = 0; // Y軸方向の移動を無視

        float currentMoveSpeed = moveSpeed;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0;

        if (isSprinting && !isStaminaDepleted)
        {
            currentMoveSpeed *= sprintSpeedMultiplier;
            currentStamina -= Time.deltaTime * (staminaRecoveryRate * 0.5f); // スプリント時のスタミナ消費
            lastStaminaConsumeTime = Time.time;
        }

        if (isStaminaDepleted)
        {
            currentMoveSpeed *= staminaDepletedMoveSpeedPenalty;
        }

        characterController.Move(moveDirection.normalized * currentMoveSpeed * Time.deltaTime);

        // プレイヤーの向きを移動方向に合わせる
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }

        animator.SetFloat(anim_MoveSpeedHash, moveDirection.magnitude);

        // 回避アクション
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
        float dodgeSpeed = 10f; // 回避中の移動速度
        float dodgeDuration = 0.3f; // 回避の継続時間
        float startTime = Time.time;

        // 一時的に無敵状態にするなど
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
            // スタミナが回復し、かつデバフ時間が経過したら解除
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
        Debug.Log("スタミナ切れ！移動速度が低下しました。");
        if (UIManager.Instance != null) UIManager.Instance.ShowDebuffIcon("StaminaDepleted");

        // スタミナが一定量回復するまでデバフを継続
        while (currentStamina <= maxStamina * 0.2f || Time.time - lastStaminaConsumeTime < staminaDepletedDuration) // 例: 20%回復するまで継続
        {
            yield return null;
        }

        moveSpeed = originalMoveSpeed;
        sprintSpeedMultiplier = originalSprintSpeedMultiplier;
        isStaminaDepleted = false;
        Debug.Log("スタミナ切れデバフ解除。");
        if (UIManager.Instance != null) UIManager.Instance.HideDebuffIcon("StaminaDepleted");
    }

    void HandleAttack()
    {
        if (playerStats == null) return; // PlayerStatsがない場合は処理しない
        if (Time.time - lastAttackTime < attackCooldown) return;

        // 通常攻撃
        if (Input.GetMouseButtonDown(0)) // 左クリック
        {
            animator.SetTrigger(anim_AttackTriggerHash);
            StartCoroutine(PerformAttack(playerStats.baseAttackDamage, playerStats.equippedWeaponElement));
            lastAttackTime = Time.time;
        }
        // 強攻撃 (右クリック長押しでチャージなど)
        else if (Input.GetMouseButtonDown(1) && currentStamina >= heavyAttackStaminaCost && !isStaminaDepleted) // 右クリック
        {
            animator.SetTrigger(anim_HeavyAttackTriggerHash);
            currentStamina -= heavyAttackStaminaCost;
            lastStaminaConsumeTime = Time.time;
            if (UIManager.Instance != null) UIManager.Instance.UpdateStaminaBar(currentStamina, maxStamina);
            StartCoroutine(PerformAttack(playerStats.baseAttackDamage * 2, playerStats.equippedWeaponElement)); // 例: ダメージ2倍
            lastAttackTime = Time.time;
        }
        else if (Input.GetMouseButtonDown(1) && currentStamina < heavyAttackStaminaCost && !isStaminaDepleted)
        {
            Debug.Log("スタミナが足りず強攻撃できません！");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("スタミナ不足！", 2f);
        }
    }

    IEnumerator PerformAttack(int baseDamage, ElementType element)
    {
        yield return new WaitForSeconds(0.2f); // アニメーションのヒットタイミングに合わせる

        if (attackPoint == null)
        {
            Debug.LogError("Attack Pointが設定されていません！", this);
            yield break;
        }

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
        foreach (Collider enemyCollider in hitEnemies)
        {
            EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(baseDamage, enemyCollider, element); // 属性とヒットコライダーも渡す
            }
        }
    }

    void HandleTargeting()
    {
        if (Input.GetMouseButtonDown(2)) // マウス中央ボタンでターゲティング切り替え
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

        // ターゲットが設定されている場合、常にターゲットの方を向く
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

        // ターゲットが有効でなくなったらクリア
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
            // EnemyHealthコンポーネントを持つもののみをターゲットとする
            if (hitCollider.GetComponent<EnemyHealth>() != null)
            {
                availableTargets.Add(hitCollider.transform);
            }
        }
        // プレイヤーからの距離が近い順にソート
        availableTargets.Sort((a, b) => Vector3.Distance(transform.position, a.position).CompareTo(Vector3.Distance(transform.position, b.position)));

        if (availableTargets.Count > 0)
        {
            currentTargetIndex = 0;
            SetTarget(availableTargets[currentTargetIndex]);
        }
        else
        {
            Debug.Log("近くにターゲットできる敵がいません。");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ターゲットなし", 1f);
        }
    }

    void CycleTarget()
    {
        if (availableTargets.Count <= 1) return;

        // 現在のターゲットがリストから消えていた場合のリフレッシュ
        availableTargets.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy || t.GetComponent<EnemyHealth>() == null);
        if (availableTargets.Count == 0)
        {
            ClearTarget();
            FindAndSetInitialTarget(); // 再度近くの敵を探す
            return;
        }

        currentTargetIndex = (currentTargetIndex + 1) % availableTargets.Count;
        SetTarget(availableTargets[currentTargetIndex]);
    }

    void SetTarget(Transform newTarget)
    {
        if (currentTargetIndicator != null)
        {
            Destroy(currentTargetIndicator); // 既存のインジケーターを破棄
        }
        currentTarget = newTarget;
        if (currentTarget != null && targetIndicatorPrefab != null)
        {
            currentTargetIndicator = Instantiate(targetIndicatorPrefab, currentTarget); // ターゲットの子として生成
            currentTargetIndicator.transform.localPosition = Vector3.zero; // 必要に応じてオフセット調整
            Debug.Log($"ターゲット: {currentTarget.name}");

            // 弱点強調表示（EnemyHealthにWeakPointがある場合）
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
            // 弱点強調解除
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
        Debug.Log("ターゲット解除。");
    }

    // プレイヤーのスタミナ残量を取得する外部公開メソッド (UIなどに利用)
    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }

    // セーブ/ロード用スタミナ設定
    public void SetCurrentStamina(float stamina)
    {
        currentStamina = stamina;
        if (UIManager.Instance != null) UIManager.Instance.UpdateStaminaBar(currentStamina, maxStamina);
    }
}