using System;
using System.Collections.Generic;

[Serializable]
public class GalleryData
{
    public List<GalleryItem> items = new();
}

[Serializable]
public class GalleryItem
{
    public string id;              // 종/개체 식별자 (예: currentEntry.topic + "-" + word)
    public string displayName;     // 도감 이름
    public string desc;            // 설명 (원하면)
    public string thumbPath;       // 썸네일 PNG 파일 경로 (persistentDataPath 하위)
    public string dateCaught;      // 잡은 날짜 (yyyy-MM-dd)
    public int meetCount;          // 만난 횟수 (중복 등록 시 +=1)
}
