# Shift Magic Spell (GitHub Game Off 2026)

> 예전 이름은 Finger Snap Shift. 이어서 작업할 때는 **`HANDOVER.md`를 먼저** 읽는다.

이 폴더가 Unity 프로젝트 루트다. Unity는 `Assets/` 안만 임포트·빌드하므로, 빌드에 들어갈 것만 `Assets/`에 두고 나머지는 루트의 다른 폴더에 둔다.

| 경로 | 내용 | 빌드 포함 |
| --- | --- | --- |
| `Assets/Art/` | 도트 에셋 원본 `.aseprite` 9개 (Tiles, Hand, Player, Fx, Ui, Font). Aseprite Importer(`com.unity.2d.aseprite`)로 바로 임포트 | 참조되면 포함 |
| `ArtSource/Scripts/` | 에셋 생성 Lua (`build_assets.lua`, `build_preview.lua`, `fss_lib.lua`) | X |
| `ArtSource/Palette/` | 12색 팔레트 `.gpl` | X |
| `ArtSource/Preview/` | 에셋 시트·게임 화면 미리보기 PNG | X |
| `ArtSource/Export/` | `.aseprite`를 가로 시트 PNG + JSON으로 내보낸 백업 | X |
| `WebPrototype/` | 웹 프로토타입 5차(줄 돌리기). `shift-magic-spell-prototype.html`을 열면 플레이. 원본은 `src/`, `node src/build.js`로 합치고 `node src/verify-rules.js`로 규칙 검증 | X |
| `Docs/DESIGN.md` | **현재 기획 기준 문서** (5차 결정 포함) | X |
| `Docs/HANDOVER-README.md` | 인수 폴더 설명 (폴더 경로는 예전 인수 폴더 기준) | X |
| `Docs/claude-personal-work-rules.md` | 작업 규칙. `CLAUDE.md`에 붙여 넣어 쓴다 | X |

에셋 다시 만들기 (Aseprite에서 손으로 고친 내용은 덮어씀). Aseprite는 Steam 설치본 `C:\Program Files (x86)\Steam\steamapps\common\Aseprite\Aseprite.exe`:
```
Aseprite.exe -b --script-param root=<이 폴더> --script ArtSource/Scripts/build_assets.lua
Aseprite.exe -b --script-param root=<이 폴더> --script ArtSource/Scripts/build_preview.lua
```
5차(줄 돌리기·공격 단어·공격 슬롯) 추가 에셋 22개는 따로 만든다. 기존 파일은 건드리지 않는다. 목록은 `Docs/DESIGN.md` 9장:
```
Aseprite.exe -b --script-param root=<이 폴더> --script ArtSource/Scripts/build_assets_v5.lua
Aseprite.exe -b --script-param root=<이 폴더> --script ArtSource/Scripts/build_preview_v5.lua
```
