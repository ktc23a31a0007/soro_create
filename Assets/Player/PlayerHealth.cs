using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("体力設定")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("被ダメージ設定")]
    public float invincibilityDuration = 1.0f; // ダメージ後の無敵時間
    private bool isInvincible = false;

    [Header("死亡設定")]
    public GameObject deathEffectPrefab; // 死亡時のエフェクト
    public AudioClip deathSound; // 死亡時のサウンド
    public string respawnSceneName = "StartMenu"; // リスポーンするシーン名

    private Animator animator;
    // アニメーターハッシュ
    private readonly int anim_HitTriggerHash = Animator.StringToHash("Hit");
    private readonly int anim_DieTriggerHash = Animator.StringToHash("Die");

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0); // 体力が0以下にならないように

        Debug.Log($"プレイヤーが {damage} ダメージを受けました。残りHP: {currentHealth}");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
            UIManager.Instance.ShowDamageOverlay(); // ダメージを受けた時の画面エフェクト
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityRoutine()); // 無敵時間開始
            if (animator != null) animator.SetTrigger(anim_HitTriggerHash);
            // ヒットサウンド、エフェクトなど
        }

        // 戦闘状態更新
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInCombat(true); // 被弾したら戦闘開始
        }
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        // 例: キャラクターの点滅エフェクトなど
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    void Die()
    {
        Debug.Log("プレイヤーは倒れました！");
        if (animator != null) animator.SetTrigger(anim_DieTriggerHash);
        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);

        // コライダーや入力、AIなどを無効化
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null) charController.enabled = false;
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverScreen();
        }

        // リスポーン処理またはゲームオーバー画面への移行
        // 時間差でシーンをロード
        StartCoroutine(ReloadSceneAfterDelay(respawnSceneName, 3f));
    }

    IEnumerator ReloadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    // 外部からHPを回復させるメソッド (ポーション使用時など)
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        Debug.Log($"HPが {amount} 回復しました。現在HP: {currentHealth}");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    // 無敵状態の切り替え (外部から制御する場合)
    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        // エフェクトの開始/停止など
    }

    // セーブ/ロード用HP設定
    public void SetCurrentHealth(int health)
    {
        currentHealth = health;
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
    }
}