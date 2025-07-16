using UnityEngine;
using System.Collections;

public class ResourceNode : MonoBehaviour
{
    [Header("資源ノード設定")]
    public string resourceItemName = "木材"; // 採集できるアイテム名
    public int minQuantity = 1; // 採集できる最小数量
    public int maxQuantity = 3; // 採集できる最大数量
    public float respawnTime = 60f; // リスポーンまでの時間 (秒)
    public GameObject harvestEffectPrefab; // 採集時のエフェクト

    private bool isDepleted = false;
    private Renderer nodeRenderer; // ノードの見た目を切り替えるため
    private Collider nodeCollider; // ノードの当たり判定を切り替えるため

    void Awake()
    {
        nodeRenderer = GetComponent<Renderer>();
        nodeCollider = GetComponent<Collider>();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.F) && !isDepleted)
        {
            Harvest();
        }
    }

    void Harvest()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManagerが見つかりません！");
            return;
        }

        isDepleted = true;
        nodeRenderer.enabled = false; // 見た目を非表示にする
        nodeCollider.enabled = false; // 当たり判定を無効にする

        if (harvestEffectPrefab != null)
        {
            Instantiate(harvestEffectPrefab, transform.position, Quaternion.identity);
        }

        int quantity = Random.Range(minQuantity, maxQuantity + 1);
        if (InventoryManager.Instance.AddItem(resourceItemName, quantity))
        {
            Debug.Log($"{resourceItemName} を {quantity} 個採集しました。");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"{resourceItemName} +{quantity}", 1.5f);

            // 経験値付与 (PlayerStatsに直接アクセス)
            PlayerStats playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddExperience(quantity * 5); // 例: 採集量に応じて経験値
            }
        }
        else
        {
            // インベントリがいっぱいで追加できなかった場合
            // アイテムを地面にドロップさせるなどの処理も検討
            Debug.LogWarning("インベントリがいっぱいでアイテムを追加できませんでした。");
        }

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        isDepleted = false;
        nodeRenderer.enabled = true; // 見た目を再表示
        nodeCollider.enabled = true; // 当たり判定を有効にする
        Debug.Log($"{resourceItemName} ノードがリスポーンしました。");
    }

    // RandomEventManagerから一時的なリソースノードとして生成された場合の消滅タイマー
    public void SetTemporary(float duration)
    {
        StartCoroutine(TemporaryDisappearRoutine(duration));
    }

    IEnumerator TemporaryDisappearRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (this != null) // イベント終了時に既に破壊されている可能性もあるためチェック
        {
            Destroy(gameObject);
            Debug.Log($"{resourceItemName} (一時的なノード) が時間切れで消滅しました。");
        }
    }
}