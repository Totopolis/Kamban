using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using Kamban.Repository.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.ViewModels.ImportScheme
{
    public class BoxImportSchemeViewModel : ReactiveObject
    {
        [Reactive] public bool HasBoards { get; set; }
        [Reactive] public bool? IsAllBoardsSelected { get; set; }
        public ReactiveCommand<bool?, Unit> AllBoardsSelectionCommand { get; set; }

        [Reactive] public bool HasColumns { get; set; }
        [Reactive] public bool? IsAllColumnsSelected { get; set; }
        public ReactiveCommand<bool?, Unit> AllColumnsSelectionCommand { get; set; }

        [Reactive] public bool HasRows { get; set; }
        [Reactive] public bool? IsAllRowsSelected { get; set; }
        public ReactiveCommand<bool?, Unit> AllRowsSelectionCommand { get; set; }

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

            var boardsPublish = _boardsSource.Connect()
                .ObserveOnDispatcher()
                .Publish();
            boardsPublish
                .Count()
                .Subscribe(x => HasBoards = x > 0);
            boardsPublish
                .Bind(out _boards)
                .Subscribe();
            boardsPublish
                .WhenPropertyChanged(x => x.IsSelected)
                .Subscribe(x =>
                    IsAllBoardsSelected = _boards.All(b => b.IsSelected)
                        ? true
                        : _boards.Any(b => b.IsSelected)
                            ? (bool?) null
                            : false);
            boardsPublish.Connect();

            var selectedBoardChanged = this.WhenAnyValue(x => x.SelectedBoard).Publish();

            var columnsPublish = _columnsSource.Connect()
                .Filter(selectedBoardChanged.Select(CreateColumnPredicate))
                .ObserveOnDispatcher()
                .Publish();
            columnsPublish
                .Count()
                .Subscribe(x => HasColumns = x > 0);
            columnsPublish
                .Bind(out _columns)
                .Subscribe();
            columnsPublish
                .WhenPropertyChanged(x => x.IsSelected)
                .Subscribe(x =>
                    IsAllColumnsSelected = _columns.All(c => c.IsSelected)
                        ? true
                        : _columns.Any(c => c.IsSelected)
                            ? (bool?) null
                            : false);
            columnsPublish.Connect();

            var rowsPublish = _rowsSource.Connect()
                .Filter(selectedBoardChanged.Select(CreateRowPredicate))
                .ObserveOnDispatcher()
                .Publish();
            rowsPublish
                .Count()
                .Subscribe(x => HasRows = x > 0);
            rowsPublish
                .Bind(out _rows)
                .Subscribe();
            rowsPublish
                .WhenPropertyChanged(x => x.IsSelected)
                .Subscribe(x =>
                    IsAllRowsSelected = _rows.All(r => r.IsSelected)
                        ? true
                        : _rows.Any(r => r.IsSelected)
                            ? (bool?) null
                            : false);
            rowsPublish.Connect();

            selectedBoardChanged.Connect();

            AllBoardsSelectionCommand = ReactiveCommand.Create<bool?>(x =>
            {
                foreach (var board in _boards)
                    board.IsSelected = board.IsEnabled && x.HasValue && x.Value;
            });
            AllColumnsSelectionCommand = ReactiveCommand.Create<bool?>(x =>
            {
                foreach (var column in _columns)
                    column.IsSelected = x.HasValue && x.Value;
            });
            AllRowsSelectionCommand = ReactiveCommand.Create<bool?>(x =>
            {
                foreach (var row in _rows)
                    row.IsSelected = x.HasValue && x.Value;
            });
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

        public void Update(BoxScheme scheme)
        {
            UpdateBoards(scheme.Boards);
            UpdateColumns(scheme.Columns);
            UpdateRows(scheme.Rows);
        }

        public void UpdateBoards(List<Board> boards)
        {
            _boardsSource.Edit(x =>
            {
                x.Clear();
                if (boards != null)
                {
                    x.AddRange(boards.Select(y => new BoardImportSchemeViewModel
                        { Id = y.Id, Name = y.Name, IsSelected = true, IsEnabled = true }));
                }
            });

            SelectedBoard = _boardsSource.Items.FirstOrDefault();
        }

        public void UpdateColumns(List<Column> columns)
        {
            _columnsSource.Edit(x =>
            {
                x.Clear();
                if (columns != null)
                {
                    x.AddRange(columns.Select(y => new ColumnImportSchemeViewModel
                        { Id = y.Id, BoardId = y.BoardId, Name = y.Name, IsSelected = true }));
                }
            });
        }

        public void UpdateRows(List<Row> rows)
        {
            _rowsSource.Edit(x =>
            {
                x.Clear();
                if (rows != null)
                {
                    x.AddRange(rows.Select(y => new RowImportSchemeViewModel
                        { Id = y.Id, BoardId = y.BoardId, Name = y.Name, IsSelected = true }));
                }
            });
        }

        public CardFilter GetCardFilter()
        {
            return new CardFilter
            {
                BoardIds = _boardsSource.Items
                    .Where(x => x.IsSelected)
                    .Select(x => x.Id)
                    .ToArray(),
                ColumnIds = _columnsSource.Items
                    .Where(x => x.IsSelected)
                    .Select(x => x.Id)
                    .ToArray(),
                RowIds = _rowsSource.Items
                    .Where(x => x.IsSelected)
                    .Select(x => x.Id)
                    .ToArray()
            };
        }

        public bool IsSchemeValid()
        {
            var boardIds = _boardsSource.Items
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToArray();

            var boardIdsFromColumns = _columnsSource.Items
                .Where(x => x.IsSelected)
                .Select(x => x.BoardId);

            if (boardIds.Except(boardIdsFromColumns).Any())
                return false;

            var boardIdsFromRows = _rowsSource.Items
                .Where(x => x.IsSelected)
                .Select(x => x.BoardId);

            if (boardIds.Except(boardIdsFromRows).Any())
                return false;

            return true;
        }
    }
}