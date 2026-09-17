// EventModel.h
// "행사(이벤트)" 단위 데이터 모델과 JSON 저장/불러오기.
// 행사 하나 = JSON 파일 하나. (요청사항: "행사 상품들은 각각의 JSON으로 관리")
#pragma once
#include <string>
#include <vector>
#include <map>
#include <set>

namespace pricecalc {

struct ProductRow {
    std::string name;                       // 상품명(품목)
    std::string sku;                        // SKU (품목 마스터에서 상품명으로 조회, 필요시 직접 수정 가능)
    std::string channel;                    // 판매 채널 (품목 마스터에 상품별로 미리 지정됨)
    std::string eventType;                  // 행사유형 (자유 텍스트 태그, 예: "버츄얼", "이마트팩")
    std::string note;                       // 비고 (예: 비노출/노출/대량구매 등 자유 텍스트)
    std::map<std::string, double> values;   // 숫자 필드 (RowEngine.h Field 참고)
    std::set<std::string> lockedFields;     // 사용자가 직접 입력(고정)한 필드 목록
};

struct EventFile {
    std::string eventName;   // 행사명 (예: "티딜 10월 21주차 특가 예정")
    std::string startDate;   // 행사 시작일 (yyyy-MM-dd, 비어있으면 미지정)
    std::string endDate;     // 행사 종료일 (yyyy-MM-dd)
    std::string memo;        // 메모
    std::vector<ProductRow> products;
};

// 성공하면 true, 실패(경로 오류/쓰기 실패)면 false 와 errorOut 채움
bool SaveEventToFile(const EventFile& ev, const std::string& filePath, std::string& errorOut);
bool LoadEventFromFile(const std::string& filePath, EventFile& out, std::string& errorOut);

// UI/DLL 경계에서 주고받기 위한 문자열 직렬화
std::string EventToJsonString(const EventFile& ev);
bool EventFromJsonString(const std::string& json, EventFile& out, std::string& errorOut);

std::string RowToJsonString(const ProductRow& row);
bool RowFromJsonString(const std::string& json, ProductRow& out, std::string& errorOut);

} // namespace pricecalc
