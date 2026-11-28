# 데모 흐름 구현 메모 (JE_Agent)

인계 문서(데모 플레이 구현 인계 문서 v1.0)의 F·D·P 항목 중 **HP·게임오버(P1)를 제외한** 작업을 구현했다.
Unity 에디터 없이 작성했으므로 **씬 연결은 아래 메뉴 한 번으로 설치**한다.

## 1. 데모 씬

데모 씬은 저장소에 들어 있다(`Assets/1.Scene/Demo`). 원본 `DanceScene_1`, `DanceScene_Rival`은 수정하지 않는다.

| 씬 | 내용 |
|---|---|
| `DemoTitle` | 카메라 + TitleController + EventSystem |
| `DemoTutorial` | DanceScene_1 복제 + PauseSystem + DancePauseHandler + DemoFlow(TutorialDirector) |
| `DemoMain` | DanceScene_1 복제 + PauseSystem + DancePauseHandler + DemoFlow(MainGameFlow, NormalBattleDirector) |
| `DemoBoss` | DanceScene_Rival 복제 + DemoFlow(BossBattleDirector) |
| `DemoResult` | 카메라 + ResultController + EventSystem |

Build Settings: DemoTitle → DemoTutorial → DemoMain → DemoBoss → DemoResult (기존 DanceScene_1은 뒤에 유지). `DemoTitle`에서 플레이.

빠진 컴포넌트가 있으면 메뉴 **Solve > Demo > Repair Demo Scenes**로 다시 넣을 수 있다(원본 씬은 건드리지 않음).

> 이전 버전의 `Setup All`을 이미 실행했다면, pull 전에 로컬의 `DanceScene_1`/`DanceScene_Rival` 변경과 `ProjectSettings/EditorBuildSettings.asset` 변경을 되돌리고 `Assets/1.Scene/Demo` 폴더(추적되지 않은 파일)를 지운다.

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
| F9 | 제한 마디, 기존 객석 점수(객석 색과 같은 값)로 승패: 파랑 객석 수 vs 빨강 객석 수 → 동률이면 총 점수가 최대치 절반 초과인지 → 완전 동률 플레이어 승. 분기 대사 | `BossBattleDirector`, `StageVisual` |
| F10 | 결과: 승패 → 점수 → 판정 개수 → 최대 콤보 → 랭크 → 대사 → [다음날] → 감사·업데이트 안내 → 타이틀 | `ResultController`, `DemoSession` |
| D4 | 피버 길이 마디 단위(`durationBars`), 시작 시 확정. 카메라 컷·HUD 복구·입력 재개가 같은 종료 박을 따름 | `RhythmWheelFever`, `DanceSceneHUD` |
| D5 | 피버: 시작 속성 고정, 마디당 자동 득점, 피버 배수 플래그 연결 | `ScoreManager` |
| D6 | 마디 콤보(`BarCombo`/`MaxBarCombo`) + 콤보 보너스, 판정별 개수 집계 | `ComboAndMagnification`, `ScoreManager` |
| D9 | ABC: 일반전은 마지막 키 배율 감소 완화, 보스전은 마지막 키 객석 점수 +40 | `ScoreManager`, `BossBattleDirector` |
| P2 | 6개 동시 태클 삭제. 3개 동시 태클은 보스전 기믹(일반전 확률 0), 보스전 빈도·불이익 강화 | `NGBubbleManager` |
| P3 | 보스전 관객 말풍선은 `Boss` 문구 사용(없으면 Dance로 대체) | `BubbleManager`, `DialogueManager`, `Dialogue.json` |
| P4 | Pause 버튼(Resume/Option/Credit/Main) 코드 연결. 프리팹 OnClick이 비어 있었음 | `PauseMenuButtons`, `SimplePauseSubPanel` |

그 외 수정
- 마름모 배치에서 점수 키 순서가 6키(`hexagonOrder`)였고 배치 변경 시 `ScoreManager.SetStageMode`가 호출되지 않던 문제 → W·D·S·A 순서로 교체하고 연결함
- 자동 Miss(키 정보 없음)가 마름모 배치에서 W 키 배율을 깎던 문제 수정
- `MultiplierUI`가 사각형 키로만 매핑되어 마름모 배율이 표시되지 않던 문제 수정
- 씬 전환 시 DontDestroyOnLoad `HUDManager`가 다음 씬의 HUD를 파괴하던 문제 → `SceneFlow.Load`에서 해제
- CP949 스크립트 15개를 UTF-8로 변환하고, 이미 깨진 채 커밋된 주석은 이전 커밋에서 복구함(HealthUI.cs는 HP 작업 충돌 방지를 위해 제외)

### 2차 수정 (플레이 피드백)
- 연출 구간(곡 시작 전·대사·곡 종료 후)에서 Pause 복귀 시 카운트다운 없이 바로 복귀
- 배율 표시가 튀던 문제: 씬마다 ScoreManager가 3개(MasterCanvas 프리팹의 Score·DanceHUD + 씬)여서 각자 계산한 배율이 같은 UI를 번갈아 덮어씀 → 처음 깨어난 하나만 계산하고 나머지는 위임(오브젝트는 삭제하지 않음)
- 마디 경계: 전환·실습 종료를 한 마디 앞서 예약해, 쉬는 박 뒤쪽에서 다음 마디 첫 박을 미리 누른 입력도 막음. 튜토리얼 메트로놈이 실습 다음 마디 첫 박에 한 번 더 울리던 문제 수정
- 보스전 기본 점수를 원래 값(10)으로 되돌림(1차 작업에서 스탯 평균으로 바꿔 객석 분위기가 한 번에 차던 문제)
- 게임 씬에서 플레이어가 춤추지 않던 문제: PlayerManager가 Player 프리팹에서 DontDestroyOnLoad를 써 튜토리얼 플레이어가 남고 게임 씬 플레이어가 파괴됨 → 스탯만 유지하고 플레이어는 씬마다 새로 사용
- Pause 패널이 대사창보다 앞에 보이도록 PauseSystem Canvas 정렬을 1500으로 올림
- 보스전 승패를 기존 객석 점수로 판정, 별도 분위기 막대 HUD 삭제

### 3차 수정
- 피버 빈도: 게이지 +0.036/성공 마디, -0.02/실패 마디(DemoMain·DemoTutorial 씬 값), 한 판 최대 2회(`maxFeversPerGame`). 피버 구간이 좌석 변경 구간과 겹치면 시작하지 않음
- 페이즈 변경: 공지가 먼저 나오고 색·BPM은 전환이 끝나는 박에 바뀌도록 변경(`ScheduledBars` 기본). 전환 중 리듬 휠 펄스 링 숨김

## 3. 미결 사항(Q1~Q7)에 적용한 기본값

| Q | 적용한 기본값 | 바꾸는 곳 |
|---|---|---|
| Q1 | Pause 복귀: 현재 BPM으로 3박을 실시간으로 센 뒤 멈춘 위치에서 곡 재개. 일반전 전환 중에도 곡은 계속 재생 | `CountDownController` |
| Q2 | 분위기는 지금처럼 모든 성공 마디에서 마지막 키 객석이 상승 | `AtmosphereController` |
| Q3 | 보스 변경 마디는 비워두면 4~5회 무작위(최소 4마디 간격), 지정도 가능. 좌석 이동과 키 적용은 새 마디 첫 박에 함께 | `BossBattleDirector.changeBars` |
| Q4 | 피버 중이거나 피버 대기 중이면 일반전 전환·보스 예고를 미룸. 전환 중 Pause 허용(박 기준이라 멈췄다가 이어짐), 카운트다운 중 Pause 입력은 기존대로 무시 | 각 Director |
| Q5 | 피버 길이는 마디 단위, Play Mode에서 바꾸면 다음 피버부터 반영 | `RhythmWheelFever.durationBars` |
| Q6 | 중립(보라) 객석은 어느 쪽에도 세지 않음 | `BossBattleDirector.EvaluateWinner` |
| Q7 | F3 카메라·대사 순서 제어는 구현(보스 등장 연출에 연결 가능). 점수에 따른 클럽 상승 연출은 이번 범위에서 제외 | `CinematicDialoguePlayer` |

## 4. 확인이 필요한 점

- **일반전 페이즈 변경**: 기본은 `ScheduledBars`. 새 페이즈가 시작되는 마디(곡의 템포 마커)를 지정하면 그 앞 2마디에서 공지·좌석 이동·3·2·1이 진행되고, 색은 곡의 BPM이 바뀌는 박에 함께 바뀐다. 현재 곡(dev 11.27.2): 0초 128BPM 팝 → 63.65초(34마디) 140BPM 테크노 → 149.36초(84마디) 90BPM 힙합. 곡 구성이 바뀌면 `Scheduled Changes`의 마디를 다시 맞춘다(마디 = 곡 시작부터 박 수 / 4).
- **튜토리얼 BGM**: `DemoTutorial`의 `FmodMusicClock > Music Event Path`에 튜토리얼 곡을 지정. 곡이 끝나면 처음부터 다시 재생한다. 메트로놈은 FMOD 이벤트가 없으면 내장 클릭음을 쓴다.
- **HP·게임오버 연동(P1)**: 보스전 패배 시 `BossBattleDirector.onPlayerLost` 이벤트가 발생한다. 게임오버 흐름이 처리하려면 `defeatHandledExternally`를 켠다. 전환·대사 중 판정 제외 여부는 `RhythmWheelUI.IsBeatBlocked(beat)` / `RhythmWheelJudgement.IsSuspended`로 확인할 수 있다.
- **아트 리소스**: Phase Change / Change Next 이미지, 대사창, 스탠딩(`PlayerManager.standingSprites`, 대사별 NPC 스프라이트), 타이틀·결과 배경은 비어 있으면 텍스트·단색으로 표시된다.
- **수치**: 결과 기준 점수(`ResultController.targetScore` 300,000), 피버 게이지 `gain`, 보스 분위기 수치는 임시값. 일반전에서 피버 1회 이상 나오도록 플레이하며 조정 필요.
- 컴파일은 Unity 2021.3 참조 어셈블리 + uGUI/TMP/InputSystem/Cinemachine 스텁으로 검증했고, 남은 오류는 Unity 6 전용 `Color.aliceBlue/deepPink`(기존 코드, Unity 6에선 정상)뿐이다. **실제 플레이 검증은 하지 못했다.**

## Pause 연출

수정 방법은 [`PAUSE_PRESENTATION_GUIDE.md`](PAUSE_PRESENTATION_GUIDE.md)에 정리했다.

### 4차 수정
- 좌석 회전: 프리팹 `StageController`의 좌석 목록을 앵커와 같은 시계 방향 순서(좌상·우상·우하·좌하)로 맞추고, 키 매핑을 사각형 Q·E·D·A / 마름모 W·D·S·A로 변경. 배치가 바뀔 때 좌석이 시계 방향으로 돌고, 눌렀을 때 반응하는 좌석은 키 위치와 일치
- 크레딧 캐릭터: 춤 동작이 잘리지 않게 위·좌우 여유를 두고 촬영(`Credit Top Margin`, `Credit Side Padding`)
- 커맨드 성공 연출(`CommandSuccessFx`, 리듬 휠에 자동 추가): 휠 중앙 번쩍임·충격파 링·별·방사형 불꽃 + 커맨드 이름(AAB TACKLE BREAK! / ABA SWING! / ABC CHAIN!) + 해당 객석 버스트 + 휠 튕김. 세 박 모두 Just면 더 크게

### 5차 수정
- 태클 처리(AAB) 연출: 기존 MetaBall 액체 연출 재사용(`LiquidBurst`). 말풍선 프리팹의 액체 파티클을 태클 말풍선 위치에서 터뜨리고(Particle Camera → Tex_MetaBall → EffectFilter), 말풍선은 눌렸다가 사라짐(`NegativeBubble.Pop`). `CommandSuccessFx`의 AAB는 글자·휠 튕김만(`Aab Label Only`)
- HUD 박자 바운스: `BeatBounce` — 붙이기만 하면 그 오브젝트가 FMOD 박에 맞춰 쿵쿵 뜀(마디 첫 박 강조). MasterCanvas `DanceHUD/Image (1)`에 추가됨. 다른 HUD에도 컴포넌트만 추가하면 됨(점수·피버 게이지 등). 기존 `PulseAnimation`은 환경 프리팹에 있어 MasterCanvas의 HUD를 참조할 수 없어 별도 컴포넌트로 만듦

## 라이벌 공중 무대 (DemoBoss)

- 프리펩: `Assets/2.Model/Prefabs/RivalStage.prefab` = `RivalStage`(FloatingRivalStage) → `Visual` → `Rival`(Rival.prefab 중첩, 원본은 수정하지 않음)
- 무대 메시(위가 평평하게 잘린 납작한 구 + 빛나는 테두리)는 실행 시 생성. 에디터에서는 Gizmo로 모양만 보인다.
  재질을 직접 넣고 싶으면 Body/Top/Rim Material 슬롯에 넣으면 된다(비우면 URP Lit 임시 재질).
- 위치: 사각형(QEAD)이면 위쪽 두 객석 사이 가운데, 마름모(WASD)이면 비는 좌상·우상 중 랜덤.
  객석 앵커에서 플레이어 무대 쪽으로 `Forward Offset`(기본 12)만큼 앞으로 당겨 둔다. 높이는 `Height Offset`.
  배치 변경(StageController.StageChangeStarted) 때 객석과 같은 시간 동안 앞으로 휘는 궤적(`Arc Forward/Height`)으로 이동.
- 회전: 가운데는 정면, 옆이면 플레이어 무대 쪽으로 `Max Yaw`(기본 18도)까지 부드럽게.
- 흐름: BossBattleDirector가 시작 시 숨기고, 인트로 컷씬(introCinematic/introDialogue)이 끝나면 위에서 내려오게 한 뒤 룰 대사 → 카운트다운.
  컷씬에서 걸어나오는 보스는 씬에 따로 두고 컷씬 끝에 꺼 주면 된다(공중 무대의 Rival과 별개 오브젝트).
- 크기: `Size From Character`를 켜면 위에 선 캐릭터 키 × `Radius Per Character Height`(0.38)로 무대 반지름을 정한다.
- 가운데(사각형)일 때는 `Center Height Offset`(-7)만큼 더 내려와 Fever UI에 가리지 않는다.
- 조명: URP 실시간 Area Light는 지원되지 않아, 넓고 부드러운 Spot Light(RivalStageLight)가 앞 위에서 은은하게 비춘다.
  `Use Light`, `Light Strength`, `Light Angle` 등으로 조정하고, 코드에서는 `SetLightEnabled(bool)`로 끄고 켠다.

## 빛기둥 / 빛기둥 조명

- `FloorLightPillar`: 높이는 플레이어 무릎(Humanoid 무릎 뼈) 기준. `Knee Height Scale`로 조정, `Match Player Knee`를 끄면 구역 폭 × `Height Multiplier`.
- `FloorPillarLight`(분리된 컴포넌트): 기둥이 솟을 때 기둥 색 Point Light를 낮게 켜서 플레이어에 실제 빛이 닿게 한다.
  전체 끄기/켜기는 `FloorPillarLight.GloballyEnabled = false/true`, 구역별로는 `UseLight`.
  둘 다 FloorSection이 실행 시 자동으로 붙인다.

## 태클 처리 액체 이펙트

- `LiquidBurst`가 터뜨린 파티클은 Particle Camera → Tex_MetaBall → MasterCanvas `EffectFilter (RawImage)`로 보인다.
- EffectFilter가 말풍선(Bubbles) UI보다 아래에 그려져 가려지던 문제 → 처음 터질 때 EffectFilter를 형제 중 맨 위로 올린다.
- 원본 파티클 수명(0.3초)·깊이(객석 거리)로는 너무 작고 짧아서, 고정 깊이 16에서 수명 2.4배로 터뜨린다.

## 배율 밸런스 (ScoreManager > Multiplier Balance)

- 누른 키 감소: 처음 10%, 같은 키를 연속으로 누를수록 5%씩 더 커짐(최대 40%).
- 다른 키 증가: 다음 키 10% / 나머지 3% + 스타일 스탯 1%/점. 배율이 높을수록 증가폭이 줄어든다(`Gain Falloff` 1.3), 증가로는 250%를 넘지 않음.
- 시뮬레이션(스타일 5): 시계 방향·무작위·AAB 등 어떤 패턴도 피버 제외 네 키 평균이 약 1.8~1.95배에서 수렴.
- 수치는 전부 Inspector에서 조정 가능(임시값).

## 보스 등장 컷씬

[`CUTSCENE_GUIDE.md`](CUTSCENE_GUIDE.md) 참고. 메뉴 `Solve/Demo/Create Boss Intro Cutscene`으로 기본 틀 생성.
