using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Kamban.Repository.Models;
using Redmine.Net.Api;
using Redmine.Net.Api.Async;
using Redmine.Net.Api.Types;

namespace Kamban.Repository.Redmine
{
    public class RedmineRepository : ILoadRepository
    {
        private RedmineManager _rm;

        private BoxScheme _scheme = new BoxScheme();
        private List<Issue> _issues;
        private List<IdentifiableName> _users;
        private List<IssueStatus> _statuses;
        private List<Project> _projects;

        public RedmineRepository(string host, string login, string password)
        {
            _rm = new RedmineManager(host, login, password);
        }

        public RedmineRepository(string host, string apiKey)
        {
            _rm = new RedmineManager(host, apiKey);
        }

        public void Dispose()
        {
        }

        public async Task<Box> Load()
        {
            var schemeTask = LoadScheme();
            var cardsTask = LoadCards(CardFilter.None);

            var scheme = await schemeTask;
            var cards = await cardsTask;

            return new Box
            {
                Boards = scheme.Boards,
                Rows = scheme.Rows,
                Columns = scheme.Columns,
                Cards = cards
            };
        }

        public async Task<BoxScheme> LoadScheme()
        {
            await EnsureSchemeLoaded();
            return _scheme;
        }

        public async Task<List<Board>> LoadSchemeBoards()
        {
            _projects = await _rm.GetObjectsAsync<Project>(new NameValueCollection());
            var boards = _projects
                .Select(project => new Board
                {
                    Id = project.Id,
                    Name = project.Name,
                    Created = project.CreatedOn.GetValueOrDefault()
                })
                .ToList();

            _scheme.Boards = boards;
            _scheme.Rows = null;

            return boards;
        }

        public async Task<List<Column>> LoadSchemeColumns(int[] boardIds = null)
        {
            _statuses = await _rm.GetObjectsAsync<IssueStatus>(new NameValueCollection());
            var columns = _projects
                .Where(project => boardIds?.Contains(project.Id) ?? true)
                .SelectMany(project =>
                    _statuses
                        .Select((s, i) =>
                            new Column
                            {
                                Id = s.Id + _statuses.Count * project.Id,
                                BoardId = project.Id,
                                Name = s.Name,
                                Order = i
                            }))
                .ToList();

            _scheme.Columns = columns;

            return columns;
        }

        public async Task<List<Row>> LoadSchemeRows(int[] boardIds = null)
        {
            var nvc = new NameValueCollection();
            if (boardIds != null)
            {
                foreach (var id in boardIds)
                {
                    nvc.Add(RedmineKeys.PROJECT_ID, id.ToString());
                }
            }

            _issues = await _rm.GetObjectsAsync<Issue>(nvc);
            _users = _issues.Select(x => x.AssignedTo).Where(x => x != null).Distinct().ToList();
            _users.Add(NoneUser);

            var rows = _projects
                .Where(project => boardIds?.Contains(project.Id) ?? true)
                .SelectMany(project =>
                    _users
                        .Select((t, i) =>
                            new Row
                            {
                                Id = t.Id + _users.Count * project.Id,
                                BoardId = project.Id,
                                Name = t.Name,
                                Order = i
                            }))
                .ToList();

            _scheme.Rows = rows;

            return rows;
        }

        public async Task<List<Card>> LoadCards(CardFilter filter)
        {
            await EnsureSchemeLoaded();

            return _issues
                .Where(x => (filter.BoardIds == null ||
                             filter.BoardIds.Contains(x.Project.Id)) &&
                            (filter.RowIds == null ||
                             filter.RowIds.Contains((x.AssignedTo ?? NoneUser).Id + _users.Count * x.Project.Id)) &&
                            (filter.ColumnIds == null ||
                             filter.ColumnIds.Contains(x.Status.Id + _statuses.Count * x.Project.Id)))
                .Select(x =>
                    new Card
                    {
                        Id = x.Id,
                        BoardId = x.Project.Id,
                        RowId = (x.AssignedTo ?? NoneUser).Id + _users.Count * x.Project.Id,
                        ColumnId = x.Status.Id + _statuses.Count * x.Project.Id,
                        Head = x.Subject,
                        Body = x.Description,
                        Created = x.CreatedOn.GetValueOrDefault(),
                    })
                .ToList();
        }


        private async Task EnsureSchemeLoaded()
        {
            if (_scheme.Boards == null)
            {
                await LoadSchemeBoards();
            }

            if (_scheme.Columns == null)
            {
                await LoadSchemeColumns();
            }

            if (_scheme.Rows == null)
            {
                await LoadSchemeRows();
            }
        }

        private static readonly IdentifiableName NoneUser = new IdentifiableName {Id = int.MaxValue / 2, Name = "None"};
    }
}