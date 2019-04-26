using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using DynamicData;
using DynamicData.Kernel;
using Kamban.Repository.LiteDb;
using Kamban.ViewModels.Core;
using Monik.Common;
using Ui.Wpf.Common;

namespace Kamban.Core
{
    public interface IAppModel
    {
        Task<BoxViewModel> Create(string uri);
        Task<BoxViewModel> Load(string uri);
        ReadOnlyObservableCollection<BoxViewModel> Boxes { get; }
        void Remove(string uri);
    }

    public class AppModel : IAppModel
    {
        private readonly IShell shell;

        private readonly SourceCache<BoxViewModel, string> BoxesCache =
            new SourceCache<BoxViewModel, string>(x => x.Uri);

        public AppModel(IShell shell, IMapper mp, IMonik m)
        {
            this.shell = shell;
            m.LogicVerbose("AppModel.ctor");

            BoxesCache.Connect()
                .Bind(out _boxes)
                .Subscribe();
        }

        private readonly ReadOnlyObservableCollection<BoxViewModel> _boxes;

        public ReadOnlyObservableCollection<BoxViewModel> Boxes => _boxes;
        public void Remove(string uri)
        {
            BoxesCache.Remove(uri);
        }

        public Task<BoxViewModel> Create(string uri)
        {
            var box = GetBox(uri);
            if (box.Loaded)
                throw new Exception("Already exists");

            box.Loaded = true;

            box.Connect(new LiteDbRepository(uri));

            return Task.FromResult(box);
        }

        public async Task<BoxViewModel> Load(string uri)
        {
            var box = GetBox(uri);
            if (box.Loaded)
                return box;

            var fi = new FileInfo(uri);

            if (!fi.Exists)
                return box;

            box.Loaded = true;

            box.Title = fi.Name;
            box.LastEdit = File.GetLastWriteTime(box.Uri);
            box.Path = fi.DirectoryName;
            box.SizeOf = SizeSuffix(fi.Length);

            var repo = new LiteDbRepository(uri);
            await box.Load(repo);
            box.Connect(repo);

            return box;
        }

        private BoxViewModel GetBox(string uri)
        {
            var o = BoxesCache.Lookup(uri);
            return o.ValueOr(() =>
            {
                var box = shell.Container.Resolve<BoxViewModel>();
                box.Uri = uri;
                BoxesCache.AddOrUpdate(box);
                return box;
            });
        }

        private static readonly string[] SizeSuffixes =
            {"bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};

        private static string SizeSuffix(long value)
        {
            if (value < 0)
            {
                return "-" + SizeSuffix(-value);
            }

            var i = 0;
            var dValue = (decimal) value;
            while (Math.Round(dValue / 1024) >= 1)
            {
                dValue /= 1024;
                i++;
            }

            return $"{dValue:n1} {SizeSuffixes[i]}";
        }
    } //end of class
}