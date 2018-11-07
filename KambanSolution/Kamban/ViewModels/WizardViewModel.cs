using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoMapper;
using DynamicData;
using FluentValidation;
using Kamban.Model;
using Kamban.Views;
using Kamban.Views.WpfResources;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class WizardViewModel : ViewModelBase, IInitializableViewModel
    {
        [Reactive] public bool IsNewFile { get; set; }
        [Reactive] public string BoardName { get; set; }

        [Reactive]
        public List<BoardTemplate> Templates { get; set; } =
            InternalBoardTemplates.Templates;

        [Reactive] public BoardTemplate SelectedTemplate { get; set; }
        [Reactive] public string FileName { get; set; }
        [Reactive] public string FolderName { get; set; }

        [Reactive] public ObservableCollection<ColumnViewModel> Columns { get; set; }
        [Reactive] public ObservableCollection<RowViewModel> Rows { get; set; }

        public ReactiveCommand<Unit, Unit> FillFromTemplateCommand { get; set; }
        public ReactiveCommand AddColumnCommand { get; set; }
        public ReactiveCommand AddRowCommand { get; set; }
        public ReactiveCommand ClearAllCommand { get; set; }

        public ReactiveCommand CreateCommand { get; set; }
        public ReactiveCommand CancelCommand { get; set; }

        public ReactiveCommand SelectFolderCommand { get; set; }

        private readonly IAppModel appModel;
        private readonly IShell shell;
        private readonly IDialogCoordinator dialCoord;
        private readonly IMapper mapper;
        private readonly IAppConfig appConfig;

        public WizardViewModel(IAppModel appModel, IShell shell, IDialogCoordinator dc, IMapper mp,
            IAppConfig cfg)
        {
            this.appModel = appModel;
            this.shell = shell;
            dialCoord = dc;
            mapper = mp;
            appConfig = cfg;

            Columns = new ObservableCollection<ColumnViewModel>();
            Rows = new ObservableCollection<RowViewModel>();

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
            CancelCommand = ReactiveCommand.Create(() => this.Close());

            SelectFolderCommand = ReactiveCommand.Create(() =>
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog
                {
                    ShowNewFolderButton = false,
                    SelectedPath = FolderName
                };
                //dialog.RootFolder = Environment.SpecialFolder.MyDocuments;

                if (dialog.ShowDialog() == DialogResult.OK)
                    FolderName = dialog.SelectedPath;
            });

            AddColumnCommand = ReactiveCommand.CreateFromTask(async _ =>
            {
                var ts = await dialCoord.ShowInputAsync(this, "Info", $"Enter new column name");

                if (string.IsNullOrEmpty(ts))
                    return;

                if (Columns.Where(x => x.Name == ts).Count() > 0)
                    return;

                Columns.Add(new ColumnViewModel { Name = ts });
            });

            AddRowCommand = ReactiveCommand.CreateFromTask(async _ =>
            {
                var ts = await dialCoord.ShowInputAsync(this, "Info", $"Enter new row name");

                if (string.IsNullOrEmpty(ts))
                    return;

                if (Rows.Where(x => x.Name == ts).Count() > 0)
                    return;

                Rows.Add(new RowViewModel { Name = ts });
            });

            Title = "Creating new board";
            FullTitle = Title;

            this.WhenAnyValue(x => x.BoardName)
                .Where(x => !string.IsNullOrWhiteSpace(x) && IsNewFile)
                .Subscribe(v => FileName = BoardNameToFileName(v));
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
            DbViewModel db;

            if (IsNewFile)
            {
                db = await appModel.CreateDb(uri);
                if (!db.Loaded)
                    throw new Exception("File not loaded");

                appConfig.UpdateRecent(uri, false);
            }
            else
            {
                db = await appModel.LoadDb(uri);
                if (!db.Loaded)
                {
                    appConfig.RemoveRecent(uri);
                    appModel.RemoveDb(uri);
                    await dialCoord.ShowMessageAsync(this, "Error", "File was damaged");
                    return;
                }

                if (db.Boards.Items.Where(x => x.Name == BoardName).Count() > 0)
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

            db.Boards.Add(bvm);

            // 3. Normalize grid
            double colSize = Columns.Count == 1 ? 100 : 100 / (Columns.Count - 1);
            for (int i = 0; i < Columns.Count; i++)
            {
                Columns[i].Order = i;
                Columns[i].Size = (int)colSize * 10;
            }

            double rowSize = Rows.Count == 1 ? 100 : 100 / (Rows.Count - 1);
            for (int i = 0; i < Rows.Count; i++)
            {
                Rows[i].Order = i;
                Rows[i].Size = (int)rowSize * 10;
            }

            // 4. Create columns
            foreach (var cvm in Columns)
            {
                var colToAdd = new ColumnViewModel
                {
                    BoardId = bvm.Id,
                    Size = cvm.Size,
                    Order = cvm.Order,
                    Name = cvm.Name
                };

                db.Columns.Add(colToAdd);
            }

            // 5. Create rows
            foreach (var rvm in Rows)
            {
                var rowToAdd = new RowViewModel
                {
                    BoardId = bvm.Id,
                    Size = rvm.Size,
                    Order = rvm.Order,
                    Name = rvm.Name
                };

                db.Rows.Add(rowToAdd);
            }

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest { ViewId = uri, Db = db, Board = bvm },
                options: new UiShowOptions { Title = BoardName });

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
    }//end of class

    public static class ExtensionMethods
    {
        public static string Replace(this string s, char[] separators, string newVal)
        {
            var temp = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return String.Join(newVal, temp);
        }
    }
}
