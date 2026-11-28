using TMPro;
using UnityEngine;

/// <summary>
/// 데모 흐름 공용 설정. Assets/Resources/DemoUISettings.asset 하나를 사용한다.
/// 코드로 생성하는 UI(카운트다운, 경고 배너, 대사창, 타이틀, 결과)의 폰트·색상과 씬 이름을 모아둔다.
/// </summary>
[CreateAssetMenu(menuName = "Solve/Demo UI Settings", fileName = "DemoUISettings")]
public class DemoUISettings : ScriptableObject
{
    [Header("Fonts (한글 글리프 포함 TMP 폰트)")]
    public TMP_FontAsset bodyFont;
    public TMP_FontAsset titleFont;

    [Tooltip("지정하면 bodyFont 대신 이 폰트 파일로 동적 TMP 폰트를 만들어 본문(대사·버튼·안내)에 쓴다. 임시: Pretendard Medium")]
    public Font bodySourceFont;

    [Header("Colors (마젠타 - 시안 - 그린)")]
    public Color magenta = new Color(1f, 0.18f, 0.78f);
    public Color cyan = new Color(0.25f, 0.85f, 1f);
    public Color green = new Color(0.75f, 1f, 0.2f);
    public Color panel = new Color(0.08f, 0.03f, 0.12f, 0.92f);
    public Color text = Color.white;

    [Header("Optional Images (비워두면 텍스트로 표시)")]
    public Sprite phaseChangeSprite;
    public Sprite changeNextSprite;
    public Sprite dialogueBoxSprite;
    public Sprite dialogueNextIcon;
    [Tooltip("Spine 대사창 프리펩(SkeletonGraphic + SpineBubbleSkin). 넣으면 대사창 이미지 대신 쓴다. SPINE_BUBBLE_GUIDE.md 참고")]
    public GameObject dialogueBoxSpinePrefab;

    [Header("Scene Names (Build Settings에 등록된 이름)")]
    public string titleScene = "DemoTitle";
    public string tutorialScene = "DemoTutorial";
    public string mainScene = "DemoMain";
    public string bossScene = "DemoBoss";
    public string resultScene = "DemoResult";
}
