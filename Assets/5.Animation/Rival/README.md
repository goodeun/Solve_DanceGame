# Rival — Club Fusion 루프 안무

기존 `Assets/2.Model/Prefabs/Rival.prefab`의 Humanoid Avatar(02.fbx)를 사용하는 라이벌 전용 안무입니다.
씬 배치는 사용자가 직접 진행합니다. DemoBoss를 포함한 씬 파일은 이 작업에서 변경하지 않았습니다.

## 구성

| 파일 | 역할 |
| --- | --- |
| Rival_ClubFusion_80Bars_128BPM.anim | 128 BPM 기준 150초 / 320박 / 80마디 전체 루프 |
| Rival_Ready.anim | 음악 시작 전·종료 후 대기 자세 |
| Rival_Dance.controller | Rival_Ready, Rival_DanceLoop 두 상태 |

전체 루프는 8마디(15초)씩 열 구간으로 구성됩니다. 구간 내부에는 좌우 교대와 크기·방향 변주가 있습니다.
다음 구간으로 넘어가기 전 2박 동안 동작을 섞으며 마지막 구간은 첫 구간으로 이어집니다.
곡선의 시작·끝 값과 기울기를 맞췄고 루트 이동은 껐습니다.

| 시간(128 BPM) | 구간 | 동작 |
| --- | --- | --- |
| 0–15초 | Low Groove | 낮은 팔, 작은 사이드 스텝, 골반·어깨의 반대 리듬 |
| 15–30초 | Step Scoop | 앞쪽 사선 스텝과 낮은 팔 스쿱 |
| 30–45초 | Diagonal Reach | 좌우 대각선으로 번갈아 뻗는 팔 |
| 45–60초 | Chest Box | 앞뒤 박스 스텝, 가슴·팔꿈치 팝 |
| 60–75초 | Overhead Sweep | 큰 V자 팔선과 빠른 스텝 |
| 75–90초 | Cross Open | 가슴 앞 교차와 벌리기 |
| 90–105초 | Knee Drive | 무릎 리프트와 반대 팔 펌프 |
| 105–120초 | Quarter Pivot | 작은 방향 전환과 손목·팔 변주 |
| 120–135초 | Side Push | 넓은 사이드 스텝과 번갈아 밀기 |
| 135–150초 | Wave Bridge | 척추·가슴·손목 웨이브, 첫 그루브로 복귀 |

## 프리팹 적용과 재생

Rival 프리팹에 Rival_Dance.controller 및 RivalDanceAnimator 컴포넌트를 연결했습니다.
기존 Avatar, 메시, 재질, 크기와 배치 좌표는 유지하고 Animator의 Apply Root Motion만 껐습니다.

- 씬에 FmodMusicClock이 있으면 음악이 시작되기 전에는 Ready, 시작되면 DanceLoop를 재생합니다.
- Animator.speed = 현재 BPM / 128로 재생 속도를 맞춥니다. 128 BPM에서는 2분 30초입니다.
- 음악 시계의 Pause 및 일반 게임 Pause를 따르고 복귀 후 이어서 재생합니다.
- 음악 시계가 없는 프리팹 확인용 씬에서는 기본적으로 루프를 자동 재생합니다. Inspector의 Play Without Music Clock으로 조절합니다.
- 종료 상태가 감지되면 Ready로 돌아갑니다.
- 리그에 맞춘 두 다리 IK는 제작 때 클립으로 베이크했습니다. 런타임에 별도 IK 솔버나 영상 분석을 실행하지 않습니다.

씬에 배치할 때 기존 Rival 프리팹을 사용하면 됩니다. BossBattleDirector의 Rival 참조와 위치·크기는 씬 작업 담당자가 연결합니다.

## 레퍼런스와 제작 방식

- [춤추는이강사 — Club dance](https://www.youtube.com/watch?v=5wTCeCw0BuQ): 작은 스텝, 골반 그루브, 느슨한 팔 리듬을 참고했습니다.
- [흥둥이 — 예나 캐치캐치 다이어트댄스](https://www.youtube.com/watch?v=ZRrWLBFNQZI): 큰 팔선, 사이드 스텝, 무릎 리프트와 운동감 있는 강약을 참고했습니다.

두 영상의 일반적인 리듬과 움직임 특성을 참고해 구간 순서, 팔·다리 조합, 방향, 타이밍을 새로 구성한 키프레임 안무입니다.
원본 영상·음원은 게임 에셋에 포함하지 않았습니다. 동작을 혼합하거나 재구성한 것만으로 권리 문제가 없다고 보장하는 자료는 아닙니다.

## 수정 범위

- `Assets/2.Model/Prefabs/Rival.prefab`: Animator Controller, Apply Root Motion, RivalDanceAnimator 부착.
- 신규 런타임 스크립트 `Assets/3.Script/Player/RivalDanceAnimator.cs`: 음악 시작·종료·BPM·Pause에 따른 재생.
- 신규 제작 도구 `AgentScripts/RivalDanceBuilder.cs`: 열 개 모티프, 구간 연결, Rival 리그에 발 IK 베이크, 곡선 축소, 루프 검사 및 프리팹 적용.
- 신규 에셋은 이 폴더의 두 클립, 컨트롤러와 이 문서입니다.
- 확인용 미리보기: `output/dance-previews/Rival_ClubFusion_10Sections_128BPM.gif`. 열 구간의 첫 8박을 나란히 보여줍니다.

## 검증 및 재생성

- Unity 6000.2.8f1 컴파일 및 새 Console Error 없음.
- 전체 150초를 4,500 구간으로 샘플링: 유효 포즈 확인, 시작·끝 뼈 회전 차이 0도, 곡선 시작·끝 값과 기울기 차이 0, 루트 이동 0.
- Play Mode: 음악 없는 프리팹 자동 재생, 음악 전 Ready, 음악 시작 후 Dance, 160 BPM 속도 비례(1.25배), 150초 경계 통과, 음악 단독 Pause, 게임 Pause 및 복귀 확인.
- 검증은 임시 Play Mode 인스턴스로 수행했고 씬 파일을 저장하지 않았습니다.
- 현재 에디터에서 전체 루프 클립 메모리는 약 4.8 MiB입니다. 빌드·플랫폼에서의 수치는 별도로 측정해야 합니다.
- 로컬 기록: `Temp/rival-dance/asset-verification.json`, `Temp/rival-dance/runtime-verification.json`.
- 변경 전 프리팹 백업: `Temp/rival-dance/restore/Rival.prefab`.

```powershell
unity command script run --file AgentScripts/RivalDanceBuilder.cs --entry RivalDanceBuilder.Build --caller plugin --skill unity-pipeline
unity command script run --file AgentScripts/RivalDanceBuilder.cs --entry RivalDanceBuilder.ApplyPrefab --caller plugin --skill unity-pipeline
```

재생성은 이 폴더의 생성 클립·컨트롤러를 갱신합니다. Animation 창에서 직접 수정했다면 먼저 사본을 보관하세요.
