﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AvalonTracker.Annotations;

namespace AvalonTracker
{
    public class MainWindowViewModel: INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            InitializeCommands();

            Services.GameService.GameStateChanged += GameStateChanged;

            Services.GameService.CurrentGameState = GameState.PlayerSelection;
        }

        private void InitializeCommands()
        {
            StartMatchCommand = new RelayCommand(PerformStartMatchCommand, CanPerformStartMatchCommand);
            GoToVoteCommand = new RelayCommand(PerformGoToVoteCommand, CanGoToVote);
            LoadDataCommand = new RelayCommand(PerformLoadDataCommand);
        }

        public void GameStateChanged(object sender, EventArgs e)
        {
            switch (Services.GameService.CurrentGameState)
            {
                case GameState.PlayerSelection:
                    PlayerSelectionVisibility = Visibility.Visible;
                    PartySelectionVisibility = Visibility.Hidden;
                    QuestResultsVisibility = Visibility.Hidden;
                    BadGuysWinVisibility = Visibility.Hidden;
                    break;
                case GameState.PartySelection:
                    OnPropertyChanged("RequiredPlayers");
                    PlayerSelectionVisibility = Visibility.Hidden;
                    PartySelectionVisibility = Visibility.Visible;
                    QuestResultsVisibility = Visibility.Hidden;
                    BadGuysWinVisibility = Visibility.Hidden;
                    break;
                case GameState.PartyVoting:
                    PlayerSelectionVisibility = Visibility.Hidden;
                    PartySelectionVisibility = Visibility.Visible;
                    QuestResultsVisibility = Visibility.Hidden;
                    BadGuysWinVisibility = Visibility.Hidden;
                    break;
                case GameState.QuestVoting:
                    PlayerSelectionVisibility = Visibility.Hidden;
                    PartySelectionVisibility = Visibility.Hidden;
                    QuestResultsVisibility = Visibility.Visible;
                    BadGuysWinVisibility = Visibility.Hidden;
                    break;
                case GameState.BadGuysWin:
                    PlayerSelectionVisibility = Visibility.Hidden;
                    PartySelectionVisibility = Visibility.Hidden;
                    QuestResultsVisibility = Visibility.Hidden;
                    BadGuysWinVisibility = Visibility.Visible;
                    break;
                case GameState.AttemptAssassination:
                    PlayerSelectionVisibility = Visibility.Hidden;
                    PartySelectionVisibility = Visibility.Hidden;
                    QuestResultsVisibility = Visibility.Hidden;
                    BadGuysWinVisibility = Visibility.Hidden;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private Visibility _playerSelectionVisibility;
        private Visibility _partySelectionVisibility;
        private Visibility _questResultsVisibility;
        private Visibility _badGuysWinVisibility;

        public Visibility BadGuysWinVisibility
        {
            get { return _badGuysWinVisibility; }
            private set
            {
                _badGuysWinVisibility = value;
                OnPropertyChanged();
            }
        }

        public Visibility QuestResultsVisibility
        {
            get { return _questResultsVisibility; }
            private set { 
                _questResultsVisibility = value;
                OnPropertyChanged();
            }
        }
        
        public Visibility PlayerSelectionVisibility { 
            get { return _playerSelectionVisibility; } 
            private set
            {
                _playerSelectionVisibility = value;
                OnPropertyChanged();
            } 
        }

        public Visibility PartySelectionVisibility
        {
            get { return _partySelectionVisibility; }
            private set
            {
                _partySelectionVisibility = value;
                OnPropertyChanged();
            }
        }

        private bool CanPerformStartMatchCommand(object obj)
        {
            return (Services.GameService.ActivePlayers.Count >= GlobalConstants.MinimumPlayers)
                    && Services.GameService.ActivePlayers.Count <= GlobalConstants.MaximumPlayers;
        }

        private void PerformStartMatchCommand(object obj)
        {
            Services.GameService.StartMatch();
            OnPropertyChanged("RequiredPlayers");
            OnPropertyChanged("NextStateBtnText");
        }

        private void PerformGoToVoteCommand(object obj)
        {
            if (Services.GameService.CurrentGameState == GameState.PartySelection)
            {
                PartySelectionHelper(obj);
            }
            else if (Services.GameService.CurrentGameState == GameState.PartyVoting)
            {
                PartyVotingHelper(obj);
            }
        }

        private void PerformLoadDataCommand(object obj)
        {
            Services.GameService.LoadData();
        }

        private void PartyVotingHelper(object obj)
        {
            Services.GameService.VoteOnActiveParty();

            //count votes
            int approveVotes = 0;
            foreach (var player in Services.GameService.ActivePlayers)
            {
                if (Services.GameService.VoteTable[player])
                    approveVotes++;

            }
            //the vote passes?
            if ((double)approveVotes / Services.GameService.ActivePlayers.Count > .5)
            {
                Services.GameService.CurrentGameState = GameState.QuestVoting;
                Services.GameService.ResetVoteTrack();
            }
            else
            {
                Services.GameService.AdvanceVoteTrack();
                OnPropertyChanged("VoteTrackMessage");
                OnPropertyChanged("NextStateBtnText");
            }
            //always advance party chooser.
            Services.GameService.AdvancePartyChooser();
        }

        private void PartySelectionHelper(object obj)
        {
            Services.GameService.CurrentGameState = GameState.PartyVoting;
            OnPropertyChanged("NextStateBtnText");
        }

        private bool CanGoToVote(object obj)
        {
            return Services.GameService.ActiveParty.Count == Services.GameService.GetPartySize(Services.GameService.CurrentQuestPhase);
        }

        public ICommand GoToVoteCommand { get; set; }
        public ICommand StartMatchCommand { get; private set; }
        public ICommand LoadDataCommand { get; private set; }
        

        public string RequiredPlayers
        {
            get { return string.Format("Quest No. {0} requires {1} players", Services.GameService.CurrentQuestPhase, Services.GameService.GetPartySize(Services.GameService.CurrentQuestPhase)); }
        }

        public string VoteTrackMessage
        {
            get { return string.Format("Vote Track: {0}/{1}", Services.GameService.VoteTrack, GlobalConstants.MaxVoteTrack); }
        }

        public string NextStateBtnText
        {
            get
            {
                switch (Services.GameService.CurrentGameState)
                {
                    case GameState.PartySelection:
                        return "Propose Party!";
                    case GameState.PartyVoting:
                        return "Confirm Votes!";
                }
                return null;
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
