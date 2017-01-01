﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework;
using System.Threading;

namespace Evolution
{
    public partial class Help : PhoneApplicationPage
    {
        static Uri Uri = new Uri("/Help.xaml", UriKind.Relative);
        private ConfigManager configmanager;
        private GameTimer timer_move, timer_reposition, timer_gesture_checker, timer_hand_animation;
        private double movedemo_velocity;
        private int vxSign, vySign;
        private Vector2 cell_originalpos;
        private Image demoPlayer;
        private const double SLIDE_SPEED = 7.5;
        private const double MOVE_SPEED = 5;
        private int handAnimationLength;

        public static Uri GetUri()
        {
            return Uri;
        }

        public Help()
        {
            InitializeComponent();
            configmanager = ConfigManager.GetInstance;
            bool showtutorial = true;
            bool.TryParse(configmanager.ReadConfig(ConfigKeys.ShowTutorial), out showtutorial);
            check_showtutorial.Checked -= check_showtutorial_Checked;
            check_showtutorial.Unchecked -= check_showtutorial_Unchecked;
            check_showtutorial.IsChecked = showtutorial;
            check_showtutorial.Checked += check_showtutorial_Checked;
            check_showtutorial.Unchecked += check_showtutorial_Unchecked;
            panel_objectinfo.Visibility = Visibility.Collapsed;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {            
            TouchPanel.EnabledGestures = GestureType.Flick;
            timer_gesture_checker = new GameTimer();
            timer_gesture_checker.Update += timer_gesture_checker_Update;
            timer_gesture_checker.Start();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (timer_gesture_checker != null)
            {
                timer_gesture_checker.Stop();
                timer_gesture_checker = null;
            }
            base.OnNavigatedFrom(e);
        }

        private void btn_help1_back_Click(object sender, RoutedEventArgs e)
        {
            HideObjectInfo();
            NavigationService.GoBack();
        }

        private void btn_help1_next_Click(object sender, RoutedEventArgs e)
        {
            HideObjectInfo();
            Help1.Visibility = Visibility.Collapsed;
            Help2.Visibility = Visibility.Visible;
        }

        private void btn_help2_back_Click(object sender, RoutedEventArgs e)
        {
            Help2.Visibility = Visibility.Collapsed;
            Help1.Visibility = Visibility.Visible;
        }

        private void btn_help2_next_Click(object sender, RoutedEventArgs e)
        {
            Help2.Visibility = Visibility.Collapsed;
            Help3.Visibility = Visibility.Visible;
            cell_originalpos = new Vector2((float)Canvas.GetLeft(movedemo_player), (float)Canvas.GetTop(movedemo_player));
        }

        private void btn_help3_back_Click(object sender, RoutedEventArgs e)
        {
            Help3.Visibility = Visibility.Collapsed;
            Help2.Visibility = Visibility.Visible;
        }

        private void btn_help3_next_Click(object sender, RoutedEventArgs e)
        {
            cell_originalpos = new Vector2((float)Canvas.GetLeft(player_bounceback), (float)Canvas.GetTop(player_bounceback));
            Help3.Visibility = Visibility.Collapsed;
            Help4.Visibility = Visibility.Visible;
        }

        private void btn_help4_back_Click(object sender, RoutedEventArgs e)
        {
            cell_originalpos = new Vector2((float)Canvas.GetLeft(movedemo_player), (float)Canvas.GetTop(movedemo_player));
            Help4.Visibility = Visibility.Collapsed;
            Help3.Visibility = Visibility.Visible;
        }

        private void btn_help4_okay_Click(object sender, RoutedEventArgs e)
        {
            if (MainPage.startedWithTutorial)
            {
                NavigationService.Navigate(GamePage.GetUri());
            }
            else NavigationService.GoBack();
        }

        private void check_showtutorial_Checked(object sender, RoutedEventArgs e)
        {
            configmanager.WriteConfig(ConfigKeys.ShowTutorial, "true");
        }

        private void check_showtutorial_Unchecked(object sender, RoutedEventArgs e)
        {
            configmanager.WriteConfig(ConfigKeys.ShowTutorial, "false");
        }

        private void panel_objectinfo_Tap(object sender, GestureEventArgs e)
        {
            HideObjectInfo();
        }

        private void HideObjectInfo()
        {
            panel_objectinfo.Visibility = Visibility.Collapsed;
        }

        private void SetObjectInfo(String ResourceName)
        {
            text_info_smallerenemy.Inlines.Clear();
            Span textSpan = (Span)this.Resources[ResourceName];
            Inline newInline;
            foreach (Inline inline in textSpan.Inlines)
            {
                if (inline is Run)
                {
                    newInline = new Run();
                    (newInline as Run).Text = (inline as Run).Text;
                }
                else if (inline is Bold)
                {
                    newInline = new Bold();
                    String text = ((inline as Bold).Inlines[0] as Run).Text;
                    (newInline as Bold).Inlines.Add(text);
                }
                else newInline = new Run();
                text_info_smallerenemy.Inlines.Add(newInline);
            }
            panel_objectinfo.Visibility = Visibility.Visible;
        }

        private void infostack_Tap(object sender, GestureEventArgs e)
        {
            string name = (sender as StackPanel).Name;
            string resource = name.Replace("stack", "info");
            SetObjectInfo(resource);
        }

        private void Ellipse_Tap(object sender, GestureEventArgs e)
        {
            MoveDemonstration(MOVE_SPEED);
        }

        private void MoveDemonstration(double StartVelocity)
        {
            demoPlayer = movedemo_player;
            if (timer_move != null)
            {
                timer_move.Stop();
                timer_move = null;
            }
            RepositionPlayerCell();
            arrow_direction.Visibility = Visibility.Collapsed;
            timer_move = new GameTimer();
            timer_move.Update += timer_move_Update;
            movedemo_velocity = StartVelocity;
            timer_move.Start();
        }

        void timer_move_Update(object sender, GameTimerEventArgs e)
        {
            if (movedemo_velocity > 0)
            {
                Canvas.SetLeft(movedemo_player, Canvas.GetLeft(movedemo_player) - movedemo_velocity);
                Canvas.SetTop(movedemo_player, Canvas.GetTop(movedemo_player) - movedemo_velocity);
                movedemo_velocity -= 0.2;
            }
            else
            {
                timer_move.Stop();
                timer_reposition = new GameTimer();
                timer_reposition.UpdateInterval = TimeSpan.FromMilliseconds(500);
                timer_reposition.Update += timer_reposition_Update;
                timer_reposition.Start();
            }
        }

        void timer_bounceback_Update(object sender, GameTimerEventArgs e)
        {
            if (movedemo_velocity > 0)
            {
                double x = Canvas.GetLeft(player_bounceback);
                double y = Canvas.GetTop(player_bounceback);
                if (y + player_bounceback.ActualHeight + movedemo_velocity * vySign > canvas_bounceback.ActualHeight) vySign *= -1;
                Canvas.SetLeft(player_bounceback, x + movedemo_velocity * vxSign);
                Canvas.SetTop(player_bounceback, y + movedemo_velocity * vySign);
                movedemo_velocity -= 0.2;
            }
            else
            {
                timer_move.Stop();
                timer_reposition = new GameTimer();
                timer_reposition.UpdateInterval = TimeSpan.FromMilliseconds(500);
                timer_reposition.Update += timer_reposition_Update;
                timer_reposition.Start();
            }
        }

        void timer_reposition_Update(object sender, GameTimerEventArgs e)
        {
            timer_reposition.Stop();
            arrow_direction.Visibility = Visibility.Visible;
            RepositionPlayerCell();
        }

        void timer_handanimation_Update(object sender, GameTimerEventArgs e)
        {
            if (handAnimationLength > 0)
            {
                double x = Canvas.GetLeft(hand_bounceback);
                double y = Canvas.GetTop(hand_bounceback);
                Canvas.SetLeft(hand_bounceback, x + 0.5);
                Canvas.SetTop(hand_bounceback, y -1);
                if (handAnimationLength == 100) BounceBackDemonstration(SLIDE_SPEED);
                handAnimationLength -= 1;
            }
            else
            {
                timer_hand_animation.Stop();
                Canvas.SetLeft(hand_bounceback, 472);
                Canvas.SetTop(hand_bounceback, 320);
            }
        }

        void timer_gesture_checker_Update(object sender, GameTimerEventArgs e)
        {
            BoostDemonstration();
        }

        private void RepositionPlayerCell()
        {
            Canvas.SetLeft(demoPlayer, cell_originalpos.X);
            Canvas.SetTop(demoPlayer, cell_originalpos.Y);
        }

        /// <summary>
        /// TODO: precise flickorigin is needed
        /// </summary>
        /// <param name="FlickOrigin"></param>
        /// <returns></returns>
        private bool FlickedFromRedPosition(Vector2 FlickOrigin)
        {
            return Utility.CircleContainsPosition(new Vector2((float)Canvas.GetLeft(ellipse_tap), (float)Canvas.GetTop(ellipse_tap)), ellipse_tap.ActualWidth, FlickOrigin);
        }

        private void BoostDemonstration()
        {
            if (TouchPanel.IsGestureAvailable)
            {
                GestureSample gesture = TouchPanel.ReadGesture();
                switch (gesture.GestureType)
                {
                    case GestureType.Flick:
                    MoveDemonstration(SLIDE_SPEED);
                    break;
                }
            }
        }

        private void Canvas_Tap(object sender, GestureEventArgs e)
        {
            AnimateHand();            
        }

        private void AnimateHand()
        {
            handAnimationLength = 150;
            timer_hand_animation = new GameTimer();
            timer_hand_animation.Update += timer_handanimation_Update;
            timer_hand_animation.UpdateInterval = TimeSpan.FromMilliseconds(3);
            timer_hand_animation.Start();
        }

        private void BounceBackDemonstration(double StartVelocity)
        {
            demoPlayer = player_bounceback;
            if (timer_move != null)
            {
                timer_move.Stop();
                timer_move = null;
            }
            vxSign = -1;
            vySign = 1;
            RepositionPlayerCell();
            timer_move = new GameTimer();
            timer_move.Update += timer_bounceback_Update;
            movedemo_velocity = StartVelocity;
            timer_move.Start();
        }
    }
}