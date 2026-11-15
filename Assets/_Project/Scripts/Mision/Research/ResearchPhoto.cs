// One photo taken in Moon Photo Safari (task 14, claude_c). Science photos carry data; the alien photo carries none.
public class ResearchPhoto
{
    public ResearchMath.TargetType type;
    public bool alien;     // bonus photo, never counted as science
    public bool gold;      // landmark centered in the inner ring
    public float bits;     // data to send home (0 for the alien)
    public bool sent;      // reached Earth in Signal Catcher
}
