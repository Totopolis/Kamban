using System;
using System.Linq;
using System.Reactive.Linq;
using AutoMapper;
using Kamban.Models;
using Kamban.SqliteLocalStorage.Entities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class IssueViewRequest : ViewRequest
    {
        public int? IssueId { get; set; }
        public IScopeModel Scope { get; set; }
        public BoardInfo Board { get; set; }
    }

    public class IssueViewModel : ViewModelBase, IInitializableViewModel
    {
        private readonly IMapper mapper;
        private IScopeModel scope;
        private BoardInfo board;

        public int Id { get; set; }
        public DateTime Created { get; set; }

        public ReactiveList<RowInfo> AwailableRows { get; set; } =
            new ReactiveList<RowInfo>();

        public ReactiveList<ColumnInfo> AwailableColumns { get; set; } =
            new ReactiveList<ColumnInfo>();

        [Reactive] public string Head { get; set; }
        [Reactive] public string Body { get; set; }
        [Reactive] public RowInfo Row { get; set; }
        [Reactive] public ColumnInfo Column { get; set; }
        [Reactive] public string Color { get; set; }

        public ReactiveCommand CancelCommand { get; set; }
        public ReactiveCommand SaveCommand { get; set; }
        [Reactive] public bool IsOpened { get; set; }
        [Reactive] public bool IssueChanged { get; set; }

        public IssueViewModel()
        {
            mapper = CreateMapper();

            var issueFilled = this.WhenAnyValue(t => t.Head, t => t.Body, t => t.Row, t => t.Column,
                (sh, sb, sr, sc) => sr != null                && sc != null &&
                                    !string.IsNullOrEmpty(sh) && !string.IsNullOrEmpty(sb));
            //TODO :add selectcommand when click uneditable with nulling all "selected" fields

            SaveCommand = ReactiveCommand.CreateFromTask(async _ =>
            {
                var editedIssue = new LocalIssue() {Board = board};

                mapper.Map(this, editedIssue);

                if (editedIssue.Id == 0)
                    editedIssue.Created = DateTime.Now;

                editedIssue.Modified = DateTime.Now;

                await scope.CreateOrUpdateIssueAsync(editedIssue);

                IsOpened = false;
                IssueChanged = true;
            }, issueFilled);

            CancelCommand = ReactiveCommand.Create(() => IsOpened = false);
        }

        public void Initialize(ViewRequest viewRequest)
        {
            if (viewRequest is IssueViewRequest request)
            {
                scope = request.Scope;

                board = request.Board;

                mapper.Map(new LocalIssue(), this);

                IssueChanged = false;

                Observable.FromAsync(() => scope.GetRowsByBoardIdAsync(board.Id))
                    .ObserveOnDispatcher()
                    .Subscribe(rows =>
                    {
                        AwailableRows.PublishCollection(rows);
                        Row = AwailableRows.First();
                    });

                Observable.FromAsync(() => scope.GetColumnsByBoardIdAsync(board.Id))
                    .ObserveOnDispatcher()
                    .Subscribe(columns =>
                    {
                        AwailableColumns.PublishCollection(columns);
                        Column = AwailableColumns.First();
                    });

                var issueId = request.IssueId;

                if (issueId != null && issueId > 0)

                    Observable.FromAsync(() => scope.LoadOrCreateIssueAsync(issueId))
                        .ObserveOnDispatcher()
                        .Subscribe(issue =>
                        {
                            mapper.Map(issue, this);
                            Row = AwailableRows.First(r => r.Id       == Row.Id);
                            Column = AwailableColumns.First(c => c.Id == Column.Id);
                        });
            }

            Title = $"Редактирование задачи {Head}";
            IsOpened = true;
        }

        private IMapper CreateMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
                cfg.AddProfile(typeof(MapperProfileSqliteRepos)));

            return mapperConfig.CreateMapper();
        }

        private class MapperProfileSqliteRepos : Profile
        {
            public MapperProfileSqliteRepos()
            {
                CreateMap<LocalIssue, IssueViewModel>();

                CreateMap<IssueViewModel, LocalIssue>();
            }
        }
    }
}
