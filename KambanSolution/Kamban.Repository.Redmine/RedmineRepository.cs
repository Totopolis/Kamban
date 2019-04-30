using System.Collections.Generic;
using Redmine.Net.Api;
using Redmine.Net.Api.Async;
using Redmine.Net.Api.Types;
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
            var box = new Box();
            var issuesTask = _rm.GetObjectsAsync<Issue>(new NameValueCollection());
            var trackerTask = _rm.GetObjectsAsync<Tracker>(new NameValueCollection());
            var statusesTask = _rm.GetObjectsAsync<IssueStatus>(new NameValueCollection());
            var projectsTask = _rm.GetObjectsAsync<Project>(
                new NameValueCollection
                {
                    {RedmineKeys.INCLUDE, RedmineKeys.TRACKERS},
                    {RedmineKeys.INCLUDE, RedmineKeys.ISSUE_CATEGORIES}
                });

            var issues = await issuesTask;
            var trackers = await trackerTask;
            var statuses = await statusesTask;
            var projects = await projectsTask;

            box.Cards = issues.Select(c =>
                new Card
                {
                    Id = c.Id,
                    BoardId = c.Project.Id,
                    RowId = c.Tracker.Id + trackers.Count * c.Project.Id,
                    ColumnId = c.Status.Id + statuses.Count * c.Project.Id,
                    Head = c.Subject,
                    Body = c.Description,
                    Created = c.CreatedOn.GetValueOrDefault(),
                }).ToList();

            box.Boards = projects.Select(x =>
                {
                    var projectTrackers = new HashSet<int>(x.Trackers.Select(t => t.Id));
                    box.Rows.AddRange(trackers
                        .Where(t => projectTrackers.Contains(t.Id))
                        .Select((t, i) =>
                            new Row
                            {
                                Id = t.Id + trackers.Count * x.Id,
                                BoardId = x.Id,
                                Name = t.Name,
                                Order = i,
                                Height = 20
                            })
                    );
                    box.Columns.AddRange(
                        statuses.Select((s, i) =>
                            new Column
                            {
                                Id = s.Id + statuses.Count * x.Id,
                                BoardId = x.Id,
                                Name = s.Name,
                                Order = i,
                                Width = 20
                            })
                    );
                    return new Board
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Created = x.CreatedOn.GetValueOrDefault()
                    };
                })
                .ToList();
            return box;
        }
    }
}