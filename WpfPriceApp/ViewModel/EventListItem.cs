using System;

namespace WpfPriceApp.ViewModel
{
    /// <summary>왼쪽 행사 목록에 보여줄 요약 정보 (파일을 전부 열지 않고 이름/기간만 미리 읽어둔 것).</summary>
    public class EventListItem
    {
        public string FilePath { get; }
        public string DisplayName { get; }
        public DateTime? StartDate { get; }
        public DateTime? EndDate { get; }

        public EventListItem(string filePath, string displayName, DateTime? startDate, DateTime? endDate)
        {
            FilePath = filePath;
            DisplayName = displayName;
            StartDate = startDate;
            EndDate = endDate;
        }

        public string PeriodText
        {
            get
            {
                if (StartDate == null && EndDate == null) return "기간 미지정";
                string s = StartDate?.ToString("yyyy-MM-dd") ?? "?";
                string e = EndDate?.ToString("yyyy-MM-dd") ?? "?";
                return $"{s} ~ {e}";
            }
        }
    }
}
