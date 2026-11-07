using Godot;

public partial class AirRing : Area2D
{
    private enum RingState
    {
        Approaching,
        Collected,
        Missed
    }

    [Export] public float ApproachDuration { get; set; } = 4.5f;
    [Export] public float AirRefill { get; set; } = 25.0f;

    private CollisionShape2D collisionShape = null;
    private Sprite2D sprite = null;
    private Sprite2D highlight = null;
    private ShaderMaterial highlightMaterial = null;
    private AirMeter airMeter = null;
    private PlayerControl player = null;
    private RingState currentState = RingState.Approaching;
    private float progress;
    private float approachElapsed;

    public override void _Ready()
    {
        Scale = Vector2.One * 0.08f;
        CollisionLayer = 0;
        CollisionMask = 1;
        collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.Scale = Vector2.One * 0.11f;
        highlightMaterial = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://assets/highlight_outline.gdshader")
        };
        highlight = new Sprite2D
        {
            Texture = sprite.Texture,
            Centered = sprite.Centered,
            Offset = sprite.Offset,
            Scale = sprite.Scale,
            ZIndex = sprite.ZIndex - 1,
            Visible = false,
            Material = highlightMaterial
        };
        highlightMaterial.SetShaderParameter("outline_width", 5.0f);
        AddChild(highlight);
        airMeter = (AirMeter)GetTree().GetFirstNodeInGroup("airmeter");
        player = (PlayerControl)GetTree().GetFirstNodeInGroup("player");
    }

    public override void _Process(double delta)
    {
        float speed = airMeter?.GetSpeedMultiplier() ?? 1.0f;

        approachElapsed += (float)delta * speed;
        progress = Mathf.Clamp(approachElapsed / ApproachDuration, 0.0f, 1.0f);

        float eased = Mathf.Pow(progress, 2.2f);
        float exitFade = Mathf.Clamp((progress - 0.53f) / 0.47f, 0.0f, 1.0f);

        Scale = Vector2.One * Mathf.Lerp(0.08f, 12.0f, eased);

        bool abovePlayer = Scale.X > 3.0f;

        if (abovePlayer && currentState == RingState.Approaching)
        {
            if (player != null && IsPlayerFullyInside(player))
                CollectAir();
            else
            {
                currentState = RingState.Missed;
                airMeter?.MissRing();
            }
        }

        if (abovePlayer) ZIndex = 2;
        else ZIndex = 0;
        
        Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f - exitFade);
        collisionShape.Disabled = currentState == RingState.Collected || currentState == RingState.Missed;
        highlight.Visible = !abovePlayer || currentState == RingState.Collected || currentState == RingState.Missed;

        Color color = currentState switch
        {
            RingState.Collected => new Color(0.45f, 1.0f, 0.38f, 1.0f),
            RingState.Missed => new Color(1.0f, 0.2f, 0.18f, 1.0f),
            _ => new Color(0.025f, 0.015f, 0.055f, 1.0f)
        };

        highlightMaterial.SetShaderParameter("highlight_color", color);

        sprite.Modulate = Colors.White;

        if (approachElapsed > ApproachDuration + 0.3f)
            QueueFree();
    }

    private void CollectAir()
    {
        if (currentState == RingState.Collected)
            return;

        currentState = RingState.Collected;
        GetNode<SoundEffects>("/root/SoundEffects").PlayRing();
        airMeter?.Refill(AirRefill);
        airMeter?.AddCombo();
    }

    private bool IsPlayerFullyInside(PlayerControl player)
    {
        CollisionShape2D playerShape = player.GetNode<CollisionShape2D>("CollisionShape2D");
        if (playerShape.Shape is not RectangleShape2D rectangle) return false;

        Vector2 halfSize = rectangle.Size * 0.5f;
        Vector2 offset = playerShape.GlobalPosition - GlobalPosition;

        float farthestX = Mathf.Abs(offset.X) + halfSize.X;
        float farthestY = Mathf.Abs(offset.Y) + halfSize.Y;
        float farthestCornerDistance = Mathf.Sqrt(farthestX * farthestX + farthestY * farthestY);
        float ringRadius = 52.0f * Scale.X;

        return farthestCornerDistance <= ringRadius;
    }
}
