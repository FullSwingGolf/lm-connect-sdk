using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using FullSwingGolf.LaunchMonitor;

namespace LmConnectDemo
{
    class Program
    {
        static private int _shotsReceived = 0;
        static private int _totalShots = 100;
        static private SemaphoreSlim _complete = new SemaphoreSlim(0);
        static private readonly string AccountId = "[Enter Account ID provided by FSG]";
        static private readonly string AccountKey = "[Enter Account Key provided by FSG]";

        static void Main(string[] args)
        {
            // Run demo code and wait for it it complete
            RunDemo().Wait();

            Console.WriteLine("Press any key to exit");

            Console.Read();
        }

        static int DisplayMenu(IReadOnlyList<IDevice> devices)
        {
            int deviceNumber = 1;
            int selection = -1;
            bool display = true;

            while (display)
            {
                Console.WriteLine("Which device would you like to connect to:");

                foreach (var deviceIter in devices)
                {
                    Console.WriteLine("{0}. {1}", deviceNumber, deviceIter.Id);
                    deviceNumber++;
                }

                Console.WriteLine("{0}. None", deviceNumber);
                Console.Write("\nSelection: ");

                ConsoleKeyInfo userinput = Console.ReadKey();
                int selectionNum = userinput.KeyChar - '0';
                Console.WriteLine();

                if (selectionNum == deviceNumber)
                {
                    // Exit
                    selection = -1;
                    display = false;
                }
                else if (selectionNum < deviceNumber)
                {
                    selection = selectionNum - 1;
                    display = false;
                }
                else
                {
                    // Invalid
                    Console.WriteLine("Invalid selection");
                }
            }

            return selection;
        }

        static async Task RunDemo()
        {
            int selection;

            try
            {
                Console.WriteLine("Initializing Connect SDK");

                // Step 1: Initialize the SDK with your account ID and account key
                await Connect.Initialize(AccountId, AccountKey);

                Console.WriteLine("Searching for Launch Monitors");

                // Step 2: Find available Launch Monitors
                IReadOnlyList<IDevice> devices = await Connect.FindDevicesAsync();

                if (devices.Count > 0)
                {
                    // Print the devices found
                    Console.WriteLine("Found Launch Monitors");

                    selection = DisplayMenu(devices);

                    if (selection != -1)
                    {
                        // Grab the first found device for demo code
                        var device = devices[selection];

                        // Register for device changed events - optional
                        device.StateChangedEvent += Device_StateChangedEvent;

                        // Step 3: Register for shot events
                        device.ShotEvent += Device_ShotEvent;

                        // Step 4: Connect to one or more launch monitors
                        try
                        {
                            await device.Connect();

                            // Set the device to auto-arm as opposed to SDK arming it
                            await device.SetConfiguration(ConfigurationId.AutoArm, true);

                            // If we connected to the device, wait for a few shots
                            await _complete.WaitAsync();

                            // Disconnect the device
                            await device.Disconnect();
                        }
                        catch (ConnectionException ex)
                        {
                            Console.WriteLine("Could not connect to Launch Monitor {0}, {1}", device.Id, ex.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Not connecting. Exiting");
                    }
                }
                else
                {
                    Console.WriteLine("Could not find any Launch Monitors. Verify they are powered on");
                }
            }
            catch (NotAuthorizedException)
            {
                Console.WriteLine("Could not initialize. Not authorized");
            }
        }

        private static void Device_StateChangedEvent(object sender, StateChangedEventArgs e)
        {
            Console.WriteLine("State changed {0}", e.State.ToString());
        }

        static void Device_ShotEvent(object sender, ShotEventArgs e)
        {
            Console.WriteLine("Shot received - Type: {0}", e.Type);

            // Only print full shot data
            if (e.Type == ShotType.Flight)
            {
                Console.WriteLine("  Shot Id: {0}", e.Shot.Id);
                Console.WriteLine("  Device Id: {0}", e.Shot.DeviceId);
                Console.WriteLine("  Timestamp: {0}", e.Shot.Timestamp.ToLocalTime());
                Console.WriteLine("  Attack Angle: {0:0.00}", e.Shot.AttackAngle);
                Console.WriteLine("  Ball Speed: {0:0.00}", e.Shot.BallSpeed);
                Console.WriteLine("  Club Path: {0:0.00}", e.Shot.ClubPath);
                Console.WriteLine("  Club Speed: {0:0.00}", e.Shot.ClubSpeed);
                Console.WriteLine("  Face Angle: {0:0.00}", e.Shot.FaceAngle);
                Console.WriteLine("  Horizontal Launch Angle: {0:0.00}", e.Shot.HorizontalLaunchAngle);
                Console.WriteLine("  Smash Factor: {0:0.00}", e.Shot.SmashFactor);
                Console.WriteLine("  Spin Axis: {0:0.00}", e.Shot.SpinAxis);
                Console.WriteLine("  Spin rate: {0:0.00}", e.Shot.SpinRate);
                Console.WriteLine("  Vertical Launch Angle: {0:0.00}", e.Shot.VerticalLaunchAngle);
                Console.WriteLine("  Apex: {0:0.00}", e.Shot.Apex);
                Console.WriteLine("  Carry Distance: {0:0.00}", e.Shot.CarryDistance);
                Console.WriteLine("  Side: {0:0.00}", e.Shot.Side);
                Console.WriteLine("  Side Total: {0:0.00}", e.Shot.SideTotal);
                Console.WriteLine("  Total Distance: {0:0.00}", e.Shot.TotalDistance);

                // Track shots received and release semaphore once desired number has been seen
                // so program can exit
                _shotsReceived++;

                if (_shotsReceived >= _totalShots)
                {
                    _complete.Release();
                }
            }
        }
    }
}
