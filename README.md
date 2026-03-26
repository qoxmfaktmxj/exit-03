# Exit 03

이 프로젝트는 이동, 수집, 회피, 탈출이라는 기본 3D 게임 루프를 끝까지 완성하는 데 집중한 미로 탈출 게임이다.

처음에는 적 AI를 넣는 것도 생각했지만, 범위가 커질 것 같아서 배터리 수집과 장애물 회피의 흐름을 더 매끄럽게 다듬는 쪽으로 방향을 정리했다.

## 게임 목표

제한 시간(90초) 안에 미로를 탐색하면서 배터리 3개를 모두 수집하고, 열린 출구를 통해 탈출한다.

## 조작법

| 키 | 동작 |
|---|---|
| W / A / S / D | 이동 |

## 주요 기능

- CharacterController 기반 플레이어 이동
- 배터리 수집 시스템 (3개 수집 시 출구 오픈)
- 조건 충족 시 출구 문이 위로 슬라이드되며 열림
- 회전 장애물 (닿으면 즉시 실패)
- 제한 시간 UI (10초 이하 빨간색 강조)
- URP Post Processing (Bloom, Color Grading, Vignette)
- Mixamo 캐릭터 기반 플레이어 모델
- PolyHaven 나무 텍스처 적용 장애물
- 클리어 / 실패 패널 (RETRY / TITLE 버튼)

## 플레이 화면

### 타이틀 화면
![타이틀](./docs/screenshots/01-title.png)

조작법을 짧게 보여주고 바로 시작할 수 있게 구성했다.

### 게임 플레이
![플레이](./docs/screenshots/02-play.png)

플레이 중에는 남은 시간과 수집 개수만 남겨 정보량을 줄였다.

### 배터리 수집 후 출구 오픈
![출구 오픈](./docs/screenshots/03-door-open.png)

배터리를 모두 모으면 출구 문이 위로 열리도록 만들어 목표 달성 흐름이 분명하게 보이게 했다.

### 장애물 구간
![장애물](./docs/screenshots/04-trap.png)

단순 이동만으로 끝나지 않게 좁은 통로에 회전 장애물을 배치했다.

### 클리어 화면
![클리어](./docs/screenshots/05-clear.png)

성공 여부를 바로 확인할 수 있도록 결과 패널에서 다시 시작과 타이틀 이동 버튼을 제공했다.

## 구현하면서 신경 쓴 점

- 맵을 불필요하게 크게 만들지 않고 길을 고르는 순간이 생기도록 분기 위주로 구성했다
- UI를 최소화해서 플레이 시야를 가리지 않게 했다
- 이동, 충돌, 수집, 종료 조건이 자연스럽게 이어지도록 정리했다
- 문이 열리는 방식을 위로 슬라이드하는 것으로 정해서 열렸는지 한눈에 알아볼 수 있게 했다
- URP Post Processing은 Bloom과 Vignette만 적용해서 연출은 살리되 성능 부담을 줄였다

## 아쉬운 점

- 적 AI와 체크포인트는 구현하지 못했다
- 사운드 연출이 아직 단순하다
- 맵 연출보다 플레이 루프 완성을 우선해서 시각적 디테일은 최소한으로 남겼다

## 다음에 보완하고 싶은 점

- 난이도 단계 추가
- 이동 장애물 패턴 다양화
- 기록 저장 기능

## 씬 구성

| 씬 | 역할 |
|---|---|
| TitleScene | 게임 이름, 조작법, START / QUIT 버튼 |
| GameScene | 실제 플레이, HUD, 클리어/실패 패널 |

## 스크립트 구조

```
Assets/Scripts/
├── GameManager.cs      # 타이머, 수집 개수, 게임 상태 관리
├── PlayerController.cs # WASD 이동, 중력, CharacterController, Animator 연동
├── CameraFollow.cs     # 플레이어를 따라가는 고정 카메라
├── Collectible.cs      # 배터리 수집 및 카운트 증가
├── DoorController.cs   # 수집 조건 충족 시 문 오픈
├── TrapSpinner.cs      # 회전 장애물, 충돌 시 게임오버
├── UIManager.cs        # 텍스트 갱신, 패널 on/off
└── SceneLoader.cs      # 씬 전환 (버튼 연결용)
```

## 환경

- **Unity**: 6000.3.11f1 (Unity 6 LTS)
- **렌더 파이프라인**: URP (Universal Render Pipeline) 17.3.0
- **패키지**: TextMeshPro, URP, Visual Studio Editor
