using System;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 말풍선(태클 말풍선, 대사창, 일반 말풍선)에 Spine 애니메이션을 입히는 공용 컴포넌트.
/// Spine 데이터가 없거나 애니메이션 이름이 없으면 아무것도 하지 않아서, 지금의 이미지 말풍선이 그대로 동작한다.
///
/// Spine 쪽 애니메이션 이름 규칙(대소문자 구분, 없는 건 건너뜀):
///   appear    : 등장(한 번)            → 끝나면 idle
///   idle      : 평소 꿈틀거림(반복)     ← 꼬리 끝이 꿈틀거리는 기본 루프
///   emphasis  : 강조 대사(한 번)        → 끝나면 idle
///   angry     : 분노 대사(반복)
///   gloomy    : 우울 대사(반복)
///   pop       : 태클을 물리쳐 터질 때(한 번)
///   disappear : 그냥 사라질 때(한 번)
///
/// 연결: 말풍선 오브젝트 아래에 Spine 오브젝트(UI면 SkeletonGraphic, 월드면 SkeletonAnimation 렌더러)를 두고,
///       이 컴포넌트를 말풍선 루트에 붙인 뒤 Spine Animation 칸에 그 오브젝트를 넣는다.
///       Hide When Ready에 기존 말풍선 이미지를 넣으면 Spine이 준비됐을 때 자동으로 숨긴다.
/// 자세한 내용은 SPINE_BUBBLE_GUIDE.md.
/// </summary>
public class SpineBubbleSkin : MonoBehaviour
{
    [Tooltip("Spine 애니메이션 컴포넌트(SkeletonAnimation). 비우면 자식에서 찾는다")]
    [SerializeField] private SkeletonAnimation spineAnimation;
    [Tooltip("UI(SkeletonGraphic)일 때 색을 입힐 그래픽. 비우면 자식에서 찾는다")]
    [SerializeField] private SkeletonGraphic spineGraphic;

    [Header("Animation Names (Spine에서 만든 이름과 똑같이)")]
    [SerializeField] private string appearAnimation = "appear";
    [SerializeField] private string idleAnimation = "idle";
    [SerializeField] private string emphasisAnimation = "emphasis";
    [SerializeField] private string angryAnimation = "angry";
    [SerializeField] private string gloomyAnimation = "gloomy";
    [SerializeField] private string popAnimation = "pop";
    [SerializeField] private string disappearAnimation = "disappear";
    [Tooltip("애니메이션이 바뀔 때 섞는 시간(초)")]
    [SerializeField, Min(0f)] private float mixDuration = 0.12f;

    [Header("Replace")]
    [Tooltip("Spine이 준비되면 숨길 기존 UI 그래픽(말풍선 이미지 등)")]
    [SerializeField] private Graphic[] hideWhenReady = new Graphic[0];
    [Tooltip("Spine이 준비되면 숨길 기존 월드 렌더러(SpriteRenderer 등)")]
    [SerializeField] private Renderer[] hideRenderersWhenReady = new Renderer[0];
    [Header("Style Tint (Spine 그림에 곱해지는 색, 흰색 = 원래 색)")]
    [Tooltip("대사 분류에 따라 색을 입힌다")]
    [SerializeField] private bool applyTint = true;
    [SerializeField] private Color normalTint = Color.white;
    [SerializeField] private Color angryTint = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color gloomyTint = new Color(0.65f, 0.72f, 0.9f, 1f);

    private bool resolved;

    /// <summary>Spine 데이터가 연결돼 있고 재생할 수 있는 상태.</summary>
    public bool IsReady
    {
        get
        {
            Resolve();
            return spineAnimation != null && spineAnimation.isActiveAndEnabled && State != null;
        }
    }

    private Spine.AnimationState State => spineAnimation != null ? spineAnimation.AnimationState : null;

    private void Awake() => Resolve();

    private void Resolve()
    {
        if (resolved) return;
        resolved = true;

        if (spineAnimation == null) spineAnimation = GetComponentInChildren<SkeletonAnimation>(true);
        if (spineGraphic == null) spineGraphic = GetComponentInChildren<SkeletonGraphic>(true);
        if (spineAnimation != null && spineAnimation.AnimationState == null) spineAnimation.Initialize(false, true);

        bool ready = spineAnimation != null && spineAnimation.AnimationState != null;
        if (!ready) return;

        foreach (Graphic g in hideWhenReady) if (g != null) g.enabled = false;
        foreach (Renderer r in hideRenderersWhenReady) if (r != null) r.enabled = false;
    }

    // ================= Play =================

    /// <summary>터짐(pop) 애니메이션이 있는지.</summary>
    public bool HasPop => IsReady && HasAnimation(popAnimation);

    public bool HasAnimation(string animationName)
    {
        Spine.AnimationState state = State;
        if (state == null || string.IsNullOrEmpty(animationName)) return false;
        SkeletonData data = state.Data != null ? state.Data.SkeletonData : null;
        return data != null && data.FindAnimation(animationName) != null;
    }

    /// <summary>등장 → idle 반복. appear가 없으면 바로 idle.</summary>
    public void PlayAppear()
    {
        if (!IsReady) return;
        SetTint(normalTint);
        if (PlayOnce(appearAnimation)) QueueIdle();
        else PlayIdle();
    }

    /// <summary>평소 꿈틀거림(반복).</summary>
    public void PlayIdle()
    {
        if (!IsReady || !HasAnimation(idleAnimation)) return;
        TrackEntry entry = State.SetAnimation(0, idleAnimation, true);
        entry.MixDuration = mixDuration;
    }

    /// <summary>대사 분류에 맞는 움직임. 해당 애니메이션이 없으면 idle.</summary>
    public void PlayStyle(DialogueStyle style)
    {
        if (!IsReady) return;
        SetTint(style == DialogueStyle.Angry ? angryTint : style == DialogueStyle.Gloomy ? gloomyTint : normalTint);
        switch (style)
        {
            case DialogueStyle.Emphasis:
                if (PlayOnce(emphasisAnimation)) { QueueIdle(); return; }
                break;
            case DialogueStyle.Angry:
                if (PlayLoop(angryAnimation)) return;
                break;
            case DialogueStyle.Gloomy:
                if (PlayLoop(gloomyAnimation)) return;
                break;
        }
        PlayIdle();
    }

    /// <summary>태클을 물리쳐 터질 때. 끝나면 onComplete(애니메이션이 없으면 바로).</summary>
    public void PlayPop(Action onComplete = null) => PlayExit(popAnimation, onComplete);

    /// <summary>그냥 사라질 때. 끝나면 onComplete(애니메이션이 없으면 바로).</summary>
    public void PlayDisappear(Action onComplete = null) => PlayExit(disappearAnimation, onComplete);

    /// <summary>말풍선 색(흰색 = 원래 색).</summary>
    public void SetTint(Color color)
    {
        if (!applyTint) return;
        Resolve();
        if (spineGraphic != null) spineGraphic.color = color;
        else if (spineAnimation != null && spineAnimation.Skeleton != null)
            spineAnimation.Skeleton.SetColor(color.r, color.g, color.b, color.a);
    }

    // ================= Helpers =================

    private bool PlayOnce(string animationName)
    {
        if (!HasAnimation(animationName)) return false;
        TrackEntry entry = State.SetAnimation(0, animationName, false);
        entry.MixDuration = mixDuration;
        return true;
    }

    private bool PlayLoop(string animationName)
    {
        if (!HasAnimation(animationName)) return false;
        TrackEntry entry = State.SetAnimation(0, animationName, true);
        entry.MixDuration = mixDuration;
        return true;
    }

    private void QueueIdle()
    {
        if (!HasAnimation(idleAnimation)) return;
        TrackEntry entry = State.AddAnimation(0, idleAnimation, true, 0f);
        entry.MixDuration = mixDuration;
    }

    private void PlayExit(string animationName, Action onComplete)
    {
        if (!IsReady || !HasAnimation(animationName))
        {
            onComplete?.Invoke();
            return;
        }

        TrackEntry entry = State.SetAnimation(0, animationName, false);
        entry.MixDuration = mixDuration;
        if (onComplete != null)
        {
            bool called = false;
            entry.Complete += _ =>
            {
                if (called) return;
                called = true;
                onComplete();
            };
        }
    }
}
