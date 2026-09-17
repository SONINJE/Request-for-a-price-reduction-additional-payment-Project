using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WpfPriceApp.Interop
{
    /// <summary>
    /// PriceCalcEngine.dll (C++) 의 함수들을 P/Invoke 로 감싼 래퍼.
    /// 문자열은 전부 UTF-8 로 주고받는다.
    /// </summary>
    internal static class NativeMethods
    {
        private const string DllName = "PriceCalcEngine.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PC_SolveRow(byte[] requestJsonUtf8, byte[] outBuffer, int outBufferSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PC_SaveEvent(byte[] filePathUtf8, byte[] eventJsonUtf8, byte[] errBuffer, int errBufferSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PC_LoadEvent(byte[] filePathUtf8, byte[] outBuffer, int outBufferSize);
    }

    /// <summary>
    /// snprintf 스타일 버퍼 규약(필요한 길이를 반환, 부족하면 재시도)을 감싸
    /// 일반 string 인자/반환으로 쓸 수 있게 해주는 헬퍼.
    /// </summary>
    internal static class NativeEngine
    {
        private static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s + "\0");

        private static string CallWithGrowingBuffer(Func<byte[], int, int> call)
        {
            int size = 4096;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var buf = new byte[size];
                int needed = call(buf, size);
                if (needed < 0)
                    throw new InvalidOperationException("PriceCalcEngine 호출 중 오류가 발생했습니다 (code=" + needed + ").");
                if (needed < size)
                    return Encoding.UTF8.GetString(buf, 0, needed);
                size = needed + 1; // 재시도
            }
            throw new InvalidOperationException("PriceCalcEngine 응답이 너무 큽니다.");
        }

        public static string SolveRow(string requestJson)
        {
            var reqBytes = Utf8(requestJson);
            return CallWithGrowingBuffer((buf, size) => NativeMethods.PC_SolveRow(reqBytes, buf, size));
        }

        public static void SaveEvent(string filePath, string eventJson)
        {
            var pathBytes = Utf8(filePath);
            var jsonBytes = Utf8(eventJson);
            var err = new byte[2048];
            int rc = NativeMethods.PC_SaveEvent(pathBytes, jsonBytes, err, err.Length);
            if (rc != 0)
            {
                string msg = Encoding.UTF8.GetString(err).TrimEnd('\0');
                throw new InvalidOperationException(string.IsNullOrEmpty(msg) ? "저장에 실패했습니다." : msg);
            }
        }

        public static string LoadEvent(string filePath)
        {
            var pathBytes = Utf8(filePath);
            return CallWithGrowingBuffer((buf, size) => NativeMethods.PC_LoadEvent(pathBytes, buf, size));
        }
    }
}
