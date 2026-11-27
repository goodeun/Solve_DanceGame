# Option · 키 바인딩 가이드

## 화면 구성
Pause → **Option**을 누르면 `OptionPanel`에 실행 시 설정 화면(`OptionsMenuView`)이 만들어진다.

| 탭 | 항목 | 저장·적용 |
|---|---|---|
| 조작 | 게임 키 6개(Q·E·A·D·W·S 위치) 바인딩, 기본값 복원 | `KeyBindings` (PlayerPrefs) |
| 사운드 | 마스터 / 음악 / 효과음 볼륨 | `GameSettings` → FMOD 버스 |
| 그래픽 | 화면 모드, 해상도, 수직 동기화, 프레임 제한, 품질 | `GameSettings` (에디터에선 해상도·화면 모드 미적용) |
| 게임 | 입력 보정(ms, ±200) | 판정 시각에 반영 |

키보드: ↑↓ 이동, ←→ 값 변경(슬라이더·선택 항목), **Enter 실행**, Q/E 또는 Tab 탭 전환, ESC 뒤로.
키 지정: 항목에서 Enter → 새 키 입력(ESC 취소). 다른 위치가 이미 쓰는 키면 서로 맞바뀐다. ESC·Enter·Tab·Windows 키는 쓸 수 없다.

## 키 바인딩이 반영되는 곳
- 입력: `RhythmWheelInput` (PlayerInput 바인딩 덮어쓰기 + 직접 감지)
- 리듬 휠 키 글자: `KeyImage`
- 리듬 UI 키 이미지: `RhythmViewUI`가 이미지 위에 TMP 글자를 얹는다. 글자 없는 이미지(`4.Sprite/DanceScene/RhythmView/Blank/`)로 교체됨
  - 글자색: `RhythmViewUI` → `Key Text Colors` (W, A, S, D, Q, E 순서, White/Black)
  - 글꼴·크기·위치: `Key Text Font`, `Key Text Size`, `Key Text Offset`
- 튜토리얼 안내 대사, 결과 화면 객석 이름

## 사운드(FMOD)
현재 FMOD 프로젝트에는 Master 버스만 있다.
- 마스터: `bus:/`
- 음악: `bus:/Music`이 있으면 버스, 없으면 곡 인스턴스(`FmodMusicClock`) 볼륨
- 효과음: `bus:/SFX`가 있으면 버스, 없으면 코드에서 재생하는 효과음(대사·메트로놈)만
FMOD Studio에서 **Music, SFX 그룹 버스**를 만들고 이벤트를 연결해 뱅크를 빌드하면 자동으로 버스 쪽이 적용된다.

## Pause 메뉴
- **Quit**: 프리팹에 `Quit_b`가 없으면 Main 버튼을 복제해 아래에 만든다 → 확인 팝업 → 게임 종료(에디터에선 플레이 중지).
- 키보드: Pause가 열리면 첫 버튼(Resume)에 포커스, ↑↓ 이동, Enter 실행.
- 전환 연출(`PausePanelView`, 임시) — 네 가지가 서로 다르다.

| 전환 | 연출 |
|---|---|
| 게임 → Pause | 버튼이 가운데에서 튀어나와 제자리로(`PausePresentation`) |
| Pause → 게임 | 버튼이 아래부터 접히듯 사라짐 |
| Pause → 서브 메뉴 | 버튼이 위부터 오른쪽으로 쓸려 나가고, 서브 패널이 오른쪽에서 비스듬히 밀려 들어옴 |
| 서브 메뉴 → Pause | 서브 패널이 오른쪽으로 빠지고, 버튼이 왼쪽에서 차례로 돌아옴 + 들어가기 전 버튼에 포커스 복원 |

시간·거리는 `PausePanelView`, `SimplePauseSubPanel` Inspector 값. 버튼 원래 위치는 `PauseButtonLayout`이 기억한다.
