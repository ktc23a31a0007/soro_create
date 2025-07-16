using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class WarpPoint : MonoBehaviour
{
    [Header("ワープポイント設定")]
    public string destinationSceneName; // ワープ先のシーン名
    public Vector3 destinationPosition; // ワープ先の座標
    public bool requireUnlock = false; // 解放条件が必要か
    public string unlockConditionFlag = ""; // GameManagerのworldFlagsでチェックするフラグ名 (例: "Boss_ForestGuardian_Defeated")

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.G)) // Gキーでワープ
            {
                AttemptWarp();
            }
        }
    }

    void AttemptWarp()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerが見つかりません！");
            return;
        }

        if (GameManager.Instance.isInCombat)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("戦闘中はワープできません！", 2f);
            Debug.LogWarning("戦闘中はワープできません。");
            return;
        }
        if (GameManager.Instance.isGamePaused)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ポーズ中はワープできません！", 2f);
            Debug.LogWarning("ポーズ中はワープできません。");
            return;
        }

        if (requireUnlock)
        {
            if (string.IsNullOrEmpty(unlockConditionFlag))
            {
                Debug.LogWarning("ワープポイントの解放条件フラグが設定されていません。", this);
                if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ワープポイントが設定されていません。", 2f);
                return;
            }

            if (!GameManager.Instance.GetWorldFlag(unlockConditionFlag))
            {
                Debug.Log($"このワープポイントはまだ解放されていません。条件: {unlockConditionFlag}");
                if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ワープポイントはロックされています。", 2f);
                return;
            }
        }

        Debug.Log($"ワープ開始: シーン {destinationSceneName}, 位置 {destinationPosition}");
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ワープ中...", 1f);

        // シーン遷移とプレイヤーの位置設定
        StartCoroutine(LoadSceneAndMovePlayer(destinationSceneName, destinationPosition));
    }

    IEnumerator LoadSceneAndMovePlayer(string sceneName, Vector3 targetPosition)
    {
        SceneManager.LoadScene(sceneName); // シーンをロード

        // シーンロードが完了するまで待つ
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);

        // プレイヤーオブジェクトを見つけて位置を設定
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            // CharacterControllerを使用している場合、直接transform.positionは推奨されないため、
            // Disableして移動後Enableするか、CharacterController.Move()を使う
            player.characterController.enabled = false;
            player.transform.position = targetPosition;
            player.characterController.enabled = true;
            Debug.Log("プレイヤーをワープ先に移動しました。");
        }
        else
        {
            Debug.LogError("プレイヤーが見つかりません！ワープ先への移動に失敗しました。");
        }
    }
}