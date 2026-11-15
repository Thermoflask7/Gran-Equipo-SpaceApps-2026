// What the research minigame produced (task 14, claude_c). Written to GameManager.flight by ResearchFlightHook.
public class ResearchResult
{
    public float experimentFraction; // captured data / planned data
    public float downlinkFraction;   // data home / planned data (<= experimentFraction)
    public float raw;                // player performance 0..1, WITHOUT the scientist skill
    public float plannedBits;
    public float capturedBits;
    public float sentBits;
    public int sciencePhotos;
    public int targetsShown;
    public float goldShare;          // gold-star landmarks / landmarks photographed
    public float alienShare;         // alien photos / alien appearances
}
