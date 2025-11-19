using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace PersonalExpenseTracker
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private List<Transaction> _allTransactions = new();

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            TransactionTypes = new ObservableCollection<string> { "รายรับ", "รายจ่าย" };
            Categories = new ObservableCollection<string> { "เงินเดือน", "โบนัส", "อาหาร", "เดินทาง", "ช้อปปิ้ง", "ค่าบ้าน/น้ำ/ไฟ", "สุขภาพ", "อื่นๆ" };

            // Initialize Years and Months
            var currentYear = DateTime.Now.Year;
            Years = new ObservableCollection<int>(Enumerable.Range(currentYear - 5, 6).Reverse());
            Months = new ObservableCollection<string> { "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };

            SelectedYear = currentYear;
            SelectedMonthIndex = DateTime.Now.Month - 1; // 0-based index
            CurrentDate = DateTime.Now;

            LoadData();
        }

        [ObservableProperty]
        private DateTime currentDate;

        [ObservableProperty]
        private ObservableCollection<string> transactionTypes;

        [ObservableProperty]
        private string selectedTransactionType = "รายจ่าย";

        [ObservableProperty]
        private string? selectedCategory;

        [ObservableProperty]
        private string amountText = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Transaction> transactions = new();

        [ObservableProperty]
        private Transaction? selectedTransaction;

        [ObservableProperty]
        private decimal totalBalance;

        [ObservableProperty]
        private decimal totalIncome;

        [ObservableProperty]
        private decimal totalExpense;

        [ObservableProperty]
        private ObservableCollection<string> categories;

        // Filter Properties
        [ObservableProperty]
        private ObservableCollection<int> years;

        [ObservableProperty]
        private ObservableCollection<string> months;

        [ObservableProperty]
        private int selectedYear;

        [ObservableProperty]
        private int selectedMonthIndex;

        // Chart Properties
        [ObservableProperty]
        private SeriesCollection series = new();

        partial void OnSelectedYearChanged(int value) => FilterData();
        partial void OnSelectedMonthIndexChanged(int value) => FilterData();

        [RelayCommand]
        private void Add()
        {
            if (string.IsNullOrWhiteSpace(SelectedCategory))
            {
                MessageBox.Show("กรุณาเลือกหมวดหมู่", "แจ้งเตือน");
                return;
            }

            if (!decimal.TryParse(AmountText, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("กรุณากรอกจำนวนเงินให้ถูกต้อง", "แจ้งเตือน");
                return;
            }

            var newTransaction = new Transaction
            {
                Date = CurrentDate,
                Type = SelectedTransactionType,
                Category = SelectedCategory,
                Amount = amount,
                Description = Description
            };

            _databaseService.AddTransaction(newTransaction);

            // Reset form
            AmountText = string.Empty;
            Description = string.Empty;

            LoadData();
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedTransaction == null) return;

            var result = MessageBox.Show($"คุณต้องการลบรายการ '{SelectedTransaction.Description}' หรือไม่?", "ยืนยันการลบ", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _databaseService.DeleteTransaction(SelectedTransaction.Id);
                LoadData();
            }
        }

        private void LoadData()
        {
            _allTransactions = _databaseService.GetAllTransactions();
            FilterData();
        }

        private void FilterData()
        {
            // Filter by Selected Month and Year
            var filtered = _allTransactions
                .Where(t => t.Date.Year == SelectedYear && t.Date.Month == (SelectedMonthIndex + 1))
                .OrderByDescending(t => t.Date)
                .ToList();

            Transactions = new ObservableCollection<Transaction>(filtered);

            TotalIncome = filtered.Where(t => t.Type == "รายรับ").Sum(t => t.Amount);
            TotalExpense = filtered.Where(t => t.Type == "รายจ่าย").Sum(t => t.Amount);
            TotalBalance = TotalIncome - TotalExpense;

            UpdateChart(filtered);
        }

        private void UpdateChart(List<Transaction> filteredData)
        {
            // Group expenses by category
            var expensesByCategory = filteredData
                .Where(t => t.Type == "รายจ่าย")
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(t => t.Amount) })
                .ToList();

            var newSeries = new SeriesCollection();

            if (expensesByCategory.Any())
            {
                foreach (var item in expensesByCategory)
                {
                    newSeries.Add(new PieSeries
                    {
                        Title = item.Category,
                        Values = new ChartValues<decimal> { item.Amount },
                        DataLabels = true,
                        LabelPoint = point => $"{point.SeriesView.Title}: {point.Y:N0}"
                    });
                }
            }

            Series = newSeries;
        }
    }
}
