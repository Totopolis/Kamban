using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using DynamicData;
using Kamban.Core;
using Kamban.Contracts;
using Kamban.Templates;
using Kamban.ViewModels.Core;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using wpf.ui;

namespace Kamban.ViewModels
{
    public class WizardViewModel : ViewModelBase, IInitializableViewModel
    {
        [Reactive] public bool IsNewFile { get; set; }
        [Reactive] public string BoardName { get; set; }

        [Reactive] public List<BoardTemplate> Templates { get; set; }

        [Reactive] public BoardTemplate SelectedTemplate { get; set; }
        [Reactive] public string FileName { get; set; }
        [Reactive] public string FolderName { get; set; }
        [Reactive] public Color ColorTheme { get; set; }
        [Reactive] public ObservableCollection<Column> Columns { get; set; }
        [Reactive] public ObservableCollection<Row> Rows { get; set; }

        public ReactiveCommand<Unit, Unit> FillFromTemplateCommand { get; set; }
        public ReactiveCommand<Unit, Unit> AddColumnCommand { get; set; }
        public ReactiveCommand<Unit, Unit> AddRowCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ClearAllCommand { get; set; }

        public ReactiveCommand<Unit, Unit> CreateCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

        public ReactiveCommand<Unit, Unit> SelectFolderCommand { get; set; }

        private readonly IAppModel appModel;
        private readonly IShell shell;
        private readonly IDialogCoordinator dialCoord;
        private readonly IAppConfig appConfig;

        public WizardViewModel(IAppModel appModel, IShell shell, IDialogCoordinator dc,
            IAppConfig cfg, ITemplates templates)
        {
            this.appModel = appModel;
            this.shell = shell;
            dialCoord = dc;
            appConfig = cfg;
            ColorTheme = appConfig.ColorTheme;
            Templates = templates.GetBoardTemplates().Result;

            Columns = new ObservableCollection<Column>();
            Rows = new ObservableCollection<Row>();

            FillFromTemplateCommand = ReactiveCommand.Create(() =>
            {
                if (SelectedTemplate == null)
                    return;

                Columns.Clear();
                Columns.AddRange(SelectedTemplate.Columns);

                Rows.Clear();
                Rows.AddRange(SelectedTemplate.Rows);
            });

            ClearAllCommand = ReactiveCommand.Create(() =>
            {
                Columns.Clear();
                Rows.Clear();
            });

            CreateCommand = ReactiveCommand.CreateFromTask(CreateCommandExecute);
            CancelCommand = ReactiveCommand.Create(this.Close);

            SelectFolderCommand = ReactiveCommand.Create(() =>
            {
                var dialog = new FolderBrowserDialog
                {
                    ShowNewFolderButton = false,
                    SelectedPath = FolderName
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                    FolderName = dialog.SelectedPath;
            });

            AddColumnCommand = ReactiveCommand.CreateFromTask(async _ =>
            {
                var ts = await dialCoord.ShowInputAsync(this, "Info", $"Enter new column name");

                if (string.IsNullOrEmpty(ts))
                    return;

                if (Columns.Any(x => x.Name == ts))
                    return;

                Columns.Add(new Column {Name = ts});
            });

            AddRowCommand = ReactiveCommand.CreateFromTask(async _ =>
            {
                var ts = await dialCoord.ShowInputAsync(this, "Info", $"Enter new row name");

                if (string.IsNullOrEmpty(ts))
                    return;

                if (Rows.Any(x => x.Name == ts))
                    return;

                Rows.Add(new Row {Name = ts});
            });

            Title = "Creating new board";
            FullTitle = Title;

            this.WhenAnyValue(x => x.BoardName)
                .Where(x => !string.IsNullOrWhiteSpace(x) && IsNewFile)
                .Subscribe(v => FileName = BoardNameToFileName(v));

            appConfig.ColorThemeObservable
                .Subscribe(x => ColorTheme = x);
        }

        private async Task CreateCommandExecute()
        {
            // 1. Checks
            if (string.IsNullOrWhiteSpace(BoardName) || string.IsNullOrWhiteSpace(FileName)
                                                     || string.IsNullOrWhiteSpace(FolderName))
            {
                await dialCoord.ShowMessageAsync(this, "Error", "Empty string");
                return;
            }

            if (!Directory.Exists(FolderName))
            {
                await dialCoord.ShowMessageAsync(this, "Error", "Directory does not exists");
                return;
            }

            string uri = FolderName + "\\" + FileName;

            if (!IsNewFile && !File.Exists(uri))
            {
                await dialCoord.ShowMessageAsync(this, "Error", "File not found");
                return;
            }

            if (IsNewFile && File.Exists(uri))
            {
                await dialCoord.ShowMessageAsync(this, "Error", "File already exists");
                return;
            }

            if (Columns.Count == 0)
            {
                await dialCoord.ShowMessageAsync(this, "Error", "Need add columns");
                return;
            }

            if (Rows.Count == 0)
            {
                await dialCoord.ShowMessageAsync(this, "Error", "Need add rows");
                return;
            }

            // 2. Create board (or get from preloaded)
            BoxViewModel box;

            if (IsNewFile)
            {
                box = await appModel.Create(uri);
                if (!box.Loaded)
                    throw new Exception("File not loaded");

                appConfig.UpdateRecent(uri, false);
            }
            else
            {
                box = await appModel.Load(uri);
                if (!box.Loaded)
                {
                    appConfig.RemoveRecent(uri);
                    appModel.Remove(uri);
                    await dialCoord.ShowMessageAsync(this, "Error", "File was damaged");
                    return;
                }

                if (box.Boards.Items.Any(x => x.Name == BoardName))
                {
                    await dialCoord.ShowMessageAsync(this, "Error", "Board name already used");
                    return;
                }
            }

            var bvm = new BoardViewModel
            {
                Name = BoardName,
                Created = DateTime.Now,
                Modified = DateTime.Now
            };

            box.Boards.Add(bvm);

            // 3. Normalize grid
            double colSize = Columns.Count == 1 ? 100 : 100 / (Columns.Count - 1);
            for (var i = 0; i < Columns.Count; i++)
                Columns[i].Order = i;

            double rowSize = Rows.Count == 1 ? 100 : 100 / (Rows.Count - 1);
            for (var i = 0; i < Rows.Count; i++)
                Rows[i].Order = i;

            // 4. Create columns
            foreach (var cvm in Columns)
            {
                var colToAdd = new ColumnViewModel
                {
                    BoardId = bvm.Id,
                    Size = (int) colSize * 10,
                    Order = cvm.Order,
                    Name = cvm.Name
                };

                box.Columns.Add(colToAdd);
            }

            // 5. Create rows
            foreach (var rvm in Rows)
            {
                var rowToAdd = new RowViewModel
                {
                    BoardId = bvm.Id,
                    Size = (int) rowSize * 10,
                    Order = rvm.Order,
                    Name = rvm.Name
                };

                box.Rows.Add(rowToAdd);
            }

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest {ViewId = uri, Box = box, Board = bvm},
                options: new UiShowOptions {Title = BoardName});

            this.Close();
        }

        private string BoardNameToFileName(string boardName)
        {
            // stop chars for short file name    +=[]:;«,./?'space'
            // stops for long                    /\:*?«<>|
            char[] separators =
            {
                '+', '=', '[', ']', ':', ';', '"', ',', '.', '/', '?', ' ',
                '\\', '*', '<', '>', '|'
            };

            string str = boardName.Replace(separators, "_");
            return str + ".kam";
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as WizardViewRequest;

            IsNewFile = request.Uri == null;

            if (IsNewFile)
            {
                FolderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                FileName = "MyBoard.kam";
                BoardName = "MyBoard";
            }
            else
            {
                FolderName = Path.GetDirectoryName(request.Uri);
                FileName = Path.GetFileName(request.Uri);
                BoardName = "MyBoard";
            }

            SelectedTemplate = Templates.First();

            FillFromTemplateCommand
                .Execute()
                .Subscribe();
        }
    } //end of class

    public static class ExtensionMethods
    {
        public static string Replace(this string s, char[] separators, string newVal)
        {
            var temp = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return String.Join(newVal, temp);
        }
    }
}
