//-------------------------------------------------------------------------
// Copyright © 2019 Province of British Columbia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-------------------------------------------------------------------------
namespace HealthGateway.Admin.Client.Theme
{
    using MudBlazor;
    using MudBlazor.Utilities;

    /// <summary>
    /// Provides settings for the Health Gateway Light theme.
    /// </summary>
    public class LightTheme : HgTheme
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightTheme"/> class.
        /// </summary>
        public LightTheme()
        {
            this.Palette = new()
            {
                Black = Colors.Shades.Black,
                White = Colors.Shades.White,
                Primary = "#003366",
                PrimaryContrastText = Colors.Shades.White,
                Secondary = "#FCBA19",
                SecondaryContrastText = Colors.Shades.Black,
                Tertiary = "#007BFF",
                TertiaryContrastText = Colors.Shades.White,
                Info = "#17A2B8",
                InfoContrastText = Colors.Shades.White,
                Success = "#2E8540",
                SuccessContrastText = Colors.Shades.White,
                Warning = "#FAA500",
                WarningContrastText = Colors.Shades.White,
                Error = "#D72E3D",
                ErrorContrastText = Colors.Shades.White,
                Dark = Colors.Grey.Darken3,
                DarkContrastText = Colors.Shades.White,
                TextPrimary = Colors.Grey.Darken3,
                TextSecondary = new MudColor(Colors.Shades.Black).SetAlpha(0.54).ToString(MudColorOutputFormats.RGBA),
                TextDisabled = new MudColor(Colors.Shades.Black).SetAlpha(0.38).ToString(MudColorOutputFormats.RGBA),
                ActionDefault = "#003366",
                ActionDisabled = new MudColor(Colors.Shades.Black).SetAlpha(0.26).ToString(MudColorOutputFormats.RGBA),
                ActionDisabledBackground = new MudColor(Colors.Shades.Black).SetAlpha(0.12).ToString(MudColorOutputFormats.RGBA),
                Background = Colors.Shades.White,
                BackgroundGrey = Colors.Grey.Lighten2,
                Surface = Colors.Shades.White,
                DrawerBackground = "#F2F2F2",
                DrawerText = Colors.Grey.Darken3,
                DrawerIcon = Colors.Grey.Darken2,
                AppbarBackground = "#003366",
                AppbarText = Colors.Shades.White,
                LinesDefault = new MudColor(Colors.Shades.Black).SetAlpha(0.12).ToString(MudColorOutputFormats.RGBA),
                LinesInputs = Colors.Grey.Lighten1,
                TableLines = new MudColor(Colors.Grey.Lighten2).SetAlpha(1.0).ToString(MudColorOutputFormats.RGBA),
                TableStriped = new MudColor(Colors.Shades.Black).SetAlpha(0.02).ToString(MudColorOutputFormats.RGBA),
                TableHover = new MudColor(Colors.Shades.Black).SetAlpha(0.04).ToString(MudColorOutputFormats.RGBA),
                Divider = Colors.Grey.Lighten2,
                DividerLight = new MudColor(Colors.Shades.Black).SetAlpha(0.8).ToString(MudColorOutputFormats.RGBA),
                HoverOpacity = 0.06,
                GrayDefault = Colors.Grey.Default,
                GrayLight = Colors.Grey.Lighten1,
                GrayLighter = Colors.Grey.Lighten2,
                GrayDark = Colors.Grey.Darken1,
                GrayDarker = Colors.Grey.Darken2,
                OverlayDark = new MudColor("#212121").SetAlpha(0.5).ToString(MudColorOutputFormats.RGBA),
                OverlayLight = new MudColor(Colors.Shades.White).SetAlpha(0.5).ToString(MudColorOutputFormats.RGBA),
            };
        }
    }
}
