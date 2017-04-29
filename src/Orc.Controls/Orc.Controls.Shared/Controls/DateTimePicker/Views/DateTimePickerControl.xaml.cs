﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimePickerControl.xaml.cs" company="WildGums">
//   Copyright (c) 2008 - 2017 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using Catel.MVVM.Views;
    using Catel.Reflection;
    using Catel.Windows.Threading;
    using Calendar = System.Windows.Controls.Calendar;
    using Converters;

    /// <summary>
    /// Interaction logic for DateTimePickerControl.xaml
    /// </summary>
    public partial class DateTimePickerControl
    {
        #region Fields
        private readonly List<TextBox> _textBoxes;
        private DateTimePart _activeDateTimePart;
        private DateTime _todayValue;
        private DateTimeFormatInfo _formatInfo;
        #endregion

        #region Constructors
        static DateTimePickerControl()
        {
            typeof(DateTimePickerControl).AutoDetectViewPropertiesToSubscribe();

            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(DateTimePickerControl), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
        }

        public DateTimePickerControl()
        {
            InitializeComponent();

            _textBoxes = new List<TextBox>()
            {
                NumericTBDay,
                NumericTBMonth,
                NumericTBYear,
                NumericTBHour,
                NumericTBMinute,
                NumericTBSecond,
                ListTBAmPm
            };

            DateTime now = DateTime.Now;
            _todayValue = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
        }

        private void SubscribeNumericTextBoxes()
        {
            // Enable support for switching between textboxes,
            // 0-5 because we can't switch to right on last textbox.
            for (int i = 0; i <= 5; i++)
            {
                _textBoxes[i].SubscribeToOnRightBoundReachedEvent(OnTextBoxRightBoundReached);
            }

            // Enable support for switching between textboxes,
            // 5-1 because we can't switch to left on first textbox.
            for (int i = 6; i >= 1; i--)
            {
                _textBoxes[i].SubscribeToOnLeftBoundReachedEvent(OnTextBoxLeftBoundReached);
            }
        }

        private void UnsubscribeNumericTextBoxes()
        {
            // Disable support for switching between textboxes,
            // 0-4 because we can't switch to right on last textbox.
            for (int i = 0; i <= 5; i++)
            {
                _textBoxes[i].UnsubscribeFromOnRightBoundReachedEvent(OnTextBoxRightBoundReached);
            }

            // Disable support for switching between textboxes,
            // 5-1 because we can't switch to left on first textbox.
            for (int i = 6; i >= 1; i--)
            {
                _textBoxes[i].UnsubscribeFromOnLeftBoundReachedEvent(OnTextBoxLeftBoundReached);
            }
        }
        #endregion

        #region Properties
        [ViewToViewModel(MappingType = ViewToViewModelMappingType.TwoWayViewWins)]
        public DateTime? Value
        {
            get { return (DateTime?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(DateTime?),
            typeof(DateTimePickerControl), new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (sender, e) => ((DateTimePickerControl)sender).OnValueChanged(e.OldValue, e.NewValue)));

        [ViewToViewModel(MappingType = ViewToViewModelMappingType.TwoWayViewWins)]
        public bool ShowOptionsButton
        {
            get { return (bool)GetValue(ShowOptionsButtonProperty); }
            set { SetValue(ShowOptionsButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowOptionsButtonProperty = DependencyProperty.Register("ShowOptionsButton", typeof(bool),
            typeof(DateTimePickerControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Brush AccentColorBrush
        {
            get { return (Brush)GetValue(AccentColorBrushProperty); }
            set { SetValue(AccentColorBrushProperty, value); }
        }

        public static readonly DependencyProperty AccentColorBrushProperty = DependencyProperty.Register("AccentColorBrush", typeof(Brush),
            typeof(DateTimePickerControl), new FrameworkPropertyMetadata(Brushes.LightGray, (sender, e) => ((DateTimePickerControl)sender).OnAccentColorBrushChanged()));

        public bool AllowNull
        {
            get { return (bool)GetValue(AllowNullProperty); }
            set { SetValue(AllowNullProperty, value); }
        }

        public static readonly DependencyProperty AllowNullProperty = DependencyProperty.Register("AllowNull", typeof(bool),
            typeof(DateTimePickerControl), new PropertyMetadata(false));

        public bool AllowCopyPaste
        {
            get { return (bool)GetValue(AllowCopyPasteProperty); }
            set { SetValue(AllowCopyPasteProperty, value); }
        }

        public static readonly DependencyProperty AllowCopyPasteProperty = DependencyProperty.Register("AllowCopyPaste", typeof(bool),
            typeof(DateTimePickerControl), new PropertyMetadata(true));

        [ViewToViewModel(MappingType = ViewToViewModelMappingType.TwoWayViewWins)]
        public bool HideTime
        {
            get { return (bool)GetValue(HideTimeProperty); }
            set { SetValue(HideTimeProperty, value); }
        }

        public static readonly DependencyProperty HideTimeProperty = DependencyProperty.Register("HideTime", typeof(bool),
            typeof(DateTimePickerControl), new FrameworkPropertyMetadata(false, (sender, e) => ((DateTimePickerControl)sender).OnHideTimeChanged()));

        [ViewToViewModel(MappingType = ViewToViewModelMappingType.TwoWayViewWins)]
        public bool HideSeconds
        {
            get { return (bool)GetValue(HideSecondsProperty); }
            set { SetValue(HideSecondsProperty, value); }
        }

        public static readonly DependencyProperty HideSecondsProperty = DependencyProperty.Register("HideSeconds", typeof(bool),
            typeof(DateTimePickerControl), new FrameworkPropertyMetadata(false, (sender, e) => ((DateTimePickerControl)sender).OnHideSecondsChanged()));

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool),
            typeof(DateTimePickerControl), new PropertyMetadata(false));

        public string Format
        {
            get { return (string)GetValue(FormatProperty); }
            set { SetValue(FormatProperty, value); }
        }

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register("Format", typeof(string),
            typeof(DateTimePickerControl), new FrameworkPropertyMetadata(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern, (sender, e) => ((DateTimePickerControl)sender).OnFormatChanged()));

        public bool IsYearShortFormat
        {
            get { return (bool)GetValue(IsYearShortFormatProperty); }
            private set { SetValue(IsYearShortFormatPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsYearShortFormatPropertyKey = DependencyProperty.RegisterReadOnly("IsYearShortFormat", typeof(bool),
            typeof(DateTimePickerControl), new PropertyMetadata(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Count(x => x == 'y') < 3 ? true : false));

        public static readonly DependencyProperty IsYearShortFormatProperty = IsYearShortFormatPropertyKey.DependencyProperty;

        public bool IsHour12Format
        {
            get { return (bool)GetValue(IsHour12FormatProperty); }
            private set { SetValue(IsHour12FormatPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsHour12FormatPropertyKey = DependencyProperty.RegisterReadOnly("IsHour12Format", typeof(bool),
            typeof(DateTimePickerControl), new PropertyMetadata(CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern.Contains("h") ? true : false));

        public static readonly DependencyProperty IsHour12FormatProperty = IsHour12FormatPropertyKey.DependencyProperty;

        public bool IsAmPmShortFormat
        {
            get { return (bool)GetValue(IsAmPmShortFormatProperty); }
            private set { SetValue(IsAmPmShortFormatPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsAmPmShortFormatPropertyKey = DependencyProperty.RegisterReadOnly("IsAmPmShortFormat", typeof(bool),
            typeof(DateTimePickerControl), new PropertyMetadata(CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern.Count(x => x == 't') < 2 ? true : false));

        public static readonly DependencyProperty IsAmPmShortFormatProperty = IsAmPmShortFormatPropertyKey.DependencyProperty;
        #endregion

        #region Methods
        private void OnTextBoxLeftBoundReached(object sender, EventArgs e)
        {
            var currentTextBoxIndex = _textBoxes.IndexOf((TextBox)sender);
            var prevTextBox = _textBoxes[currentTextBoxIndex - 1];

            prevTextBox.CaretIndex = prevTextBox.Text.Length;
            prevTextBox.Focus();
        }

        private void OnTextBoxRightBoundReached(object sender, EventArgs eventArgs)
        {
            var currentTextBoxIndex = _textBoxes.IndexOf((TextBox)sender);
            var nextTextBox = _textBoxes[currentTextBoxIndex + 1];

            nextTextBox.CaretIndex = 0;
            nextTextBox.Focus();
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            _activeDateTimePart = (DateTimePart)((ToggleButton)sender).Tag;

            var activeTextBox = (TextBox)FindName(_activeDateTimePart.GetDateTimePartName());
            var activeToggleButton = (ToggleButton)FindName(_activeDateTimePart.GetDateTimePartToggleButtonName());

            var dateTime = Value == null ? _todayValue : Value.Value;
            var dateTimePartHelper = new DateTimePartHelper(dateTime, _activeDateTimePart, _formatInfo, activeTextBox, activeToggleButton);
            dateTimePartHelper.CreatePopup();
        }

        private void NumericTBMonth_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var dateTime = Value == null ? _todayValue : Value.Value;
            var daysInMonth = DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
            NumericTBDay.SetCurrentValue(NumericTextBox.MaxValueProperty, (double)daysInMonth);
        }

        private Calendar CreateCalendarPopupSource()
        {
            var dateTime = Value == null ? _todayValue : Value.Value;
            var calendar = new Calendar()
            {
                Margin = new Thickness(0, -3, 0, -3),
                DisplayDate = dateTime,
                SelectedDate = Value
            };

            calendar.PreviewKeyDown += CalendarOnPreviewKeyDown;
            calendar.SelectedDatesChanged += CalendarOnSelectedDatesChanged;

            return calendar;
        }

        private void CalendarOnSelectedDatesChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            var calendar = (((Calendar)sender));
            if (calendar.SelectedDate.HasValue)
            {
                UpdateDate(calendar.SelectedDate.Value);
            }

            ((Popup)calendar.Parent).SetCurrentValue(Popup.IsOpenProperty, false);
        }

        private void CalendarOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var calendar = ((Calendar)sender);
            if (e.Key == Key.Escape)
            {
                ((Popup)calendar.Parent).SetCurrentValue(Popup.IsOpenProperty, false);
                NumericTBDay.Focus();
                e.Handled = true;
            }

            if (e.Key == Key.Enter)
            {
                if (calendar.SelectedDate.HasValue)
                {
                    UpdateDate(calendar.SelectedDate.Value);
                }

                ((Popup)calendar.Parent).SetCurrentValue(Popup.IsOpenProperty, false);

                e.Handled = true;
            }
        }

        private void UpdateDate(DateTime date)
        {
            var dateTime = Value == null ? _todayValue : Value.Value;
            SetCurrentValue(ValueProperty, new DateTime(date.Year, date.Month, date.Day, dateTime.Hour, dateTime.Minute, dateTime.Second));
        }

        private void UpdateDateTime(DateTime? dateTime)
        {
            var value = dateTime;
            if (dateTime != null)
            {
                value = new DateTime(dateTime.Value.Year, dateTime.Value.Month, dateTime.Value.Day, dateTime.Value.Hour, dateTime.Value.Minute, dateTime.Value.Second);
            }

            SetCurrentValue(ValueProperty, value);
        }

        private Popup CreateCalendarPopup()
        {
            var popup = new Popup
            {
                PlacementTarget = MainGrid,
                Placement = PlacementMode.Bottom,
                VerticalOffset = -4,
                IsOpen = true,
                StaysOpen = false,
            };

            return popup;
        }

        private void OnTodayButtonClick(object sender, RoutedEventArgs e)
        {
            DatePickerIcon.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
            UpdateDateTime(DateTime.Today.Date);
        }

        private void OnNowButtonClick(object sender, RoutedEventArgs e)
        {
            DatePickerIcon.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
            UpdateDateTime(DateTime.Now);
        }

        private void OnSelectDateButtonClick(object sender, RoutedEventArgs e)
        {
            DatePickerIcon.SetCurrentValue(ToggleButton.IsCheckedProperty, false);

            var calendarPopup = CreateCalendarPopup();
            var calendarPopupSource = CreateCalendarPopupSource();
            calendarPopup.SetCurrentValue(Popup.ChildProperty, calendarPopupSource);

            calendarPopupSource.Focus();
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            DatePickerIcon.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
            UpdateDateTime(null);
        }

        private void OnCopyButtonClick(object sender, RoutedEventArgs e)
        {
            DatePickerIcon.SetCurrentValue(ToggleButton.IsCheckedProperty, false);

            var value = Value;
            if (value != null)
            {
                Clipboard.SetText(DateTimeFormatter.Format(value.Value, _formatInfo), TextDataFormat.Text);
            }
        }

        private void OnPasteButtonClick(object sender, RoutedEventArgs e)
        {
            DatePickerIcon.SetCurrentValue(ToggleButton.IsCheckedProperty, false);

            if (Clipboard.ContainsData(DataFormats.Text))
            {
                var text = Clipboard.GetText(TextDataFormat.Text);
                var value = DateTime.MinValue;
                if (!string.IsNullOrEmpty(text)
                    && (DateTimeParser.TryParse(text, _formatInfo, out value)
                        || DateTime.TryParseExact(text, Format, null, DateTimeStyles.None, out value)
                        || DateTime.TryParseExact(text, CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern, null, DateTimeStyles.None, out value)
                        || DateTime.TryParseExact(text, CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.InvariantCulture.DateTimeFormat.LongTimePattern, null, DateTimeStyles.None, out value)))
                {
                    SetCurrentValue(ValueProperty, new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second));
                }
            }
        }

        private void OnAccentColorBrushChanged()
        {
            var solidColorBrush = AccentColorBrush as SolidColorBrush;
            if (solidColorBrush != null)
            {
                var accentColor = ((SolidColorBrush)AccentColorBrush).Color;
                accentColor.CreateAccentColorResourceDictionary("DateTimePicker");
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            SetCurrentValue(AccentColorBrushProperty, TryFindResource("AccentColorBrush") as SolidColorBrush);
        }

        protected override void OnLoaded(EventArgs e)
        {
            // Ensure that we have a template.
            ApplyTemplate();

            ApplyFormat();
            base.OnLoaded(e);
        }

        protected override void OnUnloaded(EventArgs e)
        {
            base.OnUnloaded(e);
            UnsubscribeNumericTextBoxes();
        }

        private void OnFormatChanged()
        {
            ApplyFormat();
        }

        private void OnValueChanged(object oldValue, object newValue)
        {
            var ov = oldValue as DateTime?;
            var nv = newValue as DateTime?;

            if (!AllowNull && newValue == null)
            {
                var dateTime = DateTime.Now;
                nv = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
            }

            if ((ov == null && nv != null)
                || (ov != null && nv == null))
            {
                ApplyFormat();
            }

            if (newValue == null && nv != null)
            {
                Dispatcher.BeginInvoke(() => SetCurrentValue(ValueProperty, nv));
            }
        }

        private void OnHideTimeChanged()
        {
            ApplyFormat();
        }

        private void OnHideSecondsChanged()
        {
            ApplyFormat();
        }

        private void ApplyFormat()
        {
            _formatInfo = DateTimeFormatHelper.GetDateTimeFormatInfo(Format, false);

            IsYearShortFormat = _formatInfo.IsYearShortFormat;
            NumericTBYear.SetCurrentValue(NumericTextBox.MinValueProperty, (double)(_formatInfo.IsYearShortFormat ? 0 : 1));
            NumericTBYear.SetCurrentValue(NumericTextBox.MaxValueProperty, (double)(_formatInfo.IsYearShortFormat ? 99 : 3000));

            IsHour12Format = _formatInfo.IsHour12Format.Value;
            NumericTBHour.SetCurrentValue(NumericTextBox.MinValueProperty, (double)(_formatInfo.IsHour12Format.Value ? 1 : 0));
            NumericTBHour.SetCurrentValue(NumericTextBox.MaxValueProperty, (double)(_formatInfo.IsHour12Format.Value ? 12 : 23));
            ToggleButtonH.SetCurrentValue(TagProperty, _formatInfo.IsHour12Format.Value ? DateTimePart.Hour12 : DateTimePart.Hour);

            IsAmPmShortFormat = _formatInfo.IsAmPmShortFormat.Value;

            EnableOrDisableYearConverterDependingOnFormat();
            EnableOrDisableHourConverterDependingOnFormat();
            EnableOrDisableAmPmConverterDependingOnFormat();

            ListTBAmPm.SetCurrentValue(ListTextBox.ListOfValuesProperty, new List<string>()
            {
                Meridiems.GetAmForFormat(_formatInfo),
                Meridiems.GetPmForFormat(_formatInfo)
            });

            NumericTBDay.SetCurrentValue(NumericTextBox.FormatProperty, NumberFormatHelper.GetFormat(_formatInfo.DayFormat.Length));
            NumericTBMonth.SetCurrentValue(NumericTextBox.FormatProperty, NumberFormatHelper.GetFormat(_formatInfo.MonthFormat.Length));
            NumericTBYear.SetCurrentValue(NumericTextBox.FormatProperty, NumberFormatHelper.GetFormat(_formatInfo.YearFormat.Length));
            NumericTBHour.SetCurrentValue(NumericTextBox.FormatProperty, NumberFormatHelper.GetFormat(_formatInfo.HourFormat.Length));
            NumericTBMinute.SetCurrentValue(NumericTextBox.FormatProperty, NumberFormatHelper.GetFormat(_formatInfo.MinuteFormat.Length));
            NumericTBSecond.SetCurrentValue(NumericTextBox.FormatProperty, NumberFormatHelper.GetFormat(_formatInfo.SecondFormat.Length));

            UnsubscribeNumericTextBoxes();

            Grid.SetColumn(NumericTBDay, GetPosition(_formatInfo.DayPosition));
            Grid.SetColumn(NumericTBMonth, GetPosition(_formatInfo.MonthPosition));
            Grid.SetColumn(NumericTBYear, GetPosition(_formatInfo.YearPosition));
            Grid.SetColumn(NumericTBHour, GetPosition(_formatInfo.HourPosition.Value));
            Grid.SetColumn(NumericTBMinute, GetPosition(_formatInfo.MinutePosition.Value));
            Grid.SetColumn(NumericTBSecond, GetPosition(_formatInfo.SecondPosition.Value));
            Grid.SetColumn(ListTBAmPm, GetPosition(_formatInfo.AmPmPosition.HasValue == false ? 6 : _formatInfo.AmPmPosition.Value));

            Grid.SetColumn(ToggleButtonD, GetPosition(_formatInfo.DayPosition) + 1);
            Grid.SetColumn(ToggleButtonMo, GetPosition(_formatInfo.MonthPosition) + 1);
            Grid.SetColumn(ToggleButtonY, GetPosition(_formatInfo.YearPosition) + 1);
            Grid.SetColumn(ToggleButtonH, GetPosition(_formatInfo.HourPosition.Value) + 1);
            Grid.SetColumn(ToggleButtonM, GetPosition(_formatInfo.MinutePosition.Value) + 1);
            Grid.SetColumn(ToggleButtonS, GetPosition(_formatInfo.SecondPosition.Value) + 1);
            Grid.SetColumn(ToggleButtonT, GetPosition(_formatInfo.AmPmPosition.HasValue == false ? 6 : _formatInfo.AmPmPosition.Value) + 1);

            // Fix positions which could be broken, because of AM/PM textblock.
            int dayPos = _formatInfo.DayPosition, monthPos = _formatInfo.MonthPosition, yearPos = _formatInfo.YearPosition,
                hourPos = _formatInfo.HourPosition.Value, minutePos = _formatInfo.MinutePosition.Value, secondPos = _formatInfo.SecondPosition.Value,
                amPmPos = _formatInfo.AmPmPosition.HasValue == false ? 6 : _formatInfo.AmPmPosition.Value;
            FixNumericTextBoxesPositions(ref dayPos, ref monthPos, ref yearPos, ref hourPos, ref minutePos, ref secondPos, ref amPmPos);

            _textBoxes[dayPos] = NumericTBDay;
            _textBoxes[monthPos] = NumericTBMonth;
            _textBoxes[yearPos] = NumericTBYear;
            _textBoxes[hourPos] = NumericTBHour;
            _textBoxes[minutePos] = NumericTBMinute;
            _textBoxes[secondPos] = NumericTBSecond;
            _textBoxes[amPmPos] = ListTBAmPm;

            // Fix tab order inside control.
            NumericTBDay.SetCurrentValue(TabIndexProperty, dayPos);
            NumericTBMonth.SetCurrentValue(TabIndexProperty, monthPos);
            NumericTBYear.SetCurrentValue(TabIndexProperty, yearPos);
            NumericTBHour.SetCurrentValue(TabIndexProperty, hourPos);
            NumericTBMinute.SetCurrentValue(TabIndexProperty, minutePos);
            NumericTBSecond.SetCurrentValue(TabIndexProperty, secondPos);
            ListTBAmPm.SetCurrentValue(TabIndexProperty, amPmPos);
            DatePickerIcon.SetCurrentValue(TabIndexProperty, Int32.MaxValue);

            SubscribeNumericTextBoxes();

            Separator1.SetCurrentValue(TextBlock.TextProperty, Value == null ? string.Empty : _formatInfo.Separator1);
            Separator2.SetCurrentValue(TextBlock.TextProperty, Value == null ? string.Empty : _formatInfo.Separator2);
            Separator3.SetCurrentValue(TextBlock.TextProperty, Value == null ? string.Empty : _formatInfo.Separator3);
            Separator4.SetCurrentValue(TextBlock.TextProperty, Value == null ? string.Empty : _formatInfo.Separator4);
            Separator5.SetCurrentValue(TextBlock.TextProperty, Value == null ? string.Empty : _formatInfo.Separator5);
            Separator6.SetCurrentValue(TextBlock.TextProperty, Value == null ? string.Empty : _formatInfo.Separator6);
            Separator7.SetCurrentValue(TextBlock.TextProperty, Value == null ? string.Empty : _formatInfo.Separator7);
        }

        private int GetPosition(int index)
        {
            return index * 2;
        }

        private void EnableOrDisableHourConverterDependingOnFormat()
        {
            var converter = TryFindResource("Hour24ToHour12Converter") as Hour24ToHour12Converter;
            if (converter != null)
            {
                converter.IsEnabled = IsHour12Format;
                BindingOperations.GetBindingExpression(NumericTBHour, NumericTextBox.ValueProperty).UpdateTarget();
            }
        }

        private void EnableOrDisableAmPmConverterDependingOnFormat()
        {
            var converter = TryFindResource("AmPmLongToAmPmShortConverter") as AmPmLongToAmPmShortConverter;
            if (converter != null)
            {
                converter.IsEnabled = IsAmPmShortFormat;
                BindingOperations.GetBindingExpression(ListTBAmPm, ListTextBox.ValueProperty).UpdateTarget();
            }
        }

        private void EnableOrDisableYearConverterDependingOnFormat()
        {
            var converter = TryFindResource("YearLongToYearShortConverter") as YearLongToYearShortConverter;
            if (converter != null)
            {
                converter.IsEnabled = IsYearShortFormat;
                BindingOperations.GetBindingExpression(NumericTBYear, NumericTextBox.ValueProperty).UpdateTarget();
            }
        }

        private void FixNumericTextBoxesPositions(ref int dayPosition, ref int monthPosition, ref int yearPosition, ref int hourPosition, ref int minutePosition, ref int secondPosition, ref int amPmPosition)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>()
            {
                { "dayPosition", dayPosition },
                { "monthPosition", monthPosition },
                { "yearPosition", yearPosition },
                { "hourPosition", hourPosition },
                { "minutePosition", minutePosition },
                { "secondPosition", secondPosition },
                { "amPmPosition", amPmPosition }
            };

            var current = 0;
            foreach (var entry in dict.OrderBy(x => x.Value))
            {
                dict[entry.Key] = current++;
            }

            dayPosition = dict["dayPosition"];
            monthPosition = dict["monthPosition"];
            yearPosition = dict["yearPosition"];
            hourPosition = dict["hourPosition"];
            minutePosition = dict["minutePosition"];
            secondPosition = dict["secondPosition"];
            amPmPosition = dict["amPmPosition"];
        }
        #endregion
    }
}