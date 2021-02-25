using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Risk.Signalr.ConsoleClient
{
    public class PlayerLogic : IPlayerLogic
    {
        public string MyPlayerName { get; set; }
        int turnNum = 0;
        Random rng = new Random();
        int sleepFrequency = 0;
        private readonly bool shouldSleep;
        int attacksMade = 0;
        int delayAttack = 0;
        int lastBoardSum = 0;

        public PlayerLogic(string playerName, bool shouldSleep)
        {
            MyPlayerName = playerName;
            this.shouldSleep = shouldSleep;
            sleepFrequency = rng.Next(5, 20);
        }


        public Location WhereDoYouWantToDeploy(IEnumerable<BoardTerritory> board)
        {
            var location = DetermineLocationToDeploy(board);
            return location;
        }

        private Location DetermineLocationToDeploy(IEnumerable<BoardTerritory> board)
        {
            var lastLocation = board.Last().Location;
            var targetLocation = DetermineInitalLocation(lastLocation.Row, lastLocation.Column);
            var squareLocations = GetSquareLocations(targetLocation.Row, targetLocation.Column);
            var location = GetDeployLocation(squareLocations, board);
            return location;
        }

        private Location GetDeployLocation(IEnumerable<Location> squareLocations, IEnumerable<BoardTerritory> board)
        {
            foreach (var l in squareLocations)
            {
                var t = board.FirstOrDefault(t => t.Location.Row == l.Row && t.Location.Column == l.Column);
                if (t != null && t.OwnerName == null)
                {
                    return t.Location;
                }
            }

            //TODO: Finish the logic in this block
            if (board.Any(t => t.OwnerName == MyPlayerName))
            {
                var territories = board.Where(t => t.OwnerName == MyPlayerName);  
          
                foreach(var t in territories)
                {
                    Int64 maxArmies = territories.Max(a => a.Armies);
                    if(t.Armies < maxArmies)
                    {
                        return t.Location;
                    }
                    else if(territories.All(ter=>ter.Armies== maxArmies))
                    {
                        return t.Location;
                    }
                }
            }

            //Last resort
            return board.FirstOrDefault(t => t.OwnerName == null).Location;
        }

        private IEnumerable<Location> GetSquareLocations(int initialRow, int initialCol)
        {
            var SquareLocations = new List<Location>();
            for (int r = initialRow; r <= initialRow + 4; r++)
            {
                for (int c = initialCol; c <= initialCol + 4; c++)
                {
                    if (r < initialRow + 4 && r > initialRow)
                    {
                        if (c == initialCol + 4 || c == initialCol)
                        {
                            SquareLocations.Add(new Location(r, c));
                        }
                    }
                    else
                    {
                        SquareLocations.Add(new Location(r, c));
                    }
                }
            }

            return SquareLocations;
        }

        private Location DetermineInitalLocation(int maxRow, int maxCol)
        {
            return new Location((maxRow / 2) - 2, (maxCol / 2) - 2);
        }

        private void randomSleep()
        {
            if (!shouldSleep)
                return;

            if (turnNum++ % sleepFrequency == 0)
            {
                int secondsToSleep = rng.Next(0, 3);
                Console.WriteLine($"Sleeping for {secondsToSleep}");
                Thread.Sleep(TimeSpan.FromSeconds(secondsToSleep));
            }
        }

        private (Location from, Location to) determineLocationToAttack(IEnumerable<BoardTerritory> board)
        {
            var lastLocation = board.Last().Location;
            var targetLocation = DetermineInitalLocation(lastLocation.Row, lastLocation.Column);
            var squareLocations = GetSquareLocations(lastLocation.Row, lastLocation.Column);
            var innerSquareLocations = getInnerSquareLocations(targetLocation.Row, targetLocation.Column);
            var hostileTerr = NotOwnedSquareTerr(targetLocation, board);
            var innerSquareHost = NotOwnedInnerSquareTerr(targetLocation, board);



            if (hostileTerr.Count() > 0)
            {
                foreach (var destination in hostileTerr.OrderByDescending(t => t.Armies))
                {
                    var myTerritories = GetNeighbors(destination, board).Where(t => t.OwnerName == MyPlayerName).Where(t => t.Armies > 1).OrderByDescending(t => t.Armies);
                    var source = myTerritories.FirstOrDefault();
                    if (myTerritories.Sum(t => t.Armies) > destination.Armies)
                    {
                        return (source.Location, destination.Location);
                    }
                }
            }

            if (innerSquareHost.Count() > 0)
            {
                foreach (var destination in innerSquareHost.OrderByDescending(t => t.Armies))
                {
                    var myTerritories = GetNeighbors(destination, board).Where(t => t.OwnerName == MyPlayerName).Where(t => t.Armies > 1).OrderByDescending(t => t.Armies);
                    var source = myTerritories.FirstOrDefault();
                    if (myTerritories.Sum(t => t.Armies) > destination.Armies)
                    {
                        return (source.Location, destination.Location);
                    }
                }
            }

            foreach (var myTerritory in board.Where(t => t.OwnerName == MyPlayerName).Where(t => t.Armies > 1).OrderByDescending(t => t.Armies))
            {
                var myNeighbors = GetNeighbors(myTerritory, board);
                var destination = myNeighbors.Where(t => t.OwnerName != MyPlayerName).OrderBy(t => t.Armies).FirstOrDefault();
                var dummyTerr = board.Where(t => t.OwnerName == MyPlayerName).Where(t => t.Armies < 2);
                if (destination != null && dummyTerr.Count() < 10 && destination.Armies < myNeighbors.Sum(t => t.Armies))
                {
                    return (myTerritory.Location, destination.Location);
                }
            }

            //If we made it here is becuase our square is completed

            if (delayAttack < 5) 
            {
                //change this
                var myMax = board.Where(t => t.OwnerName == MyPlayerName).Max(t => t.Armies);
                var maxTerritories = board.Where(t => t.OwnerName != MyPlayerName && t.Armies >= myMax).Count();
                if (maxTerritories > 0)
                {
                    delayAttack = 0;
                }
                else
                {
                    delayAttack++;
                    throw new Exception("I don't want to attack yet");
                }
            }

            if (delayAttack == 5)
            {
                foreach (var myTerritory in board.Where(t => t.OwnerName == MyPlayerName).Where(t => t.Armies > 1).OrderByDescending(t => t.Armies))
                {
                    var myNeighbors = GetNeighbors(myTerritory, board);
                    var destination = myNeighbors.Where(t => t.OwnerName != MyPlayerName).OrderBy(t => t.Armies).FirstOrDefault();
                    if (destination != null)
                    {
                        return (myTerritory.Location, destination.Location);
                    }
                }
            }

            throw new Exception("Unable to find place to attack");
        }

        private IEnumerable<BoardTerritory> NotOwnedInnerSquareTerr(Location targetLocation, IEnumerable<BoardTerritory> board)
        {
            var innerSquare = getInnerSquareLocations(targetLocation.Row, targetLocation.Column);
            var hostileTerr = new List<BoardTerritory>();
            foreach(var l in innerSquare)
            {
                var terr = board.FirstOrDefault(t => t.Location.Row == l.Row && t.Location.Column == l.Column);
                if (terr.OwnerName != MyPlayerName)
                {
                    hostileTerr.Add(terr);
                }
            }

            return hostileTerr;
        }

        private IEnumerable<BoardTerritory> NotOwnedSquareTerr(Location targetLocation, IEnumerable<BoardTerritory> board)
        {
            var squareLocations = GetSquareLocations(targetLocation.Row, targetLocation.Column);
            var hostileTerr = new List<BoardTerritory>();
            foreach (var l in squareLocations)
            {
                var terr = board.FirstOrDefault(t => t.Location.Row == l.Row && t.Location.Column == l.Column);
                if (terr.OwnerName != MyPlayerName)
                {
                    hostileTerr.Add(terr);
                }
            }

            return hostileTerr;
        }

        private IEnumerable<Location> getInnerSquareLocations(int row, int column)
        {
            List<Location> innerSquare = new List<Location>();
            for(int r=row+1; r<=row+3; r++){
                for (int c=column+1; c<=column+3; c++) {
                    innerSquare.Add(new Location(r, c));
                }
            }
            return innerSquare;
        }
        public (Location from, Location to) WhereDoYouWantToAttack(IEnumerable<BoardTerritory> board)
        {
            return determineLocationToAttack(board);
        }

        private IEnumerable<BoardTerritory> GetNeighbors(BoardTerritory territory, IEnumerable<BoardTerritory> board)
        {
            var l = territory.Location;
            var neighborLocations = new[] {
                new Location(l.Row+1, l.Column-1),
                new Location(l.Row+1, l.Column),
                new Location(l.Row+1, l.Column+1),
                new Location(l.Row, l.Column-1),
                new Location(l.Row, l.Column+1),
                new Location(l.Row-1, l.Column-1),
                new Location(l.Row-1, l.Column),
                new Location(l.Row-1, l.Column+1),
            };
            return board.Where(t => neighborLocations.Contains(t.Location));
        }
    }
}
