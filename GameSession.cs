using System;

namespace SeaBattle
{
    public enum GamePhase
    {
        NotStarted,
        FleetReady,
        InProgress,
        Finished
    }

    /// <summary>
    /// Encapsulates game logic: boards, turn management, win/lose state.
    /// </summary>
    public class GameSession
    {
        public Board MyBoard { get; }
        public Board EnemyBoard { get; }

        public bool IsHost { get; private set; }
        public bool IsMyTurn { get; private set; }
        public GamePhase Phase { get; private set; }

        /// <summary>
        /// Number of successful hits on the enemy fleet (on our local "enemy board" representation).
        /// We consider enemy defeated when this reaches Board.TotalShipCells.
        /// </summary>
        public int HitsOnEnemy { get; private set; }

        /// <summary>
        /// True if our fleet has been placed and is ready for a round.
        /// </summary>
        public bool IsFleetReady { get; private set; }

        public GameSession()
        {
            MyBoard = new Board();
            EnemyBoard = new Board();
            ClearAll();
        }

        public void SetRole(bool isHost)
        {
            IsHost = isHost;
            IsMyTurn = isHost;
        }

        public void ClearAll()
        {
            MyBoard.Clear();
            EnemyBoard.Clear();
            HitsOnEnemy = 0;
            Phase = GamePhase.NotStarted;
            IsMyTurn = false;
            IsFleetReady = false;
        }

        /// <summary>
        /// Places a full standard fleet randomly for the local player.
        /// </summary>
        public void PrepareRandomFleetForMe()
        {
            MyBoard.PlaceFleetRandom();
            EnemyBoard.Clear();
            HitsOnEnemy = 0;
            Phase = GamePhase.FleetReady;
            IsFleetReady = true;
        }

        /// <summary>
        /// Called after manual placement is completed by the UI.
        /// </summary>
        public void NotifyManualFleetCompleted()
        {
            EnemyBoard.Clear();
            HitsOnEnemy = 0;
            Phase = GamePhase.FleetReady;
            IsFleetReady = true;
        }

        public void StartRound()
        {
            if (!IsFleetReady)
                throw new InvalidOperationException("Fleet is not ready.");

            Phase = GamePhase.InProgress;
            // IsMyTurn is already set by SetRole
        }

        /// <summary>
        /// Called when enemy fires at our board.
        /// </summary>
        public (CellState state, bool isHit, bool hasLost, int destroyedShipSize) ReceiveEnemyShot(int x, int y)
        {
            bool isHit;
            bool hasLost;
            var state = MyBoard.ReceiveShot(x, y, out isHit, out hasLost, out int destroyedSize);

            if (!isHit && !hasLost)
                IsMyTurn = true; // miss => our turn

            if (hasLost)
                Phase = GamePhase.Finished;

            return (state, isHit, hasLost, destroyedSize);
        }

        /// <summary>
        /// Called when we receive result from enemy for our last shot.
        /// We determine enemy defeat based on our local hit count, not only on the "WIN" string.
        /// </summary>
        public (bool isHit, bool enemyLost, int destroyedShipSize) ApplyMyShotResult(int x, int y, string result)
        {
            bool isHit = (result == "HIT" || result == "WIN");
            bool enemyLost = false;
            int destroyedSize = 0;

            EnemyBoard.MarkShotResult(x, y, isHit);

            if (isHit)
            {
                HitsOnEnemy++;

                // إذا كانت الضربة قد دمرت سفينة:
                destroyedSize = EnemyBoard.CheckIfShipDestroyed(x, y)?.Size ?? 0;

                // إذا تم تدمير السفينة
                if (destroyedSize > 0)
                {
                    enemyLost = true;
                    Phase = GamePhase.Finished;
                }

                // إذا تم ضرب جميع الخلايا في الأسطول
                if (HitsOnEnemy >= Board.TotalShipCells)
                {
                    enemyLost = true;
                    Phase = GamePhase.Finished;
                }
            }

            if (result == "WIN")
            {
                enemyLost = true;
                Phase = GamePhase.Finished;
            }

            // إذا كانت الضربة خاطئة، يذهب الدور للخصم
            if (!isHit && !enemyLost)
            {
                IsMyTurn = false;
            }

            return (isHit, enemyLost, destroyedSize);
        }

        /// <summary>
        /// Start a new round over the same connection.
        /// </summary>
        public void StartReplay(bool randomFleet)
        {
            EnemyBoard.Clear();
            HitsOnEnemy = 0;

            if (randomFleet)
            {
                MyBoard.PlaceFleetRandom();
                IsFleetReady = true;
            }
            else
            {
                MyBoard.Clear();
                IsFleetReady = false;
            }

            Phase = GamePhase.FleetReady;
        }
    }
}
