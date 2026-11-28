# Spine 말풍선 가이드

태클 말풍선(NegativeBubble), 객석 말풍선(Bubble), 대사창(StoryDialogueView)에 Spine 애니메이션을 입히는 방법입니다.
코드는 준비돼 있어서, Spine 파일을 만들어 넣고 연결만 하면 됩니다. Spine을 연결하지 않으면 지금 이미지 말풍선이 그대로 나옵니다.

## 0. 프로젝트 조건

- 프로젝트의 Spine 런타임: **spine-unity 4.3** (`Assets/Spine/version.txt`).
  → **Spine 에디터 4.3**에서 내보내야 한다(4.2 이하로 내보낸 파일은 읽지 못함).
- 프로젝트 색 공간: **Linear** → 투명도는 **Straight Alpha**(Premultiplied Alpha 끔)로 맞춘다.

## 1. Spine에서 만들 것

### 애니메이션 이름 (대소문자까지 똑같이)

| 이름 | 용도 | 반복 |
|---|---|---|
| `idle` | **평소 꿈틀거림.** 꼬리 끝·가장자리가 페르소나처럼 살랑거리는 기본 루프 | 반복 (처음·끝 자세가 같게) |
| `appear` | 등장(팡 튀어나옴). 끝나면 자동으로 `idle` | 한 번 |
| `pop` | 태클 말풍선을 커맨드로 물리쳐 터질 때. 끝나면 말풍선이 꺼짐 | 한 번 |
| `disappear` | 그냥 사라질 때 | 한 번 |
| `emphasis` | 강조 대사(대사창). 끝나면 `idle` | 한 번 |
| `angry` | 분노 대사(대사창). 거칠게 떨림 | 반복 |
| `gloomy` | 우울 대사(대사창). 축 처진 느린 움직임 | 반복 |

- **꼭 필요한 건 `idle` 하나.** 나머지는 없으면 건너뛰고 기존 연출(크기 트윈 등)이 대신한다.
- 이름을 바꾸고 싶으면 Unity의 `SpineBubbleSkin` 컴포넌트에서 이름 칸만 바꾸면 된다.

### 만들 때 팁
- **글자는 Spine에 넣지 않는다.** 글자(TMP)는 Unity가 말풍선 위에 따로 그린다. Spine은 말풍선 모양만.
- 기존 말풍선 이미지와 **같은 크기(픽셀)** 로 그리면 교체가 쉽다(대사창은 가로가 넓게 늘어나므로 가로 1280 × 세로 290 정도 기준).
- 꿈틀거림은 꼬리·가장자리에 본을 몇 개 두고 Mesh + Weights로 살짝 휘게 하면 된다.
- 색은 흰색/밝은색 위주로 그리면 Unity에서 분노(붉게)·우울(푸르게) 색을 곱해서 입힐 수 있다.

## 2. 내보내기 (Spine 에디터: Export)

- 형식: **Binary** 권장(확장자 `.skel.bytes`) — JSON(`.json`)도 가능
- **Texture Atlas: Pack** 켜기
  - Atlas extension: **`.atlas.txt`**
  - **Premultiply alpha: 끔**, **Bleed: 켬** (Linear 색 공간 + Straight Alpha)
- 결과 파일 3개(이름은 영어로): `NegativeBubble.skel.bytes`, `NegativeBubble.atlas.txt`, `NegativeBubble.png`

## 3. Unity에 넣는 위치

```
Assets/4.Sprite/Spine/
├ NegativeBubble/     ← 태클 말풍선
├ DialogueBox/        ← 대사창
└ CrowdBubble/        ← 객석 말풍선(선택)
```
- 폴더에 파일 3개를 그대로 넣으면 Unity가 `_Atlas.asset`, `_SkeletonData.asset`, `_Material.mat`을 자동으로 만든다.
- 처음 한 번: **Edit > Preferences > Spine**에서 Default Texture Preset을 **Straight Alpha** 계열로 두고,
  만들어진 Material(SkeletonGraphic용)의 **Straight Alpha Texture** 체크를 켠다(가장자리가 검게 보이면 이 설정 문제).

## 4. 연결하기

### 태클 말풍선 (MasterCanvas > Bubbles > 각 NegativeBubble)
1. `Assets/2.Model/Prefabs/UI/MasterCanvas.prefab`을 연다.
2. `NegativeBubble`(3개) 중 하나를 선택하고, Project의 `NegativeBubble_SkeletonData`를 그 오브젝트 **위로 끌어다 놓는다**
   → 메뉴에서 **SkeletonGraphic (UI)** 선택. 자식으로 Spine 오브젝트가 생긴다(SkeletonGraphic + SkeletonAnimation).
3. 생긴 Spine 오브젝트를 **글자(Text)보다 위쪽 순서**(Hierarchy에서 위)로 올려 글자 뒤에 깔리게 하고, 크기·위치를 기존 이미지에 맞춘다.
4. `NegativeBubble` 루트에 **Add Component > SpineBubbleSkin**.
   - Spine Animation / Spine Graphic: 비워 두면 자식에서 자동으로 찾는다.
   - **Hide When Ready**: 기존 말풍선 Image를 넣는다(Spine이 있으면 자동으로 숨김).
5. 나머지 두 말풍선도 같은 방법으로(또는 컴포넌트 복사·붙여넣기).

동작: 켜질 때 `appear`→`idle`, 커맨드로 물리치면 액체 이펙트와 함께 `pop`(끝나면 꺼짐). `pop`이 없으면 기존 찌그러짐 트윈.

### 대사창 (런타임에 만들어지는 창이라 프리펩으로 넣는다)
1. 아무 씬에서 Canvas 아래에 `DialogueBox_SkeletonData`를 끌어다 놓기 → **SkeletonGraphic (UI)**.
2. 그 오브젝트의 RectTransform을 **Stretch(전체 채우기)** 로, 그림이 창 크기에 맞도록 크기를 조정.
3. 같은 오브젝트에 **SpineBubbleSkin** 추가.
4. Project 창으로 끌어서 프리펩으로 저장: `Assets/2.Model/Prefabs/UI/Spine/DialogueBoxSpine.prefab` (씬의 원본은 지운다)
5. `Assets/Resources/DemoUISettings.asset`의 **Dialogue Box Spine Prefab** 칸에 넣는다.

동작: 대사창이 처음 뜰 때 `appear`→`idle`, 대사 분류(Normal/Emphasis/Angry/Gloomy)에 따라 `idle`/`emphasis`/`angry`/`gloomy`, 분노·우울은 색도 입힌다.
프리펩이 비어 있으면 지금 대사창 이미지 그대로.

### 객석 말풍선 (월드 오브젝트, 선택)
- `Assets/2.Model/Prefabs/UI/Bubble.prefab`의 VisualRoot 아래에 SkeletonData를 끌어다 놓고 **SkeletonAnimation**(UI 아님) 선택.
- 루트에 SpineBubbleSkin 추가, **Hide Renderers When Ready**에 기존 SpriteRenderer.
- 동작: 나타날 때 `appear`→`idle`, 사라질 때 `disappear`.

## 5. 확인
- 플레이 중 말풍선이 나타나면 꿈틀거리는지, 태클을 AAB로 물리치면 `pop`이 나오는지.
- 그림 가장자리가 검게/하얗게 뜨면 2·3번의 Premultiply alpha / Straight Alpha 설정을 다시 확인.
- Spine이 안 보이고 기존 이미지가 나오면: SpineBubbleSkin이 Spine 오브젝트를 못 찾은 것(자식에 있는지), SkeletonData가 연결됐는지 확인.
