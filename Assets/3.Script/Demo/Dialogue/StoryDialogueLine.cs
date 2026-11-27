using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>대사 분류. 분류에 따라 대사창 연출이 달라진다.</summary>
public enum DialogueStyle
{
    Normal,   // 기본 대사
    Emphasis, // 강조 대사: 글씨 크게
    Angry,    // 분노 대사: 붉은 대사창 + 흔들림 + 효과음
    Gloomy,   // 우울 대사: 어두운 대사창 + 우울 이펙트 fade
}

/// <summary>플레이어 스탠딩 종류. PlayerManager에 캐싱된 스프라이트 배열의 인덱스로 사용된다.</summary>
public enum PlayerStanding
{
    Normal,
    Happy,
    Surprised,
    Sad,
    Angry,
}

/// <summary>대사 하나. 대사 로직 하나가 대사 하나의 출력을 담당한다.</summary>
[Serializable]
public class StoryDialogueLine
{
    [Tooltip("true면 PlayerManager에 캐싱된 플레이어 스탠딩을 사용")]
    public bool isPlayerLine;
    public PlayerStanding playerStanding;
    [Tooltip("NPC 스탠딩(isPlayerLine이 아닐 때)")]
    public Sprite standingSprite;
    public string speakerName;
    public DialogueStyle style;
    [TextArea(2, 5)] public string text;
    [Tooltip("대사 출력 전 대기(초)")]
    [Min(0f)] public float delayBefore;

    [Tooltip("대사가 시작될 때")]
    public UnityEvent onLineStarted = new UnityEvent();
    [Tooltip("플레이어가 대사를 넘길 때")]
    public UnityEvent onLineFinished = new UnityEvent();

    public static StoryDialogueLine Npc(string speaker, string text, DialogueStyle style = DialogueStyle.Normal)
        => new StoryDialogueLine { speakerName = speaker, text = text, style = style };

    public static StoryDialogueLine Player(string text, PlayerStanding standing = PlayerStanding.Normal, DialogueStyle style = DialogueStyle.Normal)
        => new StoryDialogueLine { isPlayerLine = true, speakerName = "나", playerStanding = standing, text = text, style = style };

    public static List<StoryDialogueLine> List(params StoryDialogueLine[] lines) => new List<StoryDialogueLine>(lines);
}
