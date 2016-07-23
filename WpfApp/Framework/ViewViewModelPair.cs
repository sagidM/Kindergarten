using System;
using System.Reflection;
using System.Windows;
using WpfApp.Framework.Core;

namespace WpfApp.Framework
{
    public class ViewViewModelPair
    {
        public ViewViewModelPair()
        {
            Id = ++_id;
        }

        public readonly long Id;
        public Uri View { get; set; }
        public Type ViewModel { get; set; }

        public void Start(Pipe pipe)
        {
            var window = (Window)Application.LoadComponent(View);

            var ctor = ViewModel.GetConstructor(ConstructorFlags, null, Type.EmptyTypes, null);
            if (ctor == null)
                throw new InvalidOperationException($"{nameof(ViewModel)} must has default public constructor");

            var viewModel = ctor.Invoke(Empty) as ViewModelBase;
            if (viewModel == null)
                throw new InvalidOperationException($"{nameof(ViewModel)} must be derived of {nameof(ViewModelBase)} type");
            
            StartLifeCycle(window, viewModel, pipe);
        }

        private static void StartLifeCycle(Window window, PipeViewModel viewModel, Pipe pipe)
        {
            viewModel.OnPreInit();

            window.DataContext = viewModel;
            window.Loaded += (s, e) => viewModel.OnLoaded();
            window.Closing += (s, e) => { e.Cancel = !viewModel.OnFinishing(); };
            bool closed = false;
            window.Closed += (s, e) =>
            {
                closed = true;
                viewModel.OnFinished();
            };
            viewModel.FinishRequest += window.Close;
            viewModel.Pipe = pipe;

            viewModel.OnInit();

            if (closed) return;

            if (pipe.IsDialog)
                window.ShowDialog();
            else
                window.Show();
        }

        public override string ToString()
        {
            return $"Id: {Id}, View: {View}, ViewModel: {ViewModel}";
        }

        private static long _id;
        private static readonly object[] Empty = new object[0];
        private const BindingFlags ConstructorFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    }
}