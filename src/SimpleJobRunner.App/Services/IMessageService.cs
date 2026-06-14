namespace SimpleJobRunner.App.Services;

public interface IMessageService
{
    void ShowInfo(string title, string message);
    void ShowError(string title, string message);
    bool Confirm(string title, string message);
}
