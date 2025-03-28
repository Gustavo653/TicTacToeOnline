namespace TicTacToe.Data
{
    public class Player
    {
        public string Name { get; set; }
        public int ConsecutiveWins { get; set; }
        public int TotalWins { get; set; }
        public long BestWinTime { get; set; }
    }
}
