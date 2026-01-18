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
    public string displayNameBit;  
    public string displayNameByte; 
    public string displayNameWord;

    public int callCount;
    public int msgCount;
    public int submitCount;
    public int lockCount;
    
    public string desc;            // 설명 (원하면)
    public string thumbPath;       // 썸네일 PNG 파일 경로 (persistentDataPath 하위)
    public string dateCaught;      // 잡은 날짜 (yyyy-MM-dd)
    public int meetCount;          // 만난 횟수 (중복 등록 시 +=1)
    public string spriteid;        // 도감을 위한 스프라이트 아이디
}
