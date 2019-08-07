using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Kamban.Repository.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.ViewModels.ImportScheme
{
    public class BoxImportSchemeViewModel : ReactiveObject
    {
        [Reactive] public BoardImportSchemeViewModel SelectedBoard { get; set; }

        private readonly SourceList<BoardImportSchemeViewModel> _boardsSource;
        private readonly SourceList<ColumnImportSchemeViewModel> _columnsSource;
        private readonly SourceList<RowImportSchemeViewModel> _rowsSource;

        private readonly ReadOnlyObservableCollection<BoardImportSchemeViewModel> _boards;
        public ReadOnlyObservableCollection<BoardImportSchemeViewModel> Boards => _boards;

        private readonly ReadOnlyObservableCollection<ColumnImportSchemeViewModel> _columns;
        public ReadOnlyObservableCollection<ColumnImportSchemeViewModel> Columns => _columns;

        private readonly ReadOnlyObservableCollection<RowImportSchemeViewModel> _rows;
        public ReadOnlyObservableCollection<RowImportSchemeViewModel> Rows => _rows;
        
        public BoxImportSchemeViewModel()
        {
            _boardsSource = new SourceList<BoardImportSchemeViewModel>();
            _columnsSource = new SourceList<ColumnImportSchemeViewModel>();
            _rowsSource = new SourceList<RowImportSchemeViewModel>();

            _boardsSource.Connect()
                .ObserveOnDispatcher()
                .Bind(out _boards)
                .DisposeMany()
                .Subscribe();

            var selectedBoardChanged = this.WhenAnyValue(x => x.SelectedBoard).Publish();

            _columnsSource.Connect()
                .Filter(selectedBoardChanged.Select(CreateColumnPredicate))
                .ObserveOnDispatcher()
                .Bind(out _columns)
                .DisposeMany()
                .Subscribe();

            _rowsSource.Connect()
                .Filter(selectedBoardChanged.Select(CreateRowPredicate))
                .ObserveOnDispatcher()
                .Bind(out _rows)
                .DisposeMany()
                .Subscribe();

            selectedBoardChanged.Connect();
        }

        private static Func<ColumnImportSchemeViewModel, bool> CreateColumnPredicate(BoardImportSchemeViewModel selectedBoard)
        {
            if (selectedBoard == null)
                return x => false;

            return x => x.BoardId == selectedBoard.Id;
        }

        private static Func<RowImportSchemeViewModel, bool> CreateRowPredicate(BoardImportSchemeViewModel selectedBoard)
        {
            if (selectedBoard == null)
                return x => false;

            return x => x.BoardId == selectedBoard.Id;
        }

        public void Reload(BoxScheme scheme)
        {
            _boardsSource.Edit(x =>
            {
                x.Clear();
                x.AddRange(scheme.Boards.Select(y => new BoardImportSchemeViewModel
                    {Id = y.Id, Name = y.Name, IsSelected = true}));
            });
            _columnsSource.Edit(x =>
            {
                x.Clear();
                x.AddRange(scheme.Columns.Select(y => new ColumnImportSchemeViewModel
                    { Id = y.Id, BoardId = y.BoardId, Name = y.Name, IsSelected = true }));
            });
            _rowsSource.Edit(x =>
            {
                x.Clear();
                x.AddRange(scheme.Rows.Select(y => new RowImportSchemeViewModel
                    { Id = y.Id, BoardId = y.BoardId, Name = y.Name, IsSelected = true }));
            });

            SelectedBoard = _boardsSource.Items.FirstOrDefault();
        }
    }
}