using System;
using System.Linq;
using System.Windows;

namespace WpfApp.Framework.Core
{
    public abstract class PipeViewModel
    {
        private const string ViewViewModel = "ViewViewModelPairs";

        public Pipe Pipe { get; internal set; }

        public abstract void OnPreInit();

        public abstract void OnInit();

        public abstract void OnLoaded();


        public void StartViewModel<T>(Pipe pipe) where T : ViewModelBase
        {
            StartViewModel(typeof(T), pipe);
        }

        public void StartViewModel(Type viewModelType)
        {
            StartViewModel(viewModelType, Pipe.Default);
        }

        private static void StartViewModel(Type type, Pipe pipe)
        {
            var pairs = (ViewViewModelPairs)Application.Current.Resources[ViewViewModel];
            var pair = pairs.Pairs.FirstOrDefault(p => p.ViewModel == type);

            if (pair == null)
                throw new ArgumentException($"ViewModel \"{type.FullName}\" doesn't register in App.xaml", nameof(type));

            pair.Start(pipe);
        }


        public void Finish()
        {
            FinishRequest?.Invoke();
        }

        public abstract void OnFinished();

        public abstract bool OnFinishing();

        internal event Action FinishRequest;
    }
}