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
using System.Windows;
using System.Windows.Media.Imaging;

namespace MinorhythmListener.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        private Minorhythm radio;
        private MediaPlayer player;
        private DispatcherTimer seakTimer;
        private bool isPlayingSeak;
        private string playImageAddress, pauseImageAddress;

        public enum State
        {
            停止中, バッファ中, 一時停止中, 再生中
        }

        #region Property

        public Minorhythm Radio
        {
            get
            {
                return radio;
            }
        }

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
                RaisePropertyChanged("MediaButtonImageAddress");
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

        #region IsShowCornersIntroduction変更通知プロパティ
        private bool _IsShowCornersIntroduction;

        public bool IsShowCornersIntroduction
        {
            get
            { return _IsShowCornersIntroduction; }
            set
            { 
                if (_IsShowCornersIntroduction == value)
                    return;
                _IsShowCornersIntroduction = value;
                if (value) IsShowThemeSongIntroduction = false;
                RaisePropertyChanged();
                RaisePropertyChanged("CornersIntroduction");
            }
        }
        #endregion

        #region IsShowThemeSongIntroduction変更通知プロパティ
        private bool _IsShowThemeSongIntroduction;

        public bool IsShowThemeSongIntroduction
        {
            get
            { return _IsShowThemeSongIntroduction; }
            set
            { 
                if (_IsShowThemeSongIntroduction == value)
                    return;
                _IsShowThemeSongIntroduction = value;
                if (value) IsShowCornersIntroduction = false;
                RaisePropertyChanged();
                RaisePropertyChanged("ThemeSongIntroduction");
            }
        }
        #endregion

        #region SeakPosition 変更通知プロパティ
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
        #endregion

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

        public string MediaButtonImageAddress
        {
            get
            {
                return PlayerState == State.再生中 ? pauseImageAddress : playImageAddress;
            }
        }

        public IReadOnlyDictionary<string, string> CornersIntroduction
        {
            get
            {
                if (IsShowCornersIntroduction) return radio.Corners;
                else return null;
            }
        }

        public IEnumerable<ThemeSong> ThemeSongIntroduction
        {
            get
            {
                if (IsShowThemeSongIntroduction) return new ThemeSong[] { radio.Opening, radio.Ending };
                else return null;
            }
        }

        #endregion

        public async void Initialize()
        {
            try
            {
                radio = await Minorhythm.Load();
            }
            catch (TypeInitializationException)
            {
                Messenger.Raise(new InformationMessage
                    ("radio minorhythm から情報を読み込めませんでした。",
                    "エラー",
                    MessageBoxImage.Error,
                    "LoadError"));
                return;
            }
            if (radio == null)
            {
                await Messenger.RaiseAsync(new InformationMessage
                    ("ネットワーク接続が無効です。",
                    "エラー",
                    MessageBoxImage.Error,
                    "NetoworkError"));
                return;
            }
            RaisePropertyChanged("Radio");
            ToggleCornersCommand.RaiseCanExecuteChanged();
            ToggleThemeSongCommand.RaiseCanExecuteChanged();

            player = new MediaPlayer();
            player.BufferingStarted += (s, e) => PlayerState = State.バッファ中;
            player.BufferingEnded += (s, e) => PlayerState = State.再生中;
            player.MediaOpened += (s, e) => IsInitializedRadio = true;
            player.MediaEnded += (s, e) =>
            {
                player.Close();
                PlayerState = State.停止中;
            };
            playImageAddress = "../Resources/Play.png";
            pauseImageAddress = "../Resources/Pause.png";
            SelectedContent = radio.Contents.First();
            seakTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100),
                                            DispatcherPriority.DataBind,
                                            (s, e) => { RaisePropertyChanged("PlayingTime"); RaisePropertyChanged("SeakPosition"); },
                                            DispatcherHelper.UIDispatcher);
            seakTimer.Start();
        }

        public void Play()
        {
            if (PlayerState == State.停止中) player.Open(PlayingContent.Address);
            player.Play();
            PlayerState = State.再生中;
        }

        public void Pause()
        {
            player.Pause();
            PlayerState = State.一時停止中;
        }

        public void PlayOrPause()
        {
            if (PlayerState == State.再生中) Pause();
            else Play();
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

        public void SelectContent(Content content)
        {
            SelectedContent = content;
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
            if (PlayerState != State.停止中)
            {
                player.Open(SelectedContent.Address);
            }
            PlayingContent = SelectedContent;
            Play();
        }
        #endregion

        #region ToggleCornersCommand
        private ViewModelCommand _ToggleCornersCommand;

        public ViewModelCommand ToggleCornersCommand
        {
            get
            {
                if (_ToggleCornersCommand == null)
                {
                    _ToggleCornersCommand = new ViewModelCommand(ToggleCorners, CanToggleCorners);
                }
                return _ToggleCornersCommand;
            }
        }

        public bool CanToggleCorners()
        {
            return radio != null;
        }

        public void ToggleCorners()
        {
            IsShowCornersIntroduction = !IsShowCornersIntroduction;
        }
        #endregion

        #region ToggleThemeSongCommand
        private ViewModelCommand _ToggleThemeSongCommand;

        public ViewModelCommand ToggleThemeSongCommand
        {
            get
            {
                if (_ToggleThemeSongCommand == null)
                {
                    _ToggleThemeSongCommand = new ViewModelCommand(ToggleThemeSong, CanToggleThemeSong);
                }
                return _ToggleThemeSongCommand;
            }
        }

        public bool CanToggleThemeSong()
        {
            return radio != null;
        }

        public void ToggleThemeSong()
        {
            IsShowThemeSongIntroduction = !IsShowThemeSongIntroduction;
        }
        #endregion
    }
}
