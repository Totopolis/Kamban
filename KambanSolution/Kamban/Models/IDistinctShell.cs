using System.Linq;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;

namespace Kamban.Models
{
    interface IDistinctShell : IShell
    {
        void ShowDistinctView<TView>(string value,
                                     ViewRequest viewRequest = null,
                                     UiShowOptions options = null) where TView : class, IView;
    }

    public class DistinctShell : Shell, IDistinctShell
    {
        public void ShowDistinctView<TView>(string value,
                                            ViewRequest viewRequest = null,
                                            UiShowOptions options = null) where TView : class, IView
        {
            var child = DocumentPane.Children
                .FirstOrDefault(ch => ch.Content is TView view && view.ViewModel.FullTitle == value);

            if (child != null)
                child.IsActive = true;

            else
                ShowView<TView>(viewRequest, options);
        }
    }
}