using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private List<string> _allEventFilePaths = new();

        public ObservableCollection<EventListItem> EventFiles { get; } = new();
        public ObservableCollection<ProductRowViewModel> Rows { get; } = new();
        public ObservableCollection<FieldEditorViewModel> FieldEditors { get; } = new();

        /// <summary>품목(SKU) 마스터 — 상품명으로 SKU/매입가/기존정산액/채널을 자동으로 채워주는 조회표.
        /// MasterData.json 에 저장됨. 실제 엑셀의 "Master" 시트에 대응.</summary>
        public ObservableCollection<MasterProduct> MasterProducts { get; } = new();

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

        /// <summary>왼쪽 행사 목록 조회 기준 날짜 범위. 기본값은 오늘 하루 — 이 범위와 행사 기간이
        /// 겹치는 행사만 보여준다. 한쪽만 지정하면 그쪽으로 열린 범위로, 둘 다 지우면(= "전체 보기")
        /// 모든 행사를 보여준다.</summary>
        private DateTime? _filterStartDate = DateTime.Today;
        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set { _filterStartDate = value; OnPropertyChanged(nameof(FilterStartDate)); ApplyEventFilter(); }
        }

        private DateTime? _filterEndDate = DateTime.Today;
        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set { _filterEndDate = value; OnPropertyChanged(nameof(FilterEndDate)); ApplyEventFilter(); }
        }

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

        private EventListItem? _selectedEventFile;
        public EventListItem? SelectedEventFile
        {
            get => _selectedEventFile;
            set { _selectedEventFile = value; OnPropertyChanged(nameof(SelectedEventFile)); }
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
        public RelayCommand ShowAllEventsCommand { get; }
        public RelayCommand DownloadEventCommand { get; }
        public RelayCommand OpenEventsFolderCommand { get; }
        public RelayCommand DeleteEventFileCommand { get; }

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
            ShowAllEventsCommand = new RelayCommand(_ => { _filterStartDate = null; _filterEndDate = null; OnPropertyChanged(nameof(FilterStartDate)); OnPropertyChanged(nameof(FilterEndDate)); ApplyEventFilter(); });
            DownloadEventCommand = new RelayCommand(_ => DownloadEvent());
            OpenEventsFolderCommand = new RelayCommand(_ => OpenEventsFolder());
            DeleteEventFileCommand = new RelayCommand(_ => DeleteEventFile(), _ => SelectedEventFile != null);

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

        // ---------------- 품목(SKU) 마스터 ----------------

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
            MasterProducts.Clear();
            foreach (var p in data.Products) MasterProducts.Add(p);
        }

        private void SaveMasterData()
        {
            try
            {
                var data = new Models.MasterData { Products = MasterProducts.ToList() };
                File.WriteAllText(_masterDataFilePath, JsonSerializer.Serialize(data, JsonOpts));
            }
            catch (Exception ex)
            {
                MessageBox.Show("품목 마스터를 저장하지 못했습니다.\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageMasterData()
        {
            var dlg = new Views.MasterDataWindow(MasterProducts) { Owner = Application.Current.MainWindow };
            dlg.ShowDialog();
            SaveMasterData();
        }

        /// <summary>상품명으로 품목 마스터를 찾는다 (정확히 일치, 없으면 null).</summary>
        private MasterProduct? FindMasterProduct(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return MasterProducts.FirstOrDefault(p => p.Name == name);
        }

        // ---------------- 행사 목록/불러오기/저장 ----------------

        public void RefreshEventList()
        {
            _allEventFilePaths = Directory.GetFiles(EventsFolder, "*.json").OrderBy(f => f).ToList();
            ApplyEventFilter();
        }

        private void ApplyEventFilter()
        {
            EventFiles.Clear();
            foreach (var path in _allEventFilePaths)
            {
                var item = PeekEvent(path);
                if (MatchesFilterRange(item)) EventFiles.Add(item);
            }
        }

        /// <summary>조회 날짜 범위(FilterStartDate~FilterEndDate)와 행사 기간이 겹치는지 확인한다.
        /// 필터 쪽이 한쪽만 지정되어 있으면 그쪽으로 열린 범위로, 둘 다 비어 있으면(전체 보기) 항상 true.</summary>
        private bool MatchesFilterRange(EventListItem item)
        {
            if (FilterStartDate == null && FilterEndDate == null) return true;
            if (item.StartDate == null && item.EndDate == null) return true; // 기간 미지정 행사는 항상 보여준다

            // 행사 기간을 [eStart, eEnd] 로, 필터 범위를 [fStart, fEnd] 로 두고 구간이 겹치는지 확인.
            // 한쪽이 비어 있으면 그 방향으로 무한히 열린 범위로 취급한다.
            DateTime eStart = item.StartDate ?? DateTime.MinValue;
            DateTime eEnd = item.EndDate ?? DateTime.MaxValue;
            DateTime fStart = FilterStartDate ?? DateTime.MinValue;
            DateTime fEnd = FilterEndDate ?? DateTime.MaxValue;
            return eStart <= fEnd && fStart <= eEnd;
        }

        /// <summary>목록 표시/필터링용으로 행사 이름과 기간만 가볍게 미리 읽는다 (엔진 DLL을 거치지 않음).</summary>
        private static EventListItem PeekEvent(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                if (root.TryGetProperty("eventName", out var n) && !string.IsNullOrWhiteSpace(n.GetString()))
                    name = n.GetString()!;
                DateTime? start = root.TryGetProperty("startDate", out var s) ? ParseDate(s.GetString() ?? "") : null;
                DateTime? end = root.TryGetProperty("endDate", out var e) ? ParseDate(e.GetString() ?? "") : null;
                return new EventListItem(path, name, start, end);
            }
            catch
            {
                return new EventListItem(path, name, null, null);
            }
        }

        /// <summary>행사(JSON 파일)를 삭제한다. 되돌릴 수 없는 작업이므로 매번 비밀번호를 확인한다.</summary>
        private void DeleteEventFile()
        {
            if (SelectedEventFile == null) return;

            //var confirm = MessageBox.Show(
            //    $"'{SelectedEventFile.DisplayName}' 행사를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.",
            //    "행사 삭제", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //if (confirm != MessageBoxResult.Yes) return;

            var pwd = new Views.PasswordDialog("삭제 확인", "행사를 삭제하려면 비밀번호를 입력하세요.", AppConfig.Password)
            { Owner = Application.Current.MainWindow };
            if (pwd.ShowDialog() != true) return;

            string path = SelectedEventFile.FilePath;
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("삭제하지 못했습니다.\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.Equals(_currentFilePath, path, StringComparison.OrdinalIgnoreCase))
            {
                _currentFilePath = null;
                _currentEvent = new EventFile();
                Rows.Clear();
                FieldEditors.Clear();
                SelectedRow = null;
                OnPropertyChanged(nameof(EventName));
                OnPropertyChanged(nameof(StartDate));
                OnPropertyChanged(nameof(EndDate));
                OnPropertyChanged(nameof(Memo));
            }
            SelectedEventFile = null;
            RefreshEventList();
            StatusMessage = "행사를 삭제했습니다.";
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

        /// <summary>팀원과 공유하기 쉽도록, 저장된 행사 JSON을 원하는 위치(기본: 바탕화면)로 복사한다.</summary>
        private void DownloadEvent()
        {
            if (Rows.Count == 0 && string.IsNullOrEmpty(_currentFilePath))
            {
                StatusMessage = "다운로드할 행사가 없습니다. 먼저 상품을 추가하고 저장하세요.";
                return;
            }

            // 공유본이 최신 상태를 반영하도록 먼저 저장한다.
            SaveEvent(false);
            if (string.IsNullOrEmpty(_currentFilePath)) return; // 저장을 취소한 경우

            var dlg = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                FileName = Path.GetFileName(_currentFilePath),
                Filter = "행사 JSON 파일 (*.json)|*.json"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                File.Copy(_currentFilePath, dlg.FileName, overwrite: true);
                StatusMessage = "다운로드했습니다: " + dlg.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("다운로드하지 못했습니다.\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>행사 JSON이 실제로 저장되는 폴더를 탐색기로 연다 (%LOCALAPPDATA%\PriceCalcApp\Events).</summary>
        private void OpenEventsFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = EventsFolder, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("폴더를 열지 못했습니다.\n" + ex.Message, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------- 상품(행) 추가/삭제 ----------------

        private void AddProduct()
        {
            // 이 창에서 품목 마스터를 직접 보고 추가/삭제도 할 수 있다 — 같은 컬렉션을 넘겨서
            // 창 안에서 바뀐 내용이 곧바로 반영되게 하고, 닫히면(취소해도) 파일로 저장해둔다.
            var dlg = new Views.AddProductDialog(MasterProducts) { Owner = Application.Current.MainWindow };
            bool ok = dlg.ShowDialog() == true;
            SaveMasterData();
            if (!ok) return;

            var row = new ProductRow { Name = dlg.ItemName, EventType = dlg.EventType, Note = dlg.Note };
            foreach (var key in FieldNames.DefaultLocked)
            {
                row.Values[key] = 0;
                row.LockedFields.Add(key);
            }

            // 품목 마스터에 있는 상품이면 SKU/채널/매입가/기존정산액(지원금_S)을 자동으로 채운다
            // (원본 엑셀의 VLOOKUP과 같은 역할). 없으면 빈 값으로 두고 나중에 직접 입력하면 된다.
            var master = FindMasterProduct(row.Name);
            if (master != null)
            {
                row.Sku = master.Sku;
                row.Channel = master.Channel;
                row.Values[FieldNames.PurchasePrice] = master.PurchasePrice;
                row.Values[FieldNames.ExistingSettlement] = master.SubsidySpot;
            }

            var vm = new ProductRowViewModel(row);
            Rows.Add(vm);
            SelectedRow = vm;

            StatusMessage = master != null
                ? $"'{row.Name}' 상품을 추가했습니다 (품목 마스터에서 SKU/채널/매입가 자동 입력). 값을 확인하고 계산하세요."
                : $"'{row.Name}' 상품을 추가했습니다. 품목 마스터에 없는 상품이라 SKU/채널/매입가를 직접 입력해야 합니다.";
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
                // 컬럼 순서는 원본 엑셀과 동일하게 GridColumns 정의 하나를 그대로 따른다 (그리드 표시 순서와도 일치).
                writer.WriteLine(string.Join(",", GridColumns.All.Select(c => CsvEscape(c.Header))));

                foreach (var r in Rows)
                {
                    var cells = GridColumns.All.Select(c => c.GetCsvText(r));
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
