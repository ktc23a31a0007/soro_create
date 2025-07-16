using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // TextMeshPro���g�p����ꍇ

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI�v�f�̎Q��")]
    public Slider healthBar;
    public Slider staminaBar;
    public TextMeshProUGUI messageText; // ���b�Z�[�W�\���p�e�L�X�g
    public CanvasGroup messagePanel; // ���b�Z�[�W�p�l���i�t�F�[�h�C��/�A�E�g�p�j
    public GameObject damageOverlayPanel; // �_���[�W���󂯂����̃I�[�o�[���C
    public GameObject gameOverPanel; // �Q�[���I�[�o�[�p�l��
    public GameObject pauseMenuPanel; // �|�[�Y���j���[�p�l��

    [Header("�f�o�t�A�C�R��")]
    public Transform debuffIconParent; // �f�o�t�A�C�R����z�u����e�I�u�W�F�N�g
    public GameObject debuffIconPrefab; // �f�o�t�A�C�R���̃v���n�u�i�摜�ƃe�L�X�g�Ȃǂ��܂ށj
    private Dictionary<string, GameObject> activeDebuffIcons = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // UI�����ݒ�
        if (messagePanel != null) messagePanel.alpha = 0; // �����ɂ��Ă���
        if (damageOverlayPanel != null) damageOverlayPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    // HP�o�[�̍X�V
    public void UpdateHealthBar(float current, float max)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value = current;
        }
    }

    // �X�^�~�i�o�[�̍X�V
    public void UpdateStaminaBar(float current, float max)
    {
        if (staminaBar != null)
        {
            staminaBar.maxValue = max;
            staminaBar.value = current;
        }
    }

    // ���b�Z�[�W�̕\��
    public void ShowMessage(string message, float duration = 3f)
    {
        if (messageText != null && messagePanel != null)
        {
            messageText.text = message;
            StopAllCoroutines(); // �����̃��b�Z�[�W�\���R���[�`�����~
            StartCoroutine(FadeMessagePanel(1, duration)); // �t�F�[�h�C�����ĕ\��
        }
    }

    // �C�x���g���b�Z�[�W�̕\�� (RandomEventManager����Ă΂��)
    public void ShowEventMessage(string eventName, string description, float duration = 5f)
    {
        string fullMessage = $"<size=24>{eventName}</size>\n<size=18>{description}</size>";
        ShowMessage(fullMessage, duration);
    }

    IEnumerator FadeMessagePanel(float targetAlpha, float duration)
    {
        float startAlpha = messagePanel.alpha;
        float timer = 0;

        // �t�F�[�h�C��
        while (timer < 0.5f) // ��: 0.5�b�Ńt�F�[�h�C��
        {
            timer += Time.deltaTime;
            messagePanel.alpha = Mathf.Lerp(startAlpha, 1, timer / 0.5f);
            yield return null;
        }
        messagePanel.alpha = 1;

        yield return new WaitForSeconds(duration); // �\���ێ�

        // �t�F�[�h�A�E�g
        timer = 0;
        startAlpha = messagePanel.alpha;
        while (timer < 0.5f) // ��: 0.5�b�Ńt�F�[�h�A�E�g
        {
            timer += Time.deltaTime;
            messagePanel.alpha = Mathf.Lerp(startAlpha, 0, timer / 0.5f);
            yield return null;
        }
        messagePanel.alpha = 0;
    }

    // �_���[�W�I�[�o�[���C�̕\��
    public void ShowDamageOverlay(float duration = 0.5f)
    {
        if (damageOverlayPanel != null)
        {
            damageOverlayPanel.SetActive(true);
            StartCoroutine(HideDamageOverlayAfterDelay(duration));
        }
    }

    IEnumerator HideDamageOverlayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (damageOverlayPanel != null)
        {
            damageOverlayPanel.SetActive(false);
        }
    }

    // �f�o�t�A�C�R���̕\��
    public void ShowDebuffIcon(string debuffName, Sprite iconSprite = null)
    {
        if (!activeDebuffIcons.ContainsKey(debuffName) && debuffIconPrefab != null && debuffIconParent != null)
        {
            GameObject icon = Instantiate(debuffIconPrefab, debuffIconParent);
            // icon.GetComponent<Image>().sprite = iconSprite; // �A�C�R���摜��ݒ�
            // icon.GetComponentInChildren<TextMeshProUGUI>().text = debuffName; // �f�o�t����ݒ�
            activeDebuffIcons.Add(debuffName, icon);
        }
    }

    // �f�o�t�A�C�R���̔�\��
    public void HideDebuffIcon(string debuffName)
    {
        if (activeDebuffIcons.ContainsKey(debuffName))
        {
            Destroy(activeDebuffIcons[debuffName]);
            activeDebuffIcons.Remove(debuffName);
        }
    }

    // �Q�[���I�[�o�[��ʂ̕\��
    public void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            // �v���C���[���͂��~����Ȃ�
            Time.timeScale = 0f; // �Q�[�������S�ɒ�~
        }
    }

    // �|�[�Y���j���[�̕\��/��\��
    public void TogglePauseMenu(bool isActive)
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isActive);
        }
    }

    // ���̃{�^���R�[���o�b�N (���ۂ�UnityUI�̃C�x���g�V�X�e���Őݒ�)
    public void OnResumeButtonClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.TogglePauseGame();
    }

    public void OnExitButtonClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.QuitGame();
    }
}