using System;
using DG.Tweening.Plugins;
using UnityEngine;

public static class KeyCount
{
    static int[] counts;
    static bool initialized;
    static int maxCount = 5;

    public static event Action<int, int> OnChanged;

    public static void Init(int length, int defaultCount, int maxcount = 5)
    {
        maxCount = Mathf.Max(1, maxcount);

        if (initialized && counts != null && counts.Length == length) return;

        counts = new int[length];
        for (int i = 0; i < length; i++)
        {
            counts[i] = Mathf.Max(0, defaultCount);
        }
        initialized = true;

        for (int i = 0; i < length; i++) OnChanged?.Invoke(i, counts[i]);
    }

    public static bool isReady => initialized && counts != null;
    public static int Length => counts?.Length ?? 0;
    public static int MaxCount => maxCount; 

    public static int Get(int index)
    {
        if(!isReady || index < 0 || index >= counts.Length)
        {
            return 0;
        }
        return counts[index];
    }

    public static void Set(int index, int value)
    {
        if (!isReady || index < 0 || index >= counts.Length)
        {
            return;
        }
        counts[index] = Mathf.Max(0, value);
    }

    public static bool TryConsume(int index, int amount)
    {
        if (!isReady || index < 0 || index >= counts.Length) return false;
        if (amount <= 0) return true;

        if (counts[index] < amount) return false;

        counts[index] -= amount;                
        OnChanged?.Invoke(index, counts[index]); 
        return true;
    }

    public static void AddAt(int index, int add)
    {
        if (!isReady || index < 0 || index >= counts.Length) return;
        if (add == 0) return;

        int prev = counts[index];
        int next = Mathf.Clamp(prev + add, 0, maxCount);
        if (next == prev) return;
        counts[index] = next;
        OnChanged?.Invoke(index, counts[index]);
    }

    public static void AddRandom(int amount)
    {
        if (!isReady || amount <= 0 || counts.Length == 0) return;

        for (int i = 0; i < amount; i++)
        {
            bool hasRoom = false;
            for (int k = 0; k < counts.Length; k++)
                if (counts[k] < maxCount) { hasRoom = true; break; }
            if (!hasRoom) break;

            for (int guard = 0; guard < 16; guard++)
            {
                int idx = UnityEngine.Random.Range(0, counts.Length);
                if (counts[idx] >= maxCount) continue;
                counts[idx]++;
                OnChanged?.Invoke(idx, counts[idx]);
                break;
            }
        }
    }
}
