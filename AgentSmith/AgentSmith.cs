//#define DEBUG_OUTPUT

using CheckersBase;
using CheckersBase.BrainBase;
using CheckersRules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Matrix
{
    static class RulesWrapper
    {
        public static List<Motion> FindAllMotions(Board board, bool isWhite)
        {
            return Rules.FindValidMotions(board, isWhite).GetAllMotions();
        }
    }

    #region Utils

    static class Utils
    {
        public static void LogMessage(string msg)
        {
#if DEBUG_OUTPUT
            Debug.WriteLine(msg);
#endif
        }

        public static void LogMotion(int value, Motion mtn, int deep)
        {
#if DEBUG_OUTPUT

            string format = "{0," + deep * 3 + "}  ";
            Debug.Write(String.Format(format, value));

            LogMotion(mtn);

#endif
        }

        public static void LogMotion(Motion mtn)
        {
#if DEBUG_OUTPUT

            for (int i = 0; i < mtn.Moves.Count; i++)
            {
                var m = mtn.Moves[i];
                Debug.Write(String.Format("[ {0} x {1} ]", m.X, m.Y));
            }

            Debug.Write("\n");

#endif
        }

        public static void LogBoard(Board board)
        {
#if DEBUG_OUTPUT

            Debug.WriteLine("Board: ");

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    string color = "/";

                    if (board.IsWhite(j, i)) color = "W";
                    if (board.IsBlack(j, i)) color = "B";

                    Debug.Write(FormatWithSpace(color + (board.IsKing(j, i) ? "K" : "")));
                }
                Debug.Write("\n");
            }

#endif
        }

        private static string FormatWithSpace(string text)
        {
            return String.Format("{0,3}", text);
        }
    }

    #endregion

    public class CheckersIntelligence
    {
        protected int _maxDeep; // количество полуходов для поиска решения

        protected int _koeffKings;
        protected int _koeffPawns;
        protected int _koeffMoves;

        protected bool _isWhiteTurn;

        Random rand = new Random();

        internal CheckersIntelligence(int koeffKings, int koeffPawns, int koeffMoves, int maxDeep)
        {
            _maxDeep = maxDeep; // количество полуходов для поиска решения

            _koeffKings = koeffKings;
            _koeffPawns = koeffPawns;
            _koeffMoves = koeffMoves;
        }

        public Motion FindMotion(Board board, bool isWhite)
        {
            _isWhiteTurn = isWhite;
            //Utils.LogBoard(board);

            int alpha = int.MinValue;
            int beta = int.MaxValue;

            var motions = RulesWrapper.FindAllMotions(board, _isWhiteTurn);

            if (motions.Count == 1)
                return motions.First();


            List<Tuple<int, Motion>> results = new List<Tuple<int, Motion>>();

            foreach (var mnt in motions.ToList())
            {
                //Utils.LogMotion(0, mnt, 0);
                int v = MaxValue(Rules.ApplyMotion(board, mnt, _isWhiteTurn), alpha, beta, 1);
                results.Add(new Tuple<int, Motion>(v, mnt));
            }

            if (results.Count == 0)
                return null;
            
            //Utils.LogMessage("After alpha-beta:");

            //foreach (var tuple in results)
            //{
            //    Utils.LogMotion(tuple.Item1, tuple.Item2, 0);
            //}

            int max = results.Select(tt => tt.Item1).Max();

            //Utils.LogMessage(String.Format("Max value is {0}", max));

            var bestMotions = results.Where(t => t.Item1 == max).Select(r => r.Item2).ToList();

            var mtn = bestMotions[rand.Next(bestMotions.Count)];

            //Utils.LogMessage("Result:");
            //Utils.LogMotion(mtn);

            return mtn;
        }

        private int MaxValue(Board board, int alpha, int beta, int deep)
        {
            if (IsMaximumDeep(deep))
                return Eval(board);

            int v = int.MinValue;

            var motions = RulesWrapper.FindAllMotions(board, !_isWhiteTurn);

            if (motions.Count == 0)
                return Eval(board);

            foreach (var mnt in motions.ToList())
            {
                Utils.LogMotion(0, mnt, deep + 1);
                v = Math.Max(v, MinValue(Rules.ApplyMotion(board, mnt, !_isWhiteTurn), alpha, beta, deep + 1));
                if (v >= beta)
                    return v;
                alpha = Math.Max(alpha, v);
            }
            return v;

        }

        private int MinValue(Board board, int alpha, int beta, int deep)
        {
            if (IsMaximumDeep(deep))
                return Eval(board);

            int v = int.MaxValue;

            var motions = RulesWrapper.FindAllMotions(board, _isWhiteTurn);

            if (motions.Count == 0) return Eval(board);

            foreach (var mnt in motions.ToList())
            {
                Utils.LogMotion(0, mnt, deep + 1);
                v = Math.Min(v, MaxValue(Rules.ApplyMotion(board, mnt, _isWhiteTurn), alpha, beta, deep + 1));
                if (v <= alpha)
                    return v;
                beta = Math.Min(beta, v);
            }
            return v;
        }

        public class PlayersCheckers
        {
            public int Kings { get; set; }
            public int Pawns { get; set; }
        }
        public class CheckersInfo
        {
            public PlayersCheckers Black { get; set; }
            public PlayersCheckers White { get; set; }

            public CheckersInfo()
            {
                Black = new PlayersCheckers();
                White = new PlayersCheckers();
            }
        }

        protected virtual int Eval(Board board)
        {
            CheckersInfo info = new CheckersInfo();

            for (int i = 0; i < Board.BOARD_SIZE; i++)
            {
                for (int j = 0; j < Board.BOARD_SIZE; j++)
                {
                    switch (board[i, j].Type)
                    {
                        case PieceTypes.BlackPawn:
                            info.Black.Pawns++;
                            break;
                        case PieceTypes.BlackKing:
                            info.Black.Kings++;
                            break;
                        case PieceTypes.WhitePawn:
                            info.White.Pawns++;
                            break;
                        case PieceTypes.WhiteKing:
                            info.White.Kings++;
                            break;
                    }
                }
            }

            var moves = Rules.GetMotionsCount(board);
            int wC = moves.Item1;
            int bC = moves.Item2;

            int f = 0;

            PlayersCheckers a, b;

            a = _isWhiteTurn ? info.White : info.Black;
            b = _isWhiteTurn ? info.Black : info.White;

            int aCount = _isWhiteTurn ? wC : bC;
            int bCount = _isWhiteTurn ? bC : wC;

            int advKings = a.Kings - b.Kings;
            int advPawns = a.Pawns - b.Pawns;
            int advMoves = aCount - bCount;

            f = _koeffKings * advKings + _koeffPawns * advPawns + _koeffMoves * advMoves;

            return f;
        }

        private bool IsMaximumDeep(int D)
        {
            return D > _maxDeep;
        }

        private bool IsWin(Board board, bool IsWhiteTurn)
        {
            var endGame = Rules.CheckGameOver(board);
            if (endGame == EndGameEnum.EG_NONE)
            {
                if (NoValidMotions(board, !IsWhiteTurn))
                    endGame = !IsWhiteTurn ? EndGameEnum.EG_WIN_BLACK : EndGameEnum.EG_WIN_WHITE;
            }

            if (IsWhiteTurn && endGame == EndGameEnum.EG_WIN_WHITE)
                return true;
            if (!IsWhiteTurn && endGame == EndGameEnum.EG_WIN_BLACK)
                return true;

            return false;
        }

        private static bool NoValidMotions(Board board, bool isWhite)
        {
            var validator = Rules.FindValidMotions(board, isWhite);
            return validator.NoValidMotions();
        }
    }

    #region Agents

    public class MyIntelligence : CheckersIntelligence
    {
        internal MyIntelligence(int koeffKings, int koeffPawns, int koeffMoves, int maxDeep) : base(koeffKings, koeffPawns, koeffMoves, maxDeep)
        {
        }

        protected override int Eval(Board board)
        {
            int pawsNearDam = 0;
            if (_isWhiteTurn)
            {
                for (int i = 0; i < Board.BOARD_SIZE; i++)
                    if (board[i, 2].Type == PieceTypes.WhitePawn)
                        pawsNearDam++;
            }
            else
            {
                for (int i = 0; i < Board.BOARD_SIZE; i++)
                    if (board[i, 5].Type == PieceTypes.BlackPawn)
                        pawsNearDam++;
            }

            int whiteSafePawns = 0, blackSafePawns = 0;
            int whiteSafeKings = 0, blackSafeKings = 0;
            int whiteCentralPawns = 0, blackCentralPawns = 0;




            //return base.Eval(board) + advSafePawns * safePawnsKoef + advSafeKings * safeKingKoef + advCentralPawns * centralPawnsKoef;


            CheckersInfo info = new CheckersInfo();

            for (int i = 0; i < Board.BOARD_SIZE; i++)
            {
                for (int j = 0; j < Board.BOARD_SIZE; j++)
                {
                    switch (board[i, j].Type)
                    {
                        case PieceTypes.BlackPawn:
                            info.Black.Pawns++;
                            break;
                        case PieceTypes.BlackKing:
                            info.Black.Kings++;
                            break;
                        case PieceTypes.WhitePawn:
                            info.White.Pawns++;
                            break;
                        case PieceTypes.WhiteKing:
                            info.White.Kings++;
                            break;
                    }

                    if (i == 0 || j == 0 || i == Board.BOARD_SIZE - 1 || j == Board.BOARD_SIZE - 1)
                    {
                        switch (board[i, j].Type)
                        {
                            case PieceTypes.BlackPawn:
                                blackSafePawns++;
                                break;
                            case PieceTypes.BlackKing:
                                blackSafeKings++;
                                break;
                            case PieceTypes.WhitePawn:
                                whiteSafePawns++;
                                break;
                            case PieceTypes.WhiteKing:
                                whiteSafeKings++;
                                break;
                        }
                    }

                    if (j == 3 || j == 4)
                    {
                        switch (board[i, j].Type)
                        {
                            case PieceTypes.BlackPawn:
                                blackCentralPawns++;
                                break;
                            case PieceTypes.WhitePawn:
                                whiteCentralPawns++;
                                break;
                        }
                    }
                }
            }

            var moves = Rules.GetMotionsCount(board);
            int wC = moves.Item1;
            int bC = moves.Item2;

            int f = 0;

            PlayersCheckers a, b;

            a = _isWhiteTurn ? info.White : info.Black;
            b = _isWhiteTurn ? info.Black : info.White;

            int aCount = _isWhiteTurn ? wC : bC;
            int bCount = _isWhiteTurn ? bC : wC;

            int advKings = a.Kings - b.Kings;
            int advPawns = a.Pawns - b.Pawns;
            int advMoves = aCount - bCount;


            int advSafePawns = _isWhiteTurn ? whiteSafePawns - blackSafePawns : blackSafePawns - whiteSafePawns;
            int advSafeKings = _isWhiteTurn ? whiteSafeKings - blackSafeKings : blackSafeKings - whiteSafeKings;
            int advCentralPawns = _isWhiteTurn ? whiteCentralPawns - blackCentralPawns : blackCentralPawns - whiteCentralPawns;
            int safePawnsKoef = 8;
            int safeKingKoef = 10;
            int centralPawnsKoef = 4;
            int nearDamKoef = 10;

            f = _koeffKings * advKings + _koeffPawns * advPawns + _koeffMoves * advMoves
                + advSafePawns * safePawnsKoef + advSafeKings * safeKingKoef + advCentralPawns * centralPawnsKoef
                + pawsNearDam * nearDamKoef;
            //+ pawsNearDam * nearDamKoef;

            return f;
        }
    }

    [BrainInfo(BrainName = "Агент Н8", Student = "Матрица", StudentGroup = "/dev/null")]
    public class AgentSmith : BrainBase
    {
        MyIntelligence intell = new MyIntelligence(6, 3, 1, 5);
        public override Motion FindMotion(Board board, bool isWhite)
        {
            return intell.FindMotion(board, isWhite);
        }
    }

    [BrainInfo(BrainName = "Агент С8", Student = "Матрица", StudentGroup = "/dev/null")]
    public class AgentBrown : BrainBase
    {
        CheckersIntelligence intell = new CheckersIntelligence(6, 3, 1, 5);
        public override Motion FindMotion(Board board, bool isWhite)
        {
            return intell.FindMotion(board, isWhite);
        }
    }

   

    #endregion

}
