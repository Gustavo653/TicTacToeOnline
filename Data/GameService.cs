using System.Collections.Concurrent;
namespace TicTacToe.Data
{
    public class GameService
    {
        static ConcurrentDictionary<string, Player> Players = new ConcurrentDictionary<string, Player>();
        static ConcurrentDictionary<string, GameEntity> Games = new ConcurrentDictionary<string, GameEntity>();
        public event Action StateChanged;
        void NotifyStateChanged() => StateChanged?.Invoke();
        public bool RegisterPlayer(string name)
        {
            var added = Players.TryAdd(name, new Player { Name = name, ConsecutiveWins = 0, TotalWins = 0, BestWinTime = long.MaxValue });
            if (added) NotifyStateChanged();
            return added;
        }
        public Player GetPlayer(string name)
        {
            Players.TryGetValue(name, out var p);
            return p;
        }
        public IEnumerable<Player> GetAllPlayers()
        {
            return Players.Values;
        }

        public GameEntity CreateGame(string creator, bool vsCpu = false, Difficulty difficulty = Difficulty.Easy)
        {
            var g = new GameEntity
            {
                Id = Guid.NewGuid().ToString(),
                PlayerX = creator,
                StartTime = DateTime.Now,
                VsCPU = vsCpu,
                AIDifficulty = difficulty
            };
            if (vsCpu) g.PlayerO = "CPU";
            Games.TryAdd(g.Id, g);
            NotifyStateChanged();
            return g;
        }

        public GameEntity JoinGame(string gameId, string playerName)
        {
            if (Games.TryGetValue(gameId, out var g))
            {
                if (!g.VsCPU && string.IsNullOrWhiteSpace(g.PlayerO) && g.PlayerX != playerName)
                {
                    g.PlayerO = playerName;
                    NotifyStateChanged();
                }
                return g;
            }
            return null;
        }
        public GameEntity GetGame(string gameId)
        {
            Games.TryGetValue(gameId, out var g);
            return g;
        }
        public IEnumerable<GameEntity> GetOpenGames()
        {
            return Games.Values.Where(x => !x.IsFinished && !x.VsCPU && string.IsNullOrWhiteSpace(x.PlayerO));
        }
        public void MakeMove(string gameId, int index, string playerName)
        {
            if (Games.TryGetValue(gameId, out var g))
            {
                if (!g.IsFinished && index >= 0 && index < 9)
                {
                    if ((g.CurrentTurn == 'X' && g.PlayerX == playerName) || (g.CurrentTurn == 'O' && g.PlayerO == playerName))
                    {
                        if (g.Board[index] == ' ')
                        {
                            g.Board[index] = g.CurrentTurn;
                            if (CheckWin(g.Board, g.CurrentTurn))
                            {
                                g.IsFinished = true;
                                g.Winner = playerName;
                                g.EndTime = DateTime.Now;
                                var t = (long)(g.EndTime - g.StartTime).TotalSeconds;
                                if (Players.TryGetValue(playerName, out var winner))
                                {
                                    winner.TotalWins++;
                                    winner.ConsecutiveWins++;
                                    if (t < winner.BestWinTime) winner.BestWinTime = t;
                                }
                                var other = g.CurrentTurn == 'X' ? g.PlayerO : g.PlayerX;
                                if (Players.TryGetValue(other, out var loser)) loser.ConsecutiveWins = 0;
                            }
                            else if (g.Board.All(c => c != ' '))
                            {
                                g.IsFinished = true;
                                g.EndTime = DateTime.Now;
                                var other = g.CurrentTurn == 'X' ? g.PlayerO : g.PlayerX;
                                if (Players.TryGetValue(other, out var p1)) p1.ConsecutiveWins = 0;
                                if (Players.TryGetValue(playerName, out var p2)) p2.ConsecutiveWins = 0;
                            }
                            else
                            {
                                g.CurrentTurn = g.CurrentTurn == 'X' ? 'O' : 'X';
                            }

                            NotifyStateChanged();

                            if (!g.IsFinished && g.VsCPU && g.CurrentTurn == 'O' && g.PlayerO == "CPU")
                            {
                                DoAIMove(g);
                            }
                        }
                    }
                }
            }
        }

        void DoAIMove(GameEntity g)
        {
            int move = g.AIDifficulty switch
            {
                Difficulty.Easy => GetRandomMove(g.Board),
                Difficulty.Medium => GetWinningOrRandomMove(g.Board, 'O', 'X'),
                Difficulty.Hard => GetBestMove(g.Board),
                _ => GetRandomMove(g.Board)
            };

            if (move != -1)
            {
                g.Board[move] = 'O';
                if (CheckWin(g.Board, 'O'))
                {
                    g.IsFinished = true;
                    g.Winner = "CPU";
                    g.EndTime = DateTime.Now;
                    if (Players.TryGetValue(g.PlayerX, out var user)) user.ConsecutiveWins = 0;
                }
                else if (g.Board.All(c => c != ' '))
                {
                    g.IsFinished = true;
                    g.EndTime = DateTime.Now;
                    if (Players.TryGetValue(g.PlayerX, out var user)) user.ConsecutiveWins = 0;
                }
                else
                {
                    g.CurrentTurn = 'X';
                }
                NotifyStateChanged();
            }
        }

        int GetRandomMove(char[] board)
        {
            var empty = board.Select((val, idx) => new { val, idx }).Where(x => x.val == ' ').Select(x => x.idx).ToList();
            return empty.Count > 0 ? empty[new Random().Next(empty.Count)] : -1;
        }

        int GetWinningOrRandomMove(char[] board, char ai, char opponent)
        {
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    board[i] = ai;
                    if (CheckWin(board, ai)) { board[i] = ' '; return i; }
                    board[i] = ' ';
                }
            }

            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    board[i] = opponent;
                    if (CheckWin(board, opponent)) { board[i] = ' '; return i; }
                    board[i] = ' ';
                }
            }
            return GetRandomMove(board);
        }

        int GetBestMove(char[] board)
        {
            int bestScore = int.MinValue;
            int move = -1;

            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    board[i] = 'O';
                    int score = Minimax(board, 0, false);
                    board[i] = ' ';
                    if (score > bestScore)
                    {
                        bestScore = score;
                        move = i;
                    }
                }
            }
            return move;
        }

        int Minimax(char[] board, int depth, bool isMaximizing)
        {
            if (CheckWin(board, 'O')) return 10 - depth;
            if (CheckWin(board, 'X')) return depth - 10;
            if (board.All(c => c != ' ')) return 0;

            int bestScore = isMaximizing ? int.MinValue : int.MaxValue;
            char player = isMaximizing ? 'O' : 'X';

            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    board[i] = player;
                    int score = Minimax(board, depth + 1, !isMaximizing);
                    board[i] = ' ';
                    bestScore = isMaximizing ? Math.Max(score, bestScore) : Math.Min(score, bestScore);
                }
            }

            return bestScore;
        }

        bool CheckWin(char[] b, char t)
        {
            int[][] w = new int[][]{
                new[]{0,1,2},new[]{3,4,5},new[]{6,7,8},
                new[]{0,3,6},new[]{1,4,7},new[]{2,5,8},
                new[]{0,4,8},new[]{2,4,6}
            };
            foreach (var c in w) if (b[c[0]] == t && b[c[1]] == t && b[c[2]] == t) return true;
            return false;
        }
    }
}