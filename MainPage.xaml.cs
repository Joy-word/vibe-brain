using BrainEx.ViewModels;

namespace BrainEx;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Entry Completed（按下 Enter / Done）时触发。
    /// 拼音输入法用 Enter 上屏英文时，Text 尚未变化且光标仍在输入法候选窗口中，
    /// 此时 Entry.Text 与上次一致，通过判断文字是否有实际内容来过滤误触。
    /// 真正想提交时，文字已通过输入法确认上屏，Text 有值才执行添加。
    /// </summary>
    private void OnManualEntryCompleted(object? sender, EventArgs e)
    {
        // Windows MAUI：输入法组合状态下 Completed 不会触发，
        // 只有输入法已上屏/直接英文输入时才到这里，直接提交即可。
        SubmitManualEntry();
    }

    private void OnAddButtonClicked(object? sender, EventArgs e)
    {
        SubmitManualEntry();
    }

    private void SubmitManualEntry()
    {
        var text = ManualEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (BindingContext is MainViewModel vm)
        {
            vm.AddManualCommand.Execute(text);
        }

        // 提交后清空并把焦点留在输入框，方便连续添加
        ManualEntry.Text = string.Empty;
        ManualEntry.Focus();
    }
}
