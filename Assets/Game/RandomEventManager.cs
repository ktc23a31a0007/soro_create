using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.AI; // NavMesh.SamplePositionのために必要

public class RandomEventManager : MonoBehaviour
{
    public static RandomEventManager Instance { get; private set; }

    [System.Serializable]
    public class RandomEvent
    {
        public string eventName;
        [TextArea(3, 5)]
        public string eventDescription;
        public float eventWeight = 1f; // イベントの発生しやすさ
        public EventSpawnType spawnType = EventSpawnType.PlayerVicinity;
        public GameObject[] enemyPrefabsToSpawn; // 出現させる敵のプレハブ
        public int minEnemyCount = 1;
        public int maxEnemyCount = 5;
        public GameObject[] friendlyNPCPrefabsToSpawn; // 友好的NPCのプレハブ
        public GameObject[] resourceNodePrefabsToSpawn; // 希少資源ノードのプレハブ
        public int minResourceNodeCount = 0;
        public int maxResourceNodeCount = 3;
        public float spawnRadius = 20f; // プレイヤーの周囲のスポーン範囲

        public float eventDuration = 120f; // イベントの最大継続時間 (敵殲滅または時間切れ)
        public GameObject eventAreaIndicatorPrefab; // イベント範囲を示すオブジェクト

        public AudioClip eventMusic; // イベント専用BGM
        [Range(0f, 1f)]
        public float eventMusicVolume = 0.5f;

        [Header("一時的な環境影響")]
        public GameObject environmentEffectPrefab; // 濃霧、嵐などの環境エフェクト
        public float playerMoveSpeedMultiplier = 1f; // プレイヤーの移動速度倍率
        public float enemySightRangeMultiplier = 1f; // イベント中に出現する敵の視認範囲倍率
        public float temporaryBuffDurationAfterEvent = 0f; // イベント終了後の短時間バフ・デバフの継続時間

        public enum EventSpawnType { PlayerVicinity, FixedLocation }
    }

    public List<RandomEvent> eventPatterns = new List<RandomEvent>();
    public float eventCheckInterval = 300f; // イベントチェックの間隔 (秒)
    [Range(0f, 1f)]
    public float eventTriggerChance = 0.05f; // 1回のチェックでイベントが発生する確率
    public float eventMinimumCooldown = 60f; // イベント終了後の最低クールダウン時間

    private Transform playerTransform;
    private bool eventActive = false;
    private RandomEvent currentActiveEvent;
    private List<GameObject> spawnedEventEntities = new List<GameObject>();
    private GameObject currentEventAreaIndicator;
    private AudioSource eventAudioSource;
    private GameObject currentEnvironmentEffect;
    private float lastEventEndTime = -Mathf.Infinity; // 最後にイベントが終了した時間

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

        eventAudioSource = gameObject.AddComponent<AudioSource>();
        eventAudioSource.loop = true;
        eventAudioSource.playOnAwake = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerTransform = GameObject.FindWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogWarning("Playerが見つかりません。Playerタグが付いているか確認してください。ランダムイベントは発生しません。", this);
            if (eventActive) EndEvent();
            return;
        }

        // シーンロード時に現在アクティブなイベントがあれば終了させる
        if (eventActive)
        {
            EndEvent(); // シーンを跨ぐイベントは非推奨
        }
    }

    void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindWithTag("Player")?.transform;
        }
        StartCoroutine(EventCheckRoutine());
    }

    IEnumerator EventCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(eventCheckInterval);

            if (GameManager.Instance != null && GameManager.Instance.isGamePaused) continue;
            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy) continue;
            if (Time.time < lastEventEndTime + eventMinimumCooldown) continue; // クールダウン中

            if (!eventActive && Random.value <= eventTriggerChance)
            {
                TriggerRandomEvent();
            }
        }
    }

    void TriggerRandomEvent()
    {
        if (eventPatterns.Count == 0)
        {
            Debug.LogWarning("ランダムイベントのパターンが設定されていません。", this);
            return;
        }

        // 重みに基づいてイベントを選択
        float totalWeight = 0f;
        foreach (var ev in eventPatterns) { totalWeight += ev.eventWeight; }

        float randomValue = Random.value * totalWeight;
        RandomEvent selectedEvent = null;
        foreach (var ev in eventPatterns)
        {
            randomValue -= ev.eventWeight;
            if (randomValue <= 0)
            {
                selectedEvent = ev;
                break;
            }
        }

        if (selectedEvent == null) return;

        StartCoroutine(StartEvent(selectedEvent));
    }

    IEnumerator StartEvent(RandomEvent selectedEvent)
    {
        eventActive = true;
        currentActiveEvent = selectedEvent;

        Debug.Log("ランダムイベント発生！: " + currentActiveEvent.eventName + " - " + currentActiveEvent.eventDescription, this);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEventMessage(currentActiveEvent.eventName, currentActiveEvent.eventDescription, 5f);
        }

        // イベントBGM再生
        if (currentActiveEvent.eventMusic != null)
        {
            eventAudioSource.clip = currentActiveEvent.eventMusic;
            eventAudioSource.volume = currentActiveEvent.eventMusicVolume;
            eventAudioSource.Play();
        }

        Vector3 spawnCenter = playerTransform.position; // プレイヤーの現在地を中心とする

        // イベント範囲インジケーターの生成
        if (currentActiveEvent.eventAreaIndicatorPrefab != null)
        {
            currentEventAreaIndicator = Instantiate(currentActiveEvent.eventAreaIndicatorPrefab, spawnCenter, Quaternion.identity);
            // 必要に応じてスケール調整: currentEventAreaIndicator.transform.localScale = Vector3.one * currentActiveEvent.spawnRadius * 2;
        }

        spawnedEventEntities.Clear();

        // 環境エフェクトの適用
        if (currentActiveEvent.environmentEffectPrefab != null)
        {
            currentEnvironmentEffect = Instantiate(currentActiveEvent.environmentEffectPrefab, playerTransform.position, Quaternion.identity);
            currentEnvironmentEffect.transform.SetParent(playerTransform); // プレイヤーに追従
        }

        // プレイヤーの移動速度に一時的な影響
        if (currentActiveEvent.playerMoveSpeedMultiplier != 1f && playerTransform != null)
        {
            PlayerController playerController = playerTransform.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.moveSpeed *= currentActiveEvent.playerMoveSpeedMultiplier;
                playerController.sprintSpeedMultiplier *= currentActiveEvent.playerMoveSpeedMultiplier;
                Debug.Log($"プレイヤーの移動速度が {currentActiveEvent.playerMoveSpeedMultiplier} 倍になりました。");
            }
        }

        // 敵の生成
        SpawnEntities(currentActiveEvent.enemyPrefabsToSpawn, currentActiveEvent.minEnemyCount, currentActiveEvent.maxEnemyCount, spawnCenter, currentActiveEvent.spawnRadius, true);
        // 友好的NPCの生成
        SpawnEntities(currentActiveEvent.friendlyNPCPrefabsToSpawn, 1, 1, spawnCenter, currentActiveEvent.spawnRadius, false); // 基本1体
        // 希少資源ノードの生成
        SpawnEntities(currentActiveEvent.resourceNodePrefabsToSpawn, currentActiveEvent.minResourceNodeCount, currentActiveEvent.maxResourceNodeCount, spawnCenter, currentActiveEvent.spawnRadius, false);


        // イベントの継続時間を監視
        float startTime = Time.time;
        while (Time.time < startTime + currentActiveEvent.eventDuration)
        {
            // イベントで生成された敵の数をチェック
            spawnedEventEntities.RemoveAll(item => item == null);
            bool enemiesRemaining = false;
            foreach (var entity in spawnedEventEntities)
            {
                if (entity != null && entity.CompareTag("Enemy")) // Enemyタグを持つもののみカウント
                {
                    enemiesRemaining = true;
                    break;
                }
            }

            if (!enemiesRemaining && currentActiveEvent.enemyPrefabsToSpawn.Length > 0)
            {
                Debug.Log("イベントで生成された敵を全て倒しました！イベント終了。");
                break; // 敵を全て倒したら強制終了
            }
            yield return null;
        }

        EndEvent();
    }

    void SpawnEntities(GameObject[] prefabs, int minCount, int maxCount, Vector3 center, float radius, bool isEnemy)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        int count = Random.Range(minCount, maxCount + 1);
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Vector3 spawnPos = GetRandomNavMeshPosition(center, radius);
            if (spawnPos != Vector3.zero)
            {
                GameObject spawnedEntity = Instantiate(prefab, spawnPos, Quaternion.identity);
                spawnedEventEntities.Add(spawnedEntity);

                if (isEnemy)
                {
                    EnemyAI enemyAI = spawnedEntity.GetComponent<EnemyAI>();
                    if (enemyAI != null)
                    {
                        enemyAI.sightRange *= currentActiveEvent.enemySightRangeMultiplier;
                        // プレイヤーレベルに応じた敵のステータス調整 (EnemyStatsScalerクラスなどを使う)
                        // EnemyStatsScaler.ScaleEnemy(spawnedEntity, playerTransform.GetComponent<PlayerController>().GetPlayerLevel());
                    }
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddActiveEnemy(spawnedEntity.GetComponent<EnemyHealth>());
                    }
                }
                else
                {
                    // 友好的NPCや資源ノードの一時的な振る舞いを設定
                    // 例: 資源ノードに消滅タイマーを付与
                    ResourceNode resourceNode = spawnedEntity.GetComponent<ResourceNode>();
                    if (resourceNode != null) resourceNode.SetTemporary(currentActiveEvent.eventDuration);
                }
            }
        }
    }

    Vector3 GetRandomNavMeshPosition(Vector3 center, float radius)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        Debug.LogWarning("NavMesh上のランダムな位置が見つかりませんでした。", this);
        return Vector3.zero;
    }

    void EndEvent()
    {
        if (!eventActive) return;

        Debug.Log("ランダムイベント終了。", this);
        eventActive = false;
        lastEventEndTime = Time.time;

        // イベント範囲インジケーターを破棄
        if (currentEventAreaIndicator != null)
        {
            Destroy(currentEventAreaIndicator);
            currentEventAreaIndicator = null;
        }

        // 環境エフェクトを解除
        if (currentEnvironmentEffect != null)
        {
            Destroy(currentEnvironmentEffect);
            currentEnvironmentEffect = null;
        }

        // プレイヤーの移動速度を元に戻す
        if (currentActiveEvent.playerMoveSpeedMultiplier != 1f && playerTransform != null)
        {
            PlayerController playerController = playerTransform.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.moveSpeed /= currentActiveEvent.playerMoveSpeedMultiplier;
                playerController.sprintSpeedMultiplier /= currentActiveEvent.playerMoveSpeedMultiplier;
                Debug.Log("プレイヤーの移動速度が元に戻りました。");
            }
        }

        // 生成されたエンティティを全て破棄
        foreach (GameObject entity in spawnedEventEntities)
        {
            if (entity != null)
            {
                Destroy(entity);
            }
        }
        spawnedEventEntities.Clear();

        // イベントBGM停止
        if (eventAudioSource.isPlaying)
        {
            eventAudioSource.Stop();
        }

        // イベント終了後の短時間バフ・デバフの適用 (オプション)
        if (currentActiveEvent.temporaryBuffDurationAfterEvent > 0 && playerTransform != null)
        {
            // 例: UIManager.Instance.ShowMessage("イベントクリアボーナス！", 2f);
            // playerTransform.GetComponent<PlayerController>().ApplyTemporaryBuff(BuffType.AttackBoost, currentActiveEvent.temporaryBuffDurationAfterEvent);
            Debug.Log($"イベント終了後、{currentActiveEvent.temporaryBuffDurationAfterEvent}秒間の一時的な影響を適用します。");
        }

        currentActiveEvent = null;
    }
}