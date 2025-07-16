using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI; // NavMeshAgentを使用

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack, Stunned, Dead, Support }

    [Header("基本AI設定")]
    public EnemyState currentState = EnemyState.Patrol;
    public float sightRange = 10f; // プレイヤーを発見する距離
    public float attackRange = 2f; // プレイヤーを攻撃する距離
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float rotationSpeed = 720f;
    public LayerMask playerLayer; // プレイヤーのレイヤー
    public LayerMask obstacleLayer; // 障害物のレイヤー

    [Header("パトロール設定")]
    public Transform[] patrolPoints;
    private int currentPatrolPointIndex;
    public float patrolPointTolerance = 0.5f;

    [Header("攻撃設定")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime;
    public int attackDamage = 10;
    public float attackAnimDuration = 1f; // 攻撃アニメーションの長さ（仮）

    [Header("感知能力")]
    public float soundDetectionRadius = 10f;
    public LayerMask footprintLayer; // 足跡のレイヤー
    public float footprintDetectionRange = 5f;

    [Header("高難度化AI")]
    public bool isBoss = false;
    [System.Serializable]
    public class BossPhase
    {
        public float healthThreshold = 0.5f; // 体力50%で移行
        public List<string> phaseSpecificAttackTriggers; // このフェーズで使う攻撃アニメーションのトリガー名など
        public float phaseSpecificMoveSpeedMultiplier = 1.2f; // フェーズ中の移動速度倍率
        // その他、フェーズ固有のスキルや行動ロジックへの参照
    }
    public List<BossPhase> bossPhases;
    private int currentBossPhaseIndex = 0;

    public bool canCooperate = false; // 連携攻撃可能か
    public float cooperativeAttackRange = 8f; // 連携攻撃を行う範囲
    public bool canUseEnvironment = false; // 環境利用可能か
    public string explosiveBarrelTag = "ExplosiveBarrel";
    public float environmentalInteractionRange = 10f;

    public bool canPredictDodge = false; // 予測回避可能か
    public float predictionDodgeChance = 0.3f; // 予測回避する確率
    public float playerAttackPredictionTime = 0.5f; // プレイヤーが攻撃入力してからヒットするまでの予測時間

    private NavMeshAgent navMeshAgent;
    private Transform player;
    private Animator animator;
    private EnemyHealth enemyHealth; // 敵のHealthコンポーネント

    // アニメーターハッシュ
    private readonly int anim_MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private readonly int anim_AttackTriggerHash = Animator.StringToHash("Attack");
    private readonly int anim_StunnedBoolHash = Animator.StringToHash("IsStunned"); // スタン状態用

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        player = GameObject.FindWithTag("Player")?.transform; // プレイヤーをタグで検索
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
            currentState = EnemyState.Chase; // パトロールポイントがなければ最初から追跡
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
                // HandleSupportState(); // 連携行動のAIロジック
                break;
        }

        // アニメーターの移動速度設定
        animator.SetFloat(anim_MoveSpeedHash, navMeshAgent.velocity.magnitude / navMeshAgent.speed);
    }

    void HandlePatrolState()
    {
        navMeshAgent.speed = patrolSpeed;
        if (player != null && Vector3.Distance(transform.position, player.position) <= sightRange)
        {
            // プレイヤーを発見
            currentState = EnemyState.Chase;
            Debug.Log("プレイヤー発見！追跡開始");
            return;
        }

        if (navMeshAgent.remainingDistance < patrolPointTolerance && !navMeshAgent.pathPending)
        {
            // 次のパトロールポイントへ
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
            currentState = EnemyState.Patrol; // プレイヤーが見つからない場合はパトロールに戻る
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
            navMeshAgent.isStopped = true;
            Debug.Log("攻撃範囲内！攻撃開始");
            return;
        }

        // プレイヤーの方向を向く
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // プレイヤーを追跡
        navMeshAgent.SetDestination(player.position);

        // 視界範囲外に出たら追跡を諦める
        if (distanceToPlayer > sightRange * 1.2f) // 少し広めに設定し、見失ったら諦める
        {
            Debug.Log("プレイヤーを見失った。");
            CheckForFootprints(); // 足跡をチェック
            // 追跡を諦めてパトロールに戻る（または探索ステートへ）
            // currentState = EnemyState.Patrol;
            // SetPatrolDestination();
        }

        // 環境利用チェック
        if (canUseEnvironment)
        {
            CheckForEnvironmentalThreats();
        }
    }

    void HandleAttackState()
    {
        navMeshAgent.isStopped = true; // 攻撃中は停止
        if (player == null || enemyHealth.currentHealth <= 0)
        {
            currentState = EnemyState.Patrol; // プレイヤーがいなくなったり、自分が死んだらパトロールに戻る
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            currentState = EnemyState.Chase; // 攻撃範囲外に出たら追跡へ
            navMeshAgent.isStopped = false;
            Debug.Log("攻撃範囲外へ。追跡再開");
            return;
        }

        // プレイヤーの方向を向く
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // 攻撃実行
            animator.SetTrigger(anim_AttackTriggerHash);
            StartCoroutine(PerformAttack(attackAnimDuration));
            lastAttackTime = Time.time;
        }

        // 予測回避のロジックをここで呼び出す
        if (canPredictDodge)
        {
            CheckForPlayerAttackPrediction();
        }

        // 連携攻撃のロジック
        if (canCooperate)
        {
            CheckForCooperativeAttack();
        }
    }

    IEnumerator PerformAttack(float duration)
    {
        // 攻撃アニメーションの途中でダメージ判定
        yield return new WaitForSeconds(duration * 0.5f);

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange + 0.5f) // 攻撃が届くか最終チェック
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
        // スタン中は何も行動しない
    }

    public void SetStunned(bool stunned)
    {
        bool wasStunned = currentState == EnemyState.Stunned;
        if (stunned)
        {
            currentState = EnemyState.Stunned;
            navMeshAgent.isStopped = true;
        }
        else if (wasStunned) // スタンが解除された場合のみ状態を戻す
        {
            currentState = EnemyState.Chase; // スタン解除後は追跡状態に戻す
            navMeshAgent.isStopped = false;
        }
        animator.SetBool(anim_StunnedBoolHash, stunned);
    }

    void HandleDeadState()
    {
        navMeshAgent.isStopped = true;
        // 死亡アニメーションなどが再生されるので、AIは停止させる
    }

    // --- 高難度化AIの具体的な実装 ---

    // フェーズ移行チェック (EnemyHealthから呼ばれることを想定)
    public void CheckBossPhase()
    {
        if (!isBoss || bossPhases.Count == 0) return;

        // 次のフェーズの体力閾値に達しているか
        if (currentBossPhaseIndex < bossPhases.Count &&
            (float)enemyHealth.currentHealth / enemyHealth.maxHealth <= bossPhases[currentBossPhaseIndex].healthThreshold)
        {
            EnterBossPhase(currentBossPhaseIndex);
            currentBossPhaseIndex++;
        }
    }

    void EnterBossPhase(int phaseIndex)
    {
        Debug.Log($"ボスがフェーズ {phaseIndex + 1} に移行！");
        // 移動速度変更
        navMeshAgent.speed *= bossPhases[phaseIndex].phaseSpecificMoveSpeedMultiplier;

        // 攻撃パターンを切り替え (ここではアニメーションのトリガー名を想定)
        foreach (string attackTrigger in bossPhases[phaseIndex].phaseSpecificAttackTriggers)
        {
            Debug.Log($"新しい攻撃パターン: {attackTrigger}");
            // アニメーターの特定の攻撃トリガーを有効にするなどのロジック
        }

        // UIManagerなどにフェーズ移行を通知
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"ボスがフェーズ {phaseIndex + 1} に移行！", 3f);
    }

    void CheckForCooperativeAttack()
    {
        // 周囲の敵を探し、連携攻撃を検討
        Collider[] nearbyEntities = Physics.OverlapSphere(transform.position, cooperativeAttackRange, playerLayer); // プレイヤーをターゲットとするが、敵のAIも考慮
        // 実際にはEnemyAIを持つオブジェクトを探す
        foreach (var entityCol in nearbyEntities)
        {
            if (entityCol.transform != this.transform && entityCol.CompareTag("Enemy"))
            {
                // 例: プレイヤーを挟み撃ちにする、またはバフをかける
                // 簡易実装として、ランダムな確率で連携攻撃に移行
                if (Random.value < 0.1f) // 10%の確率で連携
                {
                    // StartCoroutine(PerformCooperativeAction(entityCol.GetComponent<EnemyAI>()));
                    Debug.Log($"{this.name} と {entityCol.name} が連携行動を検討！");
                }
                break; // 最初の敵が見つかれば十分
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
                // 爆発物にプレイヤーが近い場合、それを攻撃するロジック
                if (player != null && Vector3.Distance(player.position, objCol.transform.position) < 5f)
                {
                    Debug.Log($"{this.name} が爆発物を狙う！");
                    navMeshAgent.SetDestination(objCol.transform.position);
                    // 爆発物を攻撃する専用のアニメーションや行動を開始
                    // StartCoroutine(AttackEnvironmentalObject(objCol.gameObject));
                    break;
                }
            }
        }
    }

    void CheckForPlayerAttackPrediction()
    {
        // 仮の実装: プレイヤーが攻撃アニメーションを開始したことをPlayerControllerから受け取ることを想定
        // PlayerController playerController = player.GetComponent<PlayerController>(); // 仮に取得
        // if (playerController != null && playerController.IsAttacking() && Random.value < predictionDodgeChance)
        // {
        //     // プレイヤーの攻撃方向から逆方向に回避
        //     Vector3 dodgeDirection = (transform.position - player.position).normalized;
        //     // navMeshAgent.Move(dodgeDirection * dodgeSpeed * Time.deltaTime);
        //     Debug.Log($"{this.name} が予測回避した！");
        //     // 回避アニメーションをトリガー
        // }
    }

    void CheckForFootprints()
    {
        Collider[] footprints = Physics.OverlapSphere(transform.position, footprintDetectionRange, footprintLayer);
        if (footprints.Length > 0)
        {
            // 最も近い足跡に向かう
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
                currentState = EnemyState.Chase; // 足跡を見つけたら追跡状態に移行
                Debug.Log($"足跡を発見！追跡中...");
            }
        }
        else // 足跡も見失ったらパトロールに戻る
        {
            currentState = EnemyState.Patrol;
            SetPatrolDestination();
            Debug.Log("足跡を見失い、パトロールに戻る。");
        }
    }

    // --- Sensing Utilities ---
    void OnDrawGizmosSelected()
    {
        // 視界範囲をEditorで描画
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // 攻撃範囲をEditorで描画
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 連携攻撃範囲をEditorで描画
        if (canCooperate)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, cooperativeAttackRange);
        }

        // 環境利用範囲をEditorで描画
        if (canUseEnvironment)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, environmentalInteractionRange);
        }

        // 音の検知範囲をEditorで描画
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, soundDetectionRadius);

        // 足跡の検知範囲をEditorで描画
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, footprintDetectionRange);
    }
}