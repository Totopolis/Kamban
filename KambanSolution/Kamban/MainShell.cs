using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using System.Reactive.Linq;
using Kamban.Views;
using System.Reactive;
using Kamban.ViewModels;
using System.Windows.Controls;
using Ui.Wpf.Common.ViewModels;
using System.Windows.Input;
using System.Windows.Data;

namespace Kamban
{
    public interface IMainShell : IShell
    {
        void ShowDistinctView<TView>(string value,
                                     ViewRequest viewRequest = null,
                                     UiShowOptions options = null) where TView : class, IView;

        CommandItem AddGlobalCommand(string menuName, string cmdName, string cmd, IViewModel vm);

        CommandItem AddVMTypeCommand(string menuName, string cmdName, string cmd, IViewModel vm);
    }

    // CommandItem: id, group (top menu), name (item name), cmd, is_global, parent
    public class CommandItem
    {
        public CommandType Type { get; set; }
        public MenuItem Item { get; set; }
        public MenuItem Parent { get; set; }

        public Type VMType { get; set; }
        public IViewModel VM { get; set; }
        //public Modifiers MyProperty { get; set; }

        public List<IViewModel> VMList { get; set; } = new List<IViewModel>();
    }

    public enum CommandType
    {
        Global, // single cmd, always visible 
        VMType, // cmd from selected view-type, disable when other view-type
        Instance // single cmd, show only for selected view
    }

    public static class DictionaryExtension
    {
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            TValue val;

            if (!dict.TryGetValue(key, out val))
            {
                val = new TValue();
                dict.Add(key, val);
            }

            return val;
        }
    }

    public class MainShell : Shell, IMainShell
    {
        public ReactiveList<MenuItem> MenuItems { get; set; } = new ReactiveList<MenuItem>();

        private List<CommandItem> GlobalCommandItems = new List<CommandItem>();
        private Dictionary<IViewModel, List<CommandItem>> InstanceCommandItems = new Dictionary<IViewModel, List<CommandItem>>();
        private Dictionary<Type, List<CommandItem>> VMTypeCommandItems = new Dictionary<Type, List<CommandItem>>();

        public CommandItem AddGlobalCommand(string menuName, string cmdName, string cmd, IViewModel vm)
        {
            var m = GetMenu(menuName);

            var exsts = GlobalCommandItems
                .Where(x => x.Parent == m && (string)x.Item.Header == cmdName)
                .Count();

            if (exsts > 0)
                throw new Exception("Command already exists");

            var c = new MenuItem { Header = cmdName, DataContext = vm };
            c.SetBinding(MenuItem.CommandProperty, new Binding(cmd));
            m.Items.Add(c);

            CommandItem ci = new CommandItem
            {
                Type = CommandType.Global,
                Item = c,
                Parent = m,
                VM = null,
                VMType = null
            };

            GlobalCommandItems.Add(ci);

            return ci;
        }

        public CommandItem AddVMTypeCommand(string menuName, string cmdName, string cmd, IViewModel vm)
        {
            var m = GetMenu(menuName);

            var cmdList = VMTypeCommandItems.GetOrCreate(vm.GetType());

            var ci = cmdList
                .Where(x => x.Parent == m && (string)x.Item.Header == cmdName)
                .FirstOrDefault();

            if (ci == null)
            {
                var c = new MenuItem { Header = cmdName, DataContext = vm };
                c.SetBinding(MenuItem.CommandProperty, new Binding(cmd));
                m.Items.Add(c);

                ci = new CommandItem
                {
                    Type = CommandType.VMType,
                    Item = c,
                    Parent = m,
                    VM = vm,
                    VMType = vm.GetType()
                };

                cmdList.Add(ci);
            }

            var exsts = ci.VMList
                .Where(x => x == vm)
                .Count();

            if (exsts > 0)
                throw new Exception("Command already exists at VM instance");

            ci.VMList.Add(vm);

            return ci;
        }

        private MenuItem GetMenu(string menuName)
        {
            var m = MenuItems
                .Where(x => (string)x.Header == menuName)
                .FirstOrDefault();

            if (m == null)
            {
                m = new MenuItem { Header = menuName };
                MenuItems.Add(m);
            }

            return m;
        }

        private IView ActualCommandView;

        private void DeactivateCommands()
        {
            if (ActualCommandView == null)
                return;

            var vm = ActualCommandView.ViewModel;
            var vmTyp = vm.GetType();
            var cmdList = VMTypeCommandItems.GetOrCreate(vmTyp);
            foreach (var ci in cmdList)
            {
                ci.Item.DataContext = null;
                ci.Item.IsEnabled = false;
            }
        }

        private void ActivateCommands()
        {
            var vm = ActualCommandView.ViewModel;
            var vmTyp = vm.GetType();
            var cmdList = VMTypeCommandItems.GetOrCreate(vmTyp);
            foreach (var ci in cmdList)
            {
                ci.Item.DataContext = vm;
                ci.Item.IsEnabled = true;
            }
        }

        public MainShell()
        {
            ActualCommandView = null;

            this.ObservableForProperty(w => w.SelectedView)
                .Where(x => x.Value != ActualCommandView)
                .Subscribe(v =>
                {
                    DeactivateCommands();
                    ActualCommandView = v.Value;
                    ActivateCommands();
                });

            //var canCreate = this.ObservableForProperty(t => t.SelectedView,
            //    (sv) => sv is BoardView);

            //CreateTiketCommand = ReactiveCommand.CreateCombined<Unit, Unit>( 
            //   new[] { GetCreateCommand() },
            //   canCreate);

            //this.ObservableForProperty(w => w.SelectedView)
            //.Where(v => v is BoardView)
            //.Subscribe(_ => );

            /* var ButtonWebRequestSingle = ReactiveCommand.CreateFromObservable(() => GenerateWebRequest());
            ButtonWebRequest = ReactiveCommand.CreateCombined(new[] { ButtonWebRequestSingle });

            ButtonWebRequest.IsExecuting
            .Subscribe(isExecuting =>
            {
                Console.WriteLine("ButtonWebRequestCombined IsExecuting: {0}", isExecuting);
            });*/
        }

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
    }//end of class
}
