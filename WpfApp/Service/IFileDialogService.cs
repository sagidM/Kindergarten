namespace WpfApp.Service
{
    public interface IFileDialogService : IWindowService
    {
        string Filter { get; set; }
        string FileName { get; set; }
    }
}