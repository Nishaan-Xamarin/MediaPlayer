﻿using AVFoundation;
using AVKit;
using System;
using System.ComponentModel;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using Foundation;

[assembly: Xamarin.Forms.ExportRenderer(typeof(Xamarians.MediaPlayer.VideoPlayer), typeof(Xamarians.MediaPlayer.iOS.VideoPlayerRenderer))]
namespace Xamarians.MediaPlayer.iOS
{
    public class VideoPlayerRenderer : ViewRenderer<VideoPlayer, UIView>, INativePlayer
    {
        AVPlayer _player;
        AVPlayerViewController _playerController;
        bool _prepared;

        public event EventHandler<bool> FullScreenStatusChanged;

        public static new void Init()
        {

        }

        protected override void OnElementChanged(ElementChangedEventArgs<VideoPlayer> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement != null)
                return;

            // Set Native Control
            _playerController = new AVPlayerViewController();
            _playerController.View.Frame = this.Frame;
            _playerController.ShowsPlaybackControls = true;
			AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);
            SetNativeControl(_playerController.View);
            Element.SetNativeContext(this);
            SetSource();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (VideoPlayer.SourceProperty.PropertyName.Equals(e.PropertyName))
            {
                SetSource();
            }
        }


        #region Private Methods

        private void SetSource()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Element.Source))
                    return;
                _prepared = false;
                if (_player != null)
                {
                    _player.Dispose();
                    _player = null;
                }

                AVPlayerItem playerItem = null;
                if (Element.Source.StartsWith("http://") || Element.Source.StartsWith("https://"))
                    playerItem = new AVPlayerItem(AVAsset.FromUrl(NSUrl.FromString(Element.Source)));
                else
                    playerItem = new AVPlayerItem(NSUrl.FromFilename(Element.Source));

                NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, DidVideoFinishPlaying, playerItem);
                NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.ItemFailedToPlayToEndTimeNotification, DidVideoErrorOcurred, playerItem);
                NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.NewErrorLogEntryNotification, DidVideoErrorOcurred, playerItem);
                //NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.PlaybackStalledNotification, DidVideoPrepared, playerItem);

                _player = new AVPlayer(playerItem);
                _player.ActionAtItemEnd = AVPlayerActionAtItemEnd.None;
                _playerController.Player = _player;
                _prepared = true;

                if (Element.AutoPlay)
                    _player.Play();

                if (_player.Error != null)
                {
                    Element.OnError(_playerController?.Player?.Error?.LocalizedDescription);
                }
            }
            catch (Exception e)
            {
                Element.OnError(e.Message);
            }
        }

        #endregion

        #region INativePlayer

        public int Duration
        {
            get
            {
                return _prepared ? (int)_player.CurrentItem.Duration.Seconds : 0;
            }
        }

        public int CurrentPosition
        {
            get
            {
                return _prepared ? (int)_player.CurrentItem.CurrentTime.Seconds : 0;
            }
        }

        public void DisplaySeekbar(bool value)
        {
            _playerController.ShowsPlaybackControls = value;
        }

        public bool IsSeekbarVisible
        {
            get
            {
                if (_playerController == null)
                    return false;
                return _playerController.ShowsPlaybackControls;
            }
        }

        public void Play()
        {
            if (!_prepared) return;
            _player.Play();
        }

        public void Pause()
        {
            if (!_prepared) return;
            _player.Pause();
        }

        public void Stop()
        {
            if (!_prepared) return;
            _player.Pause();
        }

        public void Seek(int seconds)
        {
            if (!_prepared) return;
            _player.Seek(CoreMedia.CMTime.FromSeconds(seconds, 0));
        }

        public void SetScreen(bool isPortrait)
        {

            //AVPlayerViewController provide by default this feature
        }

        //public void FullScreen()
        //{
        //    if (!_prepared) return;
        //    //_player.Frame = NativeView.Frame;
        //    //NativeView.Layer.AddSublayer(_player);
        //}

        //public void ExitFullScreen()
        //{
        //    if (!_prepared) return;
        //    //_player.Frame = NativeView.Frame;
        //    //NativeView.Layer.AddSublayer(_player);

        //}

        #endregion

        #region Events

        private void DidVideoFinishPlaying(NSNotification obj)
        {
            Element.OnCompletion();
        }

        private void DidVideoErrorOcurred(NSNotification obj)
        {
            Element.OnError(_player.Error?.Description ?? "Unable to play video.");
        }


        #endregion
    }
}
