using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum WeatherType { Clear, Rain, Foggy, Thunderstorm, Snow }

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("天候設定")]
    public WeatherType currentWeather = WeatherType.Clear;
    public float weatherChangeIntervalMinutes = 15f; // 天候が変化する間隔（分）

    [Header("天候エフェクト")]
    public GameObject rainEffectPrefab; // 雨のパーティクルシステムなど
    public GameObject fogEffectPrefab; // 霧のパーティクルシステムなど
    // その他、Thunderstorm, Snowなどのエフェクト

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
        // プレイヤーの視界や移動速度などに天候の影響を与える (例: PlayerControllerやEnemyAI内で参照)
        // if (currentWeather == WeatherType.Foggy)
        // {
        //     // PlayerController.Instance.AdjustSightRange(0.5f); // プレイヤーの視界を半減
        // }
    }

    IEnumerator WeatherChangeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(weatherChangeIntervalMinutes * 60f);

            // 次の天候をランダムに選択（確率を考慮しても良い）
            WeatherType nextWeather = GetRandomNextWeather();
            SetWeather(nextWeather);
        }
    }

    WeatherType GetRandomNextWeather()
    {
        // ここで天候遷移のロジックをより複雑にできる (例: Thunderstormの後はClearになりやすいなど)
        int randomVal = Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length);
        return (WeatherType)randomVal;
    }

    public void SetWeather(WeatherType newWeather)
    {
        if (currentWeather == newWeather) return;

        currentWeather = newWeather;
        Debug.Log($"天候が {currentWeather} に変化しました。");
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"天候: {currentWeather}", 2f);

        // 現在のエフェクトを停止/破棄
        if (currentActiveWeatherEffect != null)
        {
            Destroy(currentActiveWeatherEffect);
            currentActiveWeatherEffect = null;
        }

        // 新しい天候のエフェクトを生成
        switch (currentWeather)
        {
            case WeatherType.Rain:
                if (rainEffectPrefab != null) currentActiveWeatherEffect = Instantiate(rainEffectPrefab, Vector3.zero, Quaternion.identity);
                break;
            case WeatherType.Foggy:
                if (fogEffectPrefab != null) currentActiveWeatherEffect = Instantiate(fogEffectPrefab, Vector3.zero, Quaternion.identity);
                // Post-processing Volumeを切り替えるなども有効
                break;
                // 他の天候タイプ...
        }

        if (currentActiveWeatherEffect != null)
        {
            // 天候エフェクトをプレイヤーのカメラの子にするなどして追従させる
            // currentActiveWeatherEffect.transform.SetParent(Camera.main.transform);
            // currentActiveWeatherEffect.transform.localPosition = Vector3.zero;
        }
    }
}