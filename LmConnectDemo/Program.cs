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

                // Step 2: Initialize will throw an exception if not authorized, but
                // for completeness, verify authorization and print claims
                if (Connect.Authorization.IsAuthorized)
                {
                    Console.WriteLine("Authorization claims:");

                    foreach (KeyValuePair<string, object> claim in Connect.Authorization.Claims)
                    {
                        Console.WriteLine("  Claim: {0}, Value: {1}", claim.Key, claim.Value);
                    }

                    Console.WriteLine("Searching for Launch Monitors");

                    // Step 3: Find available Launch Monitors
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

                            // Step 4: Register for device changed events - optional
                            device.StateChangedEvent += Device_StateChangedEvent;

                            // Step 5: Register for configuration changed events - optional
                            device.ConfigurationChangedEvent += Device_ConfigurationChangedEvent;

                            // Step 6: Register for shot events
                            device.ShotEvent += Device_ShotEvent;

                            // Step 7: Register for shot video events - optional
                            device.VideoAvailableEvent += Device_VideoAvailableEvent;
                            
                            try
                            {
                                // Step 8: Connect to one or more launch monitors
                                await device.Connect();

                                Console.WriteLine("Device Information");
                                Console.WriteLine("  Software Version: {0}", device.DeviceInformation.SoftwareVersion);
                                Console.WriteLine("  Firmware Version: {0}", device.DeviceInformation.FirmwareVersion);
                                Console.WriteLine("  Hardware Revision: {0}", device.DeviceInformation.HardwareRevision);
                                Console.WriteLine("  Serial Number: {0}", device.DeviceInformation.SerialNumber);
                                Console.WriteLine("  Model Number: {0}", device.DeviceInformation.ModelNumber);

                                // Step 9: Set configuration if desired - optional
                                // Set the device to auto-arm as opposed to SDK arming it
                                await device.SetConfiguration(ConfigurationId.AutoArm, true);

                                // Step 10: Set the club type to driver
                                await device.SetConfiguration(ConfigurationId.Club, Club.Driver);

                                // Step 11: Set session related configuration - optional
                                await device.SetConfiguration(ConfigurationId.Location, Location.Screen);
                                await device.SetConfiguration(ConfigurationId.Altitude, 1100);
                                await device.SetConfiguration(ConfigurationId.Temperature, 70.0);

                                // If we connected to the device, wait for a few shots
                                await _complete.WaitAsync();

                                // Step 12: Disconnect the device
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
                else
                {
                    Console.WriteLine("Not authorized");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Could not initialize. Not authorized");
            }
        }

        private static void Device_ConfigurationChangedEvent(object sender, ConfigurationChangedEventArgs e)
        {
            Console.WriteLine("Configuration changes, ID: {0}, Value: {1}", e.Id, e.Value);
        }

        private static void Device_VideoAvailableEvent(object sender, VideoAvailableEventArgs e)
        {
            Console.WriteLine("Video available  {0}", e.Url);
        }

        private static void Device_StateChangedEvent(object sender, StateChangedEventArgs e)
        {
            Console.WriteLine("State changed {0}", e.State.ToString());
        }

        static void Device_ShotEvent(object sender, ShotEventArgs e)
        {
            Console.WriteLine("Shot received - Type: {0}", e.Type);
            Console.WriteLine("  Shot Id: {0}", e.Shot.Id);
            Console.WriteLine("  Device Id: {0}", e.Shot.DeviceId);
            Console.WriteLine("  Timestamp: {0}", e.Shot.Timestamp.ToLocalTime());
            if (e.Shot.AttackAngle.HasValue) Console.WriteLine("  Attack Angle: {0:0.00}", e.Shot.AttackAngle);
            if (e.Shot.BallSpeed.HasValue) Console.WriteLine("  Ball Speed: {0:0.00}", e.Shot.BallSpeed);
            if (e.Shot.ClubPath.HasValue) Console.WriteLine("  Club Path: {0:0.00}", e.Shot.ClubPath);
            if (e.Shot.ClubSpeed.HasValue) Console.WriteLine("  Club Speed: {0:0.00}", e.Shot.ClubSpeed);
            if (e.Shot.FaceAngle.HasValue) Console.WriteLine("  Face Angle: {0:0.00}", e.Shot.FaceAngle);
            if (e.Shot.HorizontalLaunchAngle.HasValue) Console.WriteLine("  Horizontal Launch Angle: {0:0.00}", e.Shot.HorizontalLaunchAngle);
            if (e.Shot.SmashFactor.HasValue) Console.WriteLine("  Smash Factor: {0:0.00}", e.Shot.SmashFactor);

            if (e.Type == ShotType.Flight)
            {
                if (e.Shot.SpinAxis.HasValue) Console.WriteLine("  Spin Axis: {0:0.00}", e.Shot.SpinAxis);
                if (e.Shot.SpinRate.HasValue) Console.WriteLine("  Spin rate: {0:0.00}", e.Shot.SpinRate);
                if (e.Shot.VerticalLaunchAngle.HasValue) Console.WriteLine("  Vertical Launch Angle: {0:0.00}", e.Shot.VerticalLaunchAngle);
                if (e.Shot.Apex.HasValue) Console.WriteLine("  Apex: {0:0.00}", e.Shot.Apex);
                if (e.Shot.CarryDistance.HasValue) Console.WriteLine("  Carry Distance: {0:0.00}", e.Shot.CarryDistance);
                if (e.Shot.Side.HasValue) Console.WriteLine("  Side: {0:0.00}", e.Shot.Side);
                if (e.Shot.SideTotal.HasValue) Console.WriteLine("  Side Total: {0:0.00}", e.Shot.SideTotal);
                if (e.Shot.TotalDistance.HasValue) Console.WriteLine("  Total Distance: {0:0.00}", e.Shot.TotalDistance);

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
