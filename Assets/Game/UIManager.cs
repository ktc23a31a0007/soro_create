using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // TextMeshProを使用する場合

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI要素の参照")]
    public Slider healthBar;
    public Slider staminaBar;
    public TextMeshProUGUI messageText; // メッセージ表示用テキスト
    public CanvasGroup messagePanel; // メッセージパネル（フェードイン/アウト用）
    public GameObject damageOverlayPanel; // ダメージを受けた時のオーバーレイ
    public GameObject gameOverPanel; // ゲームオーバーパネル
    public GameObject pauseMenuPanel; // ポーズメニューパネル

    [Header("デバフアイコン")]
    public Transform debuffIconParent; // デバフアイコンを配置する親オブジェクト
    public GameObject debuffIconPrefab; // デバフアイコンのプレハブ（画像とテキストなどを含む）
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

        // UI初期設定
        if (messagePanel != null) messagePanel.alpha = 0; // 透明にしておく
        if (damageOverlayPanel != null) damageOverlayPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    // HPバーの更新
    public void UpdateHealthBar(float current, float max)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value = current;
        }
    }

    // スタミナバーの更新
    public void UpdateStaminaBar(float current, float max)
    {
        if (staminaBar != null)
        {
            staminaBar.maxValue = max;
            staminaBar.value = current;
        }
    }

    // メッセージの表示
    public void ShowMessage(string message, float duration = 3f)
    {
        if (messageText != null && messagePanel != null)
        {
            messageText.text = message;
            StopAllCoroutines(); // 既存のメッセージ表示コルーチンを停止
            StartCoroutine(FadeMessagePanel(1, duration)); // フェードインして表示
        }
    }

    // イベントメッセージの表示 (RandomEventManagerから呼ばれる)
    public void ShowEventMessage(string eventName, string description, float duration = 5f)
    {
        string fullMessage = $"<size=24>{eventName}</size>\n<size=18>{description}</size>";
        ShowMessage(fullMessage, duration);
    }

    IEnumerator FadeMessagePanel(float targetAlpha, float duration)
    {
        float startAlpha = messagePanel.alpha;
        float timer = 0;

        // フェードイン
        while (timer < 0.5f) // 例: 0.5秒でフェードイン
        {
            timer += Time.deltaTime;
            messagePanel.alpha = Mathf.Lerp(startAlpha, 1, timer / 0.5f);
            yield return null;
        }
        messagePanel.alpha = 1;

        yield return new WaitForSeconds(duration); // 表示維持

        // フェードアウト
        timer = 0;
        startAlpha = messagePanel.alpha;
        while (timer < 0.5f) // 例: 0.5秒でフェードアウト
        {
            timer += Time.deltaTime;
            messagePanel.alpha = Mathf.Lerp(startAlpha, 0, timer / 0.5f);
            yield return null;
        }
        messagePanel.alpha = 0;
    }

    // ダメージオーバーレイの表示
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

    // デバフアイコンの表示
    public void ShowDebuffIcon(string debuffName, Sprite iconSprite = null)
    {
        if (!activeDebuffIcons.ContainsKey(debuffName) && debuffIconPrefab != null && debuffIconParent != null)
        {
            GameObject icon = Instantiate(debuffIconPrefab, debuffIconParent);
            // icon.GetComponent<Image>().sprite = iconSprite; // アイコン画像を設定
            // icon.GetComponentInChildren<TextMeshProUGUI>().text = debuffName; // デバフ名を設定
            activeDebuffIcons.Add(debuffName, icon);
        }
    }

    // デバフアイコンの非表示
    public void HideDebuffIcon(string debuffName)
    {
        if (activeDebuffIcons.ContainsKey(debuffName))
        {
            Destroy(activeDebuffIcons[debuffName]);
            activeDebuffIcons.Remove(debuffName);
        }
    }

    // ゲームオーバー画面の表示
    public void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            // プレイヤー入力を停止するなど
            Time.timeScale = 0f; // ゲームを完全に停止
        }
    }

    // ポーズメニューの表示/非表示
    public void TogglePauseMenu(bool isActive)
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isActive);
        }
    }

    // 仮のボタンコールバック (実際はUnityUIのイベントシステムで設定)
    public void OnResumeButtonClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.TogglePauseGame();
    }

    public void OnExitButtonClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.QuitGame();
    }
}