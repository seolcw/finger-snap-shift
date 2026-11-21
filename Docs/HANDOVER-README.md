> **참고:** 11/19 첫 인수 문서다. 3·4차 규칙과 예전 경로 기준이라 지금과 다르다. 최신 상태는 루트의 `HANDOVER.md`와 `Docs/DESIGN.md`를 본다.

# Finger Snap Shift 인수 폴더 (2026-11-19)

새 채팅에서 **Unity 프로토타입**을 만들 때 넘겨주는 폴더다. 이 README → `DESIGN.md` 순서로 읽으면 된다.

## 폴더 구성
| 경로 | 내용 |
| --- | --- |
| `DESIGN.md` | **현재 기획 전체 (기준 문서).** 웹 프로토타입에 아직 없는 4차 결정 사항 포함 |
| `01_WebPrototype/` | 웹 프로토타입 3차 원본. `finger-snap-shift-prototype.html`을 브라우저로 열면 바로 플레이. `src/game.js`가 Unity 구조와 같은 모듈로 나뉨. `web-prototype-handover.md`는 3차 규칙의 세부 결정과 봇 테스트 결과 |
| `02_Sprites/Aseprite/` | 도트 에셋 원본 `.aseprite` 9개 (Unity 프로젝트 `Assets/Art/`와 같은 파일) |
| `02_Sprites/Png/` | 위 파일을 가로 시트 PNG + 프레임·태그 JSON으로 내보낸 것 (확인용·백업용) |
| `02_Sprites/ArtSource/` | 팔레트(`.gpl`), 에셋 생성 Lua 스크립트, 미리보기 PNG |
| `03_Rules/claude-personal-work-rules.md` | 작업 규칙 (대화·Git·검증·빌드·C# 표준). 새 채팅도 이 규칙을 따른다 |

## 현재 상태
- **Unity 프로젝트:** `D:\Unity\finger-snap-shift`, Unity 6000.0.58f2, 2D Built-in, WebGL 설정 완료. `main`에 첫 커밋 `06541f1`(빈 프로젝트)만 있다.
- **도트 에셋:** 프로젝트의 `feature/pixel-art-assets` 브랜치 작업 트리에 있고 **커밋 전**이다(`Assets/Art/`, `ArtSource/`, `.gitattributes`에 `*.aseprite` LFS 추가). Aseprite Importer(`com.unity.2d.aseprite`)로 에러 없이 임포트되는 것까지 확인했고, 씬에 놓아 본 적은 없다.
- **웹 프로토타입:** 3차까지(글자 Shift·회전 밀기·파손 제거). 단어는 아직 예전 키 이름 목록(ESC 등)이다. 4차 결정(유의어 단어, 3페이즈, 사망 시 손 HP 절반 회복 등)은 `DESIGN.md`에만 있다.
- **프로젝트 `CLAUDE.md`:** 아직 없음.

## 스프라이트 목록
팔레트 12색 고정, 검은 1px 외곽선, 안티앨리어싱 없음. 키 칸은 16x16 윗면 + 아래 두께 면 4px = 16x20.

| 파일 | 크기 | 태그(프레임) | 비고 |
| --- | --- | --- | --- |
| `Tiles/Keycap` | 16x20 | Base, Pressed, Blank, Shift / Cracked, Broken, Hole | 아래 3개는 **이제 안 씀**(파손 시스템 제거) |
| `Tiles/KeyWarning` | 16x20 | Blink(2) | 공격 예고 테두리 |
| `Tiles/KeyTargetGlow` | 16x20 | Glow(1) | 다음 필요 글자 빛남 |
| `Tiles/KeySpike` | 16x20 | Spike(2) | **이제 안 씀**(가시밭 → 시프트 파동) |
| `Hand/Hand` | 32x32 | Idle(2), FlickCharge(2), FlickRelease, FlickRecover, SlamRaise, SlamHit, Click(2), Wand(2), Hurt | 손 = 마우스 커서 |
| `Player/Player` | 16x16 | Idle(2), Hurt | 분홍 유령 |
| `Fx/SummonBullet` | 8x8 | Spin(2) | 소환 탄 |
| `Ui/HpCell` | 8x8 | Full, Empty | HP 칸 |
| `Font/PixelFont` | 5x7 | A~Z (26프레임, 태그 없음, 프레임 0 = A) | 글자는 키 칸에 굽지 않고 따로 겹쳐 그린다 |

**아직 없는 에셋** (프로토타입 단계에서는 임시 도형·코드로 대체):
- 숫자 0~9와 기호 `+ - → ← ↑ ↓ ⇧` (예고 숫자용)
- 손: 돌진, 드래그, 스크롤, 지침, 쓰러짐 / 플레이어: 밀려남, 순간이동, 사망
- 키: Shift 대기 표시, 눌린 키 잠금 표시
- UI: 목표 단어 판, 입력 버퍼 판, 손 HP 바, 페이즈 표시, 타이틀, 결과 화면
- 사운드 전부

번쩍임·화면 흔들림·도미노 밀림·계기판 롤·잔상은 그림 없이 코드로 만든다.

## 새 채팅에 줄 요청 (복사해서 사용)
```
D:\Unity\finger-snap-shift-handover 폴더의 README.md와 DESIGN.md를 먼저 읽고,
03_Rules/claude-personal-work-rules.md 규칙을 따라서
D:\Unity\finger-snap-shift 에 Unity 프로토타입을 만들어줘.

- 기획은 DESIGN.md가 기준이고, 웹 프로토타입(01_WebPrototype/src/game.js)의 구조와 로직을 참고해.
- 그림은 Assets/Art/의 .aseprite 스프라이트(02_Sprites 목록 참고)를 그대로 써. 없는 그림은 임시 도형으로.
- 범위: 판 2개, 이동·Space 등록·Shift 글자 변경·SHIFT 칸, 손 공격(튕기기·클릭·내려찍기·돌진·시프트 파동부터),
  예고와 공정성 검사, 단어·3페이즈·HP·승패까지 처음부터 끝까지 플레이 가능하게.
- 수치는 ScriptableObject로 빼서 인스펙터에서 조절할 수 있게.
- 작업 전에 계획(브랜치 이름 포함)을 먼저 보여주고 확인받아. 에셋 브랜치(feature/pixel-art-assets)는 아직 커밋 전이니 어떻게 할지도 같이 물어봐.
```
