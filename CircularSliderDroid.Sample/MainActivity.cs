using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.OS;

namespace CircularSliderDroid.Sample
{
    [Activity(MainLauncher = true)]
    public class MainActivity : Activity
    {
        CircularSlider _slider;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ActivityMain);

            _slider = FindViewById<CircularSlider>(Resource.Id.ActivityMain_CircularSlider);

            //Init slider properties
            _slider.Color = Color.Red;
            _slider.ProgressColor = Color.Yellow;
            _slider.SetThumbResourceId(Resource.Drawable.circle);
            _slider.Maximum = 50;
            _slider.Progress = 20;

            new Handler(Looper.MainLooper).PostDelayed(async () => await Demo(), 2000);
        }

        async Task Demo()
        {
            await AnimateProgress();

            await Task.Delay(500);

            _slider.Progress = _slider.Maximum / 2;

            await Task.Delay(200);

            _slider.Color = Color.Brown;

            await Task.Delay(500);

            _slider.ProgressColor = Color.Purple;

            await Task.Delay(500);

            _slider.Color = Color.Silver;
            _slider.ProgressColor = Color.Blue;

            await Task.Delay(500);

            _slider.SweepAngle = 120;

            await Task.Delay(500);

            _slider.StartAngle = 45;

            await Task.Delay(500);

            _slider.StartAngle = 0;
            _slider.SweepAngle = 270;

            await Task.Delay(500);

            _slider.Clockwise = true;

            await Task.Delay(500);

            _slider.RoundEdges = false;

            for (int i = 1; i <= 12; i++)
            {
                _slider.LineWidth = i;
                await Task.Delay(150);
            }

            await Task.Delay(500);

            _slider.RoundEdges = true;

            await Task.Delay(500);

            for (int i = 12; i >= 2; i--)
            {
                _slider.LineWidth = i;
                await Task.Delay(150);
            }

            _slider.Clockwise = false;

            await Task.Delay(500);

            _slider.Thumb = null;

            _slider.SweepAngle = 360;

            await AnimateProgress();

            _slider.Clockwise = true;

            await Task.Delay(500);

            _slider.Progress = 25;

            for (int i = 25; i <= 75; i++)
            {
                _slider.Maximum = i;
                await Task.Delay(50);
            }

            await Task.Delay(500);

            _slider.Thumb = BitmapFactory.DecodeResource(Resources, Resource.Drawable.circle);
            _slider.Maximum = 33;
            _slider.Progress = 25;
            _slider.StartAngle = 180;
            _slider.SweepAngle = 180;
        }

        async Task AnimateProgress()
        {
            for (int i = 0; i <= _slider.Maximum; i++)
            {
                _slider.Progress = i;
                await Task.Delay(50);
            }

            for (int i = _slider.Maximum; i >= 0; i--)
            {
                _slider.Progress = i;
                await Task.Delay(50);
            }
        }
    }
}

