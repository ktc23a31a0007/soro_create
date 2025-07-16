using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("ゲーム状態")]
    public bool isGamePaused = false;
    public bool isInCombat = false; // 戦闘中フラグ
    public float combatEndDelay = 5f; // 敵を倒してから戦闘状態解除までの猶予
    private float lastCombatActionTime; // 最後に攻撃/被弾した時間

    [Header("セーブ/ロード")]
    public string saveFileName = "game_save.json"; // セーブファイル名

    // 仮のワールド状態管理（例: 倒したボス、完了したクエストなど）
    public Dictionary<string, bool> worldFlags = new Dictionary<string, bool>();
    public List<EnemyHealth> activeEnemies = new List<EnemyHealth>(); // 現在アクティブな敵のリスト

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
        // 初期状態のロードなど
        // LoadGame(); // 必要に応じて自動ロード
    }

    void Update()
    {
        // ポーズメニューの切り替え
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseGame();
        }

        // 戦闘状態の監視
        CheckCombatStatus();
    }

    void CheckCombatStatus()
    {
        // アクティブな敵リストを更新 (nullになったものを削除)
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
            Debug.Log($"戦闘状態: {(isInCombat ? "開始" : "終了")}");
            if (UIManager.Instance != null)
            {
                if (isInCombat) UIManager.Instance.ShowMessage("戦闘開始！", 1.5f);
                else UIManager.Instance.ShowMessage("戦闘終了", 1.5f);
            }
        }
        if (inCombat)
        {
            lastCombatActionTime = Time.time;
        }
    }

    // 敵が生成された時にリストに追加するメソッド (RandomEventManagerやEnemySpawnerから呼ぶ)
    public void AddActiveEnemy(EnemyHealth enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
            SetInCombat(true); // 敵が追加されたら戦闘開始
        }
    }

    // ポーズ/アンポーズの切り替え
    public void TogglePauseGame()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f; // 時間を停止/再開
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TogglePauseMenu(isGamePaused);
        }
        Debug.Log($"ゲームポーズ状態: {isGamePaused}");
    }

    // セーブゲーム (上書きのみ)
    public void SaveGame()
    {
        if (isInCombat)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("戦闘中はセーブできません！", 2f);
            Debug.LogWarning("戦闘中はセーブできません。");
            return;
        }
        if (isGamePaused)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ポーズ中はセーブできません！", 2f);
            Debug.LogWarning("ポーズ中はセーブできません。");
            return;
        }

        // セーブデータオブジェクトを構築 (HP, スタミナ, プレイヤー位置, ワールドフラグなど)
        SaveData saveData = new SaveData();
        // プレイヤーのデータを保存
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

        // ワールドフラグの保存
        saveData.worldFlags = new Dictionary<string, bool>(worldFlags);

        string json = JsonUtility.ToJson(saveData, true);
        string path = Application.persistentDataPath + "/" + saveFileName;
        System.IO.File.WriteAllText(path, json);
        Debug.Log("ゲームをセーブしました: " + path);
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ゲームをセーブしました！", 2f);
    }

    // ロードゲーム
    public void LoadGame()
    {
        string path = Application.persistentDataPath + "/" + saveFileName;
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            // シーンをロードし、ロード後にプレイヤー位置などを適用
            SceneManager.LoadScene(saveData.currentSceneName); // セーブしたシーン名も必要

            // ロード完了後、プレイヤーのデータを適用
            StartCoroutine(ApplyLoadedDataAfterSceneLoad(saveData));

            // ワールドフラグの適用
            worldFlags = new Dictionary<string, bool>(saveData.worldFlags);

            Debug.Log("ゲームをロードしました: " + path);
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ゲームをロードしました！", 2f);
        }
        else
        {
            Debug.LogWarning("セーブファイルが見つかりません: " + path);
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("セーブファイルが見つかりません。", 2f);
        }
    }

    IEnumerator ApplyLoadedDataAfterSceneLoad(SaveData saveData)
    {
        // シーンロードが完了するまで待つ
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == saveData.currentSceneName);

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        PlayerController playerController = FindObjectOfType<PlayerController>();
        PlayerStats playerStats = FindObjectOfType<PlayerStats>();

        if (playerHealth != null) playerHealth.SetCurrentHealth(saveData.playerHealth);
        if (playerController != null)
        {
            playerController.SetCurrentStamina(saveData.playerStamina);
            playerController.characterController.enabled = false; // 位置設定のため一時無効化
            playerController.transform.position = saveData.playerPosition;
            playerController.transform.rotation = saveData.playerRotation;
            playerController.characterController.enabled = true; // 再有効化
        }
        if (playerStats != null)
        {
            playerStats.SetStats(saveData.playerLevel, saveData.playerExperience, saveData.playerBaseAttackDamage, saveData.playerBaseDefense, saveData.equippedWeaponElement);
        }
        Debug.Log("ロードされたプレイヤーデータ適用完了。");
    }

    // ワールドフラグの設定/取得
    public void SetWorldFlag(string flagName, bool value)
    {
        worldFlags[flagName] = value;
    }

    public bool GetWorldFlag(string flagName)
    {
        return worldFlags.ContainsKey(flagName) ? worldFlags[flagName] : false;
    }

    // ゲーム終了
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("ゲームを終了します。");
    }
}

// セーブデータ構造 (別途SaveData.csとして定義するのがベスト)
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
    public ElementType equippedWeaponElement; // 装備中の武器属性

    public string currentSceneName;
    public Dictionary<string, bool> worldFlags = new Dictionary<string, bool>();

    public SaveData()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
    }
}