using UnityEngine;
using System.IO;

public class GalleryStore : MonoBehaviour
{
    public static GalleryStore Instance { get; private set; }
    public GalleryData Data { get; set; } = new();

    string JsonPath => Path.Combine(Application.persistentDataPath, "gallery.json");

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        Load();
    }

    public void Load()
    {
        if (File.Exists(JsonPath))
        {
            var json = File.ReadAllText(JsonPath);
            Data = JsonUtility.FromJson<GalleryData>(json) ?? new GalleryData();
        }
    }

    public void Save()
    {
        var json = JsonUtility.ToJson(Data, prettyPrint: true);
        File.WriteAllText(JsonPath, json);
    }

    public void Upsert(GalleryItem item)
    {
        var idx = Data.items.FindIndex(x => x.id == item.id);
        if (idx >= 0)
        {
            // 이미 있으면 카운트만 증가
            Data.items[idx].meetCount += 1;
        }
        else
        {
            item.meetCount = 1;
            Data.items.Add(item);
        }
        Save();
    }

    public void ClearAll()
    {
        // 메모리 데이터 비우기
        Data.items.Clear();

        // JSON 파일도 덮어쓰기
        Save();

        // 썸네일 파일들 삭제
        string[] thumbs = Directory.GetFiles(Application.persistentDataPath, "thumb_*.png");
        foreach (var path in thumbs)
        {
            File.Delete(path);
            Debug.Log($"삭제: {path}");
        }

        Debug.Log("GalleryStore: 모든 데이터 초기화 완료");
    }

}
