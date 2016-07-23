namespace WpfApp.Service
{
    public interface IWindowService
    {
        bool IsDialog { get; }
        bool? Show();
    }
    public interface IWindowService<in T>
    {
        bool IsDialog { get; }
        bool? Show(T parameter);
    }
}