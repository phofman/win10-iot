using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace DigitDisplay
{
    public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            PrintTime(15000);

            deferral.Complete();
        }

        private void PrintTime(int timeout)
        {
            // LCD segments pins #: 11, 7, 4, 2, 1, 10, 5, 3.
            // they all should be connected through 390 ohm resistor
            // var segmentPins = new[] { 16, 13, 25, 23, 18, 6, 12, 24 };
            var segmentPins = new[] { 23, 18, 6, 16, 13, 25, 12, 24 }; // mirror

            // LCD digits selectors #: 12, 9, 8, 6
            // no resistors required
            // var digitPins = new[] { 27, 22, 5, 26 };
            var digitPins = new[] { 26, 5, 22, 27 }; // mirror

            var dotColumn = 2;

            var digitMap = new Dictionary<char, int[]>();
            digitMap.Add(' ', new[] { 0, 0, 0, 0, 0, 0, 0 });
            digitMap.Add('0', new[] { 1, 1, 1, 1, 1, 1, 0 });
            digitMap.Add('1', new[] { 0, 1, 1, 0, 0, 0, 0 });
            digitMap.Add('2', new[] { 1, 1, 0, 1, 1, 0, 1 });
            digitMap.Add('3', new[] { 1, 1, 1, 1, 0, 0, 1 });
            digitMap.Add('4', new[] { 0, 1, 1, 0, 0, 1, 1 });
            digitMap.Add('5', new[] { 1, 0, 1, 1, 0, 1, 1 });
            digitMap.Add('6', new[] { 1, 0, 1, 1, 1, 1, 1 });
            digitMap.Add('7', new[] { 1, 1, 1, 0, 0, 0, 0 });
            digitMap.Add('8', new[] { 1, 1, 1, 1, 1, 1, 1 });
            digitMap.Add('9', new[] { 1, 1, 1, 1, 0, 1, 1 });

            PrintTime(timeout, segmentPins, digitPins, digitMap, dotColumn);
        }

        private void PrintTime(int timeout, int[] digitSegments, int[] digitSelectors, Dictionary<char, int[]> digitMap, int dotColumn)
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
                return;

            // initialize display:
            var segmentPins = new GpioPin[digitSegments.Length];
            for (int i = 0; i < digitSegments.Length; i++)
            {
                segmentPins[i] = gpio.OpenPin(digitSegments[i], GpioSharingMode.Exclusive);
                segmentPins[i].Write(GpioPinValue.High);
                segmentPins[i].SetDriveMode(GpioPinDriveMode.Output);
            }

            var selectorPins = new GpioPin[digitSelectors.Length];
            for (int i = 0; i < digitSelectors.Length; i++)
            {
                selectorPins[i] = gpio.OpenPin(digitSelectors[i], GpioSharingMode.Exclusive);
                selectorPins[i].Write(GpioPinValue.Low);
                selectorPins[i].SetDriveMode(GpioPinDriveMode.Output);
            }

            DateTime start = DateTime.Now;
            DateTime now;
            while (((now = DateTime.Now) - start).TotalMilliseconds < timeout)
            {
                var text = now.ToString("HHmm");
                PrintTime(text, segmentPins, selectorPins, digitMap, dotColumn, (now.Second % 2) != 0);
            }
        }

        private void PrintTime(string text, GpioPin[] segmentPins, GpioPin[] selectorPins, Dictionary<char, int[]> digitMap, int dotColumn, bool showDot)
        {
            // print text (char by char):
            for (int i = 0; i < 4 && i < text.Length; i++)
            {
                var c = text[i];
                var charDefinition = digitMap[c];

                // print single digit:
                for (int j = 0; j < 7; j++)
                {
                    segmentPins[j].Write(charDefinition[j] > 0 ? GpioPinValue.Low : GpioPinValue.High);
                }

                // hide the dot:
                segmentPins[7].Write(i == dotColumn && showDot ? GpioPinValue.Low : GpioPinValue.High);

                selectorPins[i].Write(GpioPinValue.High);
                Task.Delay(1).Wait();
                selectorPins[i].Write(GpioPinValue.Low);
            }
        }
    }
}
