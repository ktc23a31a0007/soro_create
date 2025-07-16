using UnityEngine;

public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance { get; private set; }

    [Header("����T�C�N���ݒ�")]
    public Light directionalLight; // �V�[����Directional Light���A�T�C��
    public float dayDurationMinutes = 10f; // �Q�[������1���̒����i���j
    public Gradient skyColorGradient; // ���ԑт��Ƃ̋�̐F
    public Gradient equatorColorGradient; // ���ԑт��Ƃ̒n�����F
    public Gradient groundColorGradient; // ���ԑт��Ƃ̒n�ʐF
    public AnimationCurve lightIntensityCurve; // ���ԑт��Ƃ̃��C�g���x

    private float _currentTimeOfDay = 0f; // 0.0 (�^�钆) ���� 1.0 (���̐^�钆)
    private float _timeMultiplier = 1f; // ����T�C�N���̐i�s���x

    public float CurrentTimeOfDayNormalized => _currentTimeOfDay;

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

    void Update()
    {
        // �Q�[�����|�[�Y���͎��Ԃ�i�߂Ȃ�
        if (GameManager.Instance != null && GameManager.Instance.isGamePaused) return;

        _currentTimeOfDay += (Time.deltaTime / (dayDurationMinutes * 60f)) * _timeMultiplier;
        if (_currentTimeOfDay >= 1f)
        {
            _currentTimeOfDay -= 1f; // 1.0�𒴂����烊�Z�b�g
            Debug.Log("�V���������}���܂����I");
        }

        UpdateLighting();
    }

    void UpdateLighting()
    {
        if (directionalLight == null) return;

        // ���C�g�̉�] (Y���𒆐S�ɉ�]�����đ��z/����\��)
        // 0.25 = ���A0.5 = ���A0.75 = �[���A0.0/1.0 = ��
        directionalLight.transform.localRotation = Quaternion.Euler((_currentTimeOfDay * 360f) - 90f, 170f, 0);

        // �����̐ݒ� (Rendering Settings��Environment Lighting)
        RenderSettings.ambientSkyColor = skyColorGradient.Evaluate(_currentTimeOfDay);
        RenderSettings.ambientEquatorColor = equatorColorGradient.Evaluate(_currentTimeOfDay);
        RenderSettings.ambientGroundColor = groundColorGradient.Evaluate(_currentTimeOfDay);

        // ���C�g�̋��x
        directionalLight.intensity = lightIntensityCurve.Evaluate(_currentTimeOfDay);

        // �|�X�g�v���Z�b�V���O�̃v���t�@�C���؂�ւ��Ȃǂ������ōs����
    }

    // ����T�C�N���̐i�s���x�𒲐�
    public void SetTimeMultiplier(float multiplier)
    {
        _timeMultiplier = multiplier;
    }
}