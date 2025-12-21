using UnityEngine;
using System.IO;
public class FileManager : MonoBehaviour
{
    // 파일로 저장할 애들 ( 껏다 켜도 유지될 애들 )

    // 워드이터 진행도
    // 정답 단어
    // 아이템
    // 자모음 개수
    // 오디오 볼륨
    // 히스토리
    // 배경화면 << O

    public WordEater.Core.WordEater wordeater;
    public SoundManager soundmanager;
    public GameManager gamemanager;


    [System.Serializable]
    public class WordEaterData
    {
        public int Level;
        public string Answer;
        public string History;
    }


    [System.Serializable]
    public class SoundData
    {
        public float BGM;
        public float SE;
    }

    [System.Serializable]
    public class ItemData
    {

    }


    // 경로 및 파일 이름 설정//
    string SoundPath =>
        Path.Combine(Application.persistentDataPath, "sound.json");
    string ItemPath =>
    Path.Combine(Application.persistentDataPath, "Item.json");

    string WordEaterPath =>
        Path.Combine(Application.persistentDataPath, "WordEaterInfo.json");


    public void SaveWordEaterInfo(int le, string an, string hi) { // 워드이터 진행도, 정답 단어 , 히스토리
        WordEaterData data = new WordEaterData
        {
            Level = le,
            Answer = an,
            History = hi
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(WordEaterPath, json);
    }

    public void RoadWordEaterInfo() {
        //파일이 없다는 건 처음 실행다는 것
        if (!File.Exists(WordEaterPath))
        {
            // 초기 실행
            wordeater.BeginStage(wordeater.ReturnStage(), initial: true);
            return;
        }

        string json = File.ReadAllText(WordEaterPath);
        WordEaterData data = JsonUtility.FromJson<WordEaterData>(json);

        wordeater.SetWordEaterFormFile(data.Level,data.Answer);
        gamemanager.HistoryLIne = data.History;
    }


    public void SaveHistory(string newHis) {
        if (!File.Exists(WordEaterPath))
        {
            return;
        }

        string json = File.ReadAllText(WordEaterPath);
        WordEaterData data = JsonUtility.FromJson<WordEaterData>(json);
        data.History = newHis;

        string json2 = JsonUtility.ToJson(data, true);
        File.WriteAllText(WordEaterPath, json2);
    }


    public void SaveSoundInfo(float bgmVolume, float seVolume) { // 오디오 볼륨 (BGM, SE)
        SoundData data = new SoundData
        {
            BGM = bgmVolume,
            SE = seVolume
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SoundPath, json);
    }

    public void RoadSoundInfo() {
        //파일이 없다는 건 처음 실행했거나 건든적이 없을 때
        if (!File.Exists(SoundPath))
        {
            // 초기값을 넣고 파일 저장까지
            soundmanager.SetBGMVolume(1f);
            soundmanager.SetSFXVolume(1f);
            return;
        }


        string json = File.ReadAllText(SoundPath);
        SoundData data = JsonUtility.FromJson<SoundData>(json);

        soundmanager.SetBGMVolume(data.BGM);
        soundmanager.SetSFXVolume(data.SE);

        soundmanager.bgmSlider.value = data.BGM;
        soundmanager.seSlider.value = data.SE;
    }



    public void SaveItemInfo() { // 아이템, 자모음 개수
    
    }

    public void RoadItemInfo()
    {

    }
}
