using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("体力設定")]
    public int maxHealth = 100;
    public int currentHealth;
    public float defenseMultiplier = 0.1f; // ダメージ軽減率 (0.1 = 10%軽減)

    [Header("ひるみ/アーマー設定")]
    public float stunThreshold = 15f; // これ以上のダメージでひるむ
    public bool hasSuperArmor = false; // スーパーアーマー状態か（常にひるまない）
    public float stunDuration = 1.0f; // ひるみ時間
    private bool isStunned = false;

    [Header("弱点部位設定")]
    [System.Serializable]
    public class WeakPoint
    {
        public Collider weakPointCollider;
        public float damageMultiplier = 1.5f; // 弱点ヒット時のダメージ倍率
        public GameObject weakPointEffectPrefab; // ヒット時のエフェクト (パーティクルなど)
        public bool canBeStunned = true; // 弱点ヒットで追加スタンするか
        [Tooltip("弱点強調表示用のマテリアルやシェーダー設定 (Runtimeで適用)")]
        public Material highlightMaterial;
    }
    public List<WeakPoint> weakPoints;
    private Dictionary<Collider, Material> originalWeakPointMaterials = new Dictionary<Collider, Material>();


    [Header("属性設定")]
    [System.Serializable]
    public class ElementalResistance
    {
        public ElementType element;
        public float resistanceMultiplier = 0.5f; // ダメージ軽減率
    }
    [System.Serializable]
    public class ElementalWeakness
    {
        public ElementType element;
        public float weaknessMultiplier = 1.5f; // ダメージ増加率
    }
    public List<ElementalResistance> resistances;
    public List<ElementalWeakness> weaknesses;

    [Header("エフェクト/サウンド")]
    public GameObject hitEffectPrefab;
    public AudioClip hitSound;
    public GameObject deathEffectPrefab;
    public AudioClip deathSound;

    private Animator animator;
    private EnemyAI enemyAI; // AIにスタン状態を通知するため
    // アニメーターハッシュ
    private readonly int anim_HitTriggerHash = Animator.StringToHash("Hit");
    private readonly int anim_DieTriggerHash = Animator.StringToHash("Die");

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        enemyAI = GetComponent<EnemyAI>();

        // 弱点の元マテリアルを保存 (強調表示用)
        foreach (var wp in weakPoints)
        {
            if (wp.weakPointCollider != null && wp.weakPointCollider.GetComponent<Renderer>() != null)
            {
                originalWeakPointMaterials[wp.weakPointCollider] = wp.weakPointCollider.GetComponent<Renderer>().material;
            }
        }
    }

    public void TakeDamage(int damage, Collider hitCollider = null, ElementType damageElement = ElementType.None)
    {
        if (currentHealth <= 0) return; // 既に死亡している場合は何もしない

        float finalDamage = damage;

        // 弱点ヒット判定
        foreach (var wp in weakPoints)
        {
            if (wp.weakPointCollider == hitCollider) // ヒットしたコライダーが弱点だったら
            {
                finalDamage *= wp.damageMultiplier;
                if (wp.weakPointEffectPrefab != null)
                {
                    Instantiate(wp.weakPointEffectPrefab, hitCollider.transform.position, Quaternion.identity);
                }
                if (wp.canBeStunned)
                {
                    ApplyStun(stunDuration); // 弱点ヒットでスタン
                }
                Debug.Log($"弱点にヒット！追加ダメージ：{finalDamage - damage}");
                break; // 複数の弱点に同時にヒットすることは想定しない
            }
        }

        // 属性ダメージ計算
        if (damageElement != ElementType.None)
        {
            foreach (var res in resistances)
            {
                if (res.element == damageElement)
                {
                    finalDamage *= res.resistanceMultiplier;
                    Debug.Log($"属性抵抗！ダメージ軽減：{res.resistanceMultiplier}");
                }
            }
            foreach (var weak in weaknesses)
            {
                if (weak.element == damageElement)
                {
                    finalDamage *= weak.weaknessMultiplier;
                    Debug.Log($"属性弱点！ダメージ増加：{weak.weaknessMultiplier}");
                }
            }
        }

        currentHealth -= Mathf.RoundToInt(finalDamage * (1f - defenseMultiplier));
        currentHealth = Mathf.Max(currentHealth, 0); // 体力が0以下にならないように

        Debug.Log($"{gameObject.name} に {Mathf.RoundToInt(finalDamage)} ダメージ！残りHP: {currentHealth}");

        // ヒットエフェクトとサウンド
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);

        // ひるみ判定
        if (!hasSuperArmor && !isStunned && finalDamage > stunThreshold)
        {
            ApplyStun(stunDuration);
            if (animator != null) animator.SetTrigger(anim_HitTriggerHash);
        }
        else if (hasSuperArmor)
        {
            Debug.Log("スーパーアーマーのためひるみません！");
        }

        // 死亡判定
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 必要に応じてUIを更新 (例: 敵のHPバー)
            // UIManager.Instance.UpdateEnemyHealthBar(currentHealth, maxHealth);
        }

        // ボスの場合、フェーズ移行チェック
        if (enemyAI != null && enemyAI.isBoss)
        {
            enemyAI.CheckBossPhase();
        }

        // 戦闘状態更新
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInCombat(true); // 被弾したら戦闘開始
            GameManager.Instance.AddActiveEnemy(this); // 敵がダメージを受けたらアクティブリストに追加
        }
    }

    public void ApplyStun(float duration)
    {
        if (isStunned) return; // 既にスタンしている場合は重ねがけしない
        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        // EnemyAIにスタン状態を通知し、行動を停止させる
        if (enemyAI != null) enemyAI.SetStunned(true);

        Debug.Log($"{gameObject.name} が {duration} 秒間スタンしました！");
        // スタンエフェクト表示

        yield return new WaitForSeconds(duration);

        isStunned = false;
        if (enemyAI != null) enemyAI.SetStunned(false);
        Debug.Log($"{gameObject.name} のスタンが解除されました。");
        // スタンエフェクト非表示
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} が倒されました！");
        if (animator != null) animator.SetTrigger(anim_DieTriggerHash);
        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);

        // コライダーやAIを無効化
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (enemyAI != null) enemyAI.enabled = false;

        // ドロップアイテム生成、経験値付与など
        // GameManager.Instance.AddExperience(expValue);

        // 死亡アニメーション終了後にオブジェクトを破棄
        Destroy(gameObject, 3f); // 例: 3秒後にオブジェクトを破棄
    }

    // 弱点強調表示のON/OFF (PlayerControllerから呼ばれることを想定)
    public void HighlightWeakPoints(bool enable)
    {
        foreach (var wp in weakPoints)
        {
            if (wp.weakPointCollider != null && wp.weakPointCollider.GetComponent<Renderer>() != null)
            {
                Renderer renderer = wp.weakPointCollider.GetComponent<Renderer>();
                if (enable && wp.highlightMaterial != null)
                {
                    renderer.material = wp.highlightMaterial;
                }
                else if (originalWeakPointMaterials.ContainsKey(wp.weakPointCollider))
                {
                    renderer.material = originalWeakPointMaterials[wp.weakPointCollider];
                }
            }
        }
    }
}