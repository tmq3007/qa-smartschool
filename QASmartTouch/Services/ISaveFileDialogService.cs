namespace QASmartClass.Services
{
    public interface ISaveFileDialogService
    {
        string? ShowSaveFileDialog(string defaultBaseName, string filter, string defaultExt);
        void ShowMessage(string message, string title, bool isError = false);
        void StartProcess(string filePath);
    }
}
