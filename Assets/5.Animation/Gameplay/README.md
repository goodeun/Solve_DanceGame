# 플레이 춤 동작 — 성공 / 실패

기존 Player / Mibit Humanoid Avatar를 사용하는 게임플레이용 AnimationClip입니다.
클립의 기준 길이는 1초이며 PlayerDanceAnimator의 Animator.speed = 1 / BeatDurationSeconds로 1박에 재생됩니다.
좌우는 캐릭터 자신의 신체 기준입니다.

| 키 / 상태 | 파일 | 내용 |
| --- | --- | --- |
| Q / Dance_Q | Dance_Q_LeftArmPop.anim | 왼팔 사이드 팝, 반대팔 카운터, 어깨·몸통 반동 |
| E / Dance_E | Dance_E_RightArmPop.anim | 오른팔 사이드 팝, 반대팔 카운터, 어깨·몸통 반동 |
| W / Dance_W | Dance_W_HeadChestIsolation.anim | 좌우 흔들림 없이 위로 한 번 치는 머리·가슴 팝 |
| A / Dance_A | Dance_A_LeftStep.anim | 왼발 사이드 리프트·탭, 오른발 지지, 팔 카운터 |
| D / Dance_D | Dance_D_RightStep.anim | 오른발 사이드 리프트·탭, 왼발 지지, 팔 카운터 |
| S / Dance_S | Dance_S_HandstandFreeze_Reused.anim | 기존 breakdance freeze var 1의 도립 자세를 재사용한 진입·프리즈·복귀 |
| Idle | Dance_Idle_Groove.anim | 공통 준비 자세와 작은 무릎 바운스 |

성공 클립은 Loop Time을 켰으며 시작·끝 자세를 공유합니다.
같은 키의 연속 입력은 루프를 되감지 않습니다. 다른 키 또는 Miss→Idle은 Inspector의 Dance Blend Beats(기본 0.12박)로 전환합니다.
다음 판정이 오기 전까지 현재 루프를 유지하며, 기존 Miss 이벤트가 오면 Idle로 복귀합니다.
지지 발·손의 위치 보정은 클립의 RootT 커브에 베이크했습니다. 추가 런타임 IK 스크립트는 없습니다.

## Bad 실패 동작

| 키 / 상태 | 파일 | 내용 |
| --- | --- | --- |
| Q / DanceFail_Q | DanceFail_Q_LeftArmFlop.anim | 왼팔 팔꿈치가 먼저 접히고 손목이 처지며 균형을 놓침 |
| E / DanceFail_E | DanceFail_E_RightArmFlop.anim | 오른팔 팔꿈치·손목이 엇나가고 반대팔로 균형을 잡음 |
| W / DanceFail_W | DanceFail_W_HeadWobble.anim | 이전 W 성공 동작의 좌우 머리 흔들림을 실패용으로 재사용 |
| A / DanceFail_A | DanceFail_A_LeftLegStumble.anim | 왼무릎이 안으로 꼬이고 팔로 허우적거리며 헛디딤 |
| D / DanceFail_D | DanceFail_D_RightLegStumble.anim | 오른무릎이 안으로 꼬이고 팔로 허우적거리며 헛디딤 |
| S / DanceFail_S | DanceFail_S_FreezeFumble.anim | 물구나무 진입을 시도하다 무릎이 접히고 자세가 무너짐 |

- Just / Good: 기존 성공 춤. 같은 키의 연속 성공은 재시작하지 않음.
- Bad: 누른 키의 실패 춤을 1박에 한 번 재생. 연속 Bad는 매번 다시 시작.
- 무입력 Miss: 실패 춤을 재생하지 않고 Idle로 복귀하며 기존 Miss 표정만 변경. 마지막 입력 키를 추정하거나 저장하지 않음.
- 실패 클립은 Loop Time을 끄고, 상태의 0.88 시점부터 0.12 길이로 Idle에 전환. 이후 입력은 복귀 도중에도 새 동작으로 전환 가능.
- 점수·판정·마디 콤보 규칙은 기존 그대로이며 Bad의 몸 동작만 변경함.
- Q/E/A/D/S 성공 및 공통 Idle은 이번 수정 전과 파일 해시가 동일함.

## 연결 및 수정 범위

- `Assets/Mibit/Animations/robo.controller`: 기존 성공 상태 연결 유지. Base Layer에 DanceFail_Q/E/W/A/D/S 상태 여섯 개 및 Idle 복귀 전환 추가.
- `Assets/3.Script/Player/PlayerDanceAnimator.cs`: 실패 상태 해시 여섯 개와 GetFailHash 추가. PlayDance는 Bad만 실패 상태로 분기. BlendDance에 restart 옵션을 추가해 실패 재입력은 재시작하고 성공 연타는 루프를 유지.
- Face 레이어, Fever_Loop / Fever_Finish의 Motion 참조 및 기존 Pause 스크립트는 변경하지 않음. Pause의 Idle 대체 상태는 새 공통 Groove를 사용함.
- 신규 제작 스크립트: `AgentScripts/GameplayDanceBuilder.cs` (Assets 밖, 게임 빌드에 포함되지 않음).

## 재생성

Unity Editor가 열린 상태에서 프로젝트 루트에서 실행합니다.

```powershell
unity command script run --file AgentScripts/GameplayDanceBuilder.cs --entry GameplayDanceBuilder.Build --caller plugin --skill unity-pipeline
```

전체 재생성은 성공/Idle 일곱 개와 실패 여섯 개를 포함해 총 열세 클립의 커브와 해당 컨트롤러 연결을 다시 씁니다.
`--args '[false]'`를 덧붙이면 기존 Q/E/A/D/S 성공 및 Idle은 보존하고 W 성공과 실패 클립 여섯 개만 재생성합니다.
클립을 Animation 창에서 수동 수정했다면 먼저 사본을 보관하세요.
제작 기준값·키포즈는 GameplayDanceBuilder의 Times / Accent / Build에서 조절할 수 있습니다.
S는 기존 동작을 1박 안에 재편집한 빠른 도립 프리즈이므로 연출 취향에 따라 유지 시간을 늘릴 수 있습니다.

## 검증

- Unity 6000.2.8f1 컴파일 성공.
- 기존 캐릭터 리그로 포즈별 스키닝 프리뷰 확인.
- DemoMain Play Mode에서 여섯 키 전환, 같은 키 연속 입력 시 normalizedTime 유지, Miss→Idle 확인.
- 여섯 Bad 실패 상태, 비루프·자동 Idle 복귀, 연속 Bad 재시작, 실패 도중 Good 전환, 무입력 Miss에서 실패 춤 미재생 확인.
- 실제 RhythmWheelJudgement.JudgeInput의 Bad 판정 → 이벤트 → 실패 춤 연결 확인.
- 기존 Fever_Loop 진입 및 Pause 시 실제 플레이어 정지·복귀 확인.
- 검증 중 새 Console Error 없음.
- 확인용 GIF: `output/dance-previews/GameplayDance_120BPM.gif` (각 동작의 독립 루프 비교).
- 실패 / 새 W 비교 GIF: `output/dance-previews/GameplayDance_Failures_and_W_120BPM.gif`.
- 로컬 검증 기록: `Temp/dance-authoring/runtime-verification.json`.
- 변경 전 로컬 백업: `Temp/dance-authoring/restore/PlayerDanceAnimator.cs`, `Temp/dance-authoring/restore/robo.controller`.
- 이번 수정 전 백업: `Temp/dance-failure/restore/` (스크립트, 컨트롤러, 일곱 기존 클립 포함).
- 이번 검증 기록: `Temp/dance-failure/asset-verification.json`, `Temp/dance-failure/runtime-verification.json`.

기존 원본 춤 클립은 수정하지 않았습니다.
