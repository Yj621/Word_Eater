using UnityEngine;
using UnityEngine.UI;

public class PairSlot : MonoBehaviour
{
    [Header("UI")]
    public Button button;
    public Image icon;              // 슬롯에 표시될 이미지 (앞/뒤 공통)

    [Header("Sprites")]
    public Sprite backSprite;       // 가려진 상태 이미지(뒷면)

    int _pairId = -1;
    Sprite _frontSprite;
    MatchPair _manager;

    bool _revealed;
    bool _matched;

    public int PairId => _pairId;
    public bool IsRevealed => _revealed;
    public bool IsMatched => _matched;


    void Reset()
    {
        button = GetComponent<Button>();
        icon = GetComponent<Image>();
    }

    void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (button) button.onClick.AddListener(OnClick);
    }

    public void Init(int pairId, Sprite frontSprite, Sprite back, MatchPair manager)
    {
        _pairId = pairId;
        _frontSprite = frontSprite;
        backSprite = back;
        _manager = manager;

        _matched = false;
        HideInstant();
        SetInteractable(true);
    }

    void OnClick()
    {
        if (_matched || _revealed) return;
        
        // [수정] 직접 Reveal 하지 않고 매니저에게 요청
        // Reveal(); 
        // _manager.OnSlotRevealed(this);

        if (_manager != null)
        {
            _manager.TrySelectSlot(this);
        }
    }

    public void Reveal()
    {
        _revealed = true;
        if (icon) icon.sprite = _frontSprite;
    }

    public void Hide()
    {
        _revealed = false;
        if (icon) icon.sprite = backSprite;
    }

    public void HideInstant() => Hide();

    public void SetMatched(bool matched)
    {
        _matched = matched;
        SetInteractable(!matched);
    }

    public void SetInteractable(bool on)
    {
        if (button) button.interactable = on;
    }
    public void ResetVisual(Sprite back)
    {
        _matched = false;
        _revealed = false;
        _pairId = -1;
        _frontSprite = null;

        backSprite = back;

        if (icon) icon.sprite = backSprite;
        if (button) button.interactable = true;
    }

}
