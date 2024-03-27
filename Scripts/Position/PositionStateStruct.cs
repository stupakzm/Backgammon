public struct PositionStateStruct
{
    public int positionIndex;
    public Player playerState;
    public int chipsCount;

    public PositionStateStruct(int positionIndex, Player playerState, int chipsCount)
    {
        this.positionIndex = positionIndex;
        this.playerState = playerState;
        this.chipsCount = chipsCount;
    }
}
