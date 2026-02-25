using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TextEditorLab
{
    public partial class MainWindow : Window
    {
        private string? _currentFilePath = null;
        private bool _isModified = false;
        private bool _suppressModifiedFlag = false;

        public MainWindow()
        {
            InitializeComponent();
            UpdateTitle();
            UpdateStatusBar();
            StatusText.Text = "Готов";
        }

        private void UpdateTitle()
        {
            string modified = _isModified ? "*" : "";
            string filename = string.IsNullOrEmpty(_currentFilePath)
                ? "Безымянный"
                : Path.GetFileName(_currentFilePath);

            Title = $"{filename}{modified} — Текстовый редактор";
        }

        private void UpdateStatusBar()
        {
            int line = EditorTextBox.GetLineIndexFromCharacterIndex(EditorTextBox.CaretIndex);
            int col = EditorTextBox.CaretIndex - EditorTextBox.GetCharacterIndexFromLineIndex(line);
            CursorPositionText.Text = $"Стр: {line + 1}  Стб: {col + 1}";

            int byteCount = Encoding.UTF8.GetByteCount(EditorTextBox.Text ?? string.Empty);
            FileSizeText.Text = $"Размер: {byteCount} байт";
        }


        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmSaveBeforeAction()) return;

            _suppressModifiedFlag = true;
            EditorTextBox.Clear();
            _suppressModifiedFlag = false;

            _currentFilePath = null;
            _isModified = false;
            UpdateTitle();
            UpdateStatusBar();
            StatusText.Text = "Создан новый файл";
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmSaveBeforeAction()) return;

            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Открыть файл"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    string content = File.ReadAllText(openDialog.FileName, Encoding.UTF8);

                    _suppressModifiedFlag = true;
                    EditorTextBox.Text = content;
                    _suppressModifiedFlag = false;

                    _currentFilePath = openDialog.FileName;
                    _isModified = false;

                    UpdateTitle();
                    UpdateStatusBar();
                    StatusText.Text = $"Файл загружен: {Path.GetFileName(_currentFilePath)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsFile_Click(sender, e);
                return;
            }

            SaveFileToPath(_currentFilePath);
        }

        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Сохранить как",
                FileName = string.IsNullOrEmpty(_currentFilePath)
                    ? "Безымянный.txt"
                    : Path.GetFileName(_currentFilePath)
            };

            if (saveDialog.ShowDialog() == true)
            {
                SaveFileToPath(saveDialog.FileName);
            }
        }

        private void SaveFileToPath(string filePath)
        {
            try
            {
                File.WriteAllText(filePath, EditorTextBox.Text ?? string.Empty, Encoding.UTF8);
                _currentFilePath = filePath;
                _isModified = false;

                UpdateTitle();
                UpdateStatusBar();
                StatusText.Text = $"Файл сохранён: {Path.GetFileName(_currentFilePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool ConfirmSaveBeforeAction()
        {
            if (!_isModified) return true;

            var result = MessageBox.Show(
                "Сохранить изменения в файле?",
                "Подтверждение",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool wasModified = _isModified;

                if (string.IsNullOrEmpty(_currentFilePath))
                    SaveAsFile_Click(null, null);
                else
                    SaveFileToPath(_currentFilePath);

                return !(wasModified && _isModified);
            }

            if (result == MessageBoxResult.No)
                return true;

            return false;
        }


        private void Undo_Click(object sender, RoutedEventArgs e) => EditorTextBox.Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => EditorTextBox.Redo();
        private void Cut_Click(object sender, RoutedEventArgs e) => EditorTextBox.Cut();
        private void Copy_Click(object sender, RoutedEventArgs e) => EditorTextBox.Copy();
        private void Paste_Click(object sender, RoutedEventArgs e) => EditorTextBox.Paste();

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.SelectedText = "";
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.SelectAll();
        }


        private void ShowTextInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;

            string header = menuItem.Header?.ToString() ?? "Информация";

            Window infoWindow = new Window
            {
                Title = header,
                Width = 500,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Content = new TextBox
                {
                    Text = $"Информация: {header}\n\nБудет реализовано в следующем этапе лабораторной работы.",
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(10),
                    FontSize = 14
                }
            };

            infoWindow.ShowDialog();
        }


        private void RunAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Clear();
            string text = EditorTextBox.Text ?? string.Empty;

            int lines = text.Length == 0 ? 0 : text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
            int words = text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            OutputTextBox.AppendText("🚀 ЗАПУСК СИНТАКСИЧЕСКОГО АНАЛИЗАТОРА\n");
            OutputTextBox.AppendText("========================================\n");
            OutputTextBox.AppendText($"📄 Длина текста: {text.Length} символов\n");
            OutputTextBox.AppendText($"📊 Количество строк: {lines}\n");
            OutputTextBox.AppendText($"🔤 Количество слов: {words}\n");
            OutputTextBox.AppendText("✅ Анализ завершён успешно!\n");
            OutputTextBox.AppendText("========================================\n");

            StatusText.Text = "Анализ выполнен";
        }


        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string helpText =
                "📚 СПРАВКА — Текстовый редактор\n" +
                "═══════════════════════════════\n\n" +
                "РЕАЛИЗОВАННЫЕ ФУНКЦИИ:\n\n" +
                "📁 Файл:\n" +
                "  • Создать (Ctrl+N) — новый документ\n" +
                "  • Открыть (Ctrl+O) — загрузить файл\n" +
                "  • Сохранить (Ctrl+S) — сохранить изменения\n" +
                "  • Сохранить как — сохранить в новый файл\n" +
                "  • Выход — закрыть программу\n\n" +
                "✏️ Правка:\n" +
                "  • Отменить (Ctrl+Z) — отмена действия\n" +
                "  • Повторить (Ctrl+Y) — повтор действия\n" +
                "  • Вырезать (Ctrl+X) — вырезать текст\n" +
                "  • Копировать (Ctrl+C) — копировать текст\n" +
                "  • Вставить (Ctrl+V) — вставить текст\n" +
                "  • Удалить (Del) — удалить выделенное\n" +
                "  • Выделить всё (Ctrl+A) — весь текст\n\n" +
                "🎯 Пуск:\n" +
                "  • Запуск анализатора — анализ текста\n\n" +
                "❓ Справка:\n" +
                "  • Вызов справки (F1) — это окно\n" +
                "  • О программе — информация о разработчике\n\n" +
                "🖱️ Панель инструментов дублирует основные функции меню.\n" +
                "📏 Размер областей можно менять перетаскиванием разделителя.";

            MessageBox.Show(helpText, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string aboutText =
                "ℹ️ О ПРОГРАММЕ\n" +
                "══════════════\n\n" +
                "Текстовый редактор\n" +
                "Лабораторная работа №1\n\n" +
                "Разработчик: Геронимус Матвей Анатольевич\n" +
                "Группа: АП-326\n\n" +
                "Язык: C#\n" +
                "GUI: WPF\n" +
                "Платформа: .NET 9\n" +
                "Год: 2026\n\n" +
                "© Учебный проект";

            MessageBox.Show(aboutText, "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressModifiedFlag)
            {
                UpdateStatusBar();
                return;
            }

            if (!_isModified)
            {
                _isModified = true;
                UpdateTitle();
            }

            UpdateStatusBar();
        }

        private void EditorTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateStatusBar();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!ConfirmSaveBeforeAction())
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }
    }
}