using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchPair : MonoBehaviour
{
    [Header("Slots (8개)")]
    public PairSlot[] slots;

    [Header("Images")]
    public List<Sprite> imagePool;
    public Sprite backSprite;

    [Header("Rule")]
    public int pairCount = 4;
    public float mismatchHideDelay = 0.6f;

    PairSlot _first;
    PairSlot _second;
    bool _busy;
    Coroutine _checkCo;

    MiniGameHook _hook;

    private void Awake()
    {
        // 같은 루트에 붙어있다는 가정이지만, 없을 수도 있으니 부모도 탐색
        _hook = GetComponent<MiniGameHook>();
        if (_hook == null) _hook = GetComponentInParent<MiniGameHook>();
    }

    private void OnEnable()
    {
        ResetGame();   // 미니게임 패널이 켜질 때마다 항상 새로 시작
    }

    private void OnDisable()
    {
        CleanupRuntime(); // 코루틴/상태만 정리(슬롯은 굳이 초기화 안 해도 됨)
    }

    /// <summary>
    /// 외부(컨트롤러)에서 필요하면 호출할 수 있는 리셋 API
    /// </summary>
    public void ResetGame()
    {
        CleanupRuntime();
        SetupGame();
    }

    void CleanupRuntime()
    {
        if (_checkCo != null)
        {
            StopCoroutine(_checkCo);
            _checkCo = null;
        }
        StopAllCoroutines();

        _first = null;
        _second = null;
        _busy = false;
    }

    public void SetupGame()
    {
        if (slots == null || slots.Length == 0)
        {
            // Debug.LogError("[MatchPair] slots가 비어있어.");
            _hook?.ReportFail();
            return;
        }

        int neededSlots = pairCount * 2;

        if (imagePool == null || imagePool.Count < pairCount)
        {
            // Debug.LogError("[MatchPair] imagePool이 부족해.");
            _hook?.ReportFail();
            return;
        }

        // 0) 슬롯 전부 켜고 "초기 상태"로 만들기
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i]) continue;
            slots[i].gameObject.SetActive(true);

            // PairSlot 쪽에 이런 Reset 함수가 있으면 가장 좋음(아래 설명 참고)
            slots[i].ResetVisual(backSprite);
        }

        // 1) pool에서 중복 없이 pairCount개 뽑기
        List<Sprite> picked = PickRandomSprites(imagePool, pairCount);

        // 2) (id, sprite) 2개씩 만들기
        var deck = new List<(int id, Sprite sprite)>(pairCount * 2);
        for (int i = 0; i < picked.Count; i++)
        {
            deck.Add((i, picked[i]));
            deck.Add((i, picked[i]));
        }

        // 3) 셔플
        Shuffle(deck);

        // 4) 슬롯에 배치
        int fill = Mathf.Min(slots.Length, deck.Count);

        for (int i = 0; i < fill; i++)
        {
            slots[i].Init(deck[i].id, deck[i].sprite, backSprite, this);
            slots[i].Hide();          // 시작은 무조건 뒤집힌 상태
            slots[i].SetMatched(false);
            slots[i].SetInteractable(true);
        }

        // 남는 슬롯이 있다면 끄기
        for (int i = fill; i < slots.Length; i++)
        {
            if (slots[i]) slots[i].gameObject.SetActive(false);
        }
    }

    public void OnSlotRevealed(PairSlot slot)
    {
        if (_busy) return;
        if (slot == null) return;
        if (slot.IsMatched) return;

        // 같은 슬롯 두 번 클릭 방지
        if (_first == slot) return;

        if (_first == null)
        {
            _first = slot;
            return;
        }

        if (_second == null)
        {
            _second = slot;

            // 두 개 공개됐으니 더 이상 클릭 못하게 잠깐 막기
            _busy = true;

            if (_checkCo != null) StopCoroutine(_checkCo);
            _checkCo = StartCoroutine(CheckMatchRoutine());
        }
    }

    IEnumerator CheckMatchRoutine()
    {
        // 혹시 모를 경우
        if (_first == null || _second == null)
        {
            ResetPick();
            _busy = false;
            yield break;
        }

        // 매칭 성공
        if (_first.PairId == _second.PairId)
        {
            _first.SetMatched(true);
            _second.SetMatched(true);

            _first.SetInteractable(false);
            _second.SetInteractable(false);

            ResetPick();
            _busy = false;

            if (IsAllMatched())
            {
                _hook?.ReportClear();
            }
            yield break;
        }

        // 매칭 실패 → 잠깐 보여주고 다시 가림
        yield return new WaitForSeconds(mismatchHideDelay);

        if (_first) _first.Hide();
        if (_second) _second.Hide();

        ResetPick();
        _busy = false;
    }

    void ResetPick()
    {
        _first = null;
        _second = null;
    }

    bool IsAllMatched()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i] || !slots[i].gameObject.activeSelf) continue;
            if (!slots[i].IsMatched) return false;
        }
        return true;
    }

    // -------------------------
    // Utils
    // -------------------------

    List<Sprite> PickRandomSprites(List<Sprite> pool, int count)
    {
        var temp = new List<Sprite>(pool);
        var result = new List<Sprite>(count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}
