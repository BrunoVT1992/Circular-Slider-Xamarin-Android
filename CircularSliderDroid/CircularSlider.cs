using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Java.Lang;
using Math = Java.Lang.Math;

namespace CircularSliderDroid
{
    public class CircularSlider : View, INotifyPropertyChanged
    {
        int _arcRadius;
        float _progressSweep;
        RectF _arcRect = new RectF();
        Paint _arcPaint;
        Paint _progressPaint;
        int _translateX;
        int _translateY;
        int _thumbXPos;
        int _thumbYPos;
        float _touchInsideIgnoreRadius;
        float _touchOutsideIgnoreRadius;

        int _touchCorrection = 40;
        /// <summary>
        /// This indicates how many points a touch event can go inside or outside the circle before the slider stops updating.
        /// This may never exceed half of the controls size (this can create unwanted behaviour).
        /// The value must be greater or equal to 0.
        /// </summary>
        public int TouchCorrection
        {
            get
            {
                return _touchCorrection;
            }
            set
            {
                if (_touchCorrection < 0)
                    throw new ArgumentOutOfRangeException(nameof(TouchCorrection), "The value must be at least 0");

                _touchCorrection = value;

                OnPropertyChanged();
            }
        }

        int _sweepAngle = 180;
        /// <summary>
        /// This indicates how many degrees the circle is used.
        /// The value must be between 0 and 360
        /// </summary>
        public int SweepAngle
        {
            get { return _sweepAngle; }
            set
            {
                if (value < 0 || value > 360)
                    throw new ArgumentOutOfRangeException(nameof(SweepAngle), "The value must be between 0 and 360");

                _sweepAngle = value;

                if (Width > 0 && Height > 0)
                {
                    CalculateArcRect(Width, Height);
                    UpdateProgress();
                }

                Invalidate();
                OnPropertyChanged();
            }
        }

        int _startAngle = 180;
        /// <summary>
        /// This indicates at how many degrees in the circle the indicator will start.
        /// The value must be between 0 and 360.
        /// </summary>
        public int StartAngle
        {
            get { return _startAngle; }
            set
            {
                if (value < 0 || value > 360)
                    throw new ArgumentOutOfRangeException(nameof(StartAngle), "The value must be between 0 and 360");

                _startAngle = value;

                if (Width > 0 && Height > 0)
                {
                    CalculateArcRect(Width, Height);
                    UpdateProgress();
                }

                Invalidate();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The color of the uncompleted progress indicator.
        /// </summary>
        public Color Color
        {
            get
            {
                return _arcPaint.Color;
            }
            set
            {
                _arcPaint.Color = value;
                Invalidate();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The color of the completed progress indicator.
        /// </summary>
        public Color ProgressColor
        {
            get
            {
                return _progressPaint.Color;
            }
            set
            {
                _progressPaint.Color = value;
                Invalidate();
                OnPropertyChanged();
            }
        }

        Bitmap _thumb;
        /// <summary>
        /// The thumb image to indicate the current progress.
        /// The bitmap must have the same width and height.
        /// </summary>
        public Bitmap Thumb
        {
            get
            {
                return _thumb;
            }
            set
            {
                if (value != null && value.Width != value.Height)
                    throw new ArgumentException("The image must be a square (same width and height)", nameof(Thumb));

                _thumb = value;

                if (Width > 0 && Height > 0)
                {
                    CalculateArcRect(Width, Height);
                    UpdateProgress();
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The line width in pixels of the circle.
        /// </summary>
        public int LineWidth
        {
            get
            {
                return (int)_arcPaint.StrokeWidth;
            }
            set
            {
                _arcPaint.StrokeWidth = value;
                _progressPaint.StrokeWidth = value;

                if (Width > 0 && Height > 0)
                {
                    CalculateArcRect(Width, Height);
                    UpdateThumbPosition();
                }

                Invalidate();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// This indicates if the circle line has rounded corners at the end of the line.
        /// </summary>
        public bool RoundEdges
        {
            get
            {
                return _arcPaint.StrokeCap == Paint.Cap.Round;
            }
            set
            {
                _arcPaint.StrokeCap = value ? Paint.Cap.Round : Paint.Cap.Square;
                _progressPaint.StrokeCap = value ? Paint.Cap.Round : Paint.Cap.Square;

                Invalidate();
                OnPropertyChanged();
            }
        }

        int _maximum;
        /// <summary>
        /// The maximum value of the progress.
        /// </summary>
        public int Maximum
        {
            get
            {
                return _maximum;
            }
            set
            {
                if (value < 0)
                    throw new IllegalStateException("Maximum can not be less than 0");

                if (value < Progress)
                    throw new IllegalStateException("Maximum can not be less than Progress value" + Progress);

                if (value != _maximum)
                {
                    _maximum = value;

                    UpdateProgress();
                    OnPropertyChanged();
                }
            }
        }

        int _progress;
        /// <summary>
        /// The current progress.
        /// </summary>
        public int Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                if (value < 0)
                    throw new IllegalStateException("Progress can not be less than 0");

                if (value > Maximum)
                    throw new IllegalStateException("Progress can not be more than Maximum value " + Maximum);

                if (value != _progress)
                {
                    _progress = value;

                    UpdateProgress();

                    ProgressChanged?.Invoke(this, value);

                    OnPropertyChanged();
                }
            }
        }

        bool _clockwise = true;
        /// <summary>
        /// This indicates if the slider should work clockwise or counter clockwise.
        /// </summary>
        public bool Clockwise
        {
            get
            {
                return _clockwise;
            }
            set
            {
                _clockwise = value;
                Invalidate();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Triggered when one of the custom properties is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggered when the progress value has changed.
        /// </summary>
        public event EventHandler<int> ProgressChanged;

        public CircularSlider(Context context)
            : base(context)
        {
            Init();
        }

        public CircularSlider(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init();
        }

        public CircularSlider(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Init();
        }

        void Init()
        {
            _arcPaint = new Paint
            {
                AntiAlias = true,
                StrokeCap = Paint.Cap.Round
            };
            _arcPaint.SetStyle(Paint.Style.Stroke);

            _progressPaint = new Paint
            {
                AntiAlias = true,
                StrokeCap = Paint.Cap.Round
            };
            _progressPaint.SetStyle(Paint.Style.Stroke);

            LineWidth = (int)(4 * Context.Resources.DisplayMetrics.Density);

            UpdateProgress();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            var width = GetDefaultSize(SuggestedMinimumWidth, widthMeasureSpec);
            var height = GetDefaultSize(SuggestedMinimumHeight, heightMeasureSpec);

            CalculateArcRect(width, height);

            UpdateThumbPosition();

            // Don't use the exact radius, makes interaction too tricky
            if (Thumb != null)
            {
                _touchInsideIgnoreRadius = _arcRadius - (Math.Min(Thumb.Width, Thumb.Height) + TouchCorrection);
                _touchOutsideIgnoreRadius = _arcRadius + (Math.Min(Thumb.Width, Thumb.Height) + TouchCorrection);
            }
            else
            {
                _touchInsideIgnoreRadius = _arcRadius - TouchCorrection;
                _touchOutsideIgnoreRadius = _arcRadius + TouchCorrection;
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (!Clockwise)
                canvas.Scale(-1, 1, _arcRect.CenterX(), _arcRect.CenterY());

            canvas.DrawArc(_arcRect, StartAngle, SweepAngle, false, _arcPaint);
            canvas.DrawArc(_arcRect, StartAngle, _progressSweep, false, _progressPaint);

            if (Thumb != null)
            {
                var left = (_translateX - _thumbXPos) - (Thumb.Width / 2);
                var top = (_translateY - _thumbYPos) - (Thumb.Height / 2);

                canvas.DrawBitmap(Thumb, left, top, null);
            }
        }

        void CalculateArcRect(int width, int height)
        {
            _translateX = (int)(width * 0.5f);
            _translateY = (int)(height * 0.5f);

            var min = Math.Min(width, height);

            if (Thumb != null)
            {
                var arcDiameter = min - (LineWidth + Thumb.Width);
                _arcRadius = arcDiameter / 2;

                var top = height / 2 - (arcDiameter / 2);
                var left = width / 2 - (arcDiameter / 2);

                _arcRect.Set(left, top, left + arcDiameter, top + arcDiameter);
            }
            else
            {
                var arcDiameter = min - LineWidth;
                _arcRadius = arcDiameter / 2;

                var top = height / 2 - (arcDiameter / 2);
                var left = width / 2 - (arcDiameter / 2);

                _arcRect.Set(left, top, left + arcDiameter, top + arcDiameter);
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (Enabled)
            {
                Parent?.RequestDisallowInterceptTouchEvent(true);

                switch (e.Action)
                {
                    case MotionEventActions.Down:
                    case MotionEventActions.Move:
                        UpdateOnTouch(e);
                        break;
                    case MotionEventActions.Up:
                    case MotionEventActions.Cancel:
                        Pressed = false;
                        Parent?.RequestDisallowInterceptTouchEvent(true);
                        break;
                }

                return true;
            }

            return false;
        }

        void UpdateOnTouch(MotionEvent e)
        {
            if (IgnoreTouch(e.GetX(), e.GetY()))
                return;

            Pressed = true;

            var touchAngle = GetTouchDegrees(e.GetX(), e.GetY());

            var progress = GetProgressForAngle(touchAngle);

            if (progress >= 0)
            {
                Progress = progress;
            }
        }

        bool IgnoreTouch(float xPos, float yPos)
        {
            var x = xPos - _translateX;
            var y = yPos - _translateY;

            return PointIsInsideCircle(_touchInsideIgnoreRadius, x, y) || !PointIsInsideCircle(_touchOutsideIgnoreRadius, x, y);
        }

        bool PointIsInsideCircle(double circleRadius, double x, double y)
        {
            return (Math.Pow(x, 2) + Math.Pow(y, 2)) < (Math.Pow(circleRadius, 2));
        }

        double GetTouchDegrees(float xPos, float yPos)
        {
            float x = xPos - _translateX;
            float y = yPos - _translateY;

            if (!Clockwise)
                x = -x;

            // convert to arc Angle
            var angle = Math.ToDegrees(Math.Atan2(y, x) + (Math.Pi / 2));

            angle -= 90;

            if (angle < 0)
            {
                angle = 360 + angle;
            }

            angle -= StartAngle;

            return angle;
        }

        int GetProgressForAngle(double angle)
        {
            var valuePerDegree = (float)Maximum / SweepAngle;

            var progress = (int)Math.Round(valuePerDegree * angle);

            if (progress < 0 || progress > Maximum)
                return -1;

            return progress;
        }

        void UpdateProgress()
        {
            _progressSweep = (float)Progress / Maximum * SweepAngle;

            UpdateThumbPosition();

            Invalidate();
        }

        void UpdateThumbPosition()
        {
            var thumbAngle = StartAngle + _progressSweep + 180;
            _thumbXPos = (int)(_arcRadius * Math.Cos(Math.ToRadians(thumbAngle)));
            _thumbYPos = (int)(_arcRadius * Math.Sin(Math.ToRadians(thumbAngle)));
        }

        /// <summary>
        /// Sets the thumb resource identifier.
        /// This will automatically be converted to a bitmap and set to the Thumb property.
        /// </summary>
        public void SetThumbResourceId(int resId)
        {
            Thumb = BitmapFactory.DecodeResource(Resources, resId);
        }

        public void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
