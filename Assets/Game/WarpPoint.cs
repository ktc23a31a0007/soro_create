using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class WarpPoint : MonoBehaviour
{
    [Header("���[�v�|�C���g�ݒ�")]
    public string destinationSceneName; // ���[�v��̃V�[����
    public Vector3 destinationPosition; // ���[�v��̍��W
    public bool requireUnlock = false; // ����������K�v��
    public string unlockConditionFlag = ""; // GameManager��worldFlags�Ń`�F�b�N����t���O�� (��: "Boss_ForestGuardian_Defeated")

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.G)) // G�L�[�Ń��[�v
            {
                AttemptWarp();
            }
        }
    }

    void AttemptWarp()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager��������܂���I");
            return;
        }

        if (GameManager.Instance.isInCombat)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�퓬���̓��[�v�ł��܂���I", 2f);
            Debug.LogWarning("�퓬���̓��[�v�ł��܂���B");
            return;
        }
        if (GameManager.Instance.isGamePaused)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�|�[�Y���̓��[�v�ł��܂���I", 2f);
            Debug.LogWarning("�|�[�Y���̓��[�v�ł��܂���B");
            return;
        }

        if (requireUnlock)
        {
            if (string.IsNullOrEmpty(unlockConditionFlag))
            {
                Debug.LogWarning("���[�v�|�C���g�̉�������t���O���ݒ肳��Ă��܂���B", this);
                if (UIManager.Instance != null) UIManager.Instance.ShowMessage("���[�v�|�C���g���ݒ肳��Ă��܂���B", 2f);
                return;
            }

            if (!GameManager.Instance.GetWorldFlag(unlockConditionFlag))
            {
                Debug.Log($"���̃��[�v�|�C���g�͂܂��������Ă��܂���B����: {unlockConditionFlag}");
                if (UIManager.Instance != null) UIManager.Instance.ShowMessage("���[�v�|�C���g�̓��b�N����Ă��܂��B", 2f);
                return;
            }
        }

        Debug.Log($"���[�v�J�n: �V�[�� {destinationSceneName}, �ʒu {destinationPosition}");
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("���[�v��...", 1f);

        // �V�[���J�ڂƃv���C���[�̈ʒu�ݒ�
        StartCoroutine(LoadSceneAndMovePlayer(destinationSceneName, destinationPosition));
    }

    IEnumerator LoadSceneAndMovePlayer(string sceneName, Vector3 targetPosition)
    {
        SceneManager.LoadScene(sceneName); // �V�[�������[�h

        // �V�[�����[�h����������܂ő҂�
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);

        // �v���C���[�I�u�W�F�N�g�������Ĉʒu��ݒ�
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            // CharacterController���g�p���Ă���ꍇ�A����transform.position�͐�������Ȃ����߁A
            // Disable���Ĉړ���Enable���邩�ACharacterController.Move()���g��
            player.characterController.enabled = false;
            player.transform.position = targetPosition;
            player.characterController.enabled = true;
            Debug.Log("�v���C���[�����[�v��Ɉړ����܂����B");
        }
        else
        {
            Debug.LogError("�v���C���[��������܂���I���[�v��ւ̈ړ��Ɏ��s���܂����B");
        }
    }
}