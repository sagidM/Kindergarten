namespace WpfApp.Service
{
    public interface IWindowService
    {
        bool IsDialog { get; }
        bool? Show();
    }
}