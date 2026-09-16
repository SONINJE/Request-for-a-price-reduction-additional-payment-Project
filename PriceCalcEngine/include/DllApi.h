// DllApi.h
// WPF(C#) 에서 P/Invoke 로 호출하는 C 스타일 진입점.
// 문자열은 모두 UTF-8 로 주고받는다 (C# 쪽에서 Encoding.UTF8 로 마샬링).
//
// 버퍼 규약 (snprintf 스타일):
//   함수는 항상 "필요한 전체 길이(널 문자 제외)"를 반환한다.
//   반환값이 outBufferSize 보다 크거나 같으면, 버퍼가 부족해 내용이 잘렸다는 뜻이므로
//   반환값+1 크기로 버퍼를 늘려 다시 호출해야 한다.
//   음수 반환은 처리 중 예외 발생(치명적 오류)을 의미한다.
#pragma once

#if defined(_WIN32)
  #define PC_API extern "C" __declspec(dllexport)
#else
  #define PC_API extern "C"
#endif

// 요청 JSON: { "values": {필드명: 숫자, ...}, "known": [필드명, ...] }
// 응답 JSON: { "ok": bool, "message": string, "values": {...}, "unresolved": [...], "conflicts": [...] }
PC_API int PC_SolveRow(const char* requestJsonUtf8, char* outBuffer, int outBufferSize);

// 행사(이벤트) 전체를 JSON 파일로 저장한다. eventJsonUtf8 은 EventFile 구조의 JSON.
PC_API int PC_SaveEvent(const char* filePathUtf8, const char* eventJsonUtf8,
                         char* errBuffer, int errBufferSize);

// 행사 JSON 파일을 읽어 그대로 EventFile JSON 문자열로 돌려준다.
PC_API int PC_LoadEvent(const char* filePathUtf8, char* outBuffer, int outBufferSize);

// 여러 행의 합계(예상수량/예상추가비용) 계산. rowsJsonUtf8 은 [{values...}, ...] 배열.
PC_API int PC_ComputeTotals(const char* rowsJsonUtf8, char* outBuffer, int outBufferSize);
