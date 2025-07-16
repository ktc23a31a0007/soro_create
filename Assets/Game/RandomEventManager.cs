using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.AI; // NavMesh.SamplePosition�̂��߂ɕK�v

public class RandomEventManager : MonoBehaviour
{
    public static RandomEventManager Instance { get; private set; }

    [System.Serializable]
    public class RandomEvent
    {
        public string eventName;
        [TextArea(3, 5)]
        public string eventDescription;
        public float eventWeight = 1f; // �C�x���g�̔������₷��
        public EventSpawnType spawnType = EventSpawnType.PlayerVicinity;
        public GameObject[] enemyPrefabsToSpawn; // �o��������G�̃v���n�u
        public int minEnemyCount = 1;
        public int maxEnemyCount = 5;
        public GameObject[] friendlyNPCPrefabsToSpawn; // �F�D�INPC�̃v���n�u
        public GameObject[] resourceNodePrefabsToSpawn; // �󏭎����m�[�h�̃v���n�u
        public int minResourceNodeCount = 0;
        public int maxResourceNodeCount = 3;
        public float spawnRadius = 20f; // �v���C���[�̎��͂̃X�|�[���͈�

        public float eventDuration = 120f; // �C�x���g�̍ő�p������ (�G�r�ł܂��͎��Ԑ؂�)
        public GameObject eventAreaIndicatorPrefab; // �C�x���g�͈͂������I�u�W�F�N�g

        public AudioClip eventMusic; // �C�x���g��pBGM
        [Range(0f, 1f)]
        public float eventMusicVolume = 0.5f;

        [Header("�ꎞ�I�Ȋ��e��")]
        public GameObject environmentEffectPrefab; // �Z���A���Ȃǂ̊��G�t�F�N�g
        public float playerMoveSpeedMultiplier = 1f; // �v���C���[�̈ړ����x�{��
        public float enemySightRangeMultiplier = 1f; // �C�x���g���ɏo������G�̎��F�͈͔{��
        public float temporaryBuffDurationAfterEvent = 0f; // �C�x���g�I����̒Z���ԃo�t�E�f�o�t�̌p������

        public enum EventSpawnType { PlayerVicinity, FixedLocation }
    }

    public List<RandomEvent> eventPatterns = new List<RandomEvent>();
    public float eventCheckInterval = 300f; // �C�x���g�`�F�b�N�̊Ԋu (�b)
    [Range(0f, 1f)]
    public float eventTriggerChance = 0.05f; // 1��̃`�F�b�N�ŃC�x���g����������m��
    public float eventMinimumCooldown = 60f; // �C�x���g�I����̍Œ�N�[���_�E������

    private Transform playerTransform;
    private bool eventActive = false;
    private RandomEvent currentActiveEvent;
    private List<GameObject> spawnedEventEntities = new List<GameObject>();
    private GameObject currentEventAreaIndicator;
    private AudioSource eventAudioSource;
    private GameObject currentEnvironmentEffect;
    private float lastEventEndTime = -Mathf.Infinity; // �Ō�ɃC�x���g���I����������

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
            Debug.LogWarning("Player��������܂���BPlayer�^�O���t���Ă��邩�m�F���Ă��������B�����_���C�x���g�͔������܂���B", this);
            if (eventActive) EndEvent();
            return;
        }

        // �V�[�����[�h���Ɍ��݃A�N�e�B�u�ȃC�x���g������ΏI��������
        if (eventActive)
        {
            EndEvent(); // �V�[�����ׂ��C�x���g�͔񐄏�
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
            if (Time.time < lastEventEndTime + eventMinimumCooldown) continue; // �N�[���_�E����

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
            Debug.LogWarning("�����_���C�x���g�̃p�^�[�����ݒ肳��Ă��܂���B", this);
            return;
        }

        // �d�݂Ɋ�Â��ăC�x���g��I��
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

        Debug.Log("�����_���C�x���g�����I: " + currentActiveEvent.eventName + " - " + currentActiveEvent.eventDescription, this);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEventMessage(currentActiveEvent.eventName, currentActiveEvent.eventDescription, 5f);
        }

        // �C�x���gBGM�Đ�
        if (currentActiveEvent.eventMusic != null)
        {
            eventAudioSource.clip = currentActiveEvent.eventMusic;
            eventAudioSource.volume = currentActiveEvent.eventMusicVolume;
            eventAudioSource.Play();
        }

        Vector3 spawnCenter = playerTransform.position; // �v���C���[�̌��ݒn�𒆐S�Ƃ���

        // �C�x���g�͈̓C���W�P�[�^�[�̐���
        if (currentActiveEvent.eventAreaIndicatorPrefab != null)
        {
            currentEventAreaIndicator = Instantiate(currentActiveEvent.eventAreaIndicatorPrefab, spawnCenter, Quaternion.identity);
            // �K�v�ɉ����ăX�P�[������: currentEventAreaIndicator.transform.localScale = Vector3.one * currentActiveEvent.spawnRadius * 2;
        }

        spawnedEventEntities.Clear();

        // ���G�t�F�N�g�̓K�p
        if (currentActiveEvent.environmentEffectPrefab != null)
        {
            currentEnvironmentEffect = Instantiate(currentActiveEvent.environmentEffectPrefab, playerTransform.position, Quaternion.identity);
            currentEnvironmentEffect.transform.SetParent(playerTransform); // �v���C���[�ɒǏ]
        }

        // �v���C���[�̈ړ����x�Ɉꎞ�I�ȉe��
        if (currentActiveEvent.playerMoveSpeedMultiplier != 1f && playerTransform != null)
        {
            PlayerController playerController = playerTransform.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.moveSpeed *= currentActiveEvent.playerMoveSpeedMultiplier;
                playerController.sprintSpeedMultiplier *= currentActiveEvent.playerMoveSpeedMultiplier;
                Debug.Log($"�v���C���[�̈ړ����x�� {currentActiveEvent.playerMoveSpeedMultiplier} �{�ɂȂ�܂����B");
            }
        }

        // �G�̐���
        SpawnEntities(currentActiveEvent.enemyPrefabsToSpawn, currentActiveEvent.minEnemyCount, currentActiveEvent.maxEnemyCount, spawnCenter, currentActiveEvent.spawnRadius, true);
        // �F�D�INPC�̐���
        SpawnEntities(currentActiveEvent.friendlyNPCPrefabsToSpawn, 1, 1, spawnCenter, currentActiveEvent.spawnRadius, false); // ��{1��
        // �󏭎����m�[�h�̐���
        SpawnEntities(currentActiveEvent.resourceNodePrefabsToSpawn, currentActiveEvent.minResourceNodeCount, currentActiveEvent.maxResourceNodeCount, spawnCenter, currentActiveEvent.spawnRadius, false);


        // �C�x���g�̌p�����Ԃ��Ď�
        float startTime = Time.time;
        while (Time.time < startTime + currentActiveEvent.eventDuration)
        {
            // �C�x���g�Ő������ꂽ�G�̐����`�F�b�N
            spawnedEventEntities.RemoveAll(item => item == null);
            bool enemiesRemaining = false;
            foreach (var entity in spawnedEventEntities)
            {
                if (entity != null && entity.CompareTag("Enemy")) // Enemy�^�O�������̂̂݃J�E���g
                {
                    enemiesRemaining = true;
                    break;
                }
            }

            if (!enemiesRemaining && currentActiveEvent.enemyPrefabsToSpawn.Length > 0)
            {
                Debug.Log("�C�x���g�Ő������ꂽ�G��S�ē|���܂����I�C�x���g�I���B");
                break; // �G��S�ē|�����狭���I��
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
                        // �v���C���[���x���ɉ������G�̃X�e�[�^�X���� (EnemyStatsScaler�N���X�Ȃǂ��g��)
                        // EnemyStatsScaler.ScaleEnemy(spawnedEntity, playerTransform.GetComponent<PlayerController>().GetPlayerLevel());
                    }
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddActiveEnemy(spawnedEntity.GetComponent<EnemyHealth>());
                    }
                }
                else
                {
                    // �F�D�INPC�⎑���m�[�h�̈ꎞ�I�ȐU�镑����ݒ�
                    // ��: �����m�[�h�ɏ��Ń^�C�}�[��t�^
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
        Debug.LogWarning("NavMesh��̃����_���Ȉʒu��������܂���ł����B", this);
        return Vector3.zero;
    }

    void EndEvent()
    {
        if (!eventActive) return;

        Debug.Log("�����_���C�x���g�I���B", this);
        eventActive = false;
        lastEventEndTime = Time.time;

        // �C�x���g�͈̓C���W�P�[�^�[��j��
        if (currentEventAreaIndicator != null)
        {
            Destroy(currentEventAreaIndicator);
            currentEventAreaIndicator = null;
        }

        // ���G�t�F�N�g������
        if (currentEnvironmentEffect != null)
        {
            Destroy(currentEnvironmentEffect);
            currentEnvironmentEffect = null;
        }

        // �v���C���[�̈ړ����x�����ɖ߂�
        if (currentActiveEvent.playerMoveSpeedMultiplier != 1f && playerTransform != null)
        {
            PlayerController playerController = playerTransform.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.moveSpeed /= currentActiveEvent.playerMoveSpeedMultiplier;
                playerController.sprintSpeedMultiplier /= currentActiveEvent.playerMoveSpeedMultiplier;
                Debug.Log("�v���C���[�̈ړ����x�����ɖ߂�܂����B");
            }
        }

        // �������ꂽ�G���e�B�e�B��S�Ĕj��
        foreach (GameObject entity in spawnedEventEntities)
        {
            if (entity != null)
            {
                Destroy(entity);
            }
        }
        spawnedEventEntities.Clear();

        // �C�x���gBGM��~
        if (eventAudioSource.isPlaying)
        {
            eventAudioSource.Stop();
        }

        // �C�x���g�I����̒Z���ԃo�t�E�f�o�t�̓K�p (�I�v�V����)
        if (currentActiveEvent.temporaryBuffDurationAfterEvent > 0 && playerTransform != null)
        {
            // ��: UIManager.Instance.ShowMessage("�C�x���g�N���A�{�[�i�X�I", 2f);
            // playerTransform.GetComponent<PlayerController>().ApplyTemporaryBuff(BuffType.AttackBoost, currentActiveEvent.temporaryBuffDurationAfterEvent);
            Debug.Log($"�C�x���g�I����A{currentActiveEvent.temporaryBuffDurationAfterEvent}�b�Ԃ̈ꎞ�I�ȉe����K�p���܂��B");
        }

        currentActiveEvent = null;
    }
}