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
        public (CellState state, bool isHit, bool hasLost) ReceiveEnemyShot(int x, int y) //???
        {
            bool isHit;
            bool hasLost;
            var state = MyBoard.ReceiveShot(x, y, out isHit, out hasLost);

            if (!isHit && !hasLost)
                IsMyTurn = true; // miss => our turn

            if (hasLost)
                Phase = GamePhase.Finished;

            return (state, isHit, hasLost);
        }

        /// <summary>
        /// Called when we receive result from enemy for our last shot.
        /// We determine enemy defeat based on our local hit count, not only on the "WIN" string.
        /// </summary>
        public (bool isHit, bool enemyLost) ApplyMyShotResult(int x, int y, string result)
        {
            bool isHit = (result == "HIT" || result == "WIN");
            bool enemyLost = false;

            EnemyBoard.MarkShotResult(x, y, isHit);

            if (isHit)
            {
                HitsOnEnemy++;

                // If we hit as many cells as the standard fleet size, enemy is logically defeated
                if (HitsOnEnemy >= Board.TotalShipCells)
                {
                    enemyLost = true;
                    Phase = GamePhase.Finished;
                }
            }

            // If the remote side explicitly tells us "WIN",
            // we also treat it as enemy defeat (extra safety).
            if (result == "WIN")
            {
                enemyLost = true;
                Phase = GamePhase.Finished;
            }

            // Turn management: if we missed and game not finished, turn goes to enemy
            if (!isHit && !enemyLost)
            {
                IsMyTurn = false;
            }

            return (isHit, enemyLost);
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
