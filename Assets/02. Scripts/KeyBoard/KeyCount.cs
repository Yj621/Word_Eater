using DG.Tweening.Plugins;
using UnityEngine;

public static class KeyCount
{
    static int[] counts;
    static bool initialized;

    public static void Init(int length, int defaultCount)
    {
        if (initialized && counts != null && counts.Length == length) return;
        counts = new int[length];
        for (int i = 0; i < length; i++)
        {
            counts[i] = Mathf.Max(0, defaultCount);
        }
        initialized = true;
    }

    public static bool isReady => initialized && counts != null;
    public static int Length => counts?.Length ?? 0;

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
        if (!isReady || index < 0 || index >= counts.Length)
        {
            return false;
        }
        if (counts[index] < amount)
        {
            return false;
        }
        return true;
    }

    public static void AddAt(int index, int add)
    {
        if(!isReady || index < 0 || index >= counts.Length)
        {
            return;
        }  
        counts[index] = Mathf.Max(0, counts[index] + add);
    }
    public static void AddRandom(int amount)
    {
        if (!isReady || amount <= 0 || counts.Length == 0) return;
        for (int i = 0; i < amount; i++)
        {
            int idx = UnityEngine.Random.Range(0, counts.Length);
            counts[idx]++;
        }
    }
}
