// 간단한 검증용 테스트. Windows/VS 없이도 g++ 로 로직만 확인하기 위한 파일.
// 실행: g++ -std=c++17 -I../include ../src/RowEngine.cpp test_main.cpp -o test_main && ./test_main
#include "RowEngine.h"
#include <iostream>
#include <iomanip>

using namespace pricecalc;

static void printResult(const char* title, const SolveResult& r) {
    std::cout << "== " << title << " == ok=" << r.ok << " msg=" << r.message << "\n";
    for (auto& kv : r.values) std::cout << "  " << kv.first << " = " << kv.second << "\n";
    for (auto& u : r.unresolved) std::cout << "  (미확정) " << u << "\n";
    for (auto& c : r.conflicts) std::cout << "  (모순) " << c << "\n";
}

int main() {
    // 1) 원본 엑셀의 지니오 S 베이직 행과 동일한 값으로 정방향 계산 검증
    {
        std::map<std::string, double> vals = {
            {Field::SalePrice, 79000}, {Field::ShippingCost, 4000}, {Field::FeeRate, 0.15},
            {Field::ExistingSubsidy, 28340}, {Field::Cost, 101320},
            {Field::RequestPlus, 12000}, {Field::ExpectedQty, 30}
        };
        std::set<std::string> known = { Field::SalePrice, Field::ShippingCost, Field::FeeRate,
            Field::ExistingSubsidy, Field::Cost, Field::RequestPlus, Field::ExpectedQty };
        auto r = SolveRow(vals, known);
        printResult("정방향(엑셀과 동일 입력)", r);
        // 기대값: 최종마진액 2170, 최종마진율 ~0.032316
    }

    // 2) 역산 예시: 최종마진율을 5%로 맞추고 싶을 때 추가요청금액(+)을 역산
    {
        std::map<std::string, double> vals = {
            {Field::SalePrice, 79000}, {Field::ShippingCost, 4000}, {Field::FeeRate, 0.15},
            {Field::ExistingSubsidy, 28340}, {Field::Cost, 101320},
            {Field::FinalMarginRate, 0.05}, {Field::ExpectedQty, 30}
        };
        std::set<std::string> known = { Field::SalePrice, Field::ShippingCost, Field::FeeRate,
            Field::ExistingSubsidy, Field::Cost, Field::FinalMarginRate, Field::ExpectedQty };
        auto r = SolveRow(vals, known);
        printResult("역산(목표 최종마진율 5% -> 추가요청금액 역산)", r);
    }

    // 3) 정보 부족 케이스
    {
        std::map<std::string, double> vals = { {Field::SalePrice, 79000} };
        std::set<std::string> known = { Field::SalePrice };
        auto r = SolveRow(vals, known);
        printResult("정보 부족", r);
    }
    return 0;
}
