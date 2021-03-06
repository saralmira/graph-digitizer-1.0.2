﻿using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;

namespace GraphDigitizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            LoadLanguage();
        }

        private void LoadLanguage()
        {
            Lang = CultureInfo.CurrentCulture.Name;
            switch(Lang)
            {
                case "en":
                case "zh-CN":
                    break;
                default:
                    Lang = "en";
                    break;
            }

            ResourceDictionary langRd = null;
            try
            {
                langRd =
                LoadComponent(
                new Uri("Resources/" + Lang + ".xaml", UriKind.Relative)) as ResourceDictionary;
            }
            catch
            {
            }
            if (langRd != null)
            {
                this.Resources.MergedDictionaries.Remove(langRd);
                this.Resources.MergedDictionaries.Insert(0, langRd);
            }
            else
                this.Shutdown(1);
        }

        public static string Lang = "en";
    }

    public static class Local
    {
        public static string Dict(string key)
        {
            return (string)Application.Current.FindResource(key);
        }
    }
}
