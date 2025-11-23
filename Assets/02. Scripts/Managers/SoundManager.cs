using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance; // 싱글톤 인스턴스

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

    // 브금
    public AudioClip MainBGM; 

    //효과음

    private void Awake()
    {
        // 씬에 이미 Instance가 있으면 자신을 파괴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;             // 싱글톤 등록
        DontDestroyOnLoad(gameObject); // 씬 전환 시 유지


        m_MusicBGMSlider.onValueChanged.AddListener(SetBGMVolume);
        m_MusicSFXSlider.onValueChanged.AddListener(SetSFXVolume);


        Bvalue = m_MusicBGMSlider.value;
        Svalue = m_MusicSFXSlider.value;
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

        if(volume > 0.01f) Bvalue = volume;
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

        if (volume > 0.01f) Svalue = volume;
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
        switch (BGMType)
        {
            case 1:
                bgmSource.clip = MainBGM;
                bgmSource.loop = true;
                bgmSource.Play();
                break;

            default:
                break;
        }

    }

    public void SFXStart(int SFXType)
    {
        switch (SFXType)
        {
            case 1:
                //SFXSource.PlayOneShot(SFX01);
                break;

            default:
                break;
        }

    }
}
