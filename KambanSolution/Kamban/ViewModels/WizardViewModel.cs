using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        [Reactive] public string BoardName { get; set; }
        [Reactive] public string FolderName { get; set; }
        [Reactive] public string FileName { get; set; }
        [Reactive] public bool InExistedFile { get; set; }
        [Reactive] public bool CanCreate { get; set; }

        public ObservableCollection<LocalDimension> ColumnList { get; set; }
        public ObservableCollection<LocalDimension> RowList { get; set; }
        public ObservableCollection<string> BoardsInFile { get; set; }
        public ReactiveCommand CreateCommand { get; set; }
        public ReactiveCommand CancelCommand { get; set; }
        public ReactiveCommand SelectFolderCommand { get; set; }

        public ReactiveCommand AddColumnCommand { get; set; }
        public ReactiveCommand<LocalDimension, Unit> DeleteColumnCommand { get; set; }
        public ReactiveCommand AddRowCommand { get; set; }
        public ReactiveCommand<LocalDimension, Unit> DeleteRowCommand { get; set; }

        private readonly IAppModel appModel;
        private readonly IShell shell;
        private readonly IDialogCoordinator dialCoord;
        private IProjectService prjService;

        public WizardViewModel(IAppModel appModel, IShell shell, IDialogCoordinator dc)
        {
            this.appModel = appModel;
            this.shell = shell;
            dialCoord = dc;

            validator = new WizardValidator();
            Title = "Creating new file";
            FullTitle = "Creating new file";
            BoardsInFile = new ObservableCollection<string>();
            
            this.WhenAnyValue(x => x.BoardName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Subscribe(v =>
                {
                    if (!InExistedFile)
                        FileName = BoardNameToFileName(v);
                });

            BoardName = "My Board";

            FolderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SelectFolderCommand = ReactiveCommand.Create(SelectFolder);

            ColumnList = new ObservableCollection<LocalDimension>
            {
                new LocalDimension("Backlog"),
                new LocalDimension("In progress"),
                new LocalDimension("Done")
            };

            AddColumnCommand =
                ReactiveCommand.Create(() => ColumnList.Add(new LocalDimension("New column")));

            DeleteColumnCommand = ReactiveCommand
                .Create<LocalDimension>(column =>
                {
                    ColumnList.Remove(column);
                    UpdateDimensionList(ColumnList);
                });


            RowList = new ObservableCollection<LocalDimension>()
            {
                new LocalDimension("Important"),
                new LocalDimension("So-so"),
                new LocalDimension("Trash")
            };

            AddRowCommand =
                ReactiveCommand.Create(() => RowList.Add(new LocalDimension("New row")));

            DeleteRowCommand = ReactiveCommand
                .Create<LocalDimension>(row =>
                {
                    RowList.Remove(row);
                    UpdateDimensionList(RowList);
                });

            CreateCommand = ReactiveCommand.CreateFromTask(Create);

            CancelCommand = ReactiveCommand.Create(Close);

            ColumnList.CollectionChanged += (ob, arg) => UpdateDimensionList(ColumnList);
            RowList.CollectionChanged += (ob, arg) => UpdateDimensionList(RowList);

            AllErrors
                .Connect()
                .Subscribe(_ => CanCreate = !AllErrors.Items.Any() &&
                                           ColumnList.Count(col => col.HasErrors) == 0 &&
                                           RowList.Count(row => row.HasErrors) == 0);
        }

        private void UpdateDimensionList(ObservableCollection<LocalDimension> list)
        {
            foreach (var dim in list)
            {
                dim.IsDuplicate = false;
                dim.RaisePropertyChanged(nameof(dim.Name));
            }

            var duplicatgroups = list
                .GroupBy(dim => dim.Name)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in duplicatgroups)
            {
                foreach (var dim in group)
                {
                    dim.IsDuplicate = true;
                    dim.RaisePropertyChanged(nameof(dim.Name));
                }
            }

            CanCreate = !AllErrors.Items.Any() &&
                        ColumnList.Count(col => col.HasErrors) == 0 &&
                        RowList.Count(row => row.HasErrors) == 0;
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
            return str + ".db";
        }

        public void SelectFolder()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                SelectedPath = FolderName
            };
            //dialog.RootFolder = Environment.SpecialFolder.MyDocuments;

            if (dialog.ShowDialog() == DialogResult.OK)
                FolderName = dialog.SelectedPath;
        }

        public async Task Create()
        {
            var uri = FolderName + "\\" + FileName;

            if (!InExistedFile)
            {
                prjService = appModel.CreateProjectService(uri);
            }
            else
            {
                var boards = await prjService.GetAllBoardsInFileAsync();
                BoardsInFile.AddRange(boards.Select(board => board.Name));

                if (BoardsInFile.Contains(BoardName))
                {
                    await dialCoord.ShowMessageAsync(this, "Can not create board",
                        "Board with such name already exists in file");
                    return;
                }
            }

            appModel.AddRecent(uri);

            appModel.SaveConfig();

            var newBoard = new BoardInfo()
            {
                Name = BoardName,
                Created = DateTime.Now,
                Modified = DateTime.Now
            };

            newBoard = prjService.CreateOrUpdateBoardAsync(newBoard);

            foreach (var colName in ColumnList.Select(column => column.Name))
                prjService.CreateOrUpdateColumnAsync(new ColumnInfo
                {
                    Name = colName,
                    BoardId = newBoard.Id
                });

            foreach (var rowName in RowList.Select(row => row.Name))
                prjService.CreateOrUpdateRowAsync(new RowInfo
                {
                    Name = rowName,
                    BoardId = newBoard.Id
                });

            Close();

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest { ViewId = uri, PrjService = prjService, NeededBoardName = BoardName },
                options: new UiShowOptions { Title = uri });
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as WizardViewRequest;

            InExistedFile = (bool)request?.InExistedFile;

            if (InExistedFile)
            {
                var uri = request.Uri;
                FolderName = Path.GetDirectoryName(uri);
                FileName = Path.GetFileName(uri);
                FullTitle = $"Creating new board";
                FullTitle = $"Creating new board in {uri}";
                prjService = appModel.CreateProjectService(uri);
                /*Observable.FromAsync(() => scope.GetAllBoardsInFileAsync())
                    .ObserveOnDispatcher()
                    .Subscribe(boards =>
                        BoardsInFile.PublishCollection(boards.Select(board => board.Name)));*/
            }
        }

        public class LocalDimension : ViewModelBase, IDataErrorInfo
        {
            public LocalDimension(string name)
            {
                Name = name;
                validator = new LocalDimensionValidator();
            }

            public bool HasErrors { get; set; }
            public bool IsDuplicate { get; set; }
            [Reactive] public string Name { get; set; }

            public new IValidator validator;

            public new string Error
            {
                get
                {
                    var results = validator?.Validate(this);

                    if (results != null && results.Errors.Any())
                    {
                        var errors = string.Join(Environment.NewLine,
                            results.Errors.Select(x => x.ErrorMessage).ToArray());
                        return errors;
                    }

                    return string.Empty;
                }
            }

            public new string this[string columnName]
            {
                get
                {
                    var errs = validator?
                        .Validate(this).Errors;

                    HasErrors = errs?.Any() ?? false;

                    if (errs != null)
                        return validator != null
                            ? string.Join("; ", errs.Select(e => e.ErrorMessage))
                            : "";
                    return "";
                }
            }
        } //class
    }

    public static class ExtensionMethods
    {
        public static string Replace(this string s, char[] separators, string newVal)
        {
            var temp = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            return String.Join(newVal, temp);
        }
    }
}
