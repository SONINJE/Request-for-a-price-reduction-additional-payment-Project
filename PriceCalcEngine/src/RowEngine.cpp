#include "RowEngine.h"
#include <cmath>
#include <sstream>

namespace pricecalc {

using namespace Field;

namespace {

constexpr double EPS = 1e-6;

// 공식 하나 = 관련된 변수 이름들 + "이 변수들 중 unknown 인덱스 하나를 풀어라"
// solver(i, vals) : vals 에는 i번째를 제외한 값들이 채워져 있다고 가정하고 i번째 값을 반환.
// 나눗셈이 필요한 경우 분모가 0이면 std::nullopt 반환.
struct Formula {
    std::string name;                              // 설명용
    std::vector<std::string> vars;                 // 이 공식에 참여하는 필드들
    std::function<std::optional<double>(int, const std::vector<double>&)> solve;
    // 검증용: 모든 값이 채워졌을 때 잔차(residual)를 계산 (0이면 일치)
    std::function<double(const std::vector<double>&)> residual;
};

// 역산 제약 전파 그래프(자유도 8 = 변수 16 - 공식 8). 기지원금(SupportAmount)은
// ROUNDUP 때문에 역으로 풀 수 없어 여기 포함하지 않고 SolveRow 마지막에 순방향으로만 계산한다.
const std::vector<Formula>& AllFormulas() {
    static const std::vector<Formula> formulas = {
        // 채널>공급가 = 판매가 * (1 - 수수료)
        {
            "채널>공급가 = 판매가*(1-수수료)",
            { SalePrice, FeeRate, SupplyPrice },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double SP = v[0], FR = v[1], SU = v[2];
                if (unknown == 0) {
                    if (std::fabs(1.0 - FR) < EPS) return std::nullopt;
                    return SU / (1.0 - FR);
                } else if (unknown == 1) {
                    if (std::fabs(SP) < EPS) return std::nullopt;
                    return 1.0 - SU / SP;
                } else {
                    return SP * (1.0 - FR);
                }
            },
            [](const std::vector<double>& v) { return v[2] - v[0] * (1.0 - v[1]); }
        },
        // 정산액포함매입가(+) = 매입가(+) - 기존정산액(+)
        {
            "정산액포함매입가(+) = 매입가(+)-기존정산액(+)",
            { PurchasePrice, ExistingSettlement, SettledPurchasePrice },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double PP = v[0], ES = v[1], SPP = v[2];
                if (unknown == 0) return SPP + ES;
                if (unknown == 1) return PP - SPP;
                return PP - ES;
            },
            [](const std::vector<double>& v) { return v[2] - (v[0] - v[1]); }
        },
        // 마진액 = 채널>공급가 - 정산액포함매입가(+) - 배송비 - 캡슐금액 - 쿠폰액 분담 쿠폰
        {
            "마진액 = 공급가-정산액포함매입가-배송비-캡슐금액-쿠폰액",
            { SupplyPrice, SettledPurchasePrice, ShippingCost, CapsuleAmount, CouponAmount, MarginAmount },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double SU = v[0], SPP = v[1], SH = v[2], CA = v[3], CO = v[4], MA = v[5];
                switch (unknown) {
                    case 0: return MA + SPP + SH + CA + CO;   // SU
                    case 1: return SU - MA - SH - CA - CO;    // SPP
                    case 2: return SU - SPP - CA - CO - MA;   // SH
                    case 3: return SU - SPP - SH - CO - MA;   // CA
                    case 4: return SU - SPP - SH - CA - MA;   // CO
                    default: return SU - SPP - SH - CA - CO;  // MA
                }
            },
            [](const std::vector<double>& v) { return v[5] - (v[0] - v[1] - v[2] - v[3] - v[4]); }
        },
        // 추가요청금액(-) = 추가요청금액(+) / 1.1   (부가세 제외 환산)
        {
            "추가요청금액(-) = 추가요청금액(+)/1.1",
            { RequestPlus, RequestMinus },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double RP = v[0], RM = v[1];
                if (unknown == 0) return RM * 1.1;
                return RP / 1.1;
            },
            [](const std::vector<double>& v) { return v[1] - v[0] / 1.1; }
        },
        // 최종마진액 = 추가요청금액(+) + 마진액
        {
            "최종마진액 = 추가요청금액(+)+마진액",
            { RequestPlus, MarginAmount, FinalMargin },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double RP = v[0], MA = v[1], FM = v[2];
                if (unknown == 0) return FM - MA;
                if (unknown == 1) return FM - RP;
                return RP + MA;
            },
            [](const std::vector<double>& v) { return v[2] - (v[0] + v[1]); }
        },
        // 최종마진율 = 최종마진액 / 채널>공급가
        {
            "최종마진율 = 최종마진액/공급가",
            { FinalMargin, SupplyPrice, FinalMarginRate },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double FM = v[0], SU = v[1], FMR = v[2];
                if (unknown == 0) return FMR * SU;
                if (unknown == 1) { if (std::fabs(FMR) < EPS) return std::nullopt; return FM / FMR; }
                if (std::fabs(SU) < EPS) return std::nullopt;
                return FM / SU;
            },
            [](const std::vector<double>& v) {
                if (std::fabs(v[1]) < EPS) return 0.0;
                return v[2] - v[0] / v[1];
            }
        },
        // 본사(확인용)vat+ = 추가요청금액(+) + 기존정산액(+)
        {
            "본사(확인용)vat+ = 추가요청금액(+)+기존정산액(+)",
            { RequestPlus, ExistingSettlement, HqCheckVatPlus },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double RP = v[0], ES = v[1], HP = v[2];
                if (unknown == 0) return HP - ES;
                if (unknown == 1) return HP - RP;
                return RP + ES;
            },
            [](const std::vector<double>& v) { return v[2] - (v[0] + v[1]); }
        },
        // 본사(확인용)vat- = 본사(확인용)vat+ / 1.1
        {
            "본사(확인용)vat- = 본사(확인용)vat+/1.1",
            { HqCheckVatPlus, HqCheckVatMinus },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double HP = v[0], HM = v[1];
                if (unknown == 0) return HM * 1.1;
                return HP / 1.1;
            },
            [](const std::vector<double>& v) { return v[1] - v[0] / 1.1; }
        },
    };
    return formulas;
}

// Excel ROUNDUP(x, -3) 과 동일: 0에서 먼 방향으로 1000 단위로 올림.
double RoundUpTo1000(double x) {
    if (x == 0.0) return 0.0;
    double sign = x > 0 ? 1.0 : -1.0;
    return sign * std::ceil(std::fabs(x) / 1000.0) * 1000.0;
}

} // anonymous namespace

const std::vector<std::string>& SolvableFields() {
    static const std::vector<std::string> f = {
        SalePrice, SupplyPrice, ShippingCost, FeeRate, ExistingSettlement, PurchasePrice,
        SettledPurchasePrice, CapsuleAmount, CouponAmount, MarginAmount, RequestPlus, RequestMinus,
        FinalMargin, FinalMarginRate, HqCheckVatPlus, HqCheckVatMinus
    };
    return f;
}

const std::vector<std::string>& AllNumericFields() {
    static const std::vector<std::string> f = [] {
        std::vector<std::string> v = SolvableFields();
        v.push_back(SupportAmount);
        return v;
    }();
    return f;
}

const std::vector<std::string>& DefaultInputFields() {
    static const std::vector<std::string> f = {
        SalePrice, ShippingCost, FeeRate, ExistingSettlement, PurchasePrice, RequestPlus,
        CapsuleAmount, CouponAmount
    };
    return f;
}

SolveResult SolveRow(const std::map<std::string, double>& valuesIn,
                      const std::set<std::string>& known) {
    SolveResult result;
    std::map<std::string, double> values = valuesIn;
    std::set<std::string> solved = known;

    bool progressed = true;
    while (progressed) {
        progressed = false;
        for (const auto& f : AllFormulas()) {
            int unknownIdx = -1;
            int unknownCount = 0;
            for (size_t i = 0; i < f.vars.size(); ++i) {
                if (!solved.count(f.vars[i])) { unknownIdx = (int)i; unknownCount++; }
            }
            if (unknownCount == 1) {
                std::vector<double> v(f.vars.size(), 0.0);
                for (size_t i = 0; i < f.vars.size(); ++i)
                    if ((int)i != unknownIdx) v[i] = values[f.vars[i]];
                auto r = f.solve(unknownIdx, v);
                if (r.has_value()) {
                    values[f.vars[unknownIdx]] = *r;
                    solved.insert(f.vars[unknownIdx]);
                    progressed = true;
                } else {
                    result.conflicts.push_back(f.name + " : 0으로 나누기 등으로 계산할 수 없습니다.");
                }
            }
        }
    }

    // 모든 변수가 이미 채워진(known 이 많아 과결정된) 공식은 정합성 검사
    for (const auto& f : AllFormulas()) {
        bool allKnown = true;
        std::vector<double> v;
        v.reserve(f.vars.size());
        for (const auto& name : f.vars) {
            if (!solved.count(name)) { allKnown = false; break; }
            v.push_back(values[name]);
        }
        if (allKnown) {
            double res = f.residual(v);
            if (std::fabs(res) > 1.0) { // 1원 이상 오차면 모순으로 간주
                std::ostringstream oss;
                oss << f.name << " 공식이 입력값과 맞지 않습니다 (오차 " << res << ").";
                result.conflicts.push_back(oss.str());
            }
        }
    }

    for (const auto& name : SolvableFields()) {
        if (solved.count(name)) result.values[name] = values[name];
        else result.unresolved.insert(name);
    }

    // 기지원금은 역산 그래프 밖에서, 마진액/정산액포함매입가가 둘 다 풀렸을 때만 순방향으로 계산.
    if (solved.count(MarginAmount) && solved.count(SettledPurchasePrice)) {
        double supportAmount = RoundUpTo1000(std::fabs(values[MarginAmount]) + values[SettledPurchasePrice] * 0.05);
        result.values[SupportAmount] = supportAmount;
    } else {
        result.unresolved.insert(SupportAmount);
    }

    result.ok = result.unresolved.empty() && result.conflicts.empty();
    if (result.ok) {
        result.message = "계산 완료";
    } else if (!result.conflicts.empty()) {
        result.message = "입력값 사이에 모순이 있습니다.";
    } else {
        std::ostringstream oss;
        oss << "값을 확정할 수 없는 필드가 " << result.unresolved.size()
            << "개 남았습니다. 입력값을 더 지정해주세요.";
        result.message = oss.str();
    }
    return result;
}

} // namespace pricecalc
