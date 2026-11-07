using Godot;

public partial class UI : Node2D
{
    private ProgressBar airBar = null!;
    private Label scoreLabel = null!;
    private Label comboLabel = null!;
    private Control hudPanel = null!;
    private PanelContainer comboPanel = null!;
    private Control gameOverPanel = null!;
    private Label finalScoreLabel = null!;
    private Label maxComboLabel = null!;
    private Control powerupPanel = null!;
    private Control boostRow = null!;
    private Label boostLabel = null!;
    private ProgressBar boostBar = null!;
    private Control scoreMultiplierRow = null!;
    private Label scoreMultiplierLabel = null!;
    private ProgressBar scoreMultiplierBar = null!;
    private AirMeter airMeter = null!;
    private Color airBarColor = new(0.96f, 0.52f, 0.6f);
    private float refillFlash, comboHeat;
    private int comboValue, previousScore;

    private static readonly Color ComboColorLow = new(1f, 0.82f, 0.42f);
    private static readonly Color ComboColorMid = new(1f, 0.55f, 0.15f);
    private static readonly Color ComboColorHigh = new(1f, 0.25f, 0.45f);

    public override void _Ready()
    {
        airBar = GetNode<ProgressBar>("CanvasLayer/HudPanel/VBoxContainer/AirRow/ProgressBar");
        scoreLabel = GetNode<Label>("CanvasLayer/HudPanel/VBoxContainer/ScoreLabel");
        comboLabel = GetNode<Label>("CanvasLayer/ComboPanel/ComboLabel");
        hudPanel = GetNode<Control>("CanvasLayer/HudPanel");
        comboPanel = GetNode<PanelContainer>("CanvasLayer/ComboPanel");
        gameOverPanel = GetNode<Control>("CanvasLayer/GameOverPanel");
        finalScoreLabel = GetNode<Label>("CanvasLayer/GameOverPanel/PanelContents/FinalScoreLabel");
        maxComboLabel = GetNode<Label>("CanvasLayer/GameOverPanel/PanelContents/MaxComboLabel");
        powerupPanel = GetNode<Control>("CanvasLayer/PowerupPanel");
        boostRow = GetNode<Control>("CanvasLayer/PowerupPanel/PowerupRows/BoostRow");
        boostLabel = GetNode<Label>("CanvasLayer/PowerupPanel/PowerupRows/BoostRow/BoostLabel");
        boostBar = GetNode<ProgressBar>("CanvasLayer/PowerupPanel/PowerupRows/BoostRow/BoostBar");
        scoreMultiplierRow = GetNode<Control>("CanvasLayer/PowerupPanel/PowerupRows/ScoreMultRow");
        scoreMultiplierLabel = GetNode<Label>("CanvasLayer/PowerupPanel/PowerupRows/ScoreMultRow/ScoreMultLabel");
        scoreMultiplierBar = GetNode<ProgressBar>("CanvasLayer/PowerupPanel/PowerupRows/ScoreMultRow/ScoreMultBar");
        GetNode<Button>("CanvasLayer/GameOverPanel/PanelContents/ButtonRow/RestartButton").Pressed += RestartGame;
        GetNode<Button>("CanvasLayer/GameOverPanel/PanelContents/ButtonRow/MenuButton").Pressed += ReturnToMenu;

        airMeter = GetNode<AirMeter>("AirMeter");
        airMeter.AirChanged += OnAirChanged;
        airMeter.ScoreChanged += OnScoreChanged;
        airMeter.ComboChanged += OnComboChanged;
        airMeter.PowerupEffectsChanged += OnPowerupEffectsChanged;
        airMeter.GameOver += OnGameOver;

        OnAirChanged(airMeter.CurrentAir, airMeter.MaximumAir, false);
        OnScoreChanged(airMeter.CurrentScore);
        OnComboChanged(airMeter.CurrentCombo, false);
        OnPowerupEffectsChanged(airMeter.Effects.BoostRemaining, airMeter.Effects.BoostDuration,
            airMeter.Effects.ScoreMultiplierRemaining, airMeter.Effects.ScoreMultiplierDuration);
    }

    public override void _Process(double delta)
    {
        float heatTarget = Mathf.Clamp(comboValue / 15f, 0f, 1f);
        comboHeat = Mathf.Lerp(comboHeat, heatTarget, 0.18f);
        Color comboColor = comboHeat < 0.5f
            ? ComboColorLow.Lerp(ComboColorMid, comboHeat * 2f)
            : ComboColorMid.Lerp(ComboColorHigh, (comboHeat - 0.5f) * 2f);
        comboLabel.AddThemeColorOverride("font_color", comboColor);
        int fontSize = (int)Mathf.Lerp(22f, 32f, Mathf.Clamp(comboValue / 20f, 0f, 1f));
        comboLabel.AddThemeFontSizeOverride("font_size", fontSize);
        refillFlash = Mathf.Max(0f, refillFlash - (float)delta * 3.5f);
    }

    private void OnAirChanged(float currentAir, float maximumAir, bool flash)
    {
        if (flash) refillFlash = 1f;
        airBar.MaxValue = maximumAir;
        airBar.Value = currentAir;
        Color normalColor = new(0.3f, 0.85f, 1f);
        Color criticalColor = new(1f, 0.3f, 0.25f);
        Color targetColor = currentAir <= maximumAir * 0.25f ? criticalColor : normalColor;
        airBarColor = airBarColor.Lerp(targetColor.Lerp(new Color(0.6f, 1f, 1f), refillFlash), 0.15f);
        airBar.Modulate = airBarColor;
        float criticalFraction = 1f - Mathf.Clamp(currentAir / (maximumAir * 0.25f), 0f, 1f);
        if (criticalFraction > 0f)
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin((float)Time.GetTicksMsec() * 0.008f);
            hudPanel.Modulate = new Color(1f, 1f - criticalFraction * 0.55f * pulse,
                1f - criticalFraction * 0.65f * pulse, 1f);
        }
        else hudPanel.Modulate = Colors.White;
    }

    private void OnScoreChanged(int score)
    {
        scoreLabel.Text = $"SCORE: {score:D6}";
        if (score / 100 > previousScore / 100)
        {
            scoreLabel.Modulate = new Color(1f, 1f, 0.4f);
            Tween scoreTween = scoreLabel.CreateTween();
            scoreTween.TweenProperty(scoreLabel, "modulate", Colors.White, 0.2f);
        }
        previousScore = score;
    }

    private void OnComboChanged(int combo, bool playPop)
    {
        comboValue = combo;
        comboLabel.Text = $"COMBO x{combo}";
        if (!playPop) return;
        float popScale = Mathf.Lerp(1.18f, 1.4f, Mathf.Clamp(combo / 20f, 0f, 1f));
        comboPanel.PivotOffset = comboPanel.Size / 2f;
        Tween comboTween = comboPanel.CreateTween();
        comboTween.SetTrans(Tween.TransitionType.Back);
        comboTween.SetEase(Tween.EaseType.Out);
        comboTween.TweenProperty(comboPanel, "scale", Vector2.One * popScale, 0.07f);
        comboTween.TweenProperty(comboPanel, "scale", Vector2.One, 0.16f);
    }

    private void OnPowerupEffectsChanged(float boostRemaining, float boostDuration,
        float scoreRemaining, float scoreDuration)
    {
        bool boostActive = boostRemaining > 0f;
        bool scoreActive = scoreRemaining > 0f;
        boostRow.Visible = boostActive;
        scoreMultiplierRow.Visible = scoreActive;
        powerupPanel.Visible = boostActive || scoreActive;
        if (boostActive)
        {
            boostLabel.Text = $"BOOST  {boostRemaining:0.0}s";
            float fraction = boostDuration > 0f ? boostRemaining / boostDuration : 0f;
            boostBar.Value = fraction * 100f;
            float urgency = 1f - Mathf.Clamp(fraction, 0f, 1f);
            boostBar.Modulate = Colors.White.Lerp(new Color(1f, 0.6f, 0.1f), urgency * 0.6f);
        }
        if (scoreActive)
        {
            scoreMultiplierLabel.Text = $"2x SCORE  {scoreRemaining:0.0}s";
            scoreMultiplierBar.Value = scoreDuration > 0f ? scoreRemaining / scoreDuration * 100f : 0f;
        }
    }

    private void OnGameOver(int score, int maximumCombo)
    {
        finalScoreLabel.Text = $"SCORE: {score:D6}";
        maxComboLabel.Text = $"MAX COMBO: x{maximumCombo}";
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 panelSize = new(460f, 300f);
        Vector2 centeredPosition = (viewportSize - panelSize) / 2f;
        gameOverPanel.Size = panelSize;
        gameOverPanel.Position = new Vector2(centeredPosition.X, viewportSize.Y + 20f);
        gameOverPanel.Visible = true;
        Tween panelTween = CreateTween();
        panelTween.SetTrans(Tween.TransitionType.Back);
        panelTween.SetEase(Tween.EaseType.Out);
        panelTween.TweenProperty(gameOverPanel, "position", centeredPosition, 0.7f);
    }

    private void RestartGame() => GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    private void ReturnToMenu() => GetTree().ChangeSceneToFile("res://scenes/Menu.tscn");
}
