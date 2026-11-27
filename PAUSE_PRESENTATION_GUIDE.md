# Pause 연출 수정 가이드

페르소나식 Pause 연출(`PausePresentation`)의 구조와 수정 방법. 지금 들어 있는 도형·색·모션은 전부 **임시**이며, 리소스가 준비되면 아래 칸만 바꾸면 된다.

수정은 모두 각 데모 씬(`DemoMain`, `DemoTutorial`, `DemoBoss`)의 **DancePauseHandler 오브젝트 → `PausePresentation` 컴포넌트**에서 한다.

## 1. 구조

게임 카메라와 실제 플레이어는 움직이지 않는다. 시간이 멈춘 게임 화면 위에 층을 쌓는다.

| 순서(뒤 → 앞) | 내용 | 캔버스 정렬 |
|---|---|---|
| 1 | 멈춘 게임 화면(실제 플레이어는 Pause 동안 숨김) | 월드 |
| 2 | Dim: 게임 화면을 어둡게 덮는 막 | 600 |
| 3 | 대사창(Pause 중 아래로 내려감) | 650 |
| 4 | 연출: 대각선 배경 패널 → 사선 패턴 → 슬래시 → 캐릭터 잔상 → 캐릭터 → 캐릭터 덮개 일러스트 → PAUSE 타이틀 → 번쩍임 | 1400 |
| 5 | PauseSystem: 버튼·서브 패널 | 1500 |
| 6 | 확인 팝업(타이틀로 등) | 2000 |

캐릭터: 시작할 때 실제 플레이어를 복제해 게임 스크립트를 뺀 **메뉴 전용 캐릭터**를 화면 밖(`Rig World Position`, 기본 y = -3000)에 두고, 전용 카메라로 찍은 RenderTexture를 화면에 띄운다. Pause에 들어가는 순간 실제 플레이어가 추던 동작을 그대로 복사한 뒤 Pause 진입 모션 → 반복 모션으로 넘어간다.

## 2. 진행 순서

| 시점 | 연출 |
|---|---|
| 진입 0% | 화면이 약하게 하얗게 번쩍이고 Dim이 깔림, 대사창이 아래로 내려감 |
| 0~24% | 마젠타 슬래시가 대각선으로 지나가고 시안 선이 살짝 늦게 따라옴, 배경 패널 펼침 |
| 12~60% | 캐릭터가 왼쪽에서 밀고 들어와 튕기며 자리잡음, 시안 잔상이 따라오다 사라짐 |
| 24~54% | PAUSE 글자가 한 자씩 튀어나옴 |
| 40%~ | 버튼(임시)이 위에서부터 차례로 가운데 → 제자리로 날아옴 |
| 정착 | 배경 패널이 멈추기 직전 BPM으로 숨쉼, 사선 패턴이 천천히 흐름 |
| 버튼 호버·선택 | 버튼이 튕기고 캐릭터가 작게 반응, 배경 패널이 살짝 기울어짐 |
| 서브 메뉴 진입 | 캐릭터가 상반신 클로즈업 + 반대편 각도로 전환 |
| 퇴장 | 역순으로 빠르게 빠짐 → 게임 중이면 3·2·1, 대사 중이면 바로 재개 |

%는 `Enter Duration`(기본 0.5초) 대비 비율이다. 값 하나만 바꿔도 전체 속도가 같이 바뀐다.

## 3. 항목별 수정 위치

### 캐릭터 촬영 (Portrait)
| 항목 | 설명 |
|---|---|
| `Crop Bottom` | 화면 아래 끝이 캐릭터 키의 어디쯤인지(0 = 발, 1 = 머리). 허벅지 위 크롭 기본 0.38 |
| `Crop Top Margin` | 머리 위 여유. 손·머리를 들어 올리는 모션이 잘리면 올린다 |
| `Portrait Yaw` | 캐릭터를 보는 수평 각도. **캐릭터가 뒤를 보고 있으면 180 근처로** |
| `Portrait Pitch` | 내려다보는 각도 |
| `Portrait Fov` | 화각. 낮을수록 납작한 원근(페르소나 느낌), 높을수록 입체감 |
| `Render Texture Size` | 캐릭터 이미지 해상도. `Character Size`와 비율을 맞춘다(기본 4:5) |

### 서브 메뉴 구도 (Portrait - Sub Menu)
| 항목 | 설명 |
|---|---|
| `Sub Menu Crop Bottom` | 서브 메뉴에서의 크롭(클로즈업) |
| `Sub Menu Yaw` | 서브 메뉴에서의 각도 |
| `Sub Menu Shot Duration` | 구도가 바뀌는 시간 |

### 모션 (Player Motion)
- 플레이어 애니메이터 컨트롤러(robo.controller) Base Layer에 **`Pause_Enter`**, **`Pause_Loop`** 이름으로 상태를 만들고 클립을 넣으면 자동으로 그 모션을 쓴다.
- `Pause_Loop` 클립은 Loop Time을 켠다.
- 상태가 없으면 임시로 `Fever_Finish` → `Idle`을 쓴다(`... Fallback` 항목).
- `Motion Cross Fade`: 동작 사이 전환 시간.

### 화면 배치 (Layout, 1920×1080 기준)
| 항목 | 설명 |
|---|---|
| `Character Position / Size / Tilt` | 캐릭터 이미지 위치·크기·기울기 |
| `Title Position` | PAUSE 타이틀 위치 |
| `Slash Angle` | 슬래시 기울기 |
| `Back Panel Angle` | 배경 패널 기울기 |

### 색 (Colors)
`Dim Color`(어두운 막, 알파로 어두운 정도), `Back Panel Color`, `Slash Color`, `Accent Color`(시안 선·잔상), `Pattern Color`, `Title Color`.

### 리소스 교체 (Sprites)
비워 두면 코드가 만든 임시 도형을 쓰고, 넣으면 그 이미지로 바뀐다.

| 항목 | 바뀌는 것 |
|---|---|
| `Back Panel Sprite` | 캐릭터 뒤 대각선 패널 |
| `Slash Sprite` | 굵은 슬래시 띠 |
| `Title Sprite` | PAUSE 글자 대신 타이틀 이미지 |
| `Character Overlay Sprite` | 캐릭터 위에 덮는 일러스트(프레임·장식) |
| `Pattern Texture` | 배경 패널 안에서 흐르는 패턴(반복 가능한 텍스처) |

### 속도 (Timing)
| 항목 | 설명 |
|---|---|
| `Enter Duration` | 진입 전체 시간(기본 0.5초) |
| `Exit Duration` | 퇴장 시간(기본 0.2초) |
| `Title Text` | 타이틀 글자(Title Sprite가 없을 때) |
| `Breathe On Last Bpm` | 끄면 고정 속도로 숨쉼 |

### 버튼 (Buttons)
- 지금 버튼 등장 궤적은 임시다. 직접 만들 때는 `Animate Buttons`를 끈다.
- 버튼 연출을 만들 때 DOTween에는 **`.SetUpdate(true)`**를 꼭 붙인다(Pause 중엔 시간이 멈춰 있어 없으면 안 움직인다). 대기는 `WaitForSecondsRealtime`.
- 넣을 곳은 `PausePanelView`의 애니메이션 코루틴(진입·서브 메뉴·복귀).
- 각 버튼에는 호버·선택 반응용 `PauseButtonFx`가 실행 시 자동으로 붙는다. 필요 없으면 `PausePresentation.CollectButtons`의 해당 줄을 지운다.
- `Button Stagger`: 버튼 사이 등장 간격.

## 4. 알아둘 점

- **서브 패널:** Option·Credit·MainConfirmPopup은 시작할 때 닫히고, PausePanel 밖(형제)으로 옮겨져 서브 메뉴에 들어가도 같이 꺼지지 않는다(`PauseMenuButtons`). 프리팹에서 처음부터 PausePanel 밖으로 빼 두면 이 처리는 필요 없다.
- **서브 메뉴 깊이 알림:** `PauseManager.SubPanelDepthChanged`(0 = 메인 메뉴). 다른 연출도 여기에 연결할 수 있다.
- **화면 비율:** 배치 값은 1920×1080 기준이다. 16:9 외 비율에서는 일러스트와 캐릭터 위치가 어긋날 수 있다.
- **조명:** 메뉴 캐릭터는 씬의 Directional Light를 그대로 받는다. 따로 조명을 주려면 메뉴 캐릭터 근처(`Rig World Position`)에 라이트를 둔다.
- **관련 파일:** `Assets/3.Script/Player/PausePresentation.cs`, `Player/DancePauseHandler.cs`, `UI/CommonUI/PauseButtonFx.cs`, `UI/CommonUI/PauseMenuButtons.cs`, `UI/CommonUI/PauseManager.cs`.

## 5. 크레딧 롤 (Pause → Credit 버튼에서만)

`CreditPanel`에 실행 시 `CreditRollView`가 붙는다.

1. 화면이 검게 페이드 → 크레딧이 아래에서 위로 올라감, 좌측 하단에 메뉴 캐릭터가 작게(전신) 춤 루프
2. 다 올라가면 글씨·캐릭터가 사라지고 검은 화면만 남음
3. 우측 하단에 "아무 키나 눌러 돌아가기"가 깜빡임 → 아무 키(또는 클릭) 입력 시 크레딧이 닫히고 바로 Pause로 복귀
4. ESC는 언제든 Pause로 복귀

| 바꾸고 싶은 것 | 위치 |
|---|---|
| 크레딧 내용(현재 임시) | `CreditRollView` → `Title`, `Subtitle`, `Sections`(제목+본문), `End Line`, `Continue Text` |
| 속도·색·캐릭터 크기 | `CreditRollView` → `Scroll Speed`, `Heading/Body Color`, `Character Size/Margin`, `Fade Duration` |
| 캐릭터 춤·구도 | `PausePresentation` → `Credit Dance State`(컨트롤러에 `Credit_Dance` 상태를 만들면 자동 사용, 임시 `Fever_Loop`), `Credit Crop Bottom`, `Credit Yaw` |
