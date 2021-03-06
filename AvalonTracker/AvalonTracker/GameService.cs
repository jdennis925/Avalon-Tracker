﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AvalonTracker.Annotations;
using System.Data.SQLite;

namespace AvalonTracker
{




    public enum GameState
    {
        PlayerSelection,
        PartySelection,
        PartyVoting,
        QuestVoting,
        BadGuysWin,
        GoodGuysWin,
        AttemptAssassination,
        RevealCharactes,
    }


    public class GameService : INotifyPropertyChanged
    {

        public GameService()
        {
        }

        private ObservableCollection<Player> _allPlayers = new ObservableCollection<Player>();

        public ObservableCollection<Player> AllPlayers
        {
            get { return _allPlayers; }
        }

        public void LoadData()
        {
            using (var context = new AvalonModelContainer())
            {
                var players = (from p in context.Player
                               select p);
                if (players != null)
                {
                    foreach (var player in players)
                    {
                        bool dupe = false;
                        foreach (var p2 in AllPlayers)
                        {
                            if (p2.Name == player.Name)
                            {
                                dupe = true;
                                break;
                            }
                        }
                        if (!dupe)
                        {
                            AllPlayers.Add(player);
                        }
                    }    
                }
            }  
        }

        public void AddNewPlayer(string name)
        {
            var p = new Player() {Name = name};

            using (var context = new AvalonModelContainer())
            {
                context.Player.Add(p);
                context.SaveChanges();
            }
            AllPlayers.Add(p);
        }

        public ObservableCollection<Player> ActivePlayers = new ObservableCollection<Player>();


        public void StartMatch()
        {
            using (var context = new AvalonModelContainer())
            {

                var maxGameId = (from g in context.Games
                             select (int?)g.Id).Max().GetValueOrDefault();

                var game = new Game()
                {
                     Id = maxGameId + 1,
                     StartTime = DateTime.Now,
                     EndTime = null
                };
                CurrentGameId = game.Id;
                context.Games.Add(game);

                context.SaveChanges();


                var maxApId = (from g in context.Games
                               select (int?)g.Id).Max().GetValueOrDefault();
                foreach (var activePlayer in ActivePlayers)
                {
                    var ap = new ActivePlayer
                    {
                        Game = game,
                        PlayerId = activePlayer.Id,
                        Id = maxApId
                    };
                    maxApId++;
                    context.ActivePlayers.Add(ap);
                }
                context.SaveChanges();
            }

            VoteTable.Clear();
            foreach (var player in Services.GameService.ActivePlayers)
            {
                VoteTable.Add(player, false);
            }

            Services.GameService.PartyChooser = Services.GameService.ActivePlayers.First();
            Services.GameService.AdvanceToNextQuest();
            Services.GameService.CurrentGameState = GameState.PartySelection;
        }

        public int CurrentGameId { get; private set; }


        public void StartPartySelection()
        {


        }

        private int _curQuestId = 0;

        public void VoteOnActiveParty()
        {
            using (var context = new AvalonModelContainer())
            {
                                   
                var maxQuest = (from q in context.Quests
                                select (int?)q.Id).Max().GetValueOrDefault();

                var quest = new Quest
                {
                    Id = maxQuest + 1, 
                    VoteTrack = VoteTrack, 
                    Stage = CurrentQuestPhase,
                };
                _curQuestId = quest.Id;
                context.Quests.Add(quest);

                var apc = (from ap in context.ActivePlayers
                           where ap.Game.Id == CurrentGameId
                           select ap);

                var apcthree = (from ap in context.ActivePlayers
                                where ap.Game.Id == CurrentGameId
                                select ap);

                int partyLeaderId = -1;
                foreach (var ap in apcthree)
                {
                    if (ap.PlayerId == PartyChooser.Id)
                    {
                        partyLeaderId = ap.Id;
                    }
                }

                var maxPartyPlayerId = (from p in context.PartyPlayers
                                        select (int?)p.Id).Max().GetValueOrDefault();

                foreach (var p in ActiveParty)
                {
                    var partyPlayer = new PartyPlayer()
                    {
                        PartyLeaderId = partyLeaderId,
                        Id = maxPartyPlayerId,
                        QuestId = quest.Id,
                    };
                    maxPartyPlayerId++;

                    foreach (var ap in apc)
                    {
                        if (ap.PlayerId == p.Id)
                        {
                            //add matching players into the party
                            partyPlayer.ActivePlayerId = ap.Id;
                            context.PartyPlayers.Add(partyPlayer);
                            break;
                        }
                    }
                }

                var partyVoteMax = (from g in context.Games
                                select (int?)g.Id).Max().GetValueOrDefault();

                foreach (var entry in VoteTable)
                {
                    var partyVote = new PartyVote();
                    partyVote.Id = partyVoteMax + 1;
                    partyVote.Quest = quest;
                    partyVote.ApproveFlag = entry.Value;
                    partyVoteMax++;

                    var apcRedux = (from ap in context.ActivePlayers 
                                    where ap.Game.Id == CurrentGameId
                                    select ap);

                    foreach (var ap in apcRedux)
                    {
                        int applayerId = ap.PlayerId;
                        int apid = ap.Id;
                        var found = false;
                        {
                            if (entry.Key.Id == applayerId)
                            {
                                partyVote.ActivePlayerId = apid;
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                    context.PartyVotes.Add(partyVote);
                }


                context.SaveChanges();
            }
        }


        public void SumbitQuestResults(IList<bool> results)
        {

            using (var context = new AvalonModelContainer())
            {
                var qvmax = (from qv in context.QuestVotes
                                    select (int?)qv.Id).Max().GetValueOrDefault();

                foreach (var result in results)
                {
                    var questVote = new QuestVote
                    {
                        PassFlag = result, 
                        Id = qvmax++,
                        QuestId = _curQuestId,
                    };
                    context.QuestVotes.Add(questVote);
                }
                context.SaveChanges();
            }

            int failureTotal = results.Count(result => !result);
            if (failureTotal >= this.QuestFailureRequirement(CurrentQuestPhase))
            {
                //quest failed!
                _questFailureTotal++;
            }
            if (_questFailureTotal < 3 && CurrentQuestPhase - _questFailureTotal < 3)
            {
                AdvanceToNextQuest();
                CurrentGameState = GameState.PartySelection;
            }
            else if (CurrentQuestPhase - _questFailureTotal == 3)
            {
                CurrentGameState = GameState.AttemptAssassination;
            }
            else
            {
                CurrentGameState = GameState.BadGuysWin;
            }
        }

        private int _questFailureTotal;


        public ObservableCollection<Player> ActiveParty = new ObservableCollection<Player>();

        private int _partyChooserIndex = 0;

        public void AdvancePartyChooser()
        {
            _partyChooserIndex++;
            if (_partyChooserIndex >= ActivePlayers.Count)
            {
                _partyChooserIndex = 0;
            }
            PartyChooser = ActivePlayers[_partyChooserIndex];
        }

        private Player _partyChooser = null;
        public Player PartyChooser
        {
            get
            {
                return _partyChooser;
            }
            set
            {
                _partyChooser = value;
                OnPartyChooserChanged(EventArgs.Empty);
                OnPropertyChanged();
            }
        }


        public int VoteTrack { get; private set; }

        public void AdvanceVoteTrack()
        {
            VoteTrack++;
            CurrentGameState = VoteTrack > GlobalConstants.MaxVoteTrack ? GameState.BadGuysWin : GameState.PartySelection;
        }

        public event EventHandler PartyChooserChanged;
        protected virtual void OnPartyChooserChanged(EventArgs e)
        {
            EventHandler handler = PartyChooserChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler GameStateChanged;
        protected virtual void OnGameStateChanged(EventArgs e)
        {
            EventHandler handler = GameStateChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void ResetVoteTrack()
        {
            VoteTrack = 0;
        }

        public int CurrentQuestPhase { get; private set; }

        public void AdvanceToNextQuest()
        {
            CurrentQuestPhase += 1;
        }


        private GameState _currentGameState;
        public GameState CurrentGameState
        {
            get { return _currentGameState; }
            set
            {
                _currentGameState = value;
                OnGameStateChanged(EventArgs.Empty);
            }
        }

        public Dictionary<Player, bool> VoteTable = new Dictionary<Player, bool>();

        //private List<CharacterClass> characterClasses = new List<CharacterClass>()
        //{
        //    //Good guys
        //    new CharacterClass(SerializableStrings.LoyalServantOfArthur, Allegiance.Good),
        //    new CharacterClass(SerializableStrings.Percival, Allegiance.Good),
        //    new CharacterClass(SerializableStrings.Merlin, Allegiance.Good),

        //    //bad guys
        //    new CharacterClass(SerializableStrings.MinionOfMordred, Allegiance.Bad),
        //    new CharacterClass(SerializableStrings.Assassin, Allegiance.Bad),
        //    new CharacterClass(SerializableStrings.Mordred, Allegiance.Bad),
        //    new CharacterClass(SerializableStrings.Oberon, Allegiance.Bad),
        //    new CharacterClass(SerializableStrings.Morgana, Allegiance.Bad),
        //};

        //public IList<CharacterClass> CharacterClasses { get { return characterClasses; } }

        public int GetPartySize(int questNumber)
        {
            switch (ActivePlayers.Count)
            {
                case 5:
                    switch (questNumber)
                    {
                        case 1: return 2;
                        case 2: return 3;
                        case 3: return 2;
                        case 4: return 3;
                        case 5: return 3;
                    }
                    break;
                case 6:
                    switch (questNumber)
                    {
                        case 1: return 2;
                        case 2: return 3;
                        case 3: return 4;
                        case 4: return 3;
                        case 5: return 4;
                    }
                    break;
                case 7:
                    switch (questNumber)
                    {
                        case 1: return 2;
                        case 2: return 3;
                        case 3: return 3;
                        case 4: return 4;
                        case 5: return 4;
                    }
                    break;
                case 8:
                case 9:
                case 10:
                switch (questNumber)
                    {
                        case 1: return 3;
                        case 2: return 4;
                        case 3: return 4;
                        case 4: return 5;
                        case 5: return 5;
                    }
                    break;
            }
            return -1;
        }

        public int QuestFailureRequirement(int questNumber)
        {
            if (ActivePlayers.Count < 7)
            {
                return 1;
            }
            else
            {
                return questNumber == 4 ? 2 : 1;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
