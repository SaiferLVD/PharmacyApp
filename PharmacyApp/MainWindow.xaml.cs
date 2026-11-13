using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;

namespace PharmacyApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadComboBoxes();
            LoadTableData(0);
        }

        private void MainDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header is string headerText)
            {
                e.Column.Header = headerText.Replace("_", " ");
            }
        }

        private void LoadComboBoxes()
        {
            try
            {
                using (var db = new PharmacyDbContext())
                {
                    CategoryCombo.ItemsSource = db.Categories.ToList();
                    if (CategoryCombo.Items.Count > 0)
                        CategoryCombo.SelectedIndex = 0;

                    SupplierCombo.ItemsSource = db.Suppliers.ToList();
                    if (SupplierCombo.Items.Count > 0)
                        SupplierCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ShowStatus("Ошибка загрузки списков: " + ex.Message, isError: true);
            }
        }

        private void AddMedicineButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ShowStatus("Введите название препарата.", isError: true);
                return;
            }

            string priceText = PriceBox.Text.Trim().Replace(',', '.');
            if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) || price <= 0)
            {
                ShowStatus("Укажите корректную цену больше нуля.", isError: true);
                return;
            }

            if (!int.TryParse(StockBox.Text.Trim(), out int stock) || stock < 0)
            {
                ShowStatus("Укажите корректное количество на складе.", isError: true);
                return;
            }

            if (CategoryCombo.SelectedValue == null || SupplierCombo.SelectedValue == null)
            {
                ShowStatus("Выберите категорию и поставщика.", isError: true);
                return;
            }

            try
            {
                using (var db = new PharmacyDbContext())
                {
                    var newMedicine = new Medicine
                    {
                        Name = name,
                        Price = price,
                        StockQuantity = stock,
                        RequiresPrescription = PrescriptionCheck.IsChecked == true,
                        CategoryID = (int)CategoryCombo.SelectedValue,
                        SupplierID = (int)SupplierCombo.SelectedValue
                    };

                    db.Medicines.Add(newMedicine);
                    db.SaveChanges();
                }

                NameBox.Clear();
                PriceBox.Clear();
                StockBox.Clear();
                PrescriptionCheck.IsChecked = false;

                ShowStatus("Препарат успешно добавлен!", isError: false);

                if (TableSelectorCombo.SelectedIndex != 0)
                    TableSelectorCombo.SelectedIndex = 0;
                else
                    LoadTableData(0);
            }
            catch (Exception ex)
            {
                ShowStatus("Ошибка сохранения: " + ex.Message, isError: true);
            }
        }

        private void DeleteMedicineButton_Click(object sender, RoutedEventArgs e)
        {
            if (TableSelectorCombo.SelectedIndex != 0)
            {
                ShowStatus("Выберите в выпадающем списке таблицу «Лекарства».", isError: true);
                return;
            }

            if (MainDataGrid.SelectedItem == null)
            {
                ShowStatus("Сначала кликните по строке с товаром в таблице.", isError: true);
                return;
            }

            try
            {
                var selectedItem = MainDataGrid.SelectedItem;
                var idProp = selectedItem.GetType().GetProperty("ID");
                if (idProp == null)
                    return;

                int medicineId = (int)idProp.GetValue(selectedItem)!;

                using (var db = new PharmacyDbContext())
                {
                    bool hasSales = db.Sales.Any(s => s.MedicineID == medicineId);
                    if (hasSales)
                    {
                        ShowStatus("Нельзя удалить: препарат уже числится в чеках продаж.", isError: true);
                        return;
                    }

                    var medicineToDelete = db.Medicines.Find(medicineId);
                    if (medicineToDelete != null)
                    {
                        db.Medicines.Remove(medicineToDelete);
                        db.SaveChanges();
                        ShowStatus($"Товар «{medicineToDelete.Name}» удален.", isError: false);
                        LoadTableData(0);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus("Ошибка удаления: " + ex.Message, isError: true);
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
        }

        private void TableSelectorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainDataGrid == null || TableSelectorCombo == null)
                return;

            LoadTableData(TableSelectorCombo.SelectedIndex);
        }

        private void LoadTableData(int tableIndex)
        {
            using (var db = new PharmacyDbContext())
            {
                switch (tableIndex)
                {
                    case 0:
                        MainDataGrid.ItemsSource = db.Medicines
                            .Include(m => m.Category)
                            .Include(m => m.Supplier)
                            .Select(m => new
                            {
                                ID = m.MedicineID,
                                Название = m.Name,
                                Цена_руб = m.Price,
                                Остаток_шт = m.StockQuantity,
                                По_рецепту = m.RequiresPrescription ? "Да" : "Нет",
                                Категория = m.Category != null ? m.Category.CategoryName : "-",
                                Поставщик = m.Supplier != null ? m.Supplier.CompanyName : "-"
                            })
                            .ToList();
                        break;

                    case 1:
                        MainDataGrid.ItemsSource = db.Sellers
                            .Select(s => new
                            {
                                ID = s.SellerID,
                                ФИО = s.FullName,
                                Должность = s.Position,
                                Дата_найма = s.HireDate.ToString("dd.MM.yyyy")
                            })
                            .ToList();
                        break;

                    case 2:
                        MainDataGrid.ItemsSource = db.Sales
                            .Include(s => s.Medicine)
                            .Include(s => s.Seller)
                            .Select(s => new
                            {
                                ID_Чека = s.SaleID,
                                Дата_продажи = s.SaleDate,
                                Препарат = s.Medicine != null ? s.Medicine.Name : "-",
                                Количество = s.QuantitySold,
                                Сумма_руб = s.TotalAmount,
                                Продавец = s.Seller != null ? s.Seller.FullName : "-"
                            })
                            .ToList();
                        break;

                    case 3:
                        MainDataGrid.ItemsSource = db.Categories
                            .Select(c => new
                            {
                                ID = c.CategoryID,
                                Название_категории = c.CategoryName
                            })
                            .ToList();
                        break;

                    case 4:
                        MainDataGrid.ItemsSource = db.Suppliers
                            .Select(sup => new
                            {
                                ID = sup.SupplierID,
                                Компания = sup.CompanyName,
                                Телефон = sup.Phone
                            })
                            .ToList();
                        break;
                }
            }
        }
    }
}