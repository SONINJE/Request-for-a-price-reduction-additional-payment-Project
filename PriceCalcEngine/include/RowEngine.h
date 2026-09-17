// RowEngine.h
// 판매가 인하 / 추가금 요청 리스트의 한 "상품 행(row)"에 대한
// 정방향/역방향(역산) 계산 엔진.
//
// 2026-09 개편: 실제로 쓰고 있는 최신 엑셀(품목 마스터 VLOOKUP 기반) 구조에 맞춰
// 필드/공식을 다시 정리했다. 매입가(+)/기존정산액(+)은 품목 마스터에서 상품명으로
// 조회해 기본값을 채우지만, 이 엔진 입장에서는 여전히 "값이 있고 잠글 수 있는 변수"
// 로 취급한다(자동으로 채워졌더라도 사용자가 고정/해제하고 역산 대상으로 둘 수 있음).
//
// 설계 원칙
// ---------
// 이 시트의 각 컬럼은 서로 수식으로 연결되어 있다 (예: 공급가 = 판매가*(1-수수료)).
// 사용자가 어떤 값을 입력하고 어떤 값을 "구해야 하는 목표(미지수)"로 둘지는
// 상황에 따라 달라진다 (정상 운영 시엔 판매가/매입가 등을 입력해서 마진을 구하지만,
// 반대로 "최종 마진율을 5%로 맞추려면 추가요청금액을 얼마로 잡아야 하나?" 처럼
// 거꾸로 계산해야 할 때도 많다).
//
// 그래서 각 컬럼을 하나의 변수로 보고, 컬럼 사이의 관계를 "공식(Formula)"으로
// 등록해둔 뒤, 사용자가 값을 입력한 변수(Known)들로부터 값이 아직 없는 변수를
// 하나씩 풀어나가는 제약 전파(constraint propagation) 방식을 사용한다.
//
// 자유도 = 변수 16개(역산 가능한 것만) - 공식 8개 = 8. 어떤 8개를 골라 입력하든
// 나머지 8개가 유일하게 결정되는 조합이면 자동으로 풀린다.
//
// "기지원금"은 ROUNDUP(절대값...) 이 들어가 역으로 풀 수 없는(정보가 손실되는)
// 단방향 공식이라 이 제약 전파 그래프에는 포함하지 않는다 — 마진액/정산액포함매입가가
// 둘 다 정해지면 그때 한 번 순방향으로만 계산한다 (SolveResult::supportAmount).

#pragma once

#include <string>
#include <vector>
#include <map>
#include <set>
#include <functional>
#include <optional>

namespace pricecalc {

// ---- 필드(컬럼) 이름 상수 ------------------------------------------------
// UI/JSON에서도 동일한 키를 사용한다. 괄호 안은 원본 엑셀 컬럼 문자.
namespace Field {
    constexpr const char* SalePrice            = "salePrice";            // 판매가 (G)
    constexpr const char* SupplyPrice           = "supplyPrice";          // 채널>공급가 (H)
    constexpr const char* ShippingCost          = "shippingCost";         // 배송비 (I)
    constexpr const char* FeeRate                = "feeRate";              // 수수료 (J, 0~1 비율)
    constexpr const char* ExistingSettlement    = "existingSettlement";   // 기존정산액(+) (K)
    constexpr const char* PurchasePrice          = "purchasePrice";        // 매입가(+) (L)
    constexpr const char* SettledPurchasePrice  = "settledPurchasePrice"; // 정산액포함매입가(+) (M)
    constexpr const char* CapsuleAmount          = "capsuleAmount";        // 캡슐금액 (N)
    constexpr const char* CouponAmount           = "couponAmount";         // 쿠폰액 분담 쿠폰 (O)
    constexpr const char* MarginAmount           = "marginAmount";         // 마진액 (P)
    constexpr const char* RequestPlus            = "requestPlus";          // 추가요청금액(+) (Q)
    constexpr const char* RequestMinus           = "requestMinus";         // 추가요청금액(-) (R)
    constexpr const char* FinalMargin            = "finalMargin";          // 최종마진액 (S)
    constexpr const char* FinalMarginRate        = "finalMarginRate";      // 최종마진율 (T)
    constexpr const char* HqCheckVatPlus         = "hqCheckVatPlus";       // 본사(확인용)vat+ (U)
    constexpr const char* HqCheckVatMinus        = "hqCheckVatMinus";      // 본사(확인용)vat- (V)
    constexpr const char* SupportAmount          = "supportAmount";        // 기지원금 (W) — 단방향 계산만
}

// 한 행에 등장하는 모든 숫자 필드 목록 (선언 순서 = UI 표시 순서 = 원본 엑셀 컬럼 순서).
// 기지원금(SupportAmount)도 포함한다 — 표시는 되지만 잠금/역산 대상은 아니다.
const std::vector<std::string>& AllNumericFields();

// 역산 제약 전파 그래프에 실제로 들어가는 필드(기지원금 제외). 잠글 수 있는 필드 목록이며
// 자유도(=이 목록 크기 - 공식 개수)만큼 정확히 잠가야 나머지가 유일하게 결정된다.
const std::vector<std::string>& SolvableFields();

// 신규 상품 추가 시 기본으로 "고정(입력)" 표시할 필드 (자유도와 같은 개수)
const std::vector<std::string>& DefaultInputFields();

struct SolveResult {
    bool ok = false;                    // 전체 필드가 모순 없이 결정되었는가
    std::map<std::string, double> values;      // 계산 후 값 (알 수 없는 값은 들어있지 않음)
    std::set<std::string> unresolved;          // 끝내 값을 구하지 못한 필드
    std::vector<std::string> conflicts;        // 입력값끼리 모순이 발견된 공식 설명
    std::string message;                       // 사용자에게 보여줄 요약 메시지 (한국어)
};

// 값이 채워진 필드(known)와 값 맵을 받아 나머지를 역산/정산한다.
// values 에는 known 에 속한 필드만 유효한 값이 들어있으면 된다.
// 마진액과 정산액포함매입가가 둘 다 풀리면 기지원금(SupportAmount)도 함께 계산해 넣는다.
SolveResult SolveRow(const std::map<std::string, double>& values,
                      const std::set<std::string>& known);

} // namespace pricecalc
