using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Inspector에서 대사 목록을 구성하는 컴포넌트(기본 대사 매니저의 데이터).
/// 몇 개의 대사를, 어떤 데이터로 출력할지 씬에서 직접 편집한다.
/// </summary>
public class StoryDialogueSequence : MonoBehaviour
{
    [SerializeField] private List<StoryDialogueLine> lines = new List<StoryDialogueLine>();
    [SerializeField] private UnityEvent onSequenceFinished = new UnityEvent();

    public List<StoryDialogueLine> Lines => lines;
    public bool HasLines => lines != null && lines.Count > 0;

    public void Play() => Play(null);

    public void Play(Action onComplete)
    {
        StoryDialogueView.GetOrCreate().Play(lines, () =>
        {
            onSequenceFinished?.Invoke();
            onComplete?.Invoke();
        });
    }

    /// <summary>대사가 비어 있을 때 코드에서 기본 대사를 채운다(씬에서 편집한 값이 있으면 유지).</summary>
    public void FillIfEmpty(List<StoryDialogueLine> defaults)
    {
        if (HasLines || defaults == null) return;
        lines = defaults;
    }
}
