using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("�̗͐ݒ�")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("��_���[�W�ݒ�")]
    public float invincibilityDuration = 1.0f; // �_���[�W��̖��G����
    private bool isInvincible = false;

    [Header("���S�ݒ�")]
    public GameObject deathEffectPrefab; // ���S���̃G�t�F�N�g
    public AudioClip deathSound; // ���S���̃T�E���h
    public string respawnSceneName = "StartMenu"; // ���X�|�[������V�[����

    private Animator animator;
    // �A�j���[�^�[�n�b�V��
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
        currentHealth = Mathf.Max(currentHealth, 0); // �̗͂�0�ȉ��ɂȂ�Ȃ��悤��

        Debug.Log($"�v���C���[�� {damage} �_���[�W���󂯂܂����B�c��HP: {currentHealth}");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
            UIManager.Instance.ShowDamageOverlay(); // �_���[�W���󂯂����̉�ʃG�t�F�N�g
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityRoutine()); // ���G���ԊJ�n
            if (animator != null) animator.SetTrigger(anim_HitTriggerHash);
            // �q�b�g�T�E���h�A�G�t�F�N�g�Ȃ�
        }

        // �퓬��ԍX�V
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInCombat(true); // ��e������퓬�J�n
        }
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        // ��: �L�����N�^�[�̓_�ŃG�t�F�N�g�Ȃ�
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    void Die()
    {
        Debug.Log("�v���C���[�͓|��܂����I");
        if (animator != null) animator.SetTrigger(anim_DieTriggerHash);
        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);

        // �R���C�_�[����́AAI�Ȃǂ𖳌���
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null) charController.enabled = false;
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverScreen();
        }

        // ���X�|�[�������܂��̓Q�[���I�[�o�[��ʂւ̈ڍs
        // ���ԍ��ŃV�[�������[�h
        StartCoroutine(ReloadSceneAfterDelay(respawnSceneName, 3f));
    }

    IEnumerator ReloadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    // �O������HP���񕜂����郁�\�b�h (�|�[�V�����g�p���Ȃ�)
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        Debug.Log($"HP�� {amount} �񕜂��܂����B����HP: {currentHealth}");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    // ���G��Ԃ̐؂�ւ� (�O�����琧�䂷��ꍇ)
    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        // �G�t�F�N�g�̊J�n/��~�Ȃ�
    }

    // �Z�[�u/���[�h�pHP�ݒ�
    public void SetCurrentHealth(int health)
    {
        currentHealth = health;
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
    }
}