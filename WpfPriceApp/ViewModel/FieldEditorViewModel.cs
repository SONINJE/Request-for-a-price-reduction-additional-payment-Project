using System.ComponentModel;

namespace WpfPriceApp.ViewModel
{
    /// <summary>
    /// 하단 상세 편집 패널의 한 줄. 어떤 필드든 잠금(입력)/해제(계산 대상)를
    /// 사용자가 자유롭게 토글할 수 있게 하여 "값을 넣으면 나머지가 역산되는" 동작을 구현한다.
    /// </summary>
    public class FieldEditorViewModel : INotifyPropertyChanged
    {
        public string Key { get; }
        public string Label { get; }

        private string _text = "0";
        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(nameof(Text)); }
        }

        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set { _isLocked = value; OnPropertyChanged(nameof(IsLocked)); }
        }

        public FieldEditorViewModel(string key, string label, double value, bool isLocked)
        {
            Key = key;
            Label = label;
            _text = value.ToString("0.####");
            _isLocked = isLocked;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
