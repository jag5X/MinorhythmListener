using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using MinorhythmListener.Models;

namespace MinorhythmListener.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        private Minorhythm radio;
        private MediaPlayer player;
        private DispatcherTimer seakTimer;
        private bool isPlayingSeak;

        public enum State
        {
            停止中, バッファ中, 一時停止中, 再生中
        }

        #region Property

        #region PlayingContent変更通知プロパティ
        private Content _PlayingContent;

        public Content PlayingContent
        {
            get
            { return _PlayingContent; }
            set
            {
                if (_PlayingContent == value)
                    return;
                _PlayingContent = value;
                RaisePropertyChanged();
                RaisePropertyChanged("TotalTime");
                PlayRadioCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region SelectedContent変更通知プロパティ
        private Content _SelectedContent;

        public Content SelectedContent
        {
            get
            { return _SelectedContent; }
            set
            {
                if (_SelectedContent == value)
                    return;
                _SelectedContent = value;
                RaisePropertyChanged();
                PlayRadioCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region PlayerState変更通知プロパティ
        private State _PlayerState;

        public State PlayerState
        {
            get
            { return _PlayerState; }
            set
            { 
                if (_PlayerState == value)
                    return;
                _PlayerState = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region IsInitializedRadio変更通知プロパティ
        private bool _IsInitializedRadio;

        public bool IsInitializedRadio
        {
            get
            { return _IsInitializedRadio; }
            set
            {
                _IsInitializedRadio = value;
                SeakPosition = 0;
                RaisePropertyChanged();
                RaisePropertyChanged("SeakMaximum");
                RaisePropertyChanged("TotalTime");
            }
        }
        #endregion

        public Minorhythm Radio
        {
            get
            {
                return radio;
            }
        }

        public double SeakPosition
        {
            get
            {
                return player.Position.TotalSeconds;
            }
            set
            {
                if (player.Position.TotalSeconds == value)
                    return;
                player.Position = TimeSpan.FromSeconds(value);
                RaisePropertyChanged();
            }
        }

        public double SeakMaximum
        {
            get
            {
                return player.NaturalDuration.TimeSpan.TotalSeconds;
            }
        }

        public string PlayingTime
        {
            get
            {
                return player.Position.ToString(@"mm\:ss");
            }
        }

        public string TotalTime
        {
            get
            {
                return player.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
            }
        }

        #endregion

        public async void Initialize()
        {
            radio = await Minorhythm.Load();
            RaisePropertyChanged("Radio");
            player = new MediaPlayer();
            player.BufferingStarted += (s, e) => PlayerState = State.バッファ中;
            player.BufferingEnded += (s, e) => PlayerState = State.再生中;
            player.MediaOpened += (s, e) => IsInitializedRadio = true;
            player.MediaEnded += (s, e) => PlayerState = State.停止中;
            SelectedContent = radio.Contents.First();
            seakTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100),
                                            DispatcherPriority.DataBind,
                                            (s, e) => { RaisePropertyChanged("PlayingTime"); RaisePropertyChanged("SeakPosition"); },
                                            DispatcherHelper.UIDispatcher);
            seakTimer.Start();
        }

        public void SelectContent(Content content)
        {
            SelectedContent = content;
        }

        public void StartSeak()
        {
            isPlayingSeak = PlayerState == State.再生中;
            player.Pause();
        }

        public void EndSeak()
        {
            if (isPlayingSeak)
            {
                player.Play();
            }
        }

        #region PlayRadioCommand
        private ViewModelCommand _PlayRadioCommand;

        public ViewModelCommand PlayRadioCommand
        {
            get
            {
                if (_PlayRadioCommand == null)
                {
                    _PlayRadioCommand = new ViewModelCommand(PlayRadio, CanPlayRadio);
                }
                return _PlayRadioCommand;
            }
        }

        public bool CanPlayRadio()
        {
            return radio != null && (PlayerState == State.停止中 || SelectedContent != PlayingContent);
        }

        public void PlayRadio()
        {
            player.Close();
            player.Open(SelectedContent.Address);
            player.Play();
            PlayingContent = SelectedContent;
            PlayerState = State.再生中;
        }
        #endregion
    }
}
