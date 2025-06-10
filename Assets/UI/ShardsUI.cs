using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShardsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI shardsText;

    private void Start()
    {
        ShardManager.Instance.onShardsChanged.AddListener(UpdateShardsUI);

        // Update UI initially
        UpdateShardsUI(ShardManager.Instance.ShardCount());
    }

    private void UpdateShardsUI(int shardCount)
    {
        if (shardsText != null)
        {
            shardsText.text = $"x {shardCount}";
        }
    }
}
