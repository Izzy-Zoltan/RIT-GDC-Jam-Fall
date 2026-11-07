using Godot;

public partial class Powerup : Area2D
{
    public enum PowerupKind
    {
        Boost,
        ScoreMultiplier,
        MaxAir
    }

    [Export] public PowerupKind Kind { get; set; } = PowerupKind.Boost;
    [Export] public float Duration { get; set; } = 2.0f;
    [Export] public float ApproachDuration { get; set; } = 3.0f;
    [Export] public float HoverDuration { get; set; } = 3.0f;
    [Export] public float FadeOutDuration { get; set; } = 0.4f;

    private enum State { Approaching, Hovering, Collected, Despawning }
    private State currentState = State.Approaching;

    private const float ArrivalScale = 1.6f;
    private const float ArrivalScaleStart = 0.08f;
    private const float PulseAmplitude = 0.08f;
    private const float PulseFrequency = 2.5f;
    private const float CollectedFlashTime = 0.25f;
    private const float CollectLockout = 0.12f;

    private Sprite2D sprite = null;
    private Sprite2D outline = null;
    private ShaderMaterial outlineMaterial = null;
    private CollisionShape2D collision = null;
    private AirMeter airMeter = null;

    private float approachElapsed = 0f;
    private float hoverElapsed = 0f;
    private float collectedElapsed = 0f;
    private float despawnElapsed = 0f;
    private Vector2 hoverPosition;

    public override void _Ready()
    {
        Scale = Vector2.One * ArrivalScaleStart;
        CollisionLayer = 0;
        CollisionMask = 1;

        sprite = GetNode<Sprite2D>("Sprite2D");
        collision = GetNode<CollisionShape2D>("CollisionShape2D");
        airMeter = GetTree().GetFirstNodeInGroup("airmeter") as AirMeter;

        outlineMaterial = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://assets/highlight_outline.gdshader")
        };
        outlineMaterial.SetShaderParameter("outline_width", 4.0f);
        outlineMaterial.SetShaderParameter("highlight_color", new Color(0.025f, 0.015f, 0.055f, 1.0f));

        outline = new Sprite2D
        {
            Texture = sprite.Texture,
            Centered = sprite.Centered,
            Offset = sprite.Offset,
            Scale = sprite.Scale,
            ZIndex = sprite.ZIndex - 1,
            Material = outlineMaterial
        };
        AddChild(outline);

        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        float deltaSeconds = (float)delta;
        float speed = airMeter?.GetSpeedMultiplier() ?? 1.0f;

        switch (currentState)
        {
            case State.Approaching: 
                ProcessApproaching(deltaSeconds, speed); 
                break;
            case State.Hovering: 
                ProcessHovering(deltaSeconds, speed); 
                break;
            case State.Collected: 
                ProcessCollected(deltaSeconds); 
                break;
            case State.Despawning: 
                ProcessDespawning(deltaSeconds); 
                break;
        }
    }

    private void ProcessApproaching(float deltaSeconds, float speed)
    {
        approachElapsed += deltaSeconds * speed * 1.5f;

        float progress = Mathf.Clamp(approachElapsed / ApproachDuration, 0f, 1f);
        float eased = Mathf.Pow(progress, 2.2f);
        float arrivalScale = Mathf.Lerp(ArrivalScaleStart, ArrivalScale, eased);

        Scale = Vector2.One * arrivalScale;
        Modulate = Colors.White;

        if (progress >= 1.0f)
            TransitionToHovering();
    }

    private void TransitionToHovering()
    {
        currentState = State.Hovering;
        Scale = Vector2.One * ArrivalScale;
        hoverPosition = Position;
    }

    private void ProcessHovering(float deltaSeconds, float speed)
    {
        hoverElapsed += deltaSeconds * speed;

        float pulse = 1f + PulseAmplitude * Mathf.Sin(hoverElapsed * PulseFrequency * Mathf.Tau);
        float depthTilt = Mathf.Sin(hoverElapsed * Mathf.Tau * 0.45f);
        Scale = new Vector2(
            ArrivalScale * pulse * (1.0f + depthTilt * 0.035f),
            ArrivalScale * pulse * (1.0f - depthTilt * 0.035f));
        Rotation = depthTilt * 0.035f;
        Position = hoverPosition + new Vector2(0.0f, Mathf.Sin(hoverElapsed * Mathf.Tau * 0.7f) * 3.0f);

        float timeLeft = HoverDuration - hoverElapsed;
        if (timeLeft < HoverDuration * 0.2f)
        {
            float flicker = 0.55f + 0.45f * Mathf.Sin(hoverElapsed * 18f);
            Modulate = new Color(1f, 1f, 1f, flicker);
        }
        else
        {
            Modulate = Colors.White;
        }

        if (hoverElapsed >= CollectLockout && !collision.Disabled)
        {
            foreach (Node2D body in GetOverlappingBodies())
            {
                if (body is PlayerControl)
                {
                    ApplyPowerup();
                    return;
                }
            }
        }

        if (hoverElapsed >= HoverDuration)
            TransitionToDespawning();
    }

    private void ProcessCollected(float deltaSeconds)
    {
        collectedElapsed += deltaSeconds;

        float collectionProgress = Mathf.Clamp(collectedElapsed / CollectedFlashTime, 0f, 1f);
        float popScale = ArrivalScale * Mathf.Lerp(1.3f, 0f, collectionProgress);
        Scale = Vector2.One * popScale;
        float brightness = Mathf.Lerp(2.0f, 0f, collectionProgress);
        Modulate = new Color(brightness, brightness, brightness, 1f - collectionProgress);

        if (collectedElapsed >= CollectedFlashTime)
            QueueFree();
    }

    private void TransitionToDespawning()
    {
        currentState = State.Despawning;
        collision.SetDeferred("disabled", true);
    }

    private void ProcessDespawning(float deltaSeconds)
    {
        despawnElapsed += deltaSeconds;
        float despawnProgress = Mathf.Clamp(despawnElapsed / FadeOutDuration, 0f, 1f);

        Scale = Vector2.One * (ArrivalScale * Mathf.Lerp(1f, 0.5f, despawnProgress));
        Modulate = new Color(1f, 1f, 1f, 1f - despawnProgress);

        if (despawnElapsed >= FadeOutDuration)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is PlayerControl && currentState == State.Hovering)
            ApplyPowerup();
    }

    private void ApplyPowerup()
    {
        if (currentState == State.Collected || airMeter == null)
            return;

        currentState = State.Collected;
        collectedElapsed = 0f;
        collision.SetDeferred("disabled", true);
        GetNode<SoundEffects>("/root/SoundEffects").PlayPowerup();

        switch (Kind)
        {
            case PowerupKind.Boost: airMeter.ActivateForwardBoost(Duration); break;
            case PowerupKind.ScoreMultiplier: airMeter.ActivateScoreMultiplier(Duration); break;
            case PowerupKind.MaxAir: airMeter.RefillToMaximum(); break;
        }
    }
}
