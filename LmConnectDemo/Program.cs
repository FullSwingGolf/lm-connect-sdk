using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using FullSwingGolf.LaunchMonitor;
#if NETCOREAPP
using Microsoft.Extensions.Configuration;
#endif
using System.Reflection;
using System.IO;

namespace LmConnectDemo
{
    class Program
    {
        static private int _shotsReceived = 0;
        static private int _totalShots = 100;
        static private SemaphoreSlim _complete = new SemaphoreSlim(0);
        // Add Account ID and Account Key provided by FSG in code or in .NET Core
        // add them to your secrets.json file
        static private string AccountId = "";
        static private string AccountKey = "";
#if NETCOREAPP
        private static IConfigurationRoot Configuration { get; set; }
#endif

        static void Main(string[] args)
        {
#if NETCOREAPP
            // Check whether or not to access secrets file for account information
            if (string.IsNullOrEmpty(AccountId) && string.IsNullOrEmpty(AccountKey)) VerifyAccountInfo();
#endif

            // Run demo code and wait for it it complete
            RunDemo().Wait();

            Console.WriteLine("Press any key to exit");

            Console.Read();
        }

#if NETCOREAPP
        static void VerifyAccountInfo()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true);

            Configuration = builder.Build();

            AccountId = Configuration["AccountId"];
            AccountKey = Configuration["AccountKey"];
        }
#endif

        static int DisplayMenu(IReadOnlyList<IDevice> devices)
        {
            int deviceNumber = 1;
            int selection = -1;
            bool display = true;

            while (display)
            {
                Console.WriteLine("Select device number and press enter to connect to device:");
                Console.WriteLine();

                foreach (var deviceIter in devices)
                {
                    Console.WriteLine("{0}. {1}", deviceNumber, deviceIter.Id);
                    deviceNumber++;
                }

                Console.WriteLine("{0}. None", deviceNumber);
                Console.Write("\nSelection: ");

                int selectionNum = Convert.ToInt32(Console.ReadLine());
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

                            device.PointCloudAvailableEvent += Device_PointCloudAvailableEvent;

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

                                // Step 9: Set the device to active state
                                await device.SetPowerState(PowerState.Active);

                                // Step 10: Set configuration if desired - optional
                                // Set the device to auto-arm as opposed to SDK arming it
                                await device.SetConfiguration(ConfigurationId.AutoArm, true);

                                // Step 11: Set the club type to driver
                                await device.SetConfiguration(ConfigurationId.Club, Club.Driver);

                                // Step 12: Set session related configuration - optional
                                await device.SetConfiguration(ConfigurationId.Location, Location.Screen);
                                await device.SetConfiguration(ConfigurationId.Altitude, 1100);
                                await device.SetConfiguration(ConfigurationId.Temperature, 70.0);
                                List<DataPoint> screenLayout = new List<DataPoint>() { DataPoint.CarryDistance, DataPoint.TotalDistance, DataPoint.LaunchAngle,
                                    DataPoint.SpinRate, DataPoint.SpinAxis,DataPoint.BallSpeed, DataPoint.ClubSpeed, DataPoint.SmashFactor, DataPoint.ClubPath,
                                    DataPoint.FaceAngle, DataPoint.FaceToPath, DataPoint.AttackAngle, DataPoint.Apex, DataPoint.HorizontalLaunchAngle,
                                    DataPoint.SideCarry, DataPoint.SideTotal };
                                await device.SetConfiguration(ConfigurationId.ScreenLayout, screenLayout);
                                await device.SetConfiguration(ConfigurationId.PlayMode, PlayMode.Normal);                              
                                await device.SetConfiguration(ConfigurationId.DataPointsHidden, false);

                                // If we connected to the device, wait for a few shots
                                await _complete.WaitAsync();

                                // Step 13: Set the device back to sleep state - optional
                                await device.SetPowerState(PowerState.Sleep);

                                // Step 14: Disconnect the device
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

        private static async void Device_PointCloudAvailableEvent(object sender, PointCloudAvailableEventArgs e)
        {
            IDevice device = (IDevice)sender;

            Console.WriteLine("Point cloud available for shot: {0}", e.ShotId);

            try
            {
                // Request point cloud
                PointCloud pointCloud = await device.GetPointCloud(e.ShotId);

                Console.Write("Point Cloud Points: ");
                for (int i = 0; i < 4; i++)
                {
                    PointCloudPoint point = pointCloud.Points[i];
                    Console.Write("[offset: {0}, x: {1}, y: {2}, z: {3}] ", point.Offset, point.X, point.Y, point.Z);
                }
                Console.Write("\n");
            }
            catch (NotAuthorizedException)
            {
                Console.WriteLine("Not authorized to pull point cloud");
            }
        }

        private static void Device_ConfigurationChangedEvent(object sender, ConfigurationChangedEventArgs e)
        {
            Console.WriteLine("Configuration changes, ID: {0}, Value: {1}", e.Id, e.Value);
        }

        private static async void Device_VideoAvailableEvent(object sender, VideoAvailableEventArgs e)
        {
            IDevice device = (IDevice)sender;

            Console.WriteLine("Video available for shot: {0}", e.ShotId);

            try
            {
                //Default location for shot video
                string videoDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToString(), @"Full Swing Golf\Launch Monitor Connect SDK\Shot Videos");
                if (!Directory.Exists(videoDirectory)) Directory.CreateDirectory(videoDirectory);

                string shotName = e.ShotId + ".mp4";
                string path = Path.Combine(videoDirectory, shotName);

                //Request shot video
                using (Stream shotVideoStream = await device.GetShotVideo(e.ShotId))
                {
                    using (Stream fileStream = File.Open(path, FileMode.Create))
                    {
                        await shotVideoStream.CopyToAsync(fileStream);
                    }
                }
            }
            catch (NotAuthorizedException)
            {
                Console.WriteLine("Not authorized to pull shot video");
            }
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
            if (e.Shot.AttackAngle.HasValue)
            {
                if (e.Shot.AttackAngleCalculationType.HasValue) Console.WriteLine("  Attack Angle: {0:0.00}    Calculation Type: {1}", e.Shot.AttackAngle, e.Shot.AttackAngleCalculationType);
                else Console.WriteLine("  Attack Angle: {0:0.00}", e.Shot.AttackAngle);
            }
            if (e.Shot.BallSpeed.HasValue)
            {
                if (e.Shot.BallSpeedCalculationType.HasValue) Console.WriteLine("  Ball Speed: {0:0.00}    Calculation Type: {1}", e.Shot.BallSpeed, e.Shot.BallSpeedCalculationType);
                else Console.WriteLine("  Ball Speed: {0:0.00}", e.Shot.BallSpeed);
            }
            if (e.Shot.ClubPath.HasValue)
            {
                if (e.Shot.ClubPathCalculationType.HasValue) Console.WriteLine("  Club Path: {0:0.00}    Calculation Type: {1}", e.Shot.ClubPath, e.Shot.ClubPathCalculationType);
                else Console.WriteLine("  Club Path: {0:0.00}", e.Shot.ClubPath);
            }
            if (e.Shot.ClubSpeed.HasValue)
            {
                if (e.Shot.ClubSpeedCalculationType.HasValue) Console.WriteLine("  Club Speed: {0:0.00}    Calculation Type: {1}", e.Shot.ClubSpeed, e.Shot.ClubSpeedCalculationType);
                else Console.WriteLine("  Club Speed: {0:0.00}", e.Shot.ClubSpeed);
            }
            if (e.Shot.FaceAngle.HasValue)
            {
                if (e.Shot.FaceAngleCalculationType.HasValue) Console.WriteLine("  Face Angle: {0:0.00}    Calculation Type: {1}", e.Shot.FaceAngle, e.Shot.FaceAngleCalculationType);
                else Console.WriteLine("  Face Angle: {0:0.00}", e.Shot.FaceAngle);
            }
            if (e.Shot.HorizontalLaunchAngle.HasValue)
            {
                if (e.Shot.HorizontalLaunchAngleCalculationType.HasValue) Console.WriteLine("  Horizontal Launch Angle: {0:0.00}    Calculation Type: {1}", e.Shot.HorizontalLaunchAngle, e.Shot.HorizontalLaunchAngleCalculationType);
                else Console.WriteLine("  Horizontal Launch Angle: {0:0.00}", e.Shot.HorizontalLaunchAngle);
            }
            if (e.Shot.SmashFactor.HasValue)
            {
                if (e.Shot.SmashFactorCalculationType.HasValue) Console.WriteLine("  Smash Factor: {0:0.00}    Calculation Type: {1}", e.Shot.SmashFactor, e.Shot.SpinRateCalculationType);
                else Console.WriteLine("  Smash Factor: {0:0.00}", e.Shot.SmashFactor);
            }
            if (e.Shot.SpinAxis.HasValue)
            {
                if (e.Shot.SpinAxisCalculationType.HasValue) Console.WriteLine("  Spin Axis: {0:0.00}    Calculation Type: {1}", e.Shot.SpinAxis, e.Shot.SpinRateCalculationType);
                else Console.WriteLine("  Spin Axis: {0:0.00}", e.Shot.SpinAxis);
            }
            if (e.Shot.SpinRate.HasValue)
            {
                if (e.Shot.SpinRateCalculationType.HasValue) Console.WriteLine("  Spin rate: {0:0.00}     Calculation Type: {1}", e.Shot.SpinRate, e.Shot.SpinRateCalculationType);
                else Console.WriteLine("  Spin rate: {0:0.00}", e.Shot.SpinRate);
            }
            if (e.Shot.VerticalLaunchAngle.HasValue)
            {
                if (e.Shot.VerticalLaunchAngleCalculationType.HasValue) Console.WriteLine("  Vertical Launch Angle: {0:0.00}    Calculation Type: {1}", e.Shot.VerticalLaunchAngle, e.Shot.VerticalLaunchAngleCalculationType);
                else Console.WriteLine("  Vertical Launch Angle: {0:0.00}", e.Shot.VerticalLaunchAngle);
            }
            if (e.Shot.Apex.HasValue)
            {
                if (e.Shot.ApexCalculationType.HasValue) Console.WriteLine("  Apex: {0:0.00}    Calculation Type: {1}", e.Shot.Apex, e.Shot.ApexCalculationType);
                else Console.WriteLine("  Apex: {0:0.00}", e.Shot.Apex);
            }
            if (e.Shot.CarryDistance.HasValue)
            {
                if (e.Shot.CarryDistanceCalculationType.HasValue) Console.WriteLine("  Carry Distance: {0:0.00}    Calculation Type: {1}", e.Shot.CarryDistance, e.Shot.CarryDistanceCalculationType);
                else Console.WriteLine("  Carry Distance: {0:0.00}", e.Shot.CarryDistance);
            }
            if (e.Shot.Side.HasValue)
            {
                if (e.Shot.SideCalculationType.HasValue) Console.WriteLine("  Side: {0:0.00}    Calculation Type: {1}", e.Shot.Side, e.Shot.SideCalculationType);
                else Console.WriteLine("  Side: {0:0.00}", e.Shot.Side);
            }
            if (e.Shot.SideTotal.HasValue)
            {
                if (e.Shot.SideTotalCalculationType.HasValue) Console.WriteLine("  Side Total: {0:0.00}    Calculation Type: {1}", e.Shot.SideTotal, e.Shot.SideTotalCalculationType);
                else Console.WriteLine("  Side Total: {0:0.00}", e.Shot.SideTotal);
            }
            if (e.Shot.TotalDistance.HasValue)
            {
                if (e.Shot.TotalDistanceCalculationType.HasValue) Console.WriteLine("  Total Distance: {0:0.00}    Calculation Type: {1}", e.Shot.TotalDistance, e.Shot.TotalDistanceCalculationType);
                else Console.WriteLine("  Total Distance: {0:0.00}", e.Shot.TotalDistance);
            }

            if (e.Type == ShotType.Normalized)
            {
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