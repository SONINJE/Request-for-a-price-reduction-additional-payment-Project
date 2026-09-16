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

const std::vector<Formula>& AllFormulas() {
    static const std::vector<Formula> formulas = {
        // 공급가 = 판매가 * (1 - 수수료)
        {
            "공급가 = 판매가*(1-수수료)",
            { SalePrice, FeeRate, SupplyPrice },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double B = v[0], E = v[1], C = v[2];
                if (unknown == 0) { // B = C/(1-E)
                    if (std::fabs(1.0 - E) < EPS) return std::nullopt;
                    return C / (1.0 - E);
                } else if (unknown == 1) { // E = 1 - C/B
                    if (std::fabs(B) < EPS) return std::nullopt;
                    return 1.0 - C / B;
                } else { // C = B*(1-E)
                    return B * (1.0 - E);
                }
            },
            [](const std::vector<double>& v) {
                return v[2] - v[0] * (1.0 - v[1]);
            }
        },
        // 실원가 = 원가 - 기존지원금
        {
            "실원가 = 원가-기존지원금",
            { Cost, ExistingSubsidy, RealCost },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double G = v[0], F = v[1], H = v[2];
                if (unknown == 0) return H + F;      // G = H+F
                if (unknown == 1) return G - H;      // F = G-H
                return G - F;                        // H = G-F
            },
            [](const std::vector<double>& v) { return v[2] - (v[0] - v[1]); }
        },
        // 마진액 = 공급가 - 실원가 - 배송비
        {
            "마진액 = 공급가-실원가-배송비",
            { SupplyPrice, RealCost, ShippingCost, MarginAmount },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double C = v[0], H = v[1], D = v[2], I = v[3];
                if (unknown == 0) return I + H + D;  // C
                if (unknown == 1) return C - I - D;  // H
                if (unknown == 2) return C - I - H;  // D
                return C - H - D;                    // I
            },
            [](const std::vector<double>& v) { return v[3] - (v[0] - v[1] - v[2]); }
        },
        // 추가요청금액(-) = 추가요청금액(+) / 1.1   (부가세 제외 환산)
        {
            "추가요청금액(-) = 추가요청금액(+)/1.1",
            { RequestPlus, RequestMinus },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double J = v[0], K = v[1];
                if (unknown == 0) return K * 1.1;    // J
                return J / 1.1;                      // K
            },
            [](const std::vector<double>& v) { return v[1] - v[0] / 1.1; }
        },
        // 최종마진액 = 추가요청금액(+) + 마진액
        {
            "최종마진액 = 추가요청금액(+)+마진액",
            { RequestPlus, MarginAmount, FinalMargin },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double J = v[0], I = v[1], L = v[2];
                if (unknown == 0) return L - I;      // J
                if (unknown == 1) return L - J;      // I
                return J + I;                        // L
            },
            [](const std::vector<double>& v) { return v[2] - (v[0] + v[1]); }
        },
        // 최종마진율 = 최종마진액 / 공급가
        {
            "최종마진율 = 최종마진액/공급가",
            { FinalMargin, SupplyPrice, FinalMarginRate },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double L = v[0], C = v[1], M = v[2];
                if (unknown == 0) return M * C;      // L
                if (unknown == 1) { if (std::fabs(M) < EPS) return std::nullopt; return L / M; } // C
                if (std::fabs(C) < EPS) return std::nullopt;
                return L / C;                         // M
            },
            [](const std::vector<double>& v) {
                if (std::fabs(v[1]) < EPS) return 0.0;
                return v[2] - v[0] / v[1];
            }
        },
        // 예상추가비용(-) = 추가요청금액(-) * 예상수량
        {
            "예상추가비용(-) = 추가요청금액(-)*예상수량",
            { RequestMinus, ExpectedQty, ExpectedCost },
            [](int unknown, const std::vector<double>& v) -> std::optional<double> {
                double K = v[0], O = v[1], P = v[2];
                if (unknown == 0) { if (std::fabs(O) < EPS) return std::nullopt; return P / O; }
                if (unknown == 1) { if (std::fabs(K) < EPS) return std::nullopt; return P / K; }
                return K * O;
            },
            [](const std::vector<double>& v) { return v[2] - v[0] * v[1]; }
        },
    };
    return formulas;
}

} // anonymous namespace

const std::vector<std::string>& AllNumericFields() {
    static const std::vector<std::string> f = {
        SalePrice, SupplyPrice, ShippingCost, FeeRate, ExistingSubsidy, Cost, RealCost,
        MarginAmount, RequestPlus, RequestMinus, FinalMargin, FinalMarginRate,
        ExpectedQty, ExpectedCost
    };
    return f;
}

const std::vector<std::string>& DefaultInputFields() {
    static const std::vector<std::string> f = {
        SalePrice, ShippingCost, FeeRate, ExistingSubsidy, Cost, RequestPlus, ExpectedQty
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

    for (const auto& name : AllNumericFields()) {
        if (solved.count(name)) result.values[name] = values[name];
        else result.unresolved.insert(name);
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

void ComputeTotals(const std::vector<std::map<std::string, double>>& rows,
                    double& outQtySum, double& outCostSum) {
    outQtySum = 0.0;
    outCostSum = 0.0;
    for (const auto& r : rows) {
        auto itQ = r.find(ExpectedQty);
        auto itC = r.find(ExpectedCost);
        if (itQ != r.end()) outQtySum += itQ->second;
        if (itC != r.end()) outCostSum += itC->second;
    }
}

} // namespace pricecalc
