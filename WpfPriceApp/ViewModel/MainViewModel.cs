using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using WpfPriceApp.Interop;
using WpfPriceApp.Models;

namespace WpfPriceApp.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
        private const string DateFormat = "yyyy-MM-dd";

        public string EventsFolder { get; }
        private readonly string _masterDataFilePath;

        public ObservableCollection<string> EventFiles { get; } = new();
        public ObservableCollection<ProductRowViewModel> Rows { get; } = new();
        public ObservableCollection<FieldEditorViewModel> FieldEditors { get; } = new();

        /// <summary>상품 추가/편집 시 고를 수 있는 채널 목록 (MasterData.json 에 저장됨).</summary>
        public ObservableCollection<string> Channels { get; } = new();
        /// <summary>상품 추가 시 고를 수 있는 품목(상품명) 목록 (MasterData.json 에 저장됨).</summary>
        public ObservableCollection<string> Items { get; } = new();

        private EventFile _currentEvent = new();
        private string? _currentFilePath;

        public string EventName
        {
            get => _currentEvent.EventName;
            set { _currentEvent.EventName = value; OnPropertyChanged(nameof(EventName)); }
        }

        public DateTime? StartDate
        {
            get => ParseDate(_currentEvent.StartDate);
            set { _currentEvent.StartDate = FormatDate(value); OnPropertyChanged(nameof(StartDate)); }
        }

        public DateTime? EndDate
        {
            get => ParseDate(_currentEvent.EndDate);
            set { _currentEvent.EndDate = FormatDate(value); OnPropertyChanged(nameof(EndDate)); }
        }

        private static DateTime? ParseDate(string s) =>
            DateTime.TryParseExact(s, DateFormat, null, System.Globalization.DateTimeStyles.None, out var d) ? d : null;
        private static string FormatDate(DateTime? d) => d?.ToString(DateFormat) ?? "";

        public string Memo
        {
            get => _currentEvent.Memo;
            set { _currentEvent.Memo = value; OnPropertyChanged(nameof(Memo)); }
        }

        private ProductRowViewModel? _selectedRow;
        public ProductRowViewModel? SelectedRow
        {
            get => _selectedRow;
            set { _selectedRow = value; OnPropertyChanged(nameof(SelectedRow)); RebuildFieldEditors(); }
        }

        private string _statusMessage = "행사를 새로 만들거나 왼쪽 목록에서 불러오세요.";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        public string LockStatusText
        {
            get
            {
                int locked = FieldEditors.Count(f => f.IsLocked);
                return $"고정(입력)된 값: {locked} / {FieldNames.RequiredKnownCount}개 필요";
            }
        }

        public RelayCommand NewEventCommand { get; }
        public RelayCommand LoadEventCommand { get; }
        public RelayCommand SaveEventCommand { get; }
        public RelayCommand SaveAsCommand { get; }
        public RelayCommand AddProductCommand { get; }
        public RelayCommand DeleteProductCommand { get; }
        public RelayCommand RecalculateCommand { get; }
        public RelayCommand ExportCsvCommand { get; }
        public RelayCommand RefreshListCommand { get; }
        public RelayCommand ManageMasterDataCommand { get; }

        public MainViewModel()
        {
            // 설치 폴더(Program Files)는 일반 사용자 권한으로 쓰기가 안 될 수 있으므로,
            // 실제 데이터(행사 JSON, 채널/품목 목록)는 사용자별 쓰기 가능한 폴더에 둔다.
            string dataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PriceCalcApp");
            Directory.CreateDirectory(dataRoot);

            EventsFolder = Path.Combine(dataRoot, "Events");
            bool firstRun = !Directory.Exists(EventsFolder);
            Directory.CreateDirectory(EventsFolder);
            if (firstRun) SeedSampleEvents();

            _masterDataFilePath = Path.Combine(dataRoot, "MasterData.json");
            LoadMasterData();

            NewEventCommand = new RelayCommand(_ => NewEvent());
            LoadEventCommand = new RelayCommand(p => LoadEvent(p as string));
            SaveEventCommand = new RelayCommand(_ => SaveEvent(false));
            SaveAsCommand = new RelayCommand(_ => SaveEvent(true));
            AddProductCommand = new RelayCommand(_ => AddProduct());
            DeleteProductCommand = new RelayCommand(_ => DeleteProduct(), _ => SelectedRow != null);
            RecalculateCommand = new RelayCommand(_ => Recalculate(), _ => SelectedRow != null);
            ExportCsvCommand = new RelayCommand(_ => ExportCsv());
            RefreshListCommand = new RelayCommand(_ => RefreshEventList());
            ManageMasterDataCommand = new RelayCommand(_ => ManageMasterData());

            RefreshEventList();
        }

        /// <summary>처음 실행할 때만: 설치 폴더에 같이 배포된 샘플 행사 JSON을 사용자 데이터 폴더로 복사.</summary>
        private void SeedSampleEvents()
        {
            string bundled = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Events");
            if (!Directory.Exists(bundled)) return;
            foreach (var src in Directory.GetFiles(bundled, "*.json"))
            {
                string dest = Path.Combine(EventsFolder, Path.GetFileName(src));
                try { File.Copy(src, dest, overwrite: false); } catch (IOException) { /* 이미 있으면 무시 */ }
            }
        }

        // ---------------- 채널/품목 마스터 목록 ----------------

        private void LoadMasterData()
        {
            Models.MasterData data = new();
            try
            {
                if (File.Exists(_masterDataFilePath))
                {
                    string json = File.ReadAllText(_masterDataFilePath);
                    data = JsonSerializer.Deserialize<Models.MasterData>(json, JsonOpts) ?? new();
                }
            }
            catch (Exception)
            {
                data = new();
            }
            Channels.Clear();
            foreach (var c in data.Channels) Channels.Add(c);
            Items.Clear();
            foreach (var i in data.Items) Items.Add(i);
        }

        private void SaveMasterData()
        {
            try
            {
                var data = new Models.MasterData
                {
                    Channels = Channels.ToList(),
                    Items = Items.ToList(),
                };
                File.WriteAllText(_masterDataFilePath, JsonSerializer.Serialize(data, JsonOpts));
            }
            catch (Exception ex)
            {
                MessageBox.Show("채널/품목 목록을 저장하지 못했습니다.\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageMasterData()
        {
            var dlg = new Views.MasterDataWindow(Channels, Items) { Owner = Application.Current.MainWindow };
            dlg.ShowDialog();
            SaveMasterData();
        }

        /// <summary>목록에 없는 새 채널/품목이면 추가해둔다 (상품 추가 다이얼로그에서 새로 입력한 경우).</summary>
        private void RegisterIfNew(ObservableCollection<string> list, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!list.Contains(value))
            {
                list.Add(value);
                SaveMasterData();
            }
        }

        // ---------------- 행사 목록/불러오기/저장 ----------------

        public void RefreshEventList()
        {
            EventFiles.Clear();
            foreach (var f in Directory.GetFiles(EventsFolder, "*.json").OrderBy(f => f))
                EventFiles.Add(f);
        }

        private void NewEvent()
        {
            var dlg = new Views.TextInputDialog("새 행사 이름을 입력하세요.", "새 행사");
            if (dlg.ShowDialog() != true) return;

            _currentEvent = new EventFile { EventName = dlg.InputText };
            _currentFilePath = null;
            Rows.Clear();
            FieldEditors.Clear();
            OnPropertyChanged(nameof(EventName));
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            OnPropertyChanged(nameof(Memo));
            StatusMessage = "새 행사를 만들었습니다. 상품을 추가한 뒤 저장하세요.";
        }

        public void LoadEvent(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            try
            {
                string json = NativeEngine.LoadEvent(filePath);
                var ev = JsonSerializer.Deserialize<EventFile>(json, JsonOpts);
                if (ev == null) throw new InvalidOperationException("행사 파일 형식이 올바르지 않습니다.");

                _currentEvent = ev;
                _currentFilePath = filePath;
                Rows.Clear();
                foreach (var p in ev.Products) Rows.Add(new ProductRowViewModel(p));
                OnPropertyChanged(nameof(EventName));
                OnPropertyChanged(nameof(StartDate));
                OnPropertyChanged(nameof(EndDate));
                OnPropertyChanged(nameof(Memo));
                SelectedRow = Rows.FirstOrDefault();
                StatusMessage = $"'{ev.EventName}' 불러왔습니다. (상품 {Rows.Count}개)";
            }
            catch (Exception ex)
            {
                MessageBox.Show("행사를 불러오지 못했습니다.\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveEvent(bool forceSaveAs)
        {
            try
            {
                string path = _currentFilePath ?? "";
                if (forceSaveAs || string.IsNullOrEmpty(path))
                {
                    string safeName = SanitizeFileName(_currentEvent.EventName);
                    var dlg = new SaveFileDialog
                    {
                        InitialDirectory = EventsFolder,
                        FileName = safeName + ".json",
                        Filter = "행사 JSON 파일 (*.json)|*.json"
                    };
                    if (dlg.ShowDialog() != true) return;
                    path = dlg.FileName;
                }

                _currentEvent.Products = Rows.Select(r => r.Model).ToList();
                string json = JsonSerializer.Serialize(_currentEvent, JsonOpts);
                NativeEngine.SaveEvent(path, json);
                _currentFilePath = path;
                RefreshEventList();
                StatusMessage = $"저장했습니다: {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("저장하지 못했습니다.\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "새행사" : name;
        }

        // ---------------- 상품(행) 추가/삭제 ----------------

        private void AddProduct()
        {
            var dlg = new Views.AddProductDialog(Items, Channels) { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() != true) return;

            var row = new ProductRow { Name = dlg.ItemName, Channel = dlg.Channel };
            foreach (var key in FieldNames.DefaultLocked)
            {
                row.Values[key] = 0;
                row.LockedFields.Add(key);
            }
            var vm = new ProductRowViewModel(row);
            Rows.Add(vm);
            SelectedRow = vm;

            RegisterIfNew(Items, row.Name);
            RegisterIfNew(Channels, row.Channel);

            StatusMessage = $"'{row.Name}' 상품을 추가했습니다. 값을 입력한 뒤 계산하세요.";
        }

        private void DeleteProduct()
        {
            if (SelectedRow == null) return;
            var name = SelectedRow.Name;
            Rows.Remove(SelectedRow);
            SelectedRow = Rows.FirstOrDefault();
            StatusMessage = $"'{name}' 상품을 삭제했습니다.";
        }

        // ---------------- 값 편집 / 역산 계산 ----------------

        private void RebuildFieldEditors()
        {
            FieldEditors.Clear();
            if (SelectedRow == null) { OnPropertyChanged(nameof(LockStatusText)); return; }

            foreach (var (key, label) in FieldNames.All)
            {
                var fe = new FieldEditorViewModel(key, label,
                    SelectedRow.Model.GetValue(key), SelectedRow.Model.IsLocked(key));
                fe.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(FieldEditorViewModel.IsLocked))
                        OnPropertyChanged(nameof(LockStatusText));
                };
                FieldEditors.Add(fe);
            }
            OnPropertyChanged(nameof(LockStatusText));
        }

        private void Recalculate()
        {
            if (SelectedRow == null) return;

            int lockedCount = FieldEditors.Count(f => f.IsLocked);
            if (lockedCount != FieldNames.RequiredKnownCount)
            {
                StatusMessage = $"고정(입력)된 값이 {lockedCount}개입니다. " +
                                 $"정확히 {FieldNames.RequiredKnownCount}개를 고정해야 나머지를 역산할 수 있습니다.";
                return;
            }

            var values = new System.Collections.Generic.Dictionary<string, double>();
            var known = new System.Collections.Generic.List<string>();
            foreach (var f in FieldEditors)
            {
                if (!f.IsLocked) continue;
                if (!double.TryParse(f.Text, out double v))
                {
                    StatusMessage = $"'{f.Label}' 값이 숫자가 아닙니다: {f.Text}";
                    return;
                }
                values[f.Key] = v;
                known.Add(f.Key);
            }

            var request = new { values, known };
            string requestJson = JsonSerializer.Serialize(request);

            try
            {
                string responseJson = NativeEngine.SolveRow(requestJson);
                var resp = JsonSerializer.Deserialize<SolveResponse>(responseJson, JsonOpts);
                if (resp == null) throw new InvalidOperationException("엔진 응답을 해석할 수 없습니다.");

                if (!resp.Ok)
                {
                    var reason = resp.Conflicts.Count > 0
                        ? string.Join(" / ", resp.Conflicts)
                        : resp.Message;
                    StatusMessage = "계산 실패: " + reason;
                    return;
                }

                SelectedRow.Model.Values = resp.Values;
                SelectedRow.Model.LockedFields = known;
                SelectedRow.RefreshAll();
                RebuildFieldEditors();
                StatusMessage = "계산 완료: " + SelectedRow.Name;
            }
            catch (Exception ex)
            {
                MessageBox.Show("계산 중 오류가 발생했습니다.\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------- 내보내기 ----------------

        private void ExportCsv()
        {
            if (Rows.Count == 0)
            {
                StatusMessage = "내보낼 상품이 없습니다.";
                return;
            }
            var dlg = new SaveFileDialog
            {
                FileName = SanitizeFileName(_currentEvent.EventName) + ".csv",
                Filter = "CSV 파일 (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var writer = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true)); // BOM 포함 (엑셀 한글 호환)
                var headers = new[] { "상품명", "채널" }
                    .Concat(FieldNames.All.Select(f => f.Label))
                    .Concat(new[] { "비고" });
                writer.WriteLine(string.Join(",", headers.Select(CsvEscape)));

                foreach (var r in Rows)
                {
                    var cells = new[] { r.Name, r.Channel }
                        .Concat(FieldNames.All.Select(f => r.Model.GetValue(f.Key).ToString("0.####")))
                        .Concat(new[] { r.Note });
                    writer.WriteLine(string.Join(",", cells.Select(CsvEscape)));
                }
                StatusMessage = "CSV로 내보냈습니다: " + dlg.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("내보내기에 실패했습니다.\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string CsvEscape(string s)
        {
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
