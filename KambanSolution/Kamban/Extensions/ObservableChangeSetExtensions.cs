using System;
using System.ComponentModel;
using System.Linq;
using DynamicData;
using DynamicData.Annotations;
using Kamban.Repository.Attributes;

namespace Kamban.Extensions
{
    public static class ObservableChangeSetExtensions
    {
        public static IObservable<TObject> WhenAnyAutoSavePropertyChanged<TObject>(
            [NotNull] this IObservable<IChangeSet<TObject>> source)
            where TObject : INotifyPropertyChanged
        {
            var properties = typeof(TObject).GetProperties()
                .Where(prop => prop.IsDefined(typeof(AutoSaveAttribute), false))
                .Select(x => x.Name)
                .ToArray();

            return source.WhenAnyPropertyChanged(properties);
        }
    }
}