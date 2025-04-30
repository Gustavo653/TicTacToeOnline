namespace TicTacToe.Data
{
    public class GameEntity
    {
        public string Id { get; set; }
        public string PlayerX { get; set; }
        public string PlayerO { get; set; }
        public char[] Board { get; set; } = new char[9];
        public bool IsFinished { get; set; }
        public string Winner { get; set; }
        public System.DateTime StartTime { get; set; }
        public System.DateTime EndTime { get; set; }
        public char CurrentTurn { get; set; } = 'X';
        public bool VsCPU { get; set; }
        public Difficulty AIDifficulty { get; set; } = Difficulty.Easy;

        public GameEntity()
        {
            for (int i = 0; i < 9; i++) Board[i] = ' ';
        }
    }
    public enum Difficulty { Easy, Medium, Hard }
}
