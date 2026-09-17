#include "DllApi.h"
#include "RowEngine.h"
#include "EventModel.h"
#include "../third_party/json.hpp"
#include <cstring>
#include <algorithm>

using json = nlohmann::json;
using namespace pricecalc;

namespace {

int WriteOut(const std::string& s, char* outBuffer, int outBufferSize) {
    int needed = (int)s.size();
    if (outBuffer != nullptr && outBufferSize > 0) {
        int toCopy = std::min(needed, outBufferSize - 1);
        if (toCopy > 0) std::memcpy(outBuffer, s.data(), toCopy);
        outBuffer[toCopy < 0 ? 0 : toCopy] = '\0';
    }
    return needed;
}

} // namespace

int PC_SolveRow(const char* requestJsonUtf8, char* outBuffer, int outBufferSize) {
    try {
        auto req = json::parse(requestJsonUtf8 ? requestJsonUtf8 : "{}");
        std::map<std::string, double> values;
        std::set<std::string> known;
        if (req.contains("values"))
            for (auto& [k, v] : req.at("values").items()) values[k] = v.get<double>();
        if (req.contains("known"))
            for (auto& v : req.at("known")) known.insert(v.get<std::string>());

        auto result = SolveRow(values, known);

        json resp;
        resp["ok"] = result.ok;
        resp["message"] = result.message;
        resp["values"] = result.values;
        resp["unresolved"] = result.unresolved;
        resp["conflicts"] = result.conflicts;
        return WriteOut(resp.dump(), outBuffer, outBufferSize);
    } catch (const std::exception&) {
        return -1;
    }
}

int PC_SaveEvent(const char* filePathUtf8, const char* eventJsonUtf8,
                  char* errBuffer, int errBufferSize) {
    try {
        EventFile ev;
        std::string err;
        if (!EventFromJsonString(eventJsonUtf8 ? eventJsonUtf8 : "{}", ev, err)) {
            return WriteOut(err, errBuffer, errBufferSize) > 0 ? -2 : -2;
        }
        std::string saveErr;
        if (!SaveEventToFile(ev, filePathUtf8 ? filePathUtf8 : "", saveErr)) {
            WriteOut(saveErr, errBuffer, errBufferSize);
            return -2;
        }
        return 0;
    } catch (const std::exception& e) {
        WriteOut(e.what(), errBuffer, errBufferSize);
        return -1;
    }
}

int PC_LoadEvent(const char* filePathUtf8, char* outBuffer, int outBufferSize) {
    try {
        EventFile ev;
        std::string err;
        if (!LoadEventFromFile(filePathUtf8 ? filePathUtf8 : "", ev, err)) {
            return WriteOut(std::string("{\"error\":\"") + err + "\"}", outBuffer, outBufferSize);
        }
        return WriteOut(EventToJsonString(ev), outBuffer, outBufferSize);
    } catch (const std::exception&) {
        return -1;
    }
}
