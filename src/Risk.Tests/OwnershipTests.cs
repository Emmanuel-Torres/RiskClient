﻿using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Risk.Game;

namespace Risk.Tests
{
    class OwnershipTests
    {
        Game.Game testgame;
        string player1;
        string player2;

        [SetUp]
        public void SetUp()
        {
            testgame = new Game.Game(new Game.GameStartOptions { Height = 2, Width = 2, StartingArmiesPerPlayer = 5 });
            player1 = testgame.AddPlayer("player1");
            player2 = testgame.AddPlayer("player2");
            testgame.TryPlaceArmy(player1, new Location(0, 0));
            testgame.TryPlaceArmy(player2, new Location(0, 1));

        }

        //Player owns first territory, attacks second territory he doesnt own | should be true
        [Test]
        public void OwnershipValidityOwnerToForeign()
        {

            var placeResult = testgame.attackOwnershipValid(player1, testgame.Board.GetTerritory(0, 0), testgame.Board.GetTerritory(0, 1));
            placeResult.Should().BeTrue();
        }

        //Player doesnt own first territory, attacks second territory he doesnt own | should be false
        [Test]
        public void OwnershipValidityForeignToForeign()
        {
            var placeResult = testgame.attackOwnershipValid(player1, testgame.Board.GetTerritory(0, 1), testgame.Board.GetTerritory(0, 1));
            placeResult.Should().BeFalse();
        }

        //Player owns first territory, attacks second territory he also owns | should be false
        [Test]
        public void OwnershipValidityOwnerToOwner()
        {
            var placeResult = testgame.attackOwnershipValid(player1, testgame.Board.GetTerritory(0, 0), testgame.Board.GetTerritory(0, 0));
            placeResult.Should().BeFalse();
        }
        //Player doesnt own first territory, attacks second territory he owns | should be false
        [Test]
        public void OwnershipValidityForeignToOwner()
        {
            var placeResult = testgame.attackOwnershipValid(player1, testgame.Board.GetTerritory(0, 1), testgame.Board.GetTerritory(0, 0));
            placeResult.Should().BeFalse();
        }




    }
}