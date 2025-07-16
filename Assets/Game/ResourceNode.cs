using UnityEngine;
using System.Collections;

public class ResourceNode : MonoBehaviour
{
    [Header("�����m�[�h�ݒ�")]
    public string resourceItemName = "�؍�"; // �̏W�ł���A�C�e����
    public int minQuantity = 1; // �̏W�ł���ŏ�����
    public int maxQuantity = 3; // �̏W�ł���ő吔��
    public float respawnTime = 60f; // ���X�|�[���܂ł̎��� (�b)
    public GameObject harvestEffectPrefab; // �̏W���̃G�t�F�N�g

    private bool isDepleted = false;
    private Renderer nodeRenderer; // �m�[�h�̌����ڂ�؂�ւ��邽��
    private Collider nodeCollider; // �m�[�h�̓����蔻���؂�ւ��邽��

    void Awake()
    {
        nodeRenderer = GetComponent<Renderer>();
        nodeCollider = GetComponent<Collider>();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.F) && !isDepleted)
        {
            Harvest();
        }
    }

    void Harvest()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager��������܂���I");
            return;
        }

        isDepleted = true;
        nodeRenderer.enabled = false; // �����ڂ��\���ɂ���
        nodeCollider.enabled = false; // �����蔻��𖳌��ɂ���

        if (harvestEffectPrefab != null)
        {
            Instantiate(harvestEffectPrefab, transform.position, Quaternion.identity);
        }

        int quantity = Random.Range(minQuantity, maxQuantity + 1);
        if (InventoryManager.Instance.AddItem(resourceItemName, quantity))
        {
            Debug.Log($"{resourceItemName} �� {quantity} �̏W���܂����B");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"{resourceItemName} +{quantity}", 1.5f);

            // �o���l�t�^ (PlayerStats�ɒ��ڃA�N�Z�X)
            PlayerStats playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddExperience(quantity * 5); // ��: �̏W�ʂɉ����Čo���l
            }
        }
        else
        {
            // �C���x���g���������ς��Œǉ��ł��Ȃ������ꍇ
            // �A�C�e����n�ʂɃh���b�v������Ȃǂ̏���������
            Debug.LogWarning("�C���x���g���������ς��ŃA�C�e����ǉ��ł��܂���ł����B");
        }

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        isDepleted = false;
        nodeRenderer.enabled = true; // �����ڂ��ĕ\��
        nodeCollider.enabled = true; // �����蔻���L���ɂ���
        Debug.Log($"{resourceItemName} �m�[�h�����X�|�[�����܂����B");
    }

    // RandomEventManager����ꎞ�I�ȃ��\�[�X�m�[�h�Ƃ��Đ������ꂽ�ꍇ�̏��Ń^�C�}�[
    public void SetTemporary(float duration)
    {
        StartCoroutine(TemporaryDisappearRoutine(duration));
    }

    IEnumerator TemporaryDisappearRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (this != null) // �C�x���g�I�����Ɋ��ɔj�󂳂�Ă���\�������邽�߃`�F�b�N
        {
            Destroy(gameObject);
            Debug.Log($"{resourceItemName} (�ꎞ�I�ȃm�[�h) �����Ԑ؂�ŏ��ł��܂����B");
        }
    }
}