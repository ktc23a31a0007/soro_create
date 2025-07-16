using UnityEngine;

public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance { get; private set; }

    [Header("昼夜サイクル設定")]
    public Light directionalLight; // シーンのDirectional Lightをアサイン
    public float dayDurationMinutes = 10f; // ゲーム内の1日の長さ（分）
    public Gradient skyColorGradient; // 時間帯ごとの空の色
    public Gradient equatorColorGradient; // 時間帯ごとの地平線色
    public Gradient groundColorGradient; // 時間帯ごとの地面色
    public AnimationCurve lightIntensityCurve; // 時間帯ごとのライト強度

    private float _currentTimeOfDay = 0f; // 0.0 (真夜中) から 1.0 (次の真夜中)
    private float _timeMultiplier = 1f; // 昼夜サイクルの進行速度

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
        // ゲームがポーズ中は時間を進めない
        if (GameManager.Instance != null && GameManager.Instance.isGamePaused) return;

        _currentTimeOfDay += (Time.deltaTime / (dayDurationMinutes * 60f)) * _timeMultiplier;
        if (_currentTimeOfDay >= 1f)
        {
            _currentTimeOfDay -= 1f; // 1.0を超えたらリセット
            Debug.Log("新しい日を迎えました！");
        }

        UpdateLighting();
    }

    void UpdateLighting()
    {
        if (directionalLight == null) return;

        // ライトの回転 (Y軸を中心に回転させて太陽/月を表現)
        // 0.25 = 朝、0.5 = 昼、0.75 = 夕方、0.0/1.0 = 夜
        directionalLight.transform.localRotation = Quaternion.Euler((_currentTimeOfDay * 360f) - 90f, 170f, 0);

        // 環境光の設定 (Rendering SettingsのEnvironment Lighting)
        RenderSettings.ambientSkyColor = skyColorGradient.Evaluate(_currentTimeOfDay);
        RenderSettings.ambientEquatorColor = equatorColorGradient.Evaluate(_currentTimeOfDay);
        RenderSettings.ambientGroundColor = groundColorGradient.Evaluate(_currentTimeOfDay);

        // ライトの強度
        directionalLight.intensity = lightIntensityCurve.Evaluate(_currentTimeOfDay);

        // ポストプロセッシングのプロファイル切り替えなどもここで行える
    }

    // 昼夜サイクルの進行速度を調整
    public void SetTimeMultiplier(float multiplier)
    {
        _timeMultiplier = multiplier;
    }
}