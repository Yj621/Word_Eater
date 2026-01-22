using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance; // 싱글톤 인스턴스

    public FileManager filemanager;


    [SerializeField] private AudioMixer m_AudioMixer;
    [SerializeField] private Slider m_MusicBGMSlider;
    [SerializeField] private Slider m_MusicSFXSlider;

    public Image BGMFillImg;
    public Image SFXFillImg;

    public AudioSource bgmSource;
    public AudioSource SFXSource;

    public Image BGMicon;
    public Image SFXicon;

    public Sprite BGMiconOn;
    public Sprite BGMiconOff;
    public Sprite SFXiconOn;
    public Sprite SFXiconOff;

    public float Bvalue;
    public float Svalue;

    public Slider bgmSlider;
    public Slider seSlider;

    // 브금
    public AudioClip MainBGM1;
    public AudioClip MainBGM2;
    public AudioClip MainBGM3;

    //효과음
    public List<AudioClip> sfxList;
    public enum SFXType 
    {
        temp, //0번 소리 = 아직 없는 소리 임시 등록
        startScene, 
        tutoClick,
        iconMove,
        keyboardOpen,
        keyboardClose,
        jaMoDrag,
        trashcan,
        upAlram,
        popup,
        miniGame,
        call,
        scrollView,
        msgPopup,
        dead,
        wrongAnswer,
        DogamAssign,
        sucess
    }

    private void Awake()
    {
        // 씬에 이미 Instance가 있으면 자신을 파괴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        Instance = this;             // 싱글톤 등록
        DontDestroyOnLoad(gameObject); // 씬 전환 시 유지


        m_MusicBGMSlider.onValueChanged.AddListener(SetBGMVolume);
        m_MusicSFXSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetBGMVolume(float volume) {
        Color c = BGMFillImg.color;

        if (volume <= 0.01f)
        {
            m_AudioMixer.SetFloat("BGM", -80f);
            c.a = 0f;
        }
        else
        {
            m_AudioMixer.SetFloat("BGM", Mathf.Log10(volume) * 20);
            c.a = 1f;
        }

        BGMFillImg.color = c;

        if (volume <= 0.01f)
            BGMicon.sprite = BGMiconOff;
        else
            BGMicon.sprite = BGMiconOn;
            
        Bvalue = volume;

        filemanager.SaveSoundInfo(volume, Svalue);

    }

    public void SetSFXVolume(float volume)
    {
        Color c = SFXFillImg.color;


        if (volume <= 0.01f)
        {
            m_AudioMixer.SetFloat("SFX", -80f);
            c.a = 0f;
        }
        else
        {
            m_AudioMixer.SetFloat("SFX", Mathf.Log10(volume) * 20);
            c.a = 1f;
        }

        SFXFillImg.color = c;

        if (volume <= 0.01f)
            SFXicon.sprite = SFXiconOff;
        else
            SFXicon.sprite = SFXiconOn;

        Svalue = volume;

        filemanager.SaveSoundInfo(Svalue, volume);
    }


    public void Bbtn() {
        // 꺼져 있었을 때
        if (m_MusicBGMSlider.value <= 0.01f) m_MusicBGMSlider.value = Bvalue;

        // 안꺼져 있었을 때
        else m_MusicBGMSlider.value = 0.01f;
    }

    public void Sbtn()
    {
        // 꺼져 있었을 때
        if (m_MusicSFXSlider.value <= 0.01f) m_MusicSFXSlider.value = Svalue;

        // 안꺼져 있었을 때
        else m_MusicSFXSlider.value = 0.01f;
    }


    public void BGMStart(int BGMType) {
        if (bgmSource.isPlaying)
            bgmSource.Stop();

        switch (BGMType)
        {
            case 1:
                bgmSource.clip = MainBGM1;
                break;

            case 2:
                bgmSource.clip = MainBGM2;
                break;

            case 3:
                bgmSource.clip = MainBGM3;
                break;

            default:
                break;
        }

        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void SFXStart(SFXType type)
    {
        int index = (int)type;
        if (index < sfxList.Count)
        {
                SFXSource.PlayOneShot(sfxList[index]);
        }

    }

    // 전화 효과음 출력 함수
    // 브금이랑 같이 들려도 되나 싶긴 한데 브금 잠깐 꺼놓으면 진짜 전화라고 생각할듯?
    public void SFXCall() {
        SFXSource.clip = sfxList[11];
        SFXSource.loop = true;
        SFXSource.Play();
    }
    // 전화 효과음 종료 함수
    public void SFXCallClose() {
        SFXSource.Stop();
        SFXSource.loop = false;
    }
}
