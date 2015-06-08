using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace LightDiode
{
    public sealed class StartupTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            await ShowLight(26, 5000);

            deferral.Complete();
        }

        private async Task ShowLight(int diodePin, int timeout)
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
                return;

            var boardPin = gpio.OpenPin(diodePin, GpioSharingMode.Exclusive);
            if (boardPin == null)
                return;

            boardPin.Write(GpioPinValue.High);
            boardPin.SetDriveMode(GpioPinDriveMode.Output);

            await Task.Delay(timeout);
            boardPin.Write(GpioPinValue.Low);
        }
    }
}
