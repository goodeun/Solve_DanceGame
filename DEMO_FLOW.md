# 데모 흐름 구현 메모 (JE_Agent)

인계 문서(데모 플레이 구현 인계 문서 v1.0)의 F·D·P 항목 중 **HP·게임오버(P1)를 제외한** 작업을 구현했다.
Unity 에디터 없이 작성했으므로 **씬 연결은 아래 메뉴 한 번으로 설치**한다.

## 1. 설치 (Unity에서 1회)

1. 프로젝트를 열고 컴파일이 끝나기를 기다린다.
2. 상단 메뉴 **Solve > Demo > Setup All (전체 설치)** 실행
   - `DanceScene_1`: `DemoFlow` 오브젝트(MainGameFlow, NormalBattleDirector, CountDownController) + PauseSystem 프리팹 + DancePauseHandler 설치
   - `DanceScene_Rival`: `DemoFlow`(BossBattleDirector, CountDownController) + Pause 설치
   - `Assets/1.Scene/Demo/`에 `DemoTitle`, `DemoResult` 새로 생성, `DanceScene_1`을 복사해 `DemoTutorial` 생성(TutorialDirector로 교체)
   - Build Settings: DemoTitle → DemoTutorial → DanceScene_1 → DanceScene_Rival → DemoResult
3. `DemoTitle`에서 플레이.

이미 설치된 항목은 건너뛰므로 여러 번 실행해도 된다. 씬 하나만 설치하려면 `Install Main/Boss Flow In Open Scene`.

폰트·색·씬 이름·경고 이미지: `Assets/Resources/DemoUISettings.asset` (한글 폰트 SF레몬빙수 / 뚝상체 지정됨).

## 2. 구현 내용

| 번호 | 내용 | 주요 파일 |
|---|---|---|
| 공통 | 판정 게이트: 지정 박 이전·대사 중에는 입력·자동 Miss·마디 실패·분위기/배율/피버 변화가 모두 막힘 | `RhythmWheelJudgement`, `RhythmWheelUI`, `ComboAndMagnification` |
| 공통 | 카운트다운 3·2·1 숫자당 1박. Pause 복귀(실시간, 현재 BPM)와 전환(FMOD 박) 공용. 독립 Canvas라 MasterCanvas가 숨겨져도 보임 | `CountDownController`, `CountdownView` |
| F1 | 타이틀: 새로하기 / 이어하기(데모 안내 팝업) / 종료, 데모 범위 안내 | `TitleController` |
| F2 | 진행 대사: 타이핑, ▼ 깜빡임, 플레이어/NPC 스탠딩, 대사 분류(기본·강조·분노·우울), 대사별 UnityEvent, 출력 전 대기 | `StoryDialogueView`, `StoryDialogueLine`, `StoryDialogueSequence` |
| F3 | 카메라 1개 + 대사 n개. 무빙이 끝나도 대사가 남으면 다음 카메라로 안 넘어감. SplineDolly + DOTween Ease | `CinematicDialoguePlayer` |
| F4 | 튜토리얼: 안내↔실습 반복(2/4/6/2마디), 메트로놈, 다음 마디 첫 박부터 실습, 태클 결과별 대사, SKIP 팝업 | `TutorialDirector` |
| F5 | 게임 전 대사 → 카운트다운 → 곡 시작. 대사 중 조작 차단 | `MainGameFlow` |
| F6/D7 | 일반전 좌석 변경 최대 2회. 2마디 전환(경고·이동 → 3·2·1 → 다음 마디 재개), 피버와 겹치면 연기 | `NormalBattleDirector` |
| F7 | 곡 종료 감지 → 판정 정리 → 보스전 대사 → 보스 씬 | `MainGameFlow`, `FmodMusicClock.MusicEnded` |
| F8/D8 | 보스 등장(라이벌 낙하·연출 대사·규칙 안내) → UI 오픈 → 시작. Villain 고정, 4키 유지, CHANGE NEXT 예고 후 다음 마디 첫 박에 배치 변경(입력 계속) 4~5회 | `BossBattleDirector` |
| F9 | 객석별 플레이어·라이벌 분위기, 제한 마디, 승패(객석 수 → 총 분위기 → 완전 동률 플레이어 승) + 분기 대사, 분위기 막대 HUD | `BossBattleDirector`, `BossMoodHUD` |
| F10 | 결과: 승패 → 점수 → 판정 개수 → 최대 콤보 → 랭크 → 대사 → [다음날] → 감사·업데이트 안내 → 타이틀 | `ResultController`, `DemoSession` |
| D4 | 피버 길이 마디 단위(`durationBars`), 시작 시 확정. 카메라 컷·HUD 복구·입력 재개가 같은 종료 박을 따름 | `RhythmWheelFever`, `DanceSceneHUD` |
| D5 | 피버: 시작 속성 고정, 마디당 자동 득점, 피버 배수 플래그 연결 | `ScoreManager` |
| D6 | 마디 콤보(`BarCombo`/`MaxBarCombo`) + 콤보 보너스, 판정별 개수 집계 | `ComboAndMagnification`, `ScoreManager` |
| D9 | ABC: 일반전은 마지막 키 배율 감소 완화, 보스전은 라이벌 분위기 감소 | `ScoreManager`, `BossBattleDirector` |
| P2 | 6개 동시 태클 삭제. 3개 동시 태클은 보스전 기믹(일반전 확률 0), 보스전 빈도·불이익 강화 | `NGBubbleManager` |
| P3 | 보스전 관객 말풍선은 `Boss` 문구 사용(없으면 Dance로 대체) | `BubbleManager`, `DialogueManager`, `Dialogue.json` |
| P4 | Pause 버튼(Resume/Option/Credit/Main) 코드 연결. 프리팹 OnClick이 비어 있었음 | `PauseMenuButtons`, `SimplePauseSubPanel` |

그 외 수정
- 마름모 배치에서 점수 키 순서가 6키(`hexagonOrder`)였고 배치 변경 시 `ScoreManager.SetStageMode`가 호출되지 않던 문제 → W·D·S·A 순서로 교체하고 연결함
- 자동 Miss(키 정보 없음)가 마름모 배치에서 W 키 배율을 깎던 문제 수정
- `MultiplierUI`가 사각형 키로만 매핑되어 마름모 배율이 표시되지 않던 문제 수정
- 씬 전환 시 DontDestroyOnLoad `HUDManager`가 다음 씬의 HUD를 파괴하던 문제 → `SceneFlow.Load`에서 해제
- CP949 스크립트 15개를 UTF-8로 변환하고, 이미 깨진 채 커밋된 주석은 이전 커밋에서 복구함(HealthUI.cs는 HP 작업 충돌 방지를 위해 제외)

## 3. 미결 사항(Q1~Q7)에 적용한 기본값

| Q | 적용한 기본값 | 바꾸는 곳 |
|---|---|---|
| Q1 | Pause 복귀: 현재 BPM으로 3박을 실시간으로 센 뒤 멈춘 위치에서 곡 재개. 일반전 전환 중에도 곡은 계속 재생 | `CountDownController` |
| Q2 | 분위기는 지금처럼 모든 성공 마디에서 마지막 키 객석이 상승 | `AtmosphereController` |
| Q3 | 보스 변경 마디는 비워두면 4~5회 무작위(최소 4마디 간격), 지정도 가능. 좌석 이동과 키 적용은 새 마디 첫 박에 함께 | `BossBattleDirector.changeBars` |
| Q4 | 피버 중이거나 피버 대기 중이면 일반전 전환·보스 예고를 미룸. 전환 중 Pause 허용(박 기준이라 멈췄다가 이어짐), 카운트다운 중 Pause 입력은 기존대로 무시 | 각 Director |
| Q5 | 피버 길이는 마디 단위, Play Mode에서 바꾸면 다음 피버부터 반영 | `RhythmWheelFever.durationBars` |
| Q6 | 객석별 분위기가 같으면 어느 쪽 소유도 아님 | `BossBattleDirector.EvaluateWinner` |
| Q7 | F3 카메라·대사 순서 제어는 구현(보스 등장 연출에 연결 가능). 점수에 따른 클럽 상승 연출은 이번 범위에서 제외 | `CinematicDialoguePlayer` |

## 4. 확인이 필요한 점

- **일반전 좌석 변경 트리거**: 기본값은 곡의 BPM 변화로 페이즈가 바뀔 때(`OnPhaseChange`). 현재 FMOD 곡에는 템포 마커가 2개(128 → 82.5초에 90)뿐이라 **좌석 변경이 1회만, 힙합 페이즈 없이** 진행된다. 곡을 3구간으로 편집하거나, `NormalBattleDirector`의 Trigger Mode를 `ScheduledBars`로 바꿔 마디를 지정하면 2회가 된다.
- **튜토리얼 BGM**: `DemoTutorial`의 `FmodMusicClock > Music Event Path`에 튜토리얼 곡을 지정. 곡이 끝나면 처음부터 다시 재생한다. 메트로놈은 FMOD 이벤트가 없으면 내장 클릭음을 쓴다.
- **HP·게임오버 연동(P1)**: 보스전 패배 시 `BossBattleDirector.onPlayerLost` 이벤트가 발생한다. 게임오버 흐름이 처리하려면 `defeatHandledExternally`를 켠다. 전환·대사 중 판정 제외 여부는 `RhythmWheelUI.IsBeatBlocked(beat)` / `RhythmWheelJudgement.IsSuspended`로 확인할 수 있다.
- **아트 리소스**: Phase Change / Change Next 이미지, 대사창, 스탠딩(`PlayerManager.standingSprites`, 대사별 NPC 스프라이트), 타이틀·결과 배경은 비어 있으면 텍스트·단색으로 표시된다.
- **수치**: 결과 기준 점수(`ResultController.targetScore` 300,000), 피버 게이지 `gain`, 보스 분위기 수치는 임시값. 일반전에서 피버 1회 이상 나오도록 플레이하며 조정 필요.
- 컴파일은 Unity 2021.3 참조 어셈블리 + uGUI/TMP/InputSystem/Cinemachine 스텁으로 검증했고, 남은 오류는 Unity 6 전용 `Color.aliceBlue/deepPink`(기존 코드, Unity 6에선 정상)뿐이다. **실제 플레이 검증은 하지 못했다.**
