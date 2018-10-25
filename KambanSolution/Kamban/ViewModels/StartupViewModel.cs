using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using DynamicData;
using Kamban.Model;
using Kamban.Views;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class RecentTile : ReactiveObject
    {
        [Reactive] public int TotalTickets { get; set; }
        [Reactive] public string BoardList { get; set; }
        [Reactive] public string Uri { get; set; }
    }

    public class StartupViewModel : ViewModelBase, IInitializableViewModel
    {
        private readonly IShell shell;
        private readonly IAppModel appModel;
        private readonly IDialogCoordinator dialCoord;
        private readonly SourceList<RecentTile> recentList;

        public ReadOnlyObservableCollection<RecentTile> Recents { get; set; }
        public ReactiveCommand NewFileCommand { get; set; }
        public ReactiveCommand OpenFileCommand { get; set; }
        public ReactiveCommand<string, Unit> OpenRecentDbCommand { get; set; }
        public ReactiveCommand<string, Unit> RemoveRecentCommand { get; set; }
        public ReactiveCommand ExportCommand { get; set; }
        public ReactiveCommand ExitCommand { get; set; }

        [Reactive]
        public bool IsLoading { get; set; }

        public StartupViewModel(IShell shell, IAppModel appModel, IDialogCoordinator dc)
        {
            this.shell = shell as IShell;
            this.appModel = appModel;
            dialCoord = dc;

            recentList = new SourceList<RecentTile>();
            recentList
                .Connect()
                .Bind(out ReadOnlyObservableCollection<RecentTile> temp)
                .Subscribe();

            Recents = temp;

            OpenRecentDbCommand = ReactiveCommand.Create<string>(async (uri) =>
            {
                IsLoading = true;

                if (!await OpenBoardView(uri))
                {
                    RemoveRecent(uri);

                    await dialCoord.ShowMessageAsync(this, "Error",
                        "File was deleted or moved");
                }

                IsLoading = false;
            });

            RemoveRecentCommand = ReactiveCommand.Create<string>(RemoveRecent);

            NewFileCommand = ReactiveCommand.Create(() =>
                this.shell.ShowView<WizardView>(
                    new WizardViewRequest { ViewId = "Creating new file", InExistedFile = false }));

            OpenFileCommand = ReactiveCommand.Create(async () =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = @"SQLite DataBase | *.db",
                    Title = @"Select exists database"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    this.IsLoading = true;

                    var uri = dialog.FileName;
                    await OpenBoardView(uri);
                    AddRecent(uri);

                    this.IsLoading = false;
                }
            });

            var canExport = recentList
                .Connect()
                .Select(x => x.Count > 0);

            ExportCommand = ReactiveCommand.Create(() => 
                this.shell.ShowView<ExportView>(), canExport);

            ExitCommand = ReactiveCommand.Create(() => App.Current.Shutdown());

            this.IsLoading = false;
        } //ctor

        private void RemoveRecent(string uri)
        {
            appModel.RemoveRecent(uri);
            appModel.SaveConfig();

            var tile = recentList.Items.Where(x => x.Uri == uri).FirstOrDefault();
            if (tile != null)
                recentList.Remove(tile);
        }

        private void AddRecent(string uri)
        {
            appModel.AddRecent(uri);
            appModel.SaveConfig();

            var tile = recentList.Items.Where(x => x.Uri == uri).FirstOrDefault();
            if (tile != null)
                recentList.Remove(tile);
            else
            {
                tile = new RecentTile { Uri = uri };
                Task.Run(() => LoadStatAsync(new[] { tile }));
            }

            recentList.Insert(0, tile);

            if (recentList.Count > 6)
                recentList.RemoveAt(6);
        }

        private async Task<bool> OpenBoardView(string uri)
        {
            var file = new FileInfo(uri);

            if (!file.Exists)
                return false;

            IProjectService service = await Task.Run(() => appModel.LoadProjectService(uri));

            var title = file.FullName;

            await Task.Delay(200);

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest { ViewId = title, PrjService = service },
                options: new UiShowOptions { Title = title });

            AddRecent(uri);

            return true;
        }

        public void Initialize(ViewRequest viewRequest)
        {
            shell.AddGlobalCommand("File", "New db", "NewFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.N);

            shell.AddGlobalCommand("File", "Open", "OpenFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.O);

            shell.AddGlobalCommand("File", "Export", "ExportCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.U);

            shell.AddGlobalCommand("File", "Exit", "ExitCommand", this);

            UpdateRecent();

            Task.Run(() => LoadStatAsync(recentList.Items));
        }

        private void UpdateRecent()
        {
            this.appModel.LoadConfig();
            var rcnts = this.appModel.GetRecentDocuments();
            recentList.Clear();

            var tiles = rcnts.Select(x => new RecentTile { Uri = x }).Take(6);
            recentList.AddRange(tiles);
        }

        private async Task LoadStatAsync(IEnumerable<RecentTile> tiles)
        {
            foreach (var it in tiles)
            {
                if (!File.Exists(it.Uri))
                    continue;

                try
                {
                    var prj = appModel.LoadProjectService(it.Uri);
                    var boards = await prj.GetAllBoardsInFileAsync();

                    it.BoardList = string.Join(",", boards.Select(x => x.Name));

                    it.TotalTickets = 0;
                    foreach (var brd in boards)
                        it.TotalTickets += (await prj.GetIssuesByBoardIdAsync(brd.Id)).Count();
                }
                // Skip broken file
                catch { }
            }//foreach
        }
    }//end of classs
}
