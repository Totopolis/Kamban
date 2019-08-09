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

        private bool _redmineDataLoaded;
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
            var scheme = new BoxScheme();

            await LoadRedmineData();

            var boards = new List<Board>(_projects.Count);
            foreach (var project in _projects)
            {
                scheme.Rows.AddRange(_users
                    .Select((t, i) =>
                        new Row
                        {
                            Id = t.Id + _users.Count * project.Id,
                            BoardId = project.Id,
                            Name = t.Name,
                            Order = i
                        })
                );
                scheme.Columns.AddRange(_statuses
                    .Select((s, i) =>
                        new Column
                        {
                            Id = s.Id + _statuses.Count * project.Id,
                            BoardId = project.Id,
                            Name = s.Name,
                            Order = i
                        })
                );
                boards.Add(
                    new Board
                    {
                        Id = project.Id,
                        Name = project.Name,
                        Created = project.CreatedOn.GetValueOrDefault()
                    }
                );
            }

            scheme.Boards = boards;

            return scheme;
        }

        public async Task<List<Card>> LoadCards(CardFilter filter)
        {
            await LoadRedmineData();

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
                    }).ToList();
        }


        private async Task LoadRedmineData()
        {
            if (_redmineDataLoaded)
                return;

            _redmineDataLoaded = true;

            var issuesTask = _rm.GetObjectsAsync<Issue>(new NameValueCollection());
            var statusesTask = _rm.GetObjectsAsync<IssueStatus>(new NameValueCollection());
            var projectsTask = _rm.GetObjectsAsync<Project>(new NameValueCollection());

            _issues = await issuesTask;
            _users = _issues.Select(x => x.AssignedTo).Where(x => x != null).Distinct().ToList();
            _users.Add(NoneUser);
            _statuses = await statusesTask;
            _projects = await projectsTask;
        }

        private static readonly IdentifiableName NoneUser = new IdentifiableName {Id = int.MaxValue / 2, Name = "None"};
    }
}