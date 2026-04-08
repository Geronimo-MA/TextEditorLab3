using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using TextEditorLab.Models;
using TextEditorLab.Services;

namespace TextEditorLab
{
    public partial class MainWindow : Window
    {
        private string? _currentFilePath = null;
        private bool _isModified = false;
        private bool _suppressModifiedFlag = false;

        private readonly LexicalAnalyzer _lexicalAnalyzer = new LexicalAnalyzer();
        private readonly SyntaxAnalyzer _syntaxAnalyzer = new SyntaxAnalyzer();

        public MainWindow()
        {
            InitializeComponent();
            UpdateTitle();
            UpdateStatusBar();
            StatusText.Text = "Готов";
        }
        private List<SyntaxError> ConvertLexicalErrorsToDisplay(List<Token> tokens)
        {
            var errors = new List<SyntaxError>();

            foreach (var token in tokens)
            {
                if (!token.IsError)
                    continue;

                errors.Add(new SyntaxError
                {
                    InvalidFragment = token.Lexeme,
                    Line = token.Line,
                    StartColumn = token.StartColumn,
                    EndColumn = token.EndColumn,
                    StartIndex = token.StartIndex,
                    Length = token.Length > 0 ? token.Length : 1,
                    Description = token.TypeName
                });
            }

            return errors;
        }
        private List<SyntaxError> AnalyzeAllLines(string text)
        {
            var allErrors = new List<SyntaxError>();

            string normalizedText = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalizedText.Split('\n');

            int globalStartIndex = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string lineText = lines[i];
                int lineNumber = i + 1;

                if (string.IsNullOrWhiteSpace(lineText))
                {
                    globalStartIndex += lineText.Length + 1;
                    continue;
                }

                List<Token> tokens = _lexicalAnalyzer.Analyze(lineText);

                foreach (var token in tokens)
                {
                    token.Line = lineNumber;
                    token.StartIndex += globalStartIndex;
                }

                var lexicalErrors = ConvertLexicalErrorsToDisplay(tokens);
                if (lexicalErrors.Count > 0)
                {
                    foreach (var error in lexicalErrors)
                    {
                        error.Line = lineNumber;
                        error.StartIndex += globalStartIndex;
                    }

                    allErrors.AddRange(lexicalErrors);
                    globalStartIndex += lineText.Length + 1;
                    continue;
                }

                var syntaxResult = _syntaxAnalyzer.Analyze(tokens);

                foreach (var error in syntaxResult.Errors)
                {
                    error.Line = lineNumber;
                    error.StartIndex += globalStartIndex;
                }

                allErrors.AddRange(syntaxResult.Errors);

                globalStartIndex += lineText.Length + 1;
            }

            return allErrors;
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

            ResultsDataGrid.ItemsSource = null;

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

                    ResultsDataGrid.ItemsSource = null;

                    _currentFilePath = openDialog.FileName;
                    _isModified = false;

                    UpdateTitle();
                    UpdateStatusBar();
                    StatusText.Text = $"Файл загружен: {Path.GetFileName(_currentFilePath)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при открытии файла:\n{ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
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
                MessageBox.Show(
                    $"Ошибка при сохранении файла:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            ResultsDataGrid.ItemsSource = null;

            string text = EditorTextBox.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                var emptyErrors = new List<SyntaxError>
        {
            new SyntaxError
            {
                InvalidFragment = "<пустая строка>",
                Line = 1,
                StartColumn = 1,
                EndColumn = 1,
                StartIndex = 0,
                Length = 1,
                Description = "Пустая строка. Ожидалась конструкция тернарного оператора."
            }
        };

                ResultsDataGrid.ItemsSource = emptyErrors;
                StatusText.Text = "Анализ завершён, ошибок: 1";
                return;
            }

            var errors = AnalyzeAllLines(text);

            ResultsDataGrid.ItemsSource = errors;

            StatusText.Text = errors.Count == 0
                ? "Анализ выполнен: ошибок нет"
                : $"Анализ завершён, ошибок: {errors.Count}";
        }

        private void ResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsDataGrid.SelectedItem is not SyntaxError error)
                return;

            if (error.StartIndex < 0 || error.StartIndex > EditorTextBox.Text.Length)
                return;

            EditorTextBox.Focus();
            EditorTextBox.CaretIndex = error.StartIndex;
            EditorTextBox.Select(error.StartIndex, error.Length > 0 ? error.Length : 1);

            int lineIndex = EditorTextBox.GetLineIndexFromCharacterIndex(error.StartIndex);
            EditorTextBox.ScrollToLine(lineIndex);

            StatusText.Text = $"Переход к ошибке: строка {error.Line}, позиция {error.StartColumn}";
            UpdateStatusBar();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string helpText =
                "СПРАВКА — Текстовый редактор\n" +
                "═══════════════════════════════\n\n" +
                "РЕАЛИЗОВАННЫЕ ФУНКЦИИ:\n\n" +
                "Файл:\n" +
                "  • Создать (Ctrl+N) — новый документ\n" +
                "  • Открыть (Ctrl+O) — загрузить файл\n" +
                "  • Сохранить (Ctrl+S) — сохранить изменения\n" +
                "  • Сохранить как — сохранить в новый файл\n" +
                "  • Выход — закрыть программу\n\n" +
                "Правка:\n" +
                "  • Отменить (Ctrl+Z) — отмена действия\n" +
                "  • Повторить (Ctrl+Y) — повтор действия\n" +
                "  • Вырезать (Ctrl+X) — вырезать текст\n" +
                "  • Копировать (Ctrl+C) — копировать текст\n" +
                "  • Вставить (Ctrl+V) — вставить текст\n" +
                "  • Удалить (Del) — удалить выделенное\n" +
                "  • Выделить всё (Ctrl+A) — весь текст\n\n" +
                "Пуск:\n" +
                "  • Запуск лексического анализатора\n\n" +
                "Справка:\n" +
                "  • Вызов справки (F1) — это окно\n" +
                "  • О программе — информация о разработчике\n\n" +
                "Панель инструментов дублирует основные функции меню.\n" +
                "Размер областей можно менять перетаскиванием разделителя.";

            MessageBox.Show(helpText, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string aboutText =
                "О ПРОГРАММЕ\n" +
                "══════════════\n\n" +
                "Текстовый редактор с лексическим анализатором\n" +
                "Лабораторная работа №2\n\n" +
                "Разработчик: Геронимус Матвей Анатольевич\n" +
                "Группа: АП-326\n\n" +
                "Язык: C#\n" +
                "GUI: WPF\n" +
                "Платформа: .NET 9\n" +
                "Год: 2026\n\n" +
                "Учебный проект";

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