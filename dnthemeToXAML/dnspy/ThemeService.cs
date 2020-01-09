/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using dnSpy.Contracts.Themes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows;
using System.Xml.Linq;

namespace dnSpy.Themes
{
    sealed class ThemeService
    {
        readonly Dictionary<Guid, Theme> themes;

        public ITheme Theme
        {
            get => theme;
            set
            {
                if (theme != value)
                {
                    theme = value;
                    InitializeResources();
                }
            }
        }
        ITheme theme;

        public IEnumerable<ITheme> AllThemes => themes.Values.OrderBy(x => x.Order);

        public IEnumerable<ITheme> VisibleThemes
        {
            get
            {
                foreach (var theme in AllThemes)
                {
                    if (!IsHighContrast && theme.IsHighContrast)
                        continue;
                    yield return theme;
                }
            }
        }

        public bool IsHighContrast
        {
            get => isHighContrast;
            set
            {
                if (isHighContrast != value)
                {
                    isHighContrast = value;
                    SwitchThemeIfNecessary();
                }
            }
        }
        bool isHighContrast;


        public ThemeService()
        {
            themes = new Dictionary<Guid, Theme>();
            Load();
            Debug.Assert(themes.Count != 0);
            SystemEvents.UserPreferenceChanged += (s, e) => IsHighContrast = SystemParameters.HighContrast;
            IsHighContrast = SystemParameters.HighContrast;
            Initialize(DefaultThemeGuid);
        }

        void InitializeResources()
        {
            //var app = Application.Current;
            //Debug.Assert(app != null);
            //if (app != null)
            //    ((Theme)Theme).UpdateResources(app.Resources);
        }

        Guid CurrentDefaultThemeGuid => IsHighContrast ? DefaultHighContrastThemeGuid : DefaultThemeGuid;
        static readonly Guid DefaultThemeGuid = ThemeConstants.THEME_BLUE_GUID;
        static readonly Guid DefaultHighContrastThemeGuid = ThemeConstants.THEME_HIGHCONTRAST_GUID;

        ITheme GetThemeOrDefault(Guid guid)
        {
            if (themes.TryGetValue(guid, out var theme))
                return theme;
            if (themes.TryGetValue(DefaultThemeGuid, out theme))
                return theme;
            return AllThemes.FirstOrDefault();
        }

        void SwitchThemeIfNecessary()
        {
            if (Theme == null || Theme.IsHighContrast != IsHighContrast)
                Theme = GetThemeOrDefault(CurrentDefaultThemeGuid);
        }

        void Load()
        {
            foreach (var basePath in GetDnthemePaths())
            {
                string[] files;
                try
                {
                    if (!Directory.Exists(basePath))
                        continue;
                    files = Directory.GetFiles(basePath, "*.dntheme", SearchOption.TopDirectoryOnly);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (SecurityException)
                {
                    continue;
                }

                foreach (var filename in files)
                    Load(filename);
            }
        }

        IEnumerable<string> GetDnthemePaths() => GetDirectories("Themes");

        Theme Load(string filename)
        {
            try
            {
                var root = XDocument.Load(filename).Root;
                if (root.Name != "theme")
                    return null;

                var theme = new Theme(root);
                if (string.IsNullOrEmpty(theme.MenuName))
                    return null;

                themes[theme.Guid] = theme;
                return theme;
            }
            catch (Exception)
            {
                Debug.Fail($"Failed to load file '{filename}'");
            }
            return null;
        }

        void Initialize(Guid themeGuid)
        {
            var theme = GetThemeOrDefault(themeGuid);
            if (theme.IsHighContrast != IsHighContrast)
                theme = GetThemeOrDefault(CurrentDefaultThemeGuid) ?? theme;
            Theme = theme;
        }

        public static string BinDirectory = AppDomain.CurrentDomain.BaseDirectory;
        /// <summary>
        /// Returns directories relative to <see cref="BinDirectory"/> and <see cref="DataDirectory"/>
        /// in that order. If they're identical, only one path is returned.
        /// </summary>
        /// <param name="subDir">Sub directory</param>
        /// <returns></returns>
        public static IEnumerable<string> GetDirectories(string subDir)
        {
            yield return Path.Combine(BinDirectory, subDir);
        }
    }
}
