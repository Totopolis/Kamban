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
using DynamicData.Binding;
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
        [Reactive] public DateTime LastAccess { get; set; }
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
                .AutoRefresh()
                .Sort(SortExpressionComparer<RecentTile>.Descending(x => x.LastAccess))
                //.Top(3) // ?
                .Bind(out ReadOnlyObservableCollection<RecentTile> temp)
                .Subscribe();

            Recents = temp;

            OpenRecentDbCommand = ReactiveCommand.Create<string>(async (uri) =>
            {
                IsLoading = true;
                await OpenBoardView(uri);
                IsLoading = false;
            });

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
                    this.IsLoading = false;
                }
            });

            var canExport = recentList
                .Connect()
                .AutoRefresh()
                .Select(x => x.Count > 0);

            ExportCommand = ReactiveCommand.Create(() => 
                this.shell.ShowView<ExportView>(), canExport);

            ExitCommand = ReactiveCommand.Create(() => App.Current.Shutdown());

            this.IsLoading = false;
        } //ctor

        private async Task OpenBoardView(string uri)
        {
            var file = new FileInfo(uri);

            if (!file.Exists)
            {
                await dialCoord.ShowMessageAsync(this, "Error", "File was deleted or moved");
                return;
            }

            IProjectService service;

            try
            {
                service = await Task.Run(() => appModel.LoadProjectService(uri));
            }
            catch
            {
                await dialCoord.ShowMessageAsync(this, "Error", "File was damaged");
                return;
            }

            var title = file.FullName;

            await Task.Delay(200);

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest { ViewId = title, PrjService = service },
                options: new UiShowOptions { Title = title });

            var tile = recentList.Items.Where(x => x.Uri == uri).FirstOrDefault();
            if (tile == null)
            {
                tile = new RecentTile { Uri = uri };
                await LoadTileAsync(tile);
                recentList.Add(tile);

                appModel.AddRecent(uri);
                appModel.SaveConfig();
            }
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

            Observable.FromAsync(() => LoadRecentsAsync())
                .ObserveOnDispatcher()
                .Subscribe(tiles => recentList.Edit(il =>
                {
                    recentList.Clear();
                    recentList.AddRange(tiles);
                }));
        }

        private async Task<RecentTile[]> LoadRecentsAsync()
        {
            this.appModel.LoadConfig();
            var rcnts = this.appModel.GetRecentDocuments();
            var tiles = rcnts.Select(x => new RecentTile { Uri = x }).ToList(); //.Take(6);

            foreach (var it in tiles)
                await LoadTileAsync(it);

            return tiles.ToArray();
        }

        private async Task LoadTileAsync(RecentTile tile)
        {
            if (!File.Exists(tile.Uri))
                return;

            try
            {
                tile.LastAccess = File.GetLastWriteTime(tile.Uri);

                var prj = appModel.LoadProjectService(tile.Uri);
                var boards = await prj.GetAllBoardsInFileAsync();

                var lst = boards.Select(x => x.Name).ToList();
                var str = string.Join(",", lst);
                tile.BoardList = str;

                tile.TotalTickets = 0;
                foreach (var brd in boards)
                    tile.TotalTickets += (await prj.GetIssuesByBoardIdAsync(brd.Id)).Count();
            }
            // Skip broken file
            catch { }
        }

    }//end of classs
}
