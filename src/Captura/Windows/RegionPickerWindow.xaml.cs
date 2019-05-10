﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Captura.Models;
using Screna;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace Captura
{
    public partial class RegionPickerWindow
    {
        readonly IWindow[] _windows;
        readonly IPlatformServices _platformServices;

        Predicate<IWindow> Predicate { get; set; }

        RegionPickerWindow()
        {
            InitializeComponent();

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            UpdateBackground();

            _platformServices = ServiceProvider.Get<IPlatformServices>();

            _windows = _platformServices.EnumerateWindows().ToArray();
        }

        void UpdateBackground()
        {
            using (var bmp = ScreenShot.Capture())
            {
                var stream = new MemoryStream();
                bmp.Save(stream, ImageFormats.Png);

                stream.Seek(0, SeekOrigin.Begin);

                var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                BgImg.Source = decoder.Frames[0];
            }
        }

        void CloseClick(object Sender, RoutedEventArgs E)
        {
            _start = _end = null;

            Close();
        }

        void UpdateSizeDisplay(Rect? Rect)
        {
            if (Rect == null)
            {
                SizeText.Visibility = Visibility.Collapsed;
            }
            else
            {
                var rect = Rect.Value;

                SizeText.Text = $"{(int) rect.Width} x {(int) rect.Height}";

                SizeText.Margin = new Thickness(rect.Left + rect.Width / 2 - SizeText.ActualWidth / 2, rect.Top + rect.Height / 2 - SizeText.ActualHeight / 2, 0, 0);

                SizeText.Visibility = Visibility.Visible;
            }
        }

        void WindowMouseMove(object Sender, MouseEventArgs E)
        {
            if (_isDragging)
            {
                _end = E.GetPosition(Grid);

                var r = GetRegion();

                UpdateSizeDisplay(r);

                if (r == null)
                {
                    Unhighlight();
                    return;
                }

                HighlightRegion(r.Value);
            }
            else
            {
                var point = _platformServices.CursorPosition;

                _selectedWindow = _windows
                        .Where(M => Predicate?.Invoke(M) ?? true)
                        .FirstOrDefault(M => M.Rectangle.Contains(point));

                if (_selectedWindow == null)
                {
                    Unhighlight();
                }
                else
                {
                    var rect = GetSelectedWindowRectangle().Value;

                    HighlightRegion(rect);
                }
            }
        }

        Rect? GetSelectedWindowRectangle()
        {
            if (_selectedWindow == null)
                return null;

            var rect = _selectedWindow.Rectangle;

            return new Rect(-Left + rect.X / Dpi.X,
                -Top + rect.Y / Dpi.Y,
                rect.Width / Dpi.X,
                rect.Height / Dpi.Y);
        }

        bool _isDragging;
        Point? _start, _end;
        IWindow _selectedWindow;

        void WindowMouseLeftButtonDown(object Sender, MouseButtonEventArgs E)
        {
            _isDragging = true;
            _start = E.GetPosition(Grid);
            _end = null;
        }

        void WindowMouseLeftButtonUp(object Sender, MouseButtonEventArgs E)
        {
            var current = E.GetPosition(Grid);

            if (current != _start)
            {
                _end = E.GetPosition(Grid);
            }
            else if (GetSelectedWindowRectangle() is Rect rect)
            {
                _start = rect.Location;
                _end = new Point(rect.Right, rect.Bottom);
            }

            Close();
        }

        Rect? GetRegion()
        {
            if (_start == null || _end == null)
            {
                return null;
            }

            var end = _end.Value;
            var start = _start.Value;

            if (end.X < start.X)
            {
                var t = start.X;
                start.X = end.X;
                end.X = t;
            }

            if (end.Y < start.Y)
            {
                var t = start.Y;
                start.Y = end.Y;
                end.Y = t;
            }

            var width = end.X - start.X;
            var height = end.Y - start.Y;

            if (width < 0.01 || height < 0.01)
            {
                return null;
            }

            return new Rect(start.X, start.Y, width, height);
        }

        Rectangle? GetRegionScaled()
        {
            var rect = GetRegion();

            if (rect == null)
            {
                return null;
            }

            var r = rect.Value;

            return new Rectangle((int) ((Left + r.X) * Dpi.X),
                (int)((Top + r.Y) * Dpi.Y),
                (int)(r.Width * Dpi.X),
                (int)(r.Height * Dpi.Y));
        }

        public static Rectangle? PickRegion()
        {
            var picker = new RegionPickerWindow();

            picker.ShowDialog();

            return picker.GetRegionScaled();
        }

        void Unhighlight()
        {
            Border.Visibility
                = BorderTop.Visibility
                = BorderBottom.Visibility
                = BorderLeft.Visibility
                = BorderRight.Visibility
                = Visibility.Collapsed;
        }

        void HighlightRegion(Rect Region)
        {
            Border.Margin = new Thickness(Region.X, Region.Y, 0, 0);
            Border.Width = Region.Width;
            Border.Height = Region.Height;

            BorderTop.Margin = new Thickness();
            BorderTop.Width = Width;
            BorderTop.Height = Region.Top.Clip(0, Height);

            BorderBottom.Margin = new Thickness(0, Region.Bottom, 0, 0);
            BorderBottom.Width = Width;
            BorderBottom.Height = (Height - Region.Bottom).Clip(0, Height);

            BorderLeft.Margin = new Thickness(0, Region.Top, 0, 0);
            BorderLeft.Width = Region.Left.Clip(0, Width);
            BorderLeft.Height = Region.Height;

            BorderRight.Margin = new Thickness(Region.Right, Region.Top, 0, 0);
            BorderRight.Width = (Width - Region.Right).Clip(0, Width);
            BorderRight.Height = Region.Height;

            Border.Visibility
                = BorderTop.Visibility
                = BorderBottom.Visibility
                = BorderLeft.Visibility
                = BorderRight.Visibility
                = Visibility.Visible;
        }
    }
}
