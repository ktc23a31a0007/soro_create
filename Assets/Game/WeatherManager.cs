using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum WeatherType { Clear, Rain, Foggy, Thunderstorm, Snow }

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("�V��ݒ�")]
    public WeatherType currentWeather = WeatherType.Clear;
    public float weatherChangeIntervalMinutes = 15f; // �V�󂪕ω�����Ԋu�i���j

    [Header("�V��G�t�F�N�g")]
    public GameObject rainEffectPrefab; // �J�̃p�[�e�B�N���V�X�e���Ȃ�
    public GameObject fogEffectPrefab; // ���̃p�[�e�B�N���V�X�e���Ȃ�
    // ���̑��AThunderstorm, Snow�Ȃǂ̃G�t�F�N�g

    private GameObject currentActiveWeatherEffect;

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
        StartCoroutine(WeatherChangeRoutine());
    }

    void Update()
    {
        // �v���C���[�̎��E��ړ����x�ȂǂɓV��̉e����^���� (��: PlayerController��EnemyAI���ŎQ��)
        // if (currentWeather == WeatherType.Foggy)
        // {
        //     // PlayerController.Instance.AdjustSightRange(0.5f); // �v���C���[�̎��E�𔼌�
        // }
    }

    IEnumerator WeatherChangeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(weatherChangeIntervalMinutes * 60f);

            // ���̓V��������_���ɑI���i�m�����l�����Ă��ǂ��j
            WeatherType nextWeather = GetRandomNextWeather();
            SetWeather(nextWeather);
        }
    }

    WeatherType GetRandomNextWeather()
    {
        // �����œV��J�ڂ̃��W�b�N����蕡�G�ɂł��� (��: Thunderstorm�̌��Clear�ɂȂ�₷���Ȃ�)
        int randomVal = Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length);
        return (WeatherType)randomVal;
    }

    public void SetWeather(WeatherType newWeather)
    {
        if (currentWeather == newWeather) return;

        currentWeather = newWeather;
        Debug.Log($"�V�� {currentWeather} �ɕω����܂����B");
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"�V��: {currentWeather}", 2f);

        // ���݂̃G�t�F�N�g���~/�j��
        if (currentActiveWeatherEffect != null)
        {
            Destroy(currentActiveWeatherEffect);
            currentActiveWeatherEffect = null;
        }

        // �V�����V��̃G�t�F�N�g�𐶐�
        switch (currentWeather)
        {
            case WeatherType.Rain:
                if (rainEffectPrefab != null) currentActiveWeatherEffect = Instantiate(rainEffectPrefab, Vector3.zero, Quaternion.identity);
                break;
            case WeatherType.Foggy:
                if (fogEffectPrefab != null) currentActiveWeatherEffect = Instantiate(fogEffectPrefab, Vector3.zero, Quaternion.identity);
                // Post-processing Volume��؂�ւ���Ȃǂ��L��
                break;
                // ���̓V��^�C�v...
        }

        if (currentActiveWeatherEffect != null)
        {
            // �V��G�t�F�N�g���v���C���[�̃J�����̎q�ɂ���Ȃǂ��ĒǏ]������
            // currentActiveWeatherEffect.transform.SetParent(Camera.main.transform);
            // currentActiveWeatherEffect.transform.localPosition = Vector3.zero;
        }
    }
}