namespace MusicLabel;

public sealed record ScoreConfig(int TrackWeight, int PlatformWeight, int ExplicitBonus);

public static class Scoring
{
    public static readonly ScoreConfig ReachConfig = new(TrackWeight: 1, PlatformWeight: 5, ExplicitBonus: 0);
    public static readonly ScoreConfig AlbumConfig = new(TrackWeight: 10, PlatformWeight: 1, ExplicitBonus: 5);
    
    public static Func<ValidRelease, int> CreateScore(ScoreConfig config) => 
        release => release.TracksCount * config.TrackWeight
                   + release.TargetPlatformsCount * config.PlatformWeight
                   + (release.HasExplicitContent ? config.ExplicitBonus : 0);
}