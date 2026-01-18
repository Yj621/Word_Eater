using UnityEngine;
using System.IO;
using System.Collections.Generic;
using static FileManager;
using WordEater.Systems;
using System;

/// <summary>
/// 게임 전체 데이터(진행도, 사운드, 도감)를 파일로 저장하고 불러오는 통합 관리자
/// </summary>
public class FileManager : MonoBehaviour
{
    // 파일로 저장할 애들 ( 껏다 켜도 유지될 애들 )

    // 워드이터 진행도
    // 정답 단어
    // 아이템
    // 자모음 개수
    // 오디오 볼륨
    // 히스토리
    // 배경화면

    // ++ 튜토리얼 여부
    

    public static FileManager Instance { get; private set; }
    public string CurrentPlayerName { get; private set; } = "워드이터";
    public event Action<string> OnNameChanged;

    [Header("Scene References")]
    public WordEater.Core.WordEater wordeater;
    public SoundManager soundmanager;
    public GameManager gamemanager;

    [Header("Data Cache")]
    public GalleryData galleryData = new GalleryData(); // 도감 데이터 메모리 캐시

    // 배터리 데이터 메모리 캐시
    public BatteryData batteryData = new BatteryData();

    // 인벤토리 데이터 캐시
    public InventoryData inventoryData = new InventoryData();

    // --- 데이터 클래스 정의 ---
    [System.Serializable]
    public class WordEaterData
    {
        public int Level;
        public string Answer;
        public string History;
        public string RelevantLine;
        public List<string> Relevant; //관련 단어
        public string Name;
        public string ImgId;
        public List<int> KeyCounts; // [추가] 자모음 개수 저장
        public int MaxKeyCount; // [추가] 자모음 최대 개수 저장
        public bool LockLength;
        public bool LockFirst;
        public bool LockLast;
    }

    [System.Serializable]
    public class SoundData
    {
        public float BGM;
        public float SE;
    }

    [System.Serializable]
    public class CountData
    {
        public int call;
        public int msg;
        public int submit;
        public int lockc;
    }

    [System.Serializable]
    public class BatteryData
    {
        public int SavedBattery = 100;    // 저장된 배터리 잔량
        public string ExitTime = "";      // 나간 시간 (Binary String)
        public bool IsFirstRun = true;    // 첫 실행 여부
    }

    // 아이템 저장용 클래스
    [System.Serializable]
    public class ItemSaveEntry
    {
        public ItemType type;
        public int count;
    }

    [System.Serializable]
    public class InventoryData
    {
        public List<ItemSaveEntry> items = new List<ItemSaveEntry>();
    }

    // --- 파일 경로 프로퍼티 ---
    string SoundPath => Path.Combine(Application.persistentDataPath, "sound.json");
    string WordEaterPath => Path.Combine(Application.persistentDataPath, "WordEaterInfo.json");
    string GalleryPath => Path.Combine(Application.persistentDataPath, "gallery.json");
    string BatteryPath => Path.Combine(Application.persistentDataPath, "battery.json");
    string InventoryPath => Path.Combine(Application.persistentDataPath, "inventory.json");
    string countPath => Path.Combine(Application.persistentDataPath, "count.json");
    private void Awake()
    {
        // 싱글톤 설정 (필요하다면 DontDestroyOnLoad 사용, 여기서는 씬 내 관리자로 가정)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 시작 시 모든 데이터 로드
        LoadAllData();
    }

    public void LoadAllData()
    {
        LoadSoundInfo();
        LoadWordEaterInfo();
        LoadGallery();
        LoadBatteryInfo();
        LoadInventory(); // [추가] 인벤토리 로드 누락된 부분 추가
    }

    // ========================================================================
    // [Part 1] 워드이터 게임 데이터 (레벨, 정답, 히스토리)
    // ========================================================================
    public void SaveWordEaterInfo(int le, string an, string hi, List<string> RR , string id , string RRL,bool LLen,bool LF, bool LLast)
    {
        WordEaterData data = new WordEaterData
        {
            Level = le,
            Answer = an,
            History = hi,
            Relevant = RR,
            RelevantLine = RRL,
            Name = CurrentPlayerName,
            ImgId = id,
            // [추가] 현재 KeyCount 상태 저장
            KeyCounts = new List<int>(KeyCount.GetAllCounts()),
            MaxKeyCount = KeyCount.MaxCount,
            // [추가] 히스토리에 Lock에서 본 정보 저장
            LockLength = LLen,
            LockFirst = LF,
            LockLast = LLast
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(WordEaterPath, json);
    }

    /// <summary>
    /// [추가] 다른 데이터는 건드리지 않고, 현재 자모(키) 개수와 최대 용량만 갱신하여 저장함
    /// (아이템 사용, 자모 소모 후 즉시 저장용)
    /// </summary>
    public void UpdateAndSaveKeyCounts()
    {
        if (!File.Exists(WordEaterPath)) return;

        try 
        {
            // 1. 기존 데이터 읽기
            string json = File.ReadAllText(WordEaterPath);
            WordEaterData data = JsonUtility.FromJson<WordEaterData>(json);
            
            // 2. 키 데이터만 최신값으로 덮어쓰기
            data.KeyCounts = new List<int>(KeyCount.GetAllCounts());
            data.MaxKeyCount = KeyCount.MaxCount;

            // 3. 다시 저장
            File.WriteAllText(WordEaterPath, JsonUtility.ToJson(data, true));
            Debug.Log("[FileManager] 자모 데이터 부분 저장 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FileManager] 자모 저장 실패: {e.Message}");
        }
    }

    public void SaveLockHistoryInfo() {
        try
        {
            // 1. 기존 데이터 읽기
            string json = File.ReadAllText(WordEaterPath);
            WordEaterData data = JsonUtility.FromJson<WordEaterData>(json);

            // 2. Lock 데이터만 최신값으로 덮어쓰기
            data.LockLength = gamemanager.isLength;
            data.LockFirst = gamemanager.isFirst;
            data.LockLast = gamemanager.isLast;

            // 3. 다시 저장
            File.WriteAllText(WordEaterPath, JsonUtility.ToJson(data, true));

        }
        catch (System.Exception e)
        {
        }
    }

    public void LoadWordEaterInfo()
    {
        if (!File.Exists(WordEaterPath))
        {
            if (wordeater != null)
                wordeater.BeginStage(wordeater.CurrentStage, initial: true);
            return;
        }

        string json = File.ReadAllText(WordEaterPath);
        WordEaterData data = JsonUtility.FromJson<WordEaterData>(json);

        // 이름 로드 (비어있으면 기본값)
        if (!string.IsNullOrEmpty(data.Name))
            CurrentPlayerName = data.Name;
        else
            CurrentPlayerName = "워드이터";

        if (wordeater != null) { 
            wordeater.LoadFromSaveData(data.Level, data.Answer);
            wordeater.wordImgString = data.ImgId;
        }
        if (gamemanager != null)
        {
            gamemanager.HistoryLIne = data.History;
            gamemanager.RelevantResult = data.Relevant;
            gamemanager.RelevantLine = data.RelevantLine;
            gamemanager.isLength = data.LockLength;
            gamemanager.isFirst = data.LockFirst;
            gamemanager.isLast = data.LockLast;
        }

        // [추가] 로드된 키 데이터를 임시 보관 또는 즉시 적용
        // KeyBoardManager가 아직 Init 안됐을 수 있으므로 여기서는 프로퍼티에 들고 있거나
        // KeyBoardManager가 Start에서 FileManager를 참조해서 가져가도록 함
        tempLoadedKeyCounts = data.KeyCounts;
        tempLoadedMaxKeyCount = data.MaxKeyCount > 0 ? data.MaxKeyCount : 5;
    }

    // KeyBoardManager가 씬 로드 후 가져갈 데이터
    public List<int> tempLoadedKeyCounts;
    public int tempLoadedMaxKeyCount = 5;

    /// <summary>
    /// 외부에서 이름을 변경할 때 호출 (변경 후 저장까지 수행)
    /// </summary>
    public void SetPlayerName(string newName)
    {
        CurrentPlayerName = newName;

        // 이름만 바뀐 시점에 저장이 필요하다면, 현재 상태를 읽어와서 저장
        if (wordeater != null && gamemanager != null)
        {
            string json = File.ReadAllText(WordEaterPath);
            WordEaterData data = JsonUtility.FromJson<WordEaterData>(json);
            data.Name = newName;

            File.WriteAllText(WordEaterPath, JsonUtility.ToJson(data, true));
        }
        OnNameChanged?.Invoke(CurrentPlayerName);
    }

    public void SaveRelevant(List<string> Rel)
    {
        if (!File.Exists(WordEaterPath)) return;

        string json = File.ReadAllText(WordEaterPath);
        WordEaterData data = JsonUtility.FromJson<WordEaterData>(json);
        data.Relevant = Rel;

        File.WriteAllText(WordEaterPath, JsonUtility.ToJson(data, true));
    }

    public void SavaRelevantLine(string RRL) {
        if (!File.Exists(WordEaterPath)) return;

        string json = File.ReadAllText(WordEaterPath);
        WordEaterData data = JsonUtility.FromJson<WordEaterData>(json);
        data.RelevantLine = RRL;

        File.WriteAllText(WordEaterPath, JsonUtility.ToJson(data, true));
    }




    public void SaveHistory(string newHis)
    {
        if (!File.Exists(WordEaterPath)) return;

        string json = File.ReadAllText(WordEaterPath);
        WordEaterData data = JsonUtility.FromJson<WordEaterData>(json);
        data.History = newHis;

        File.WriteAllText(WordEaterPath, JsonUtility.ToJson(data, true));
    }


    // ========================================================================
    // 이용 횟수 데이터
    // ========================================================================

    public void SaveCountData(int callc , int msgc, int submitc , int lockcc) {
        CountData data = new CountData { call = callc, msg = msgc, submit = submitc, lockc = lockcc };
        File.WriteAllText(countPath, JsonUtility.ToJson(data, true));
    }

    public void LoadCountData() {
        if (!File.Exists(countPath))
        {
            return;
        }
        string json = File.ReadAllText(countPath);
        CountData data = JsonUtility.FromJson<CountData>(json);

        gamemanager.callCount = data.call;
        gamemanager.msgCount = data.msg;
        gamemanager.submitCount = data.submit;
        gamemanager.lockCount = data.lockc;
    }

    // ========================================================================
    // [Part 2] 사운드 데이터
    // ========================================================================

    public void SaveSoundInfo(float bgmVolume, float seVolume)
    {
        SoundData data = new SoundData { BGM = bgmVolume, SE = seVolume };
        File.WriteAllText(SoundPath, JsonUtility.ToJson(data, true));
    }

    public void LoadSoundInfo()
    {
        if (soundmanager == null) return;

        if (!File.Exists(SoundPath))
        {
            // 파일이 없으면 초기화
            soundmanager.SetBGMVolume(1f);
            soundmanager.SetSFXVolume(1f);
            return;
        }

        string json = File.ReadAllText(SoundPath);

        // [중요] 변환하기 전에 먼저 비어있는지 확인해야 합니다!
        if (string.IsNullOrEmpty(json) || json == "{}" || json.Trim() == "")
        {
            Debug.LogWarning("저장된 사운드 데이터가 비어있습니다. 기본값으로 초기화합니다.");
            soundmanager.SetBGMVolume(1f);
            soundmanager.SetSFXVolume(1f);
            return; // 여기서 함수 종료
        }

        // 데이터가 안전하다는 것이 확인된 후 변환 시도
        try
        {
            SoundData data = JsonUtility.FromJson<SoundData>(json);

            // 데이터 적용
            soundmanager.SetBGMVolume(data.BGM);
            soundmanager.SetSFXVolume(data.SE);

            if (soundmanager.bgmSlider != null) soundmanager.bgmSlider.value = data.BGM;
            if (soundmanager.seSlider != null) soundmanager.seSlider.value = data.SE;
        }
        catch (System.Exception e)
        {
            // 혹시 모를 깨진 파일 에러 방지
            Debug.LogError("사운드 데이터 파싱 오류: " + e.Message);
            soundmanager.SetBGMVolume(1f);
            soundmanager.SetSFXVolume(1f);
        }
    }

    // ========================================================================
    // [Part 3] 도감(Gallery) 데이터
    // ========================================================================

    public void LoadGallery()
    {
        if (File.Exists(GalleryPath))
        {
            var json = File.ReadAllText(GalleryPath);
            galleryData = JsonUtility.FromJson<GalleryData>(json) ?? new GalleryData();
        }
        else
        {
            galleryData = new GalleryData();
        }
    }

    public string LoadGallerySpriteId() {
        string rets = "";
        if (File.Exists(GalleryPath))
        {

            var json = File.ReadAllText(GalleryPath);
            galleryData = JsonUtility.FromJson<GalleryData>(json) ?? new GalleryData();

            foreach (var item in galleryData.items) {
                rets += item.spriteid;
            }
        }
        else
        {
            galleryData = new GalleryData();
        }

        return rets;
    }

    public void SaveGallery()
    {
        var json = JsonUtility.ToJson(galleryData, true);
        File.WriteAllText(GalleryPath, json);
    }

    /// <summary>
    /// 도감에 아이템을 추가하거나 업데이트합니다.
    /// </summary>
    public void UpsertGalleryItem(GalleryItem item)
    {
        // 이미 있는지 확인
        var idx = galleryData.items.FindIndex(x => x.id == item.id);

        if (idx >= 0)
        {
            // 이미 있으면 만난 횟수 증가
            galleryData.items[idx].meetCount += 1;

            // 필요하다면 날짜 등 최신 정보로 갱신 가능
            // galleryData.items[idx].dateCaught = item.dateCaught; 
        }
        else
        {
            // 없으면 새로 추가
            item.meetCount = 1;
            galleryData.items.Add(item);
        }

        SaveGallery();
    }

    /// <summary>
    /// 도감 데이터 및 관련 이미지 파일을 모두 삭제합니다.
    /// </summary>
    public void ClearGalleryData()
    {
        // 메모리 비우기
        galleryData.items.Clear();
        SaveGallery();

        // 썸네일 파일들 삭제
        string[] thumbs = Directory.GetFiles(Application.persistentDataPath, "thumb_*.png");
        foreach (var path in thumbs)
        {
            File.Delete(path);
            Debug.Log($"[FileManager] 삭제: {path}");
        }

        Debug.Log("[FileManager] 도감 데이터 초기화 완료");
    }


    // ========================================================================
    // [Part 4] 배터리 
    // ======================
    public void LoadBatteryInfo()
    {
        if (File.Exists(BatteryPath))
        {
            string json = File.ReadAllText(BatteryPath);
            batteryData = JsonUtility.FromJson<BatteryData>(json) ?? new BatteryData();
        }
        else
        {
            // 파일 없으면 기본값 (100%, 현재시간, 첫실행 True)
            batteryData = new BatteryData();
            batteryData.SavedBattery = 100;
            batteryData.ExitTime = System.DateTime.UtcNow.ToBinary().ToString();
            batteryData.IsFirstRun = true;
        }
    }

    public void SaveBatteryInfo(int battery, string time, bool firstRun)
    {
        BatteryData data = new BatteryData
        {
            SavedBattery = battery,
            ExitTime = time,
            IsFirstRun = firstRun
        };

        // 메모리 업데이트
        batteryData = data;

        // 파일 저장
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(BatteryPath, json);
    }


    // ========================================================================
    // [Part 5] 아이템(Inventory) 데이터 - [새로 추가된 부분]
    // ========================================================================

    public void LoadInventory()
    {
        if (File.Exists(InventoryPath))
        {
            string json = File.ReadAllText(InventoryPath);
            inventoryData = JsonUtility.FromJson<InventoryData>(json) ?? new InventoryData();
        }
        else
        {
            inventoryData = new InventoryData();
        }
    }

    public void SaveInventory()
    {
        string json = JsonUtility.ToJson(inventoryData, true);
        File.WriteAllText(InventoryPath, json);
    }

    /// <summary>
    /// 외부(ItemManager)에서 개수를 조회할 때 사용
    /// </summary>
    public int GetItemCount(ItemType type)
    {
        var item = inventoryData.items.Find(x => x.type == type);
        return item != null ? item.count : 0;
    }

    /// <summary>
    /// 외부(ItemManager)에서 아이템 개수를 변경할 때 사용 (증가/감소 통합)
    /// </summary>
    public void UpdateItemCount(ItemType type, int delta)
    {
        var item = inventoryData.items.Find(x => x.type == type);

        if (item != null)
        {
            item.count += delta;
            // 개수가 0 미만이면 0으로 보정 (선택사항)
            if (item.count < 0) item.count = 0;
        }
        else if (delta > 0)
        {
            // 없는데 추가하는 경우 새로 생성
            inventoryData.items.Add(new ItemSaveEntry { type = type, count = delta });
        }

        // 변경 즉시 저장
        SaveInventory();
    }

    /// <summary>
    /// 게임의 모든 데이터(진행도, 사운드, 도감, 배터리)를 초기화하고 파일을 삭제합니다.
    /// </summary>
    public void ClearAllData()
    {
        Debug.Log("[FileManager] 모든 데이터 초기화 시작...");

        // JSON 파일들 삭제
        DeleteFileIfExists(WordEaterPath);
        DeleteFileIfExists(SoundPath);
        DeleteFileIfExists(GalleryPath);
        DeleteFileIfExists(BatteryPath);
        DeleteFileIfExists(InventoryPath);

        // 썸네일 이미지들 삭제
        string[] thumbs = Directory.GetFiles(Application.persistentDataPath, "thumb_*.png");
        foreach (var path in thumbs)
        {
            File.Delete(path);
        }

        // 메모리 데이터 초기화
        galleryData = new GalleryData();

        batteryData = new BatteryData();
        batteryData.SavedBattery = 100;
        batteryData.IsFirstRun = true;
        batteryData.ExitTime = System.DateTime.UtcNow.ToBinary().ToString();
        inventoryData = new InventoryData();

        // 인게임 상태 즉시 리셋 (게임 재시작 없이 반영하고 싶은 경우)

        // [사운드]
        if (soundmanager != null)
        {
            soundmanager.SetBGMVolume(1f);
            soundmanager.SetSFXVolume(1f);
            if (soundmanager.bgmSlider != null) soundmanager.bgmSlider.value = 1f;
            if (soundmanager.seSlider != null) soundmanager.seSlider.value = 1f;
        }

        // [히스토리]
        if (gamemanager != null)
        {
            gamemanager.HistoryLIne = "";
        }

        // [워드이터 본체] - 완전 초기화 상태로 되돌리기
        if (wordeater != null)
        {
            wordeater.BeginStage(WordEater.Core.GrowthStage.Bit, initial: true);
        }

        // [배터리] - 배터리 시스템은 FileManager에 직접 연결이 안되어 있으므로
        // 보통은 여기서 씬을 재로딩(SceneManager.LoadScene)하는 것이 가장 깔끔합니다.
        // 만약 즉시 반영하려면 BatterySystem.Instance.RefillToMax() 같은 걸 호출해야 합니다.

        Debug.Log("[FileManager] 모든 데이터가 초기화되었습니다.");

        // (선택사항) 깔끔하게 모든 시스템(배터리 포함)을 리셋하기 위해 현재 씬 재시작
        // UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 파일이 존재하면 삭제하는 헬퍼 함수
    /// </summary>
    private void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

}