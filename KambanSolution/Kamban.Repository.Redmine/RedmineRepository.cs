using Kamban.Repository.Models;
using Redmine.Net.Api;
using Redmine.Net.Api.Async;
using Redmine.Net.Api.Types;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Kamban.Repository.Redmine
{
    public class RedmineRepository : ILoadRepository
    {
        private RedmineManager _rm;

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
            var trackerTask = _rm.GetObjectsAsync<Tracker>(new NameValueCollection());
            var statusesTask = _rm.GetObjectsAsync<IssueStatus>(new NameValueCollection());
            var projectsTask = _rm.GetObjectsAsync<Project>(
                new NameValueCollection
                {
                    {RedmineKeys.INCLUDE, RedmineKeys.TRACKERS},
                    {RedmineKeys.INCLUDE, RedmineKeys.ISSUE_CATEGORIES}
                });

            var trackers = await trackerTask;
            var statuses = await statusesTask;
            var projects = await projectsTask;

            var boards = new List<Board>(projects.Count);
            foreach (var project in projects)
            {
                scheme.Rows.AddRange(trackers
                    .Where(t => project.Trackers.Any(x => x.Id == t.Id))
                    .Select((t, i) =>
                        new Row
                        {
                            Id = t.Id + trackers.Count * project.Id,
                            BoardId = project.Id,
                            Name = t.Name,
                            Order = i
                        })
                );
                scheme.Columns.AddRange(statuses
                    .Select((s, i) =>
                        new Column
                        {
                            Id = s.Id + statuses.Count * project.Id,
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
            var trackerTask = _rm.GetObjectsAsync<Tracker>(new NameValueCollection());
            var statusesTask = _rm.GetObjectsAsync<IssueStatus>(new NameValueCollection());
            var issuesTask = _rm.GetObjectsAsync<Issue>(new NameValueCollection());

            var trackers = await trackerTask;
            var statuses = await statusesTask;
            var issues = await issuesTask;

            return issues
                .Where(x => (filter.BoardIds == null || filter.BoardIds.Contains(x.Project.Id)) &&
                            (filter.RowIds == null || filter.RowIds.Contains(x.Tracker.Id + trackers.Count * x.Project.Id)) &&
                            (filter.ColumnIds == null || filter.ColumnIds.Contains(x.Status.Id + statuses.Count * x.Project.Id)))
                .Select(x =>
                    new Card
                    {
                        Id = x.Id,
                        BoardId = x.Project.Id,
                        RowId = x.Tracker.Id + trackers.Count * x.Project.Id,
                        ColumnId = x.Status.Id + statuses.Count * x.Project.Id,
                        Head = x.Subject,
                        Body = x.Description,
                        Created = x.CreatedOn.GetValueOrDefault(),
                    }).ToList();
        }
    }
}