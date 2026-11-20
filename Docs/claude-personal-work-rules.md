# Claude 작업 규칙 (개인 프로젝트용)

마법공방 작업에서 쓰던 방식 중 개인 프로젝트에도 그대로 쓸 수 있는 것만 정리했습니다.
팀 전용 항목(Plane·Mintlify·Forge Brain·TeamAI, 팀 PR 리뷰 절차)은 뺐습니다.

사용법: 새 프로젝트의 `CLAUDE.md`에 아래 "규칙" 부분을 붙여 넣으면, 그 프로젝트에서 여는 모든 세션에 적용됩니다.
`<...>`로 표시된 값은 프로젝트에 맞게 바꿔 주세요.

---

## 규칙

### 대화
- 답변은 한국어로 합니다.
- 작업 전에 이해한 내용과 수정 계획을 먼저 말하고, 확인을 받은 뒤 수정합니다. ("내 말 이해했나 확인하고 수정하자" 방식)
- 로그인·승인·토큰 입력처럼 내가 직접 해야 하는 일이 생기면, 할 일 목록으로 미루지 말고 그 시점에 하나씩 물어봅니다.
- 요청하지 않은 작업(빌드, 업로드 등)을 먼저 시작하지 않습니다.

### Git
- 커밋·푸시는 내가 말할 때만 합니다. 올리기 전에 브랜치 이름과 계획을 먼저 보여 줍니다.
- 새 작업은 최신 `main`에서 브랜치를 만들어 시작합니다. 브랜치 이름: `유형/짧은-설명` (영문 소문자·하이픈, 예: `bugfix/weapon-category-reset`).
- 커밋 메시지: `type: 한국어 제목` (예: `fix: ...`, `feat: ...`, `chore: ...`).
- 커밋에 `Co-Authored-By: Claude` 같은 AI 표시를 넣지 않고, PR 본문에도 AI 생성 문구를 넣지 않습니다.
- PR을 연 뒤에는 강제 푸시로 기록을 다시 쓰지 않고, 수정은 새 커밋으로 추가합니다.
- 툴이 자동으로 바꾼 파일(Unity의 폰트 SDF·ProjectSettings 등)은 커밋에서 뺍니다. 작업한 파일만 골라서 `git add` 합니다.
- 저장소에 불필요한 문서·로그·캡처를 올리지 않습니다.

### 비밀값
- API 키·토큰·쿠키·비밀번호를 코드, 문서, 로그, 채팅, git 기록에 넣지 않습니다.
- 키가 든 설정 파일(예: `StreamingAssets/config.json`)은 커밋하지 않고, 배포용 빌드에도 넣지 않습니다.

### 확인(검증)
- "잘 작동하는지 확인"은 컴파일 확인이 아니라 실제로 실행(Unity는 Play Mode)해서 확인하는 것입니다.
- 테스트가 실패하면 수정 전 `main`에서도 같은 테스트를 돌려, 원래 있던 실패인지 이번 수정 때문인지 구분해서 알려 줍니다.
- 확인하지 못한 부분은 확인했다고 말하지 않고, 무엇을 확인하지 않았는지 분명히 적습니다.
- 버그 수정에는 재발을 막는 테스트를 함께 추가합니다.

### Unity
- 확인용으로 에디터를 열 때는 실행 인자를 붙입니다: `<예: --forge-demo --forge-enable-style-estimation>`
- 테스트는 에디터를 닫은 상태에서 배치 모드로 돌립니다.
  ```
  "<Unity.exe 경로>" -batchmode -projectPath "<프로젝트>" -runTests -testPlatform PlayMode -testFilter "<테스트 클래스>" -testResults results.xml -logFile test.log
  ```
- 같은 프로젝트를 에디터로 열어 둔 채 배치 실행을 하면 실패하므로, 먼저 에디터를 닫습니다.

### 빌드
- 빌드 전에 빌드 설정과 빌드 이름을 먼저 알려 줍니다.
- 이름 규칙: `<프로젝트>-v<기준 버전>.sha.<빌드한 커밋 7자리>` (예: `magic-forge-v0.1.0-alpha.sha.834603b`)
  - 커밋은 `git rev-parse --short=7 HEAD`로 구합니다.
  - 게임 안 버전(Player Settings `bundleVersion`)도 같은 값으로 맞춥니다. 빌드할 때만 바꾸고 끝나면 되돌립니다(버전 변경은 커밋하지 않음).
  - 기준 버전은 내가 바꾸라고 할 때까지 유지합니다.
- 빌드 전용 폴더(별도 worktree)를 다시 써서 빠르게 빌드합니다. Library 폴더를 복사하지 않습니다.
  ```
  git checkout -- .
  git fetch origin
  git checkout --detach origin/main
  ```
- 빌드가 끝나면 짧게 실행해서 오류 없이 뜨는지, 버전이 들어갔는지 확인합니다. (Player.log 확인)
- 공유용 압축 파일
  - 디버그 전용 폴더(`*_BurstDebugInformation_DoNotShip`)와 키가 든 설정 파일은 넣지 않습니다.
  - `사용 방법.txt`(실행 방법, 버전, 로그 위치)를 함께 넣습니다. Windows용 한글 텍스트는 UTF-8(BOM 포함)으로 저장합니다.
  - 드라이브 업로드는 내가 직접 합니다. 결과물 폴더 위치를 알려 주세요.

---

## Unity C# 개발 표준

출처: GAME DEVELOPMENT CLUB "[유니티] 개발 표준" v0.3.0 (2026-05-05) 요약.
기존 코드는 표준과 다를 수 있습니다. **새로 쓰거나 고친 코드만** 표준에 맞추고, 관련 없는 기존 코드를 한꺼번에 고치지 않습니다.

### 네이밍
| 대상 | 규칙 | 예 |
|---|---|---|
| private 필드 | `_camelCase` | `_moveSpeed` |
| public 필드 / 프로퍼티 | PascalCase (필드보다 프로퍼티 권장) | `Health` |
| 백킹 필드 | `_camelCase` + 같은 이름의 PascalCase 프로퍼티 | `_health` / `Health` |
| 지역 변수·매개변수 | camelCase, `_` 없음 | `processedCount` |
| 상수 | `UPPER_SNAKE_CASE` | `MAX_HEALTH` |
| readonly 필드 | 상수 규칙이 아니라 일반 필드 규칙, 생성자에서 초기화 | `_initialHealth` |
| 클래스·구조체·네임스페이스 | PascalCase, 명사. 파일명 = 클래스명 | `PlayerController.cs` |
| 인터페이스 | `I` + PascalCase | `IDamageable` |
| enum 값 | PascalCase (대문자_언더스코어 금지) | `Melee` |
| `[Flags]` enum | 이름 끝 `Flags`, 값은 2의 거듭제곱, `None = 0` 필수 | `ItemAttributesFlags` |
| 메서드 | PascalCase, 동사로 시작 | `MovePlayer()` |
| Unity 콜백 | Unity 이름 그대로 | `Start`, `OnCollisionEnter` |
| 코루틴 | PascalCase + `Coroutine`, `IEnumerator` 반환 | `MovePlayerCoroutine` |
| 이벤트 | `OnXxx`, `Action`/`EventHandler` 사용 | `OnPlayerDeath` |
| bool 필드·메서드 | `is`/`has`/`can`/`should` 접두사, 부정형 이름 금지 | `_isGrounded`, `HasKey()` |
| null을 반환할 수 있는 메서드 | 접미사 `OrNull` | `GetUserOrNull` |
| null 대신 기본값을 반환하는 메서드 | 접미사 `OrDefault` | `GetUserOrDefault` |

약어
- 단어처럼 첫 글자만 대문자로 씁니다: `UserId`, `XmlParser` (`userID` ✕).
- 공용 멤버는 약어를 거의 쓰지 않습니다. 허용: `Id`, `Url`, `Api`, `Ui`, `Db`, `Config` 정도.
- private 멤버는 문맥이 분명한 약어 허용: `_itemDict`, `_playerDb`.
- 짧은 루프(5줄 미만)는 `i`, `j` 가능. 길어지면 `enemyIndex`처럼 역할이 드러나는 이름.

### 구조
- 접근 제어자는 항상 명시합니다. 기본은 `private`, 필요할 때만 넓힙니다.
- Unity 오브젝트 참조는 `[SerializeField] private`로 인스펙터에서 연결합니다. `GameObject.Find()` 같은 런타임 검색은 최소화합니다.
- 중괄호는 줄을 바꿔서 열고, 코드 블록 사이에는 빈 줄을 둡니다.
- `partial` 클래스는 기능별로 나누고 폴더를 만듭니다: `Player/Player.cs`, `Player/Player.Actions.cs`. 작은 클래스는 쪼개지 않습니다.
- static 클래스는 상태 없는 유틸리티만 만들고, 이름 끝에 `Utils`/`Utilities`/`Helper`를 붙입니다.
- 상태가 있는 전역 관리자는 싱글톤으로 만듭니다(`Instance` 프로퍼티, `Awake`에서 중복 제거, 이름 끝 `Manager`/`Service`).
- 확장 메서드는 `static class XxxExtensions` 안의 `static` 메서드로 만들고, 첫 매개변수에 `this`를 붙입니다.
- `var`는 오른쪽에서 타입이 분명할 때만, `dynamic`은 런타임 타입이 정말 유동적일 때만 씁니다.

### 흐름 제어·예외
- `switch`: 모든 case를 `break`로 끝냅니다(의도한 fall-through만 `goto case`). `default`를 넣고, switch 안에 switch를 넣지 않습니다(안쪽을 메서드로 분리).
- 재귀: 종료 조건을 분명히 하고, 깊어질 수 있으면 반복문으로 바꿉니다.
- null 처리: `?.`, `??`, `??=`, Try 패턴(`TryGetXxx(out ...)`)을 씁니다.
- 예외는 `try-catch`로 처리하거나, 조건을 검사해 오류 로그·예외를 남깁니다. `Debug.Assert`는 디버깅 보조용이지 처리 로직이 아닙니다.

### 로그
- 심각도에 따라 `Debug.Log` / `LogWarning` / `LogError` / `LogAssertion`을 씁니다.
- 형식: `[LEVEL] ClassName::MethodName() - Action/State. Details: [VariableName=Value]`
  - 예: `[ERROR] PlayerController::TakeDamage() - Health below zero. Details: [Health=-5]`
- 비밀번호·API 키·토큰은 로그에 남기지 않습니다.
- 에디터 전용 진단 로그는 `#if UNITY_EDITOR`로 감쌉니다.

### 디렉터리
- 폴더 이름은 PascalCase, 공백 대신 `_`나 `-`, 그 밖의 특수문자는 쓰지 않습니다. 목적이 드러나는 이름을 씁니다.

### 주석
- "무엇"이 아니라 "왜"를 씁니다. 보면 아는 코드에는 주석을 달지 않습니다.
- 코드를 바꾸면 주석도 같이 고칩니다. 나중에 할 일은 `// TODO:`로 남깁니다.

### 작성 후 점검
1. 새로 만들거나 고친 이름이 네이밍 표와 맞는지 확인합니다.
2. 새 로그가 로그 형식을 따르는지, 비밀 정보가 없는지 확인합니다.
3. 파일명이 클래스명과 같은지, 새 폴더명이 규칙에 맞는지 확인합니다.
4. 표준과 다르게 둔 부분이 있으면 그 이유를 알려 줍니다.

---

## GitHub 작업 흐름 (GitHub Flow)

목적: `main`을 항상 배포 가능한 상태로 유지합니다.

1. **브랜치 생성:** 작업 하나(기능·버그 수정 단위)마다 `main`에서 새 브랜치를 만듭니다. 이름은 소문자로 시작하는 `유형/짧은-설명` (예: `feature/login-page`, `bugfix/button-color`).
   - 유형: `feature`, `bugfix`, `hotfix`, `refactor`, `perf`, `style`, `chore`, `docs`, `test`, `build`, `ci`, `revert`
   - 다른 PR에 의존하면 그 브랜치에서 시작하고, PR 본문에 병합 순서를 적습니다.
2. **개발·커밋·푸시:** 작은 단위로 자주 커밋합니다.
3. **PR 생성:** 작업이 끝나면 `main` 대상으로 PR을 만듭니다. UI 변경은 스크린샷이나 전후 비교를 붙입니다.
4. **수정:** PR을 연 뒤에는 강제 푸시하지 않고 커밋을 추가합니다. PR 본문(커밋 목록·테스트 결과·이미지)을 실제와 맞게 갱신합니다.
5. **병합:** 병합 전에 최신 `main`과 충돌·테스트 결과를 확인합니다. 병합은 내가 요청할 때만 합니다.
6. **병합 후:** 병합된 작업 브랜치는 확인 후 삭제합니다. `main` 같은 보호 브랜치는 건드리지 않습니다.

### 커밋 메시지
```
type: 제목

본문 (생략 가능 — 무엇을, 왜)

Resolves: #이슈번호 (생략 가능)
Ref: #이슈번호 (생략 가능)
```
- 이슈·태스크 번호가 있으면 `fix(#82): 제목`처럼 type 뒤 괄호에 씁니다.
- 제목은 영문 기준 50자 이내, 끝에 마침표를 찍지 않고, 과거형이 아닌 현재형/명령형으로 씁니다. 한국어로 써도 됩니다.
- 제목과 본문 사이에 빈 줄을 두고, 본문은 "어떻게"보다 "무엇과 왜"를 씁니다.
- 성격이 다른 작업은 가능하면 커밋을 나눕니다. 섞이면 `feat & refactor: ...`로 쓰고 본문에 나눠 적습니다.

| type | 사용 시점 |
|---|---|
| `feat` | 새로운 기능 추가 |
| `fix` | 버그 수정 |
| `docs` | 문서 수정 |
| `style` | 포매팅 등 코드 스타일 변경 (기능 변화 없음) |
| `design` | UI 수정 (기능 변화 없음) |
| `test` | 테스트 코드 추가 |
| `refactor` | 리팩토링 |
| `chore` | .gitignore 등 빌드·설정 수정 |
| `rename` | 파일·폴더 이름만 변경 |
| `remove` | 파일·폴더 삭제만 |

---

## 참고: 같은 내용이 들어 있는 개인 스킬
이 PC의 `C:\Users\설창원\.claude\skills\`에 있어 모든 프로젝트 세션에서 자동으로 쓸 수 있습니다. 다른 PC라면 이 폴더를 복사하거나 위 내용을 `CLAUDE.md`에 붙여 넣으면 됩니다.
- `unity-csharp-standards`: 위 "Unity C# 개발 표준" (원문 전체: `unity-csharp-standards/reference/unity-code-standards.md`)
- `github-flow-workflow`: 위 "GitHub 작업 흐름"

## 참고: 개인 프로젝트에서는 뺀 팀 전용 항목
- Plane 작업 상태 관리(완료 전환 금지, 시작일·종료일 입력 등)
- Mintlify 문서·Decision 대조, Forge Brain 동기화 확인
- TeamAI 동기화 스킬, 팀 PR 템플릿, PR 리뷰 요청 절차
