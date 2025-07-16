using UnityEngine;

public class SavePoint : MonoBehaviour
{
    [Header("�Z�[�u�|�C���g�ݒ�")]
    public string requiredItemName = "�L�����v�L�b�g"; // �Z�[�u�ɕK�v�ȃA�C�e����
    public int requiredItemCount = 1; // �K�v�ȃA�C�e����

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.F)) // F�L�[�ŃC���^���N�g
            {
                AttemptSave();
            }
        }
    }

    void AttemptSave()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager��������܂���I");
            return;
        }
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager��������܂���I");
            return;
        }

        if (GameManager.Instance.isInCombat)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�퓬���̓Z�[�u�ł��܂���I", 2f);
            Debug.LogWarning("�퓬���̓Z�[�u�ł��܂���B");
            return;
        }
        if (GameManager.Instance.isGamePaused)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�|�[�Y���̓Z�[�u�ł��܂���I", 2f);
            Debug.LogWarning("�|�[�Y���̓Z�[�u�ł��܂���B");
            return;
        }

        if (InventoryManager.Instance.HasItem(requiredItemName, requiredItemCount))
        {
            InventoryManager.Instance.RemoveItem(requiredItemName, requiredItemCount);
            GameManager.Instance.SaveGame(); // GameManager��SaveGame���Ăяo�� (�㏑���̂�)
            Debug.Log("�Q�[�����Z�[�u���܂����I");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�Q�[�����Z�[�u���܂����I", 2f);
        }
        else
        {
            Debug.Log($"�Z�[�u����ɂ́u{requiredItemName}�v��{requiredItemCount}�K�v�ł��B");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"�u{requiredItemName}�v������܂���I", 2f);
        }
    }
}