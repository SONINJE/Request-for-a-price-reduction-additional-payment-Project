// RowEngine.h
// 판매가 인하 / 추가금 요청 리스트의 한 "상품 행(row)"에 대한
// 정방향/역방향(역산) 계산 엔진.
//
// 설계 원칙
// ---------
// 이 시트의 각 컬럼은 서로 수식으로 연결되어 있다 (예: 공급가 = 판매가*(1-수수료)).
// 사용자가 어떤 값을 입력하고 어떤 값을 "구해야 하는 목표(미지수)"로 둘지는
// 상황에 따라 달라진다 (정상 운영 시엔 판매가/원가 등을 입력해서 마진을 구하지만,
// 반대로 "최종 마진율을 5%로 맞추려면 추가요청금액을 얼마로 잡아야 하나?" 처럼
// 거꾸로 계산해야 할 때도 많다).
//
// 그래서 각 컬럼을 하나의 변수로 보고, 컬럼 사이의 관계를 "공식(Formula)"으로
// 등록해둔 뒤, 사용자가 값을 입력한 변수(Known)들로부터 값이 아직 없는 변수를
// 하나씩 풀어나가는 제약 전파(constraint propagation) 방식을 사용한다.
// 즉 "어떤 값을 넣어도 나머지는 역산 가능"이라는 요구사항을 하드코딩된 몇 가지
// 케이스가 아니라 일반적인 방식으로 만족시킨다.
//
// 필요한 최소 입력 개수는 7개이며(자유도 = 변수 14개 - 공식 7개), 어떤 7개를
// 골라 입력하든 나머지 7개가 유일하게 결정되는 조합이면 자동으로 풀린다.
// 풀 수 없는 조합이면(정보 부족/모순) 결과에 그 사실을 알려준다.

#pragma once

#include <string>
#include <vector>
#include <map>
#include <set>
#include <functional>
#include <optional>

namespace pricecalc {

// ---- 필드(컬럼) 이름 상수 ------------------------------------------------
// UI/JSON에서도 동일한 키를 사용한다.
namespace Field {
    constexpr const char* SalePrice        = "salePrice";        // 판매가 (B)
    constexpr const char* FeeRate          = "feeRate";          // 수수료 (E, 0~1 비율)
    constexpr const char* SupplyPrice      = "supplyPrice";      // 공급가 (C)
    constexpr const char* ShippingCost     = "shippingCost";     // 배송비 (D)
    constexpr const char* ExistingSubsidy  = "existingSubsidy";  // 기존지원금 (F)
    constexpr const char* Cost             = "cost";             // 원가 (G)
    constexpr const char* RealCost         = "realCost";         // 실원가 (H)
    constexpr const char* MarginAmount     = "marginAmount";     // 마진액 (I)
    constexpr const char* RequestPlus      = "requestPlus";      // 추가요청금액(+) (J)
    constexpr const char* RequestMinus     = "requestMinus";     // 추가요청금액(-) (K, 부가세 제외)
    constexpr const char* FinalMargin      = "finalMargin";      // 최종마진액 (L)
    constexpr const char* FinalMarginRate  = "finalMarginRate";  // 최종마진율 (M)
    constexpr const char* ExpectedQty      = "expectedQty";      // 예상수량 (O)
    constexpr const char* ExpectedCost     = "expectedCost";     // 예상추가비용(-) (P)
}

// 한 행에 등장하는 모든 숫자 필드 목록 (선언 순서 = UI 표시 순서로 사용해도 됨)
const std::vector<std::string>& AllNumericFields();

// 기존 스프레드시트가 기본으로 사용자가 채워 넣던 7개 입력 필드
// (신규 상품 추가 시 기본 잠금 필드로 사용)
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
SolveResult SolveRow(const std::map<std::string, double>& values,
                      const std::set<std::string>& known);

// 완전히 계산된(또는 일부만 계산된) 값들로부터 합계 행(Total) 계산: 수량/예상비용 합
void ComputeTotals(const std::vector<std::map<std::string, double>>& rows,
                    double& outQtySum, double& outCostSum);

} // namespace pricecalc
