using Godot;

public partial class PlayerControl : CharacterBody2D
{
    [Export] public float FlySpeed = 600.0f;
    [Export] public float Acceleration = 1600.0f;
    [Export] public float Deceleration = 600.0f;
    [Export] public float FallAcceleration = 900.0f;

    private AnimatedSprite2D sprite = null;
    private CollisionShape2D collisionShape = null;
    private bool airDepleted;
    private float tilt;
    private float bumpTimeRemaining;
    private Vector2 bumpOffset;
    private float afterimageTimer;
    private bool reportedFall;

    public override void _Ready()
    {
        AddToGroup("player");
        sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        sprite.Play("default");
        collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        UpdateSprite();
    }

    public void SetAirDepleted(bool depleted)
    {
        airDepleted = depleted;
    }

    public override void _PhysicsProcess(double delta)
    {
        float frameDelta = (float)delta;

        if (airDepleted)
        {
            Velocity = Velocity.MoveToward(new Vector2(0.0f, FallAcceleration), FallAcceleration * frameDelta);
            UpdateBump(frameDelta);
            UpdateTilt(0.0f, frameDelta);
            MoveAndSlide();
            UpdateAfterimages(frameDelta);
            if (!reportedFall && GlobalPosition.Y > GetViewport().GetVisibleRect().End.Y + 80.0f)
            {
                reportedFall = true;
                AirMeter.Instance?.PlayerFellOutOfScreen();
            }
            return;
        }

        float speedMultiplier = AirMeter.Instance?.GetSpeedMultiplier() ?? 1.0f;
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        float rate = direction == Vector2.Zero ? Deceleration : Acceleration;
        Velocity = Velocity.MoveToward(direction * FlySpeed * speedMultiplier, rate * speedMultiplier * frameDelta);

        UpdateBump(frameDelta);
        UpdateTilt(Velocity.X / FlySpeed * 0.35f, frameDelta);
        MoveAndSlide();
        ClampToScreen();
        UpdateAfterimages(frameDelta);
    }

    private void UpdateAfterimages(float delta)
    {
        afterimageTimer -= delta;
        if (afterimageTimer > 0.0f)
            return;

        afterimageTimer = 0.055f;
        bool boosted = AirMeter.Instance?.IsForwardBoostActive() ?? false;
        Texture2D frame = sprite.SpriteFrames.GetFrameTexture(sprite.Animation, sprite.Frame);
        Sprite2D afterimage = new()
        {
            Texture = frame,
            Centered = sprite.Centered,
            Offset = sprite.Offset,
            FlipH = sprite.FlipH,
            FlipV = sprite.FlipV,
            Rotation = sprite.GlobalRotation,
            Scale = sprite.GlobalScale * (boosted ? 1.08f : 1.0f), //ternary gaming
            Modulate = boosted // 2x ternary gaming
                ? new Color(1.0f, 0.28f, 0.24f, 0.48f)
                : new Color(0.7f, 0.9f, 1.0f, 0.3f),
            ZIndex = -1
        };
        GetParent().AddChild(afterimage);
        afterimage.GlobalPosition = sprite.GlobalPosition + new Vector2(0.0f, boosted ? 25.0f : 14.0f);

        Tween fade = CreateTween();
        fade.SetParallel();
        float lifetime = boosted ? 0.3f : 0.24f;
        fade.TweenProperty(afterimage, "modulate:a", 0.0f, lifetime);
        fade.TweenProperty(afterimage, "scale", afterimage.Scale * 0.92f, lifetime);
        fade.Chain().TweenCallback(Callable.From(afterimage.QueueFree));
    }

    private void ClampToScreen()
    {
        Transform2D canvasTransform = GetViewport().GetCanvasTransform();
        Vector2 screenPosition = canvasTransform * GlobalPosition;
        Vector2 halfSize = Vector2.Zero;

        if (collisionShape.Shape is RectangleShape2D rectangle)
            halfSize = rectangle.Size * 0.5f * collisionShape.GlobalScale;

        Vector2 basisX = canvasTransform.X;
        Vector2 basisY = canvasTransform.Y;

        Vector2 screenHalfSize = new Vector2(
            Mathf.Abs(basisX.X) * halfSize.X + Mathf.Abs(basisY.X) * halfSize.Y,
            Mathf.Abs(basisX.Y) * halfSize.X + Mathf.Abs(basisY.Y) * halfSize.Y);

        Rect2 screen = GetViewport().GetVisibleRect();

        screenPosition.X = Mathf.Clamp(screenPosition.X,
            screen.Position.X + screenHalfSize.X, screen.End.X - screenHalfSize.X);
        screenPosition.Y = Mathf.Clamp(screenPosition.Y,
            screen.Position.Y + screenHalfSize.Y, screen.End.Y - screenHalfSize.Y);
        GlobalPosition = canvasTransform.AffineInverse() * screenPosition;
    }

    private void UpdateTilt(float target, float delta)
    {
        target = Mathf.Clamp(target, -0.35f, 0.35f);
        tilt = Mathf.Lerp(tilt, target, 1.0f - Mathf.Exp(-8.0f * delta));
        UpdateSprite();
    }

    public void HitObstacle(Vector2 obstaclePosition)
    {
        Vector2 knockback = (GlobalPosition - obstaclePosition).Normalized();

        if (knockback == Vector2.Zero)
            knockback = Vector2.Up;

        Velocity = knockback * 520.0f;
        bumpOffset = knockback * 30.0f;
        bumpTimeRemaining = 0.35f;
        UpdateSprite();
    }

    private void UpdateBump(float delta)
    {
        bumpTimeRemaining = Mathf.Max(0.0f, bumpTimeRemaining - delta);
        bumpOffset = bumpOffset.MoveToward(Vector2.Zero, 120.0f * delta);
    }

    private void UpdateSprite()
    {
        if (sprite == null)
            return;

        float bump = Mathf.Clamp(bumpTimeRemaining / 0.35f, 0.0f, 1.0f);
        sprite.Position = bumpOffset;
        sprite.Rotation = tilt;
        sprite.Scale = new Vector2(0.85f + 0.4f * bump, 0.85f - 0.32f * bump);
    }
}
