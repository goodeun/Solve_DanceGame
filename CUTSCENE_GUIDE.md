# 보스 등장 컷씬 만들기 가이드

Unity Timeline을 처음 써 보는 사람 기준으로 정리했습니다.

## 0. 규칙 요약

- **새 씬을 만들지 않는다.** 컷씬은 `DemoBoss` 씬 안에 오브젝트로 들어간다(게임 화면에서 그대로 이어지게).
- 컷씬 묶음 오브젝트 이름은 `BossIntroCutscene`. 컷씬에 필요한 것(카메라, 걷는 보스, 경로 점)은 전부 이 아래에 둔다.
- 컷씬 전용 보스는 `WalkingRival`(Rival 프리펩을 씬에 놓은 것). 공중 무대 위 보스(`RivalStage`)와는 다른 오브젝트다.
- 카메라는 **Cinemachine Camera**를 `Cameras` 아래에 두고, Timeline의 **Activation 트랙**으로 켜고 끈다.
  켜져 있는 동안은 우선순위(100)가 높아서 게임 카메라 대신 화면을 잡고, 꺼지면 게임 카메라로 부드럽게 돌아간다.
- 대사는 Timeline이 아니라 `TimelineCutscene` 컴포넌트의 **Dialogue Cues**에 시간과 함께 적는다(그 시간에 Timeline이 멈추고 대사가 나온다).
- 보스 이동은 `TimelineCutscene`의 **Moves**에서 경로 점(`RivalPath/P0, P1, P2`)과 시간으로 정한다. 애니메이션(걷기·멈춤)은 Timeline이 담당한다.

## 1. 게임에서 실행되는 순서 (DemoBoss)

1. 화면 HUD 숨김, 위아래 검은 띠(레터박스) 등장
2. **컷씬(Timeline) 재생** - 보스가 걸어나오고 카메라가 바뀐다. 중간 대사(Dialogue Cues)
3. 컷씬이 끝나면 마지막 장면에서 멈춘 채 **인트로 대사**(BossBattleDirector의 Intro Dialogue)
4. 컷씬 정리: 걷던 보스·컷씬 카메라가 꺼지고 게임 카메라로 복귀
5. 공중 무대(RivalStage)가 위에서 내려온다(무대 위 보스는 대기 자세)
6. 룰 설명 대사 → 3·2·1 → 음악 시작 → 보스가 춤춘다

플레이어는 **Space를 길게 눌러** 컷씬을 건너뛸 수 있다.

## 2. 기본 틀 만들기 (한 번만)

1. `Assets/1.Scene/Demo/DemoBoss.unity`를 연다.
2. 상단 메뉴 **Solve > Demo > Create Boss Intro Cutscene** 클릭.
3. 아래가 자동으로 만들어지고 BossBattleDirector에 연결된다.
   ```
   BossIntroCutscene      (PlayableDirector + TimelineCutscene)
   ├ RivalPath / P0_Start, P1, P2_End   ← 보스가 걸어갈 경로
   ├ LookTarget                         ← 도착해서 바라볼 곳(플레이어)
   ├ Cameras / Cam1_Wide, Cam2_Close    ← 컷씬 카메라
   └ WalkingRival                       ← 걸어나오는 보스
   ```
   Timeline 파일: `Assets/5.Animation/Cutscene/BossIntro.playable`
4. **Ctrl+S로 씬 저장.**
5. 그냥 플레이해 보면(타이틀의 [DEBUG] 라이벌전 버튼) 기본 컷씬이 나온다. 위치는 임시값이라 아래처럼 맞춘다.

## 3. Timeline 창 열기

1. 메뉴 **Window > Sequencing > Timeline**.
2. Hierarchy에서 `BossIntroCutscene`을 클릭하면 Timeline 창에 트랙들이 보인다.
   - `보스 애니메이션`: 걷기(0~4초) → 멈춰 서기(4~6초)
   - `카메라1 와이드`: 0~3.5초 동안 Cam1이 켜짐
   - `카메라2 클로즈업`: 3.5~6초 동안 Cam2가 켜짐
3. Timeline 창 위쪽 눈금(시간 막대)을 클릭·드래그하면 **그 시간의 장면을 미리 볼 수 있다**(Scene/Game 창).
   - Game 창에서 카메라 화면을 보려면 Main Camera에 Cinemachine Brain이 있어야 한다(이미 있음).
   - 미리보기에서는 경로 이동(Moves)은 안 보이고 애니메이션·카메라만 보인다. 이동까지 보려면 Play로 확인.

### Timeline 기본 조작
- **클립 길이·위치 바꾸기**: 클립(색깔 막대)을 드래그해서 옮기고, 양 끝을 드래그해서 늘리거나 줄인다.
- **클립 추가**: 트랙 위에서 우클릭 → Add ... (Activation 트랙은 Add Activation Clip)
- **트랙 추가**: 왼쪽 트랙 목록 빈 곳 우클릭 → Activation Track / Animation Track 등
- **트랙에 오브젝트 연결**: 트랙 이름 오른쪽의 동그란 칸에 Hierarchy의 오브젝트를 끌어다 놓는다.
- 시간 단위는 초. 클립을 선택하면 Inspector에 Start / Duration이 숫자로 나와서 직접 입력해도 된다.

## 4. 자주 할 작업

### 보스가 걸어갈 경로 바꾸기
1. `RivalPath`의 `P0_Start`, `P1`, `P2_End`를 Scene 창에서 옮긴다(W 키 → 화살표 드래그).
   `BossIntroCutscene`을 선택하면 분홍 점·선으로 경로가 보인다.
2. 점을 더 넣고 싶으면 `P1`을 복제(Ctrl+D)해서 옮기고, `TimelineCutscene > Moves > Points`에 순서대로 끌어 넣는다.
3. 걷는 시간은 `Moves`의 `Start Time`, `Duration`. **Timeline의 걷기 클립 길이도 같이 맞춘다**(안 맞으면 미끄러지거나 제자리걸음).
4. 도착 후 바라볼 방향: `Look At When Arrived`(기본: LookTarget = 플레이어 위치).

### 카메라 위치·각도 바꾸기 (가장 쉬운 방법)
1. Scene 창에서 원하는 구도가 보이도록 화면을 움직인다(우클릭 + WASD, 마우스 휠).
2. Hierarchy에서 `Cam1_Wide` 선택 → 메뉴 **GameObject > Align With View** (Ctrl+Shift+F).
   카메라가 지금 Scene 창 구도로 옮겨진다.
3. 화각(줌)은 카메라 Inspector의 Lens > Field Of View(작을수록 확대).

### 카메라 컷 추가
1. `Cam2_Close`를 복제(Ctrl+D) → 이름 바꾸기(예: Cam3_Face) → Align With View로 구도 잡기.
2. Timeline 트랙 목록에서 우클릭 → **Activation Track** 추가 → 트랙 칸에 Cam3_Face를 끌어다 놓기.
3. 트랙 위 우클릭 → Add Activation Clip → 원하는 시간·길이로 맞추기.
- 카메라 클립끼리 겹치면 나중 트랙이 우선. 사이가 비면 게임 카메라가 잠깐 보인다.
- 카메라 사이 전환은 Main Camera의 Cinemachine Brain > Default Blend(기본 Ease In Out)로 부드럽게 바뀐다. 딱 끊기게 하려면 Cut.

### 컷씬 중간 대사
`BossIntroCutscene > TimelineCutscene > Dialogue Cues`
- `Time`: 몇 초에 나올지(이때 Timeline이 멈추고 대사가 끝나면 이어진다)
- `Lines`: 대사(화자, 스탠딩, 스타일, 문장). 일반 대사 시퀀스와 같은 형식
- 필요 없으면 항목을 지운다(- 버튼).
- 컷씬이 끝난 뒤 나오는 대사는 `DemoFlow`(BossBattleDirector)의 Intro Dialogue에 있다.

### 보스 애니메이션 바꾸기
- `보스 애니메이션` 트랙의 클립을 다른 애니메이션으로: Project 창의 애니메이션(.anim)을 트랙 위로 끌어다 놓는다.
  - 걷기: `Assets/5.Animation/Cutscene/Rival_Walk_InPlace.anim` (제자리 걷기 반복. 이동은 Moves가 담당)
  - 대기: `Assets/5.Animation/Rival/Rival_Ready.anim`
- 두 클립을 살짝 겹치면 자연스럽게 섞인다(Blend).

### 컷씬 길이
- Timeline에서 가장 늦게 끝나는 클립이 컷씬의 끝. 마지막 장면은 인트로 대사가 끝날 때까지 유지된다.

### 컷씬 끄기
- `DemoFlow`(BossBattleDirector) > Intro Cutscene 칸을 비우면 컷씬 없이 시작한다.

## 5. 문제가 생기면
- **보스가 원점으로 순간이동한다**: `보스 애니메이션` 트랙 선택 → Inspector의 Track Offsets가 `Apply Scene Offsets`인지 확인.
- **카메라가 안 바뀐다**: Activation 트랙에 카메라 오브젝트가 연결돼 있는지, 카메라의 Priority가 게임 카메라보다 높은지(100) 확인.
- **에디터에서 WalkingRival이 계속 보인다**: 정상. 플레이하면 시작 시 꺼지고 컷씬 동안만 보인다.
- 메뉴를 다시 실행해도 이미 컷씬이 있으면 새로 만들지 않는다. 처음부터 다시 하려면 `BossIntroCutscene`을 지우고 실행.
