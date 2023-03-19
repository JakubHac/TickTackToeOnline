using System.Linq;

public class PerspectiveBoardState
{
    public readonly RoomData Game;
    private readonly Client Client;
    private readonly ulong enemyId;
    public readonly BoardState State;
    
    public int GetEnemyGemIndex()
    {
        return Game.playersGems[enemyId];
    }
    
    
    public PerspectiveBoardState(RoomData game, Client client, bool instantWin = false)
    {
        Game = game;
        Client = client;
        State = instantWin ? BoardState.WeWon : BoardState.NotResolved;
        if (State == BoardState.NotResolved && Game.playersGems.Count == 1)
        {
            State = BoardState.WaitingForPlayer;
        }
        enemyId = State == BoardState.WaitingForPlayer ? 0 : Game.playersGems.Keys.First(x => x != Client.ID);
        if (State != BoardState.NotResolved) return;
        if (DidPlayerWin(Client.ID))
        {
            State = BoardState.WeWon;
        }
        else if (DidPlayerWin(enemyId))
        {
            State = BoardState.EnemyWon;
        }
        else if (IsDraw(Client.ID, enemyId))
        {
            State = BoardState.Draw;
        }
        else if (game.HostID == Client.ID && game.turn == 0)
        {
            State = BoardState.OurTurn;
        }
        else if (game.HostID != Client.ID && game.turn == 1)
        {
            State = BoardState.OurTurn;
        }
        else if (game.HostID == Client.ID && game.turn == 1)
        {
            State = BoardState.EnemyTurn;
        }
        else if (game.HostID != Client.ID && game.turn == 0)
        {
            State = BoardState.EnemyTurn;
        }
    }

    private bool IsDraw(ulong clientID, ulong enemyId1)
    {
        if (DidPlayerWin(clientID) || DidPlayerWin(enemyId1)) return false;
        if (Game.board.Any(x => x.Any(y => y == 0)))
        {
            return false;
        }

        return true;
    }

    private bool DidPlayerWin(ulong playerID)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Game.board[i][0] == playerID && Game.board[i][1] == playerID && Game.board[i][2] == playerID)
            {
                return true;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            if (Game.board[0][i] == playerID && Game.board[1][i] == playerID && Game.board[2][i] == playerID)
            {
                return true;
            }
        }

        if (Game.board[0][0] == playerID && Game.board[1][1] == playerID && Game.board[2][2] == playerID)
        {
            return true;
        }

        if (Game.board[0][2] == playerID && Game.board[1][1] == playerID && Game.board[2][0] == playerID)
        {
            return true;
        }

        return false;
    }
    
    
}
