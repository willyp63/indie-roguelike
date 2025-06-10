using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ShardManager : Singleton<ShardManager>
{
    private int shardCount = 0;

    [NonSerialized]
    public UnityEvent<int> onShardsChanged = new();

    public int ShardCount()
    {
        return shardCount;
    }

    public void AddShards(int amount)
    {
        if (amount > 0)
        {
            shardCount += amount;
            onShardsChanged?.Invoke(shardCount);
            Debug.Log($"Added {amount} shards. Total: {shardCount}");
        }
    }

    public bool SpendShards(int amount)
    {
        if (amount > 0 && HasEnoughShards(amount))
        {
            shardCount -= amount;
            onShardsChanged?.Invoke(shardCount);
            Debug.Log($"Spent {amount} shards. Remaining: {shardCount}");
            return true;
        }

        Debug.LogWarning($"Cannot spend {amount} shards. Current balance: {shardCount}");
        return false;
    }

    public bool HasEnoughShards(int amount)
    {
        return shardCount >= amount;
    }
}
