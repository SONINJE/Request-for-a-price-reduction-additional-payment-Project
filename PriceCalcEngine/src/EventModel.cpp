#include "EventModel.h"
#include "../third_party/json.hpp"
#include <fstream>
#include <sstream>

#if defined(_WIN32)
#define NOMINMAX
#include <windows.h>
#endif

using json = nlohmann::json;

namespace pricecalc {

#if defined(_WIN32)
// filePath 는 UTF-8 로 인코딩되어 있다. std::ifstream/ofstream 의 narrow-char
// 생성자는 Windows 에서 현재 ANSI 코드페이지(예: 949)로 경로를 해석하므로,
// 한글 등 비-ASCII 문자가 포함된 경로는 파일을 찾지 못하는 문제가 있었다.
// UTF-8 -> UTF-16 으로 직접 변환해 wide 오버로드(CreateFileW 경로)를 사용한다.
static std::wstring Utf8PathToWide(const std::string& utf8Path) {
    if (utf8Path.empty()) return std::wstring();
    int len = MultiByteToWideChar(CP_UTF8, 0, utf8Path.c_str(), (int)utf8Path.size(), nullptr, 0);
    if (len <= 0) return std::wstring();
    std::wstring wide(len, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, utf8Path.c_str(), (int)utf8Path.size(), wide.data(), len);
    return wide;
}
#endif

static json RowToJson(const ProductRow& row) {
    json j;
    j["name"] = row.name;
    j["sku"] = row.sku;
    j["channel"] = row.channel;
    j["eventType"] = row.eventType;
    j["note"] = row.note;
    j["values"] = row.values;
    j["lockedFields"] = row.lockedFields;
    return j;
}

// 예전 버전(2025-09 이전)의 필드 이름을 새 이름으로 옮겨준다 — 구버전으로 저장된
// 행사 파일을 열었을 때 값/잠금 상태가 조용히 사라지지 않도록 하는 하위호환 처리.
// (예상수량/예상추가비용은 새 구조에 없는 필드라 그대로 버려진다.)
static const std::pair<const char*, const char*> kLegacyFieldRenames[] = {
    {"existingSubsidy", "existingSettlement"},
    {"cost", "purchasePrice"},
    {"realCost", "settledPurchasePrice"},
};

static void MigrateLegacyFieldNames(std::map<std::string, double>& values) {
    for (const auto& [oldKey, newKey] : kLegacyFieldRenames) {
        auto it = values.find(oldKey);
        if (it != values.end() && values.find(newKey) == values.end()) {
            values[newKey] = it->second;
        }
    }
}

static void MigrateLegacyLockedFields(std::set<std::string>& lockedFields) {
    for (const auto& [oldKey, newKey] : kLegacyFieldRenames) {
        if (lockedFields.count(oldKey)) lockedFields.insert(newKey);
    }
}

static ProductRow RowFromJson(const json& j) {
    ProductRow row;
    row.name = j.value("name", "");
    row.sku = j.value("sku", "");
    row.channel = j.value("channel", "");
    row.eventType = j.value("eventType", "");
    row.note = j.value("note", "");
    if (j.contains("values"))
        for (auto& [k, v] : j.at("values").items()) row.values[k] = v.get<double>();
    MigrateLegacyFieldNames(row.values);
    if (j.contains("lockedFields"))
        for (auto& v : j.at("lockedFields")) row.lockedFields.insert(v.get<std::string>());
    MigrateLegacyLockedFields(row.lockedFields);
    return row;
}

std::string RowToJsonString(const ProductRow& row) {
    return RowToJson(row).dump(2);
}

bool RowFromJsonString(const std::string& jsonStr, ProductRow& out, std::string& errorOut) {
    try {
        auto j = json::parse(jsonStr);
        out = RowFromJson(j);
        return true;
    } catch (const std::exception& e) {
        errorOut = e.what();
        return false;
    }
}

static json EventToJson(const EventFile& ev) {
    json j;
    j["eventName"] = ev.eventName;
    j["startDate"] = ev.startDate;
    j["endDate"] = ev.endDate;
    j["memo"] = ev.memo;
    j["products"] = json::array();
    for (const auto& p : ev.products) j["products"].push_back(RowToJson(p));
    return j;
}

static EventFile EventFromJson(const json& j) {
    EventFile ev;
    ev.eventName = j.value("eventName", "");
    ev.startDate = j.value("startDate", "");
    ev.endDate = j.value("endDate", "");
    ev.memo = j.value("memo", "");
    if (j.contains("products"))
        for (auto& pj : j.at("products")) ev.products.push_back(RowFromJson(pj));
    return ev;
}

std::string EventToJsonString(const EventFile& ev) {
    return EventToJson(ev).dump(2);
}

bool EventFromJsonString(const std::string& jsonStr, EventFile& out, std::string& errorOut) {
    try {
        auto j = json::parse(jsonStr);
        out = EventFromJson(j);
        return true;
    } catch (const std::exception& e) {
        errorOut = e.what();
        return false;
    }
}

bool SaveEventToFile(const EventFile& ev, const std::string& filePath, std::string& errorOut) {
    try {
#if defined(_WIN32)
        std::ofstream ofs(Utf8PathToWide(filePath), std::ios::binary);
#else
        std::ofstream ofs(filePath, std::ios::binary);
#endif
        if (!ofs.is_open()) { errorOut = "파일을 열 수 없습니다: " + filePath; return false; }
        ofs << EventToJsonString(ev);
        return true;
    } catch (const std::exception& e) {
        errorOut = e.what();
        return false;
    }
}

bool LoadEventFromFile(const std::string& filePath, EventFile& out, std::string& errorOut) {
    try {
#if defined(_WIN32)
        std::ifstream ifs(Utf8PathToWide(filePath), std::ios::binary);
#else
        std::ifstream ifs(filePath, std::ios::binary);
#endif
        if (!ifs.is_open()) { errorOut = "파일을 찾을 수 없습니다: " + filePath; return false; }
        std::ostringstream oss;
        oss << ifs.rdbuf();
        return EventFromJsonString(oss.str(), out, errorOut);
    } catch (const std::exception& e) {
        errorOut = e.what();
        return false;
    }
}

} // namespace pricecalc
