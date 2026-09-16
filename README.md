# 판매가 인하 / 추가금 요청 관리 프로그램

기존 엑셀( `판매가_인하_추가금_요청_리스트.xlsx` )의 업무를 대체하기 위한
**WPF(C#) + C++ 네이티브 엔진** 데스크톱 프로그램입니다.

## 설치해서 바로 쓰기

개발 환경 없이 바로 설치해서 쓰려면 `installer\out\PriceCalcAppSetup.msi` 를 실행하세요.
.NET 런타임을 따로 설치할 필요 없이(자체 포함 배포) 설치 후 시작 메뉴/바탕화면 바로가기로
바로 실행할 수 있습니다. 자세한 사용법은 [`docs/사용자매뉴얼.docx`](docs/사용자매뉴얼.docx) 를 참고하세요.

설치 파일이 없거나 소스를 수정한 뒤 새로 만들어야 한다면:

```powershell
installer\build_installer.ps1
```

위 스크립트가 솔루션 빌드 → self-contained 게시 → `installer\out\PriceCalcAppSetup.msi` 생성까지
한 번에 처리합니다 (Visual Studio 2022, .NET 8 SDK 필요).

## 구성

```
PriceCalcApp.sln
├─ PriceCalcEngine/     C++ 계산 엔진 (DLL) — Visual Studio 2022, x64
│   ├─ include/RowEngine.h      정방향/역산 계산 로직 (제약 전파 방식)
│   ├─ include/EventModel.h     행사(이벤트) JSON 모델
│   ├─ include/DllApi.h         WPF 에서 P/Invoke 로 부르는 C API
│   ├─ src/*.cpp
│   ├─ third_party/json.hpp     nlohmann/json (헤더 1개, 이미 포함됨)
│   └─ tests/test_main.cpp      Windows 없이도 로직만 확인하는 콘솔 테스트
└─ WpfPriceApp/         WPF(C#) UI — .NET 8, net8.0-windows
    ├─ Models/          ProductRow / EventFile (JSON 스키마)
    ├─ ViewModel/        MainViewModel, 필드 정의, 계산 요청/응답
    ├─ Interop/          NativeEngine (P/Invoke 래퍼)
    ├─ Views/            간단한 입력 다이얼로그
    ├─ Themes/Colors.xaml 화이트 톤 UI 테마
    └─ Events/           원본 엑셀에서 추출한 실제 행사 데이터 (JSON, 24개)
```

## 빌드 방법 (Visual Studio 2022)

1. `PriceCalcApp.sln` 을 Visual Studio 2022 로 엽니다. (Desktop C++ 워크로드,
   .NET 데스크톱 개발 워크로드가 설치되어 있어야 합니다.)
2. 플랫폼을 **x64** 로 맞춥니다 (두 프로젝트 모두 x64 로 설정되어 있습니다).
3. `PriceCalcEngine` 프로젝트를 먼저 빌드합니다 → `PriceCalcEngine.dll` 생성.
4. `WpfPriceApp` 프로젝트를 빌드/실행합니다.
   - `WpfPriceApp.csproj` 에 `PriceCalcEngine.dll` 을 빌드 출력 폴더로 자동 복사하는
     설정이 들어있습니다 (두 프로젝트가 같은 솔루션 폴더 아래 있어야 함).
   - 만약 DLL 을 찾지 못한다는 오류가 나면, `PriceCalcEngine\x64\Debug\PriceCalcEngine.dll`
     파일을 `WpfPriceApp` 실행 폴더( `bin\x64\Debug\net8.0-windows\` )에 직접 복사하세요.
5. 처음 실행하면 `Events/` 폴더의 샘플 행사(원본 엑셀에서 추출한 실제 데이터)가
   왼쪽 목록에 나타납니다.

## 사용 방법

- **행사 만들기/불러오기**: 왼쪽에서 새 행사를 만들거나 목록에서 더블클릭해 불러옵니다.
  행사 하나 = JSON 파일 하나로 저장됩니다. **행사 기간**은 자유 텍스트가 아니라
  시작일/종료일 달력(DatePicker) 선택으로 고정되어 있습니다.
- **상품 추가**: "상품 추가" 버튼 → 품목(상품명)과 채널을 드롭다운에서 고르거나 새로 입력
  → 판매가/배송비/수수료/기존지원금/원가/추가요청금액(+)/예상수량 7개 값을 기본 입력값으로
  채우도록 만들어집니다. 새로 입력한 품목/채널은 자동으로 마스터 목록에 등록됩니다.
- **채널/품목 관리**: 툴바의 "채널/품목 관리" 버튼에서 채널·품목 목록을 미리 추가/삭제해둘 수
  있습니다 (`MasterData.json`).
- **값 편집 & 역산**: 표에서 상품을 선택하면 아래쪽에 14개 값 카드가 나타납니다.
  각 카드의 체크박스(🔒)를 켜면 그 값을 직접 입력하는 값(고정값)으로,
  끄면 자동으로 계산되는 값(역산 대상)으로 취급됩니다.
  - **반드시 정확히 7개**를 고정해야 나머지 7개가 유일하게 계산됩니다.
  - 예: 판매가/원가/배송비/수수료/기존지원금/예상수량을 고정하고, "추가요청금액(+)"
    대신 "최종마진율"을 고정해서 5%를 입력하면 → 그 마진율을 맞추기 위해 필요한
    추가요청금액이 역산됩니다.
  - 값을 다 입력했으면 "역산/계산 실행" 버튼을 누릅니다.
- **저장**: "저장" / "다른 이름으로 저장" → 행사 JSON 파일로 저장.
- **CSV 내보내기**: 현재 행사의 상품 표를 CSV 로 저장 (엑셀에서 바로 열림, 한글 깨짐 방지용 BOM 포함).

## 계산 로직 (역산이 항상 되는 이유)

원본 엑셀의 각 컬럼은 아래 7개의 수식으로 서로 묶여 있습니다.

| 수식 | 관련 값 |
|---|---|
| 공급가 = 판매가 × (1-수수료) | 판매가, 수수료, 공급가 |
| 실원가 = 원가 - 기존지원금 | 원가, 기존지원금, 실원가 |
| 마진액 = 공급가 - 실원가 - 배송비 | 공급가, 실원가, 배송비, 마진액 |
| 추가요청금액(-) = 추가요청금액(+) / 1.1 | 추가요청금액(+), 추가요청금액(-) |
| 최종마진액 = 추가요청금액(+) + 마진액 | 추가요청금액(+), 마진액, 최종마진액 |
| 최종마진율 = 최종마진액 / 공급가 | 최종마진액, 공급가, 최종마진율 |
| 예상추가비용(-) = 추가요청금액(-) × 예상수량 | 추가요청금액(-), 예상수량, 예상추가비용(-) |

변수는 총 14개, 수식은 7개이므로 **7개 값을 입력하면 나머지 7개가 정해집니다.**
`RowEngine.cpp` 는 특정 조합을 하드코딩하지 않고, "값이 이미 정해진 변수들로부터
아직 모르는 변수를 하나씩 풀어나가는" 방식(제약 전파)을 쓰기 때문에,
사용자가 **어떤 7개를 고정하든** 풀 수 있는 조합이면 자동으로 역산됩니다.
(풀 수 없는 조합이거나 값끼리 모순이면 화면에 안내 메시지가 표시됩니다.)

## 알려진 이슈 수정 이력

- **빌드 실패(코드페이지 오류)**: C++ 엔진 소스가 BOM 없는 UTF-8이라 MSVC가 시스템 코드페이지(949)로
  잘못 해석해 컴파일이 깨지던 문제 → `PriceCalcEngine.vcxproj` 에 `/utf-8` 옵션 추가로 해결.
- **DLL 배포 경로 불일치**: `.sln` 으로 빌드하면 `PriceCalcEngine.dll` 이 `WpfPriceApp.csproj` 가
  기대하는 `PriceCalcEngine\x64\<Config>\` 가 아니라 솔루션 최상위 `x64\<Config>\` 에 생성되어,
  Debug 빌드는 DLL이 아예 복사되지 않고 Release 빌드는 옛 DLL이 그대로 남는 문제가 있었음 →
  `PriceCalcEngine.vcxproj` 에 `OutDir`/`IntDir` 을 프로젝트 폴더 기준으로 고정해 해결.
- **한글 파일명 로드/저장 실패**: `EventModel.cpp` 가 UTF-8 파일 경로를 narrow-char
  `std::ifstream`/`std::ofstream` 으로 그대로 열어, Windows가 이를 시스템 코드페이지로 해석하면서
  한글이 포함된 모든 행사 파일에서 "파일을 찾을 수 없습니다" 오류가 발생하던 심각한 버그 →
  UTF-8 경로를 UTF-16으로 변환해 여는 방식으로 수정.
- **저장 데이터 위치**: 행사 JSON과 채널/품목 목록을 설치 폴더(Program Files) 밑에 두면 관리자
  권한이 없는 일반 사용자는 쓰기가 막힐 수 있음 → `%LOCALAPPDATA%\PriceCalcApp\` 으로 이전.
  최초 실행 시 설치 폴더에 내장된 샘플 행사 24개를 이 폴더로 자동 복사.

## 신규 기능

- **행사 기간 날짜 선택**: `MainWindow.xaml` 의 기간 입력이 자유 텍스트 `TextBox` 에서
  `DatePicker` 2개(시작일/종료일)로 바뀌었습니다. `EventFile` 의 `period` 필드는
  `startDate`/`endDate` 로 대체되었습니다 (C++/C# 양쪽 모델 동일하게 수정).
- **채널 & 품목 마스터 목록**: `ProductRow` 에 `channel` 필드가 추가되었고, 상품 추가 시
  뜨는 창(`AddProductDialog`)에서 품목/채널을 드롭다운으로 고르거나 새로 입력할 수 있습니다.
  목록은 "채널/품목 관리" 버튼(`MasterDataWindow`)에서 직접 추가/삭제할 수 있고,
  `%LOCALAPPDATA%\PriceCalcApp\MasterData.json` 에 저장됩니다.

## 참고

- `PriceCalcEngine/tests/test_main.cpp` 는 Windows 없이 Linux/g++ 로도 로직만
  검증할 수 있도록 만든 콘솔 테스트입니다 (실제로 원본 엑셀 값과 일치하는지 확인됨).
  ```
  g++ -std=c++17 -Iinclude src/RowEngine.cpp tests/test_main.cpp -o test_main && ./test_main
  ```
- `Events/` 폴더의 JSON 파일들은 업로드하신 엑셀의 10월/11월/12월/카카오선물하기
  시트에 있던 실제 행사와 상품 값을 그대로 변환한 것입니다. 그대로 열어서 확인하거나
  삭제하고 새로 시작해도 됩니다.
