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

namespace Evolution
{
    public partial class HighScores : PhoneApplicationPage
    {
        static Uri Uri = new Uri("/HighScores.xaml", UriKind.Relative);
        public static Uri GetUri()
        {
            return Uri;
        }

        public HighScores()
        {
            InitializeComponent();
            int.TryParse(ConfigManager.GetInstance.ReadConfig(ConfigKeys.HighScore), out hScore);
            int.TryParse(ConfigManager.GetInstance.ReadConfig(ConfigKeys.MaxLevel), out hLevel);
            int.TryParse(ConfigManager.GetInstance.ReadConfig(ConfigKeys.LastLevel), out lastLevel);
            txt_hl.Text = hLevel.ToString();
            txt_hs.Text = hScore.ToString();
            txt_lastlevel.Text = lastLevel.ToString();
        }

        private static int hScore = 0;
        private static int hLevel = 0;
        private static int lastLevel = 1;

        public static void ResetLastLevel()
        {
            lastLevel = 1;
            ConfigManager.GetInstance.WriteConfig(ConfigKeys.LastLevel, "1");
        }

        public static void SetHighScore(int Value)
        {
            if (Value > hScore)
            {
                hScore = Value;
                ConfigManager.GetInstance.WriteConfig(ConfigKeys.HighScore, Value.ToString());
            }
        }

        public static void SetMaxLevel(int Value)
        {
            if (Value > hLevel)
            {
                hLevel = Value;
                ConfigManager.GetInstance.WriteConfig(ConfigKeys.MaxLevel, Value.ToString());
            }
        }

        public static void SetLastLevel(int Value)
        {
            if (Value > lastLevel)
            {
                lastLevel = Value;
                ConfigManager.GetInstance.WriteConfig(ConfigKeys.LastLevel, Value.ToString());
            }
        }


    }
}