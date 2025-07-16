using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("�Q�[�����")]
    public bool isGamePaused = false;
    public bool isInCombat = false; // �퓬���t���O
    public float combatEndDelay = 5f; // �G��|���Ă���퓬��ԉ����܂ł̗P�\
    private float lastCombatActionTime; // �Ō�ɍU��/��e��������

    [Header("�Z�[�u/���[�h")]
    public string saveFileName = "game_save.json"; // �Z�[�u�t�@�C����

    // ���̃��[���h��ԊǗ��i��: �|�����{�X�A���������N�G�X�g�Ȃǁj
    public Dictionary<string, bool> worldFlags = new Dictionary<string, bool>();
    public List<EnemyHealth> activeEnemies = new List<EnemyHealth>(); // ���݃A�N�e�B�u�ȓG�̃��X�g

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
    }

    void Start()
    {
        // ������Ԃ̃��[�h�Ȃ�
        // LoadGame(); // �K�v�ɉ����Ď������[�h
    }

    void Update()
    {
        // �|�[�Y���j���[�̐؂�ւ�
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseGame();
        }

        // �퓬��Ԃ̊Ď�
        CheckCombatStatus();
    }

    void CheckCombatStatus()
    {
        // �A�N�e�B�u�ȓG���X�g���X�V (null�ɂȂ������̂��폜)
        activeEnemies.RemoveAll(enemy => enemy == null || enemy.currentHealth <= 0);

        if (activeEnemies.Count > 0)
        {
            SetInCombat(true);
        }
        else if (isInCombat && Time.time - lastCombatActionTime > combatEndDelay)
        {
            SetInCombat(false);
        }
    }

    public void SetInCombat(bool inCombat)
    {
        if (isInCombat != inCombat)
        {
            isInCombat = inCombat;
            Debug.Log($"�퓬���: {(isInCombat ? "�J�n" : "�I��")}");
            if (UIManager.Instance != null)
            {
                if (isInCombat) UIManager.Instance.ShowMessage("�퓬�J�n�I", 1.5f);
                else UIManager.Instance.ShowMessage("�퓬�I��", 1.5f);
            }
        }
        if (inCombat)
        {
            lastCombatActionTime = Time.time;
        }
    }

    // �G���������ꂽ���Ƀ��X�g�ɒǉ����郁�\�b�h (RandomEventManager��EnemySpawner����Ă�)
    public void AddActiveEnemy(EnemyHealth enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
            SetInCombat(true); // �G���ǉ����ꂽ��퓬�J�n
        }
    }

    // �|�[�Y/�A���|�[�Y�̐؂�ւ�
    public void TogglePauseGame()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f; // ���Ԃ��~/�ĊJ
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TogglePauseMenu(isGamePaused);
        }
        Debug.Log($"�Q�[���|�[�Y���: {isGamePaused}");
    }

    // �Z�[�u�Q�[�� (�㏑���̂�)
    public void SaveGame()
    {
        if (isInCombat)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�퓬���̓Z�[�u�ł��܂���I", 2f);
            Debug.LogWarning("�퓬���̓Z�[�u�ł��܂���B");
            return;
        }
        if (isGamePaused)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�|�[�Y���̓Z�[�u�ł��܂���I", 2f);
            Debug.LogWarning("�|�[�Y���̓Z�[�u�ł��܂���B");
            return;
        }

        // �Z�[�u�f�[�^�I�u�W�F�N�g���\�z (HP, �X�^�~�i, �v���C���[�ʒu, ���[���h�t���O�Ȃ�)
        SaveData saveData = new SaveData();
        // �v���C���[�̃f�[�^��ۑ�
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        PlayerController playerController = FindObjectOfType<PlayerController>();
        PlayerStats playerStats = FindObjectOfType<PlayerStats>();

        if (playerHealth != null) saveData.playerHealth = playerHealth.currentHealth;
        if (playerController != null)
        {
            saveData.playerStamina = playerController.GetCurrentStamina();
            saveData.playerPosition = playerController.transform.position;
            saveData.playerRotation = playerController.transform.rotation;
        }
        if (playerStats != null)
        {
            saveData.playerLevel = playerStats.currentLevel;
            saveData.playerExperience = playerStats.currentExperience;
            saveData.playerBaseAttackDamage = playerStats.baseAttackDamage;
            saveData.playerBaseDefense = playerStats.baseDefense;
            saveData.equippedWeaponElement = playerStats.equippedWeaponElement;
        }

        // ���[���h�t���O�̕ۑ�
        saveData.worldFlags = new Dictionary<string, bool>(worldFlags);

        string json = JsonUtility.ToJson(saveData, true);
        string path = Application.persistentDataPath + "/" + saveFileName;
        System.IO.File.WriteAllText(path, json);
        Debug.Log("�Q�[�����Z�[�u���܂���: " + path);
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�Q�[�����Z�[�u���܂����I", 2f);
    }

    // ���[�h�Q�[��
    public void LoadGame()
    {
        string path = Application.persistentDataPath + "/" + saveFileName;
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            // �V�[�������[�h���A���[�h��Ƀv���C���[�ʒu�Ȃǂ�K�p
            SceneManager.LoadScene(saveData.currentSceneName); // �Z�[�u�����V�[�������K�v

            // ���[�h������A�v���C���[�̃f�[�^��K�p
            StartCoroutine(ApplyLoadedDataAfterSceneLoad(saveData));

            // ���[���h�t���O�̓K�p
            worldFlags = new Dictionary<string, bool>(saveData.worldFlags);

            Debug.Log("�Q�[�������[�h���܂���: " + path);
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�Q�[�������[�h���܂����I", 2f);
        }
        else
        {
            Debug.LogWarning("�Z�[�u�t�@�C����������܂���: " + path);
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("�Z�[�u�t�@�C����������܂���B", 2f);
        }
    }

    IEnumerator ApplyLoadedDataAfterSceneLoad(SaveData saveData)
    {
        // �V�[�����[�h����������܂ő҂�
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == saveData.currentSceneName);

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        PlayerController playerController = FindObjectOfType<PlayerController>();
        PlayerStats playerStats = FindObjectOfType<PlayerStats>();

        if (playerHealth != null) playerHealth.SetCurrentHealth(saveData.playerHealth);
        if (playerController != null)
        {
            playerController.SetCurrentStamina(saveData.playerStamina);
            playerController.characterController.enabled = false; // �ʒu�ݒ�̂��߈ꎞ������
            playerController.transform.position = saveData.playerPosition;
            playerController.transform.rotation = saveData.playerRotation;
            playerController.characterController.enabled = true; // �ėL����
        }
        if (playerStats != null)
        {
            playerStats.SetStats(saveData.playerLevel, saveData.playerExperience, saveData.playerBaseAttackDamage, saveData.playerBaseDefense, saveData.equippedWeaponElement);
        }
        Debug.Log("���[�h���ꂽ�v���C���[�f�[�^�K�p�����B");
    }

    // ���[���h�t���O�̐ݒ�/�擾
    public void SetWorldFlag(string flagName, bool value)
    {
        worldFlags[flagName] = value;
    }

    public bool GetWorldFlag(string flagName)
    {
        return worldFlags.ContainsKey(flagName) ? worldFlags[flagName] : false;
    }

    // �Q�[���I��
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("�Q�[�����I�����܂��B");
    }
}

// �Z�[�u�f�[�^�\�� (�ʓrSaveData.cs�Ƃ��Ē�`����̂��x�X�g)
[System.Serializable]
public class SaveData
{
    public int playerHealth;
    public float playerStamina;
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public int playerLevel;
    public int playerExperience;
    public int playerBaseAttackDamage;
    public float playerBaseDefense;
    public ElementType equippedWeaponElement; // �������̕��푮��

    public string currentSceneName;
    public Dictionary<string, bool> worldFlags = new Dictionary<string, bool>();

    public SaveData()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
    }
}