using Godot;

public partial class SoundEffects : Node
{
    private AudioStream ring = null;
    private AudioStream warning = null;
    private AudioStream signHit = null;
    private AudioStream powerup = null;
    private AudioStreamPlayer musicPlayer = null;

    public override void _Ready()
    {
        ring = GD.Load<AudioStream>("res://assets/Sound Effects/wind_ring.mp3");
        warning = GD.Load<AudioStream>("res://assets/Sound Effects/warning_sound.mp3");
        signHit = GD.Load<AudioStream>("res://assets/Sound Effects/Sign_hit.mp3");
        powerup = GD.Load<AudioStream>("res://assets/Sound Effects/Power_up.mp3");

        AudioStreamMP3 music = GD.Load<AudioStreamMP3>("res://assets/kissan4-pixel-paradise-358340.mp3");
        music.Loop = true;
        musicPlayer = new AudioStreamPlayer
        {
            Name = "MusicPlayer",
            Stream = music,
            Bus = "Master",
            VolumeDb = -8.0f
        };
        AddChild(musicPlayer);
        musicPlayer.Play();
    }

    public void PlayRing() => Play(ring);
    public void PlayWarning() => Play(warning);
    public void PlaySignHit() => Play(signHit);
    public void PlayPowerup() => Play(powerup);

    private void Play(AudioStream stream)
    {
        if (stream == null)
            return;

        AudioStreamPlayer player = new() { Stream = stream, Bus = "Master", VolumeDb = -10.0f };
        GetTree().Root.AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }
}
