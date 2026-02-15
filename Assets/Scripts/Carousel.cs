using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Carousel : MonoBehaviour, IEndDragHandler
{
    [Header("Parts Setup")]
    [SerializeField] private List<GameObject> entries = new List<GameObject>();

    [Space]
    [SerializeField] private ScrollRect scrollRect;

    [Space]
    [SerializeField] private List<CarouselIndicator> indicators = new List<CarouselIndicator>();

    [Header("Animation Setup")]
    [SerializeField, Range(0.25f, 1f)] private float duration = 0.5f;
    [SerializeField] private AnimationCurve easeCurve;

    [Header("Auto Scroll Setup")]
    [SerializeField] private bool autoScroll = false;
    [SerializeField] private float autoScrollInterval = 5f;
    private float _autoScrollTimer;

    private int _currentIndex = 0;
    private Coroutine _scrollCoroutine;

    private void Reset()
    {
        scrollRect = GetComponentInChildren<ScrollRect>();
    }

    private void Start()
    {
        indicators[0].Activate(0.1f);
        _autoScrollTimer = autoScrollInterval;

        for  (int i = 0; i < indicators.Count; i++)
        {
            indicators[i].Initialize(() => ScrollToIndex(i));
        }
    }

    private void ClearCurrentIndex()
    {
        indicators[_currentIndex].Deactivate(duration);
    }

    private void ScrollToIndex(int index)
    {
        ClearCurrentIndex();
        ScrollTo(index);
    }

    private void ScrollTo(int index)
    {
        _currentIndex = index;
        _autoScrollTimer = autoScrollInterval;
        float targetHorizontalPos = (float)_currentIndex / (entries.Count - 1);

        if (_scrollCoroutine != null)
        {
            StopCoroutine(_scrollCoroutine);
        }

        _scrollCoroutine = StartCoroutine(LerpToPos(targetHorizontalPos));

        indicators[_currentIndex].Activate(duration);
    }

    public void ScrollToNext()
    {
        ClearCurrentIndex();
        _currentIndex = (_currentIndex + 1) % entries.Count;
        ScrollTo(_currentIndex);
    }

    public void ScrollToPrevious()
    {
        ClearCurrentIndex();
        _currentIndex = (_currentIndex - 1) % entries.Count;
        ScrollTo(_currentIndex);
    }

    private IEnumerator LerpToPos(float targetPos)
    {
        float elapsedTime = 0f;
        float initialPos = scrollRect.horizontalNormalizedPosition;

        if (duration > 0)
        {
            while (elapsedTime <= duration)
            {
                float easeValue = easeCurve.Evaluate(elapsedTime / duration);
                float newPos = Mathf.Lerp(initialPos, targetPos, easeValue);
                scrollRect.horizontalNormalizedPosition = newPos;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        scrollRect.horizontalNormalizedPosition = targetPos;
    }

    private void Update()
    {
        if (!autoScroll)
        {
            return;
        }

        _autoScrollTimer -= Time.deltaTime;
        if (_autoScrollTimer <= 0)
        {
            ScrollToNext();
            _autoScrollTimer = autoScrollInterval;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.delta.x != 0)
        {
            if (eventData.delta.x > 0)
            {
                ScrollToPrevious();
            }
            else if (eventData.delta.x < 0)
            {
                ScrollToNext();
            }
        }
        else
        {
            ScrollToIndex(_currentIndex);
        }
    }
}
