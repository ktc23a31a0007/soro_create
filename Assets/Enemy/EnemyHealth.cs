using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("�̗͐ݒ�")]
    public int maxHealth = 100;
    public int currentHealth;
    public float defenseMultiplier = 0.1f; // �_���[�W�y���� (0.1 = 10%�y��)

    [Header("�Ђ��/�A�[�}�[�ݒ�")]
    public float stunThreshold = 15f; // ����ȏ�̃_���[�W�łЂ��
    public bool hasSuperArmor = false; // �X�[�p�[�A�[�}�[��Ԃ��i��ɂЂ�܂Ȃ��j
    public float stunDuration = 1.0f; // �Ђ�ݎ���
    private bool isStunned = false;

    [Header("��_���ʐݒ�")]
    [System.Serializable]
    public class WeakPoint
    {
        public Collider weakPointCollider;
        public float damageMultiplier = 1.5f; // ��_�q�b�g���̃_���[�W�{��
        public GameObject weakPointEffectPrefab; // �q�b�g���̃G�t�F�N�g (�p�[�e�B�N���Ȃ�)
        public bool canBeStunned = true; // ��_�q�b�g�Œǉ��X�^�����邩
        [Tooltip("��_�����\���p�̃}�e���A����V�F�[�_�[�ݒ� (Runtime�œK�p)")]
        public Material highlightMaterial;
    }
    public List<WeakPoint> weakPoints;
    private Dictionary<Collider, Material> originalWeakPointMaterials = new Dictionary<Collider, Material>();


    [Header("�����ݒ�")]
    [System.Serializable]
    public class ElementalResistance
    {
        public ElementType element;
        public float resistanceMultiplier = 0.5f; // �_���[�W�y����
    }
    [System.Serializable]
    public class ElementalWeakness
    {
        public ElementType element;
        public float weaknessMultiplier = 1.5f; // �_���[�W������
    }
    public List<ElementalResistance> resistances;
    public List<ElementalWeakness> weaknesses;

    [Header("�G�t�F�N�g/�T�E���h")]
    public GameObject hitEffectPrefab;
    public AudioClip hitSound;
    public GameObject deathEffectPrefab;
    public AudioClip deathSound;

    private Animator animator;
    private EnemyAI enemyAI; // AI�ɃX�^����Ԃ�ʒm���邽��
    // �A�j���[�^�[�n�b�V��
    private readonly int anim_HitTriggerHash = Animator.StringToHash("Hit");
    private readonly int anim_DieTriggerHash = Animator.StringToHash("Die");

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        enemyAI = GetComponent<EnemyAI>();

        // ��_�̌��}�e���A����ۑ� (�����\���p)
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
        if (currentHealth <= 0) return; // ���Ɏ��S���Ă���ꍇ�͉������Ȃ�

        float finalDamage = damage;

        // ��_�q�b�g����
        foreach (var wp in weakPoints)
        {
            if (wp.weakPointCollider == hitCollider) // �q�b�g�����R���C�_�[����_��������
            {
                finalDamage *= wp.damageMultiplier;
                if (wp.weakPointEffectPrefab != null)
                {
                    Instantiate(wp.weakPointEffectPrefab, hitCollider.transform.position, Quaternion.identity);
                }
                if (wp.canBeStunned)
                {
                    ApplyStun(stunDuration); // ��_�q�b�g�ŃX�^��
                }
                Debug.Log($"��_�Ƀq�b�g�I�ǉ��_���[�W�F{finalDamage - damage}");
                break; // �����̎�_�ɓ����Ƀq�b�g���邱�Ƃ͑z�肵�Ȃ�
            }
        }

        // �����_���[�W�v�Z
        if (damageElement != ElementType.None)
        {
            foreach (var res in resistances)
            {
                if (res.element == damageElement)
                {
                    finalDamage *= res.resistanceMultiplier;
                    Debug.Log($"������R�I�_���[�W�y���F{res.resistanceMultiplier}");
                }
            }
            foreach (var weak in weaknesses)
            {
                if (weak.element == damageElement)
                {
                    finalDamage *= weak.weaknessMultiplier;
                    Debug.Log($"������_�I�_���[�W�����F{weak.weaknessMultiplier}");
                }
            }
        }

        currentHealth -= Mathf.RoundToInt(finalDamage * (1f - defenseMultiplier));
        currentHealth = Mathf.Max(currentHealth, 0); // �̗͂�0�ȉ��ɂȂ�Ȃ��悤��

        Debug.Log($"{gameObject.name} �� {Mathf.RoundToInt(finalDamage)} �_���[�W�I�c��HP: {currentHealth}");

        // �q�b�g�G�t�F�N�g�ƃT�E���h
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);

        // �Ђ�ݔ���
        if (!hasSuperArmor && !isStunned && finalDamage > stunThreshold)
        {
            ApplyStun(stunDuration);
            if (animator != null) animator.SetTrigger(anim_HitTriggerHash);
        }
        else if (hasSuperArmor)
        {
            Debug.Log("�X�[�p�[�A�[�}�[�̂��߂Ђ�݂܂���I");
        }

        // ���S����
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // �K�v�ɉ�����UI���X�V (��: �G��HP�o�[)
            // UIManager.Instance.UpdateEnemyHealthBar(currentHealth, maxHealth);
        }

        // �{�X�̏ꍇ�A�t�F�[�Y�ڍs�`�F�b�N
        if (enemyAI != null && enemyAI.isBoss)
        {
            enemyAI.CheckBossPhase();
        }

        // �퓬��ԍX�V
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInCombat(true); // ��e������퓬�J�n
            GameManager.Instance.AddActiveEnemy(this); // �G���_���[�W���󂯂���A�N�e�B�u���X�g�ɒǉ�
        }
    }

    public void ApplyStun(float duration)
    {
        if (isStunned) return; // ���ɃX�^�����Ă���ꍇ�͏d�˂������Ȃ�
        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        // EnemyAI�ɃX�^����Ԃ�ʒm���A�s�����~������
        if (enemyAI != null) enemyAI.SetStunned(true);

        Debug.Log($"{gameObject.name} �� {duration} �b�ԃX�^�����܂����I");
        // �X�^���G�t�F�N�g�\��

        yield return new WaitForSeconds(duration);

        isStunned = false;
        if (enemyAI != null) enemyAI.SetStunned(false);
        Debug.Log($"{gameObject.name} �̃X�^������������܂����B");
        // �X�^���G�t�F�N�g��\��
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} ���|����܂����I");
        if (animator != null) animator.SetTrigger(anim_DieTriggerHash);
        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);

        // �R���C�_�[��AI�𖳌���
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (enemyAI != null) enemyAI.enabled = false;

        // �h���b�v�A�C�e�������A�o���l�t�^�Ȃ�
        // GameManager.Instance.AddExperience(expValue);

        // ���S�A�j���[�V�����I����ɃI�u�W�F�N�g��j��
        Destroy(gameObject, 3f); // ��: 3�b��ɃI�u�W�F�N�g��j��
    }

    // ��_�����\����ON/OFF (PlayerController����Ă΂�邱�Ƃ�z��)
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