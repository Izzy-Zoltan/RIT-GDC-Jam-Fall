using Godot;

public partial class MainMenu : Node2D
{
    public override void _Ready()
    {
        Button startButton = GetNode<Button>("CanvasLayer/CenterContainer/VBoxContainer/StartButton");
        startButton.Pressed += StartGame;
        startButton.GrabFocus();

        GetNode<Button>("CanvasLayer/CenterContainer/VBoxContainer/HelpButton").Pressed += ShowHelp;
        GetNode<Button>("CanvasLayer/CenterContainer/VBoxContainer/CreditsButton").Pressed += ShowCredits;
        GetNode<Button>("CanvasLayer/CenterContainer/VBoxContainer/ExitButton").Pressed += ShowExitConfirmation;
        GetNode<Button>("CanvasLayer/HelpOverlay/CenterContainer/Panel/Content/CloseButton").Pressed += HideHelp;
        GetNode<Button>("CanvasLayer/CreditsOverlay/CenterContainer/Panel/Content/CloseButton").Pressed += HideCredits;
        GetNode<Button>("CanvasLayer/ExitOverlay/CenterContainer/Panel/Content/ExitButtons/CancelButton").Pressed += HideExitConfirmation;
        GetNode<Button>("CanvasLayer/ExitOverlay/CenterContainer/Panel/Content/ExitButtons/ConfirmButton").Pressed += ExitGame;
    }

    private void StartGame()
    {
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    private void ShowHelp() => GetNode<Control>("CanvasLayer/HelpOverlay").Show();
    private void HideHelp() => GetNode<Control>("CanvasLayer/HelpOverlay").Hide();
    private void ShowCredits() => GetNode<Control>("CanvasLayer/CreditsOverlay").Show();
    private void HideCredits() => GetNode<Control>("CanvasLayer/CreditsOverlay").Hide();
    private void ShowExitConfirmation() => GetNode<Control>("CanvasLayer/ExitOverlay").Show();
    private void HideExitConfirmation() => GetNode<Control>("CanvasLayer/ExitOverlay").Hide();
    private void ExitGame() => GetTree().Quit();
}
