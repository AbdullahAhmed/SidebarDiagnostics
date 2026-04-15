using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media;
using LibreHardwareMonitor.Hardware;
using Newtonsoft.Json;
using SidebarDiagnostics.Framework;

namespace SidebarDiagnostics.Monitoring
{
    public class MonitorManager : INotifyPropertyChanged, IDisposable
    {
        public MonitorManager(MonitorConfig[] config)
        {
            _computer = new Computer()
            {
                IsCpuEnabled = true,
                IsControllerEnabled = true,
                IsGpuEnabled = true,
                IsStorageEnabled = false,
                IsMotherboardEnabled = true,
                IsMemoryEnabled = true,
                IsNetworkEnabled = false
            };
            _computer.Open();
            _board = GetHardware(HardwareType.Motherboard).FirstOrDefault();

            UpdateBoard();

            MonitorPanels = config.Where(c => c.Enabled).OrderByDescending(c => c.Order).Select(c => NewPanel(c)).ToArray();
            _allMonitors = MonitorPanels.SelectMany(p => p.Monitors).ToArray();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (MonitorPanel _panel in MonitorPanels)
                    {
                        _panel.Dispose();
                    }

                    _computer.Close();

                    _monitorPanels = null;
                    _allMonitors = null;
                    _computer = null;
                    _board = null;
                }

                _disposed = true;
            }
        }

        ~MonitorManager()
        {
            Dispose(false);
        }

        public HardwareConfig[] GetHardware(MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                case MonitorType.RAM:
                case MonitorType.GPU:
                    return GetHardware(type.GetHardwareTypes()).Select(h => new HardwareConfig() { ID = h.Identifier.ToString(), Name = h.Name, ActualName = h.Name }).ToArray();

                case MonitorType.HD:
                    return DriveMonitor.GetHardware().ToArray();

                case MonitorType.Network:
                    return NetworkMonitor.GetHardware().ToArray();

                case MonitorType.Process:
                    return new HardwareConfig[0];

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public void Update()
        {
            UpdateBoard();

            foreach (iMonitor _monitor in _allMonitors)
            {
                _monitor.Update();
            }
        }

        private iMonitor[] _allMonitors { get; set; }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private IEnumerable<IHardware> GetHardware(params HardwareType[] types)
        {
            return _computer.Hardware.Where(h => types.Contains(h.HardwareType));
        }

        private MonitorPanel NewPanel(MonitorConfig config)
        {
            switch (config.Type)
            {
                case MonitorType.CPU:
                    return OHMPanel(
                        config.Type,
                        "M 19,19L 57,19L 57,22.063C 56.1374,22.285 55.5,23.0681 55.5,24C 55.5,24.9319 56.1374,25.715 57,25.937L 57,57L 19,57L 19,27.937C 19.8626,27.715 20.5,26.9319 20.5,26C 20.5,25.0681 19.8626,24.285 19,24.063L 19,19 Z M 21.9998,22.0005L 21.9998,24.0005L 23.9998,24.0005L 23.9998,22.0005L 21.9998,22.0005 Z M 24.9998,22.0005L 24.9998,24.0005L 26.9998,24.0005L 26.9998,22.0005L 24.9998,22.0005 Z M 27.9998,22.0005L 27.9998,24.0005L 29.9998,24.0005L 29.9998,22.0005L 27.9998,22.0005 Z M 30.9998,22.0005L 30.9998,24.0005L 32.9998,24.0005L 32.9998,22.0005L 30.9998,22.0005 Z M 33.9998,22.0005L 33.9998,24.0005L 35.9998,24.0005L 35.9998,22.0005L 33.9998,22.0005 Z M 36.9998,22.0005L 36.9998,24.0005L 38.9998,24.0005L 38.9998,22.0005L 36.9998,22.0005 Z M 39.9998,22.0005L 39.9998,24.0005L 41.9998,24.0005L 41.9998,22.0005L 39.9998,22.0005 Z M 42.9995,22.0005L 42.9995,24.0005L 44.9995,24.0005L 44.9995,22.0005L 42.9995,22.0005 Z M 45.9995,22.0005L 45.9995,24.0005L 47.9995,24.0005L 47.9995,22.0005L 45.9995,22.0005 Z M 48.9995,22.0004L 48.9995,24.0004L 50.9995,24.0004L 50.9995,22.0004L 48.9995,22.0004 Z M 51.9996,22.0004L 51.9996,24.0004L 53.9996,24.0004L 53.9996,22.0004L 51.9996,22.0004 Z M 21.9998,25.0004L 21.9998,27.0004L 23.9998,27.0004L 23.9998,25.0004L 21.9998,25.0004 Z M 24.9998,25.0004L 24.9998,27.0004L 26.9998,27.0004L 26.9998,25.0004L 24.9998,25.0004 Z M 27.9998,25.0004L 27.9998,27.0004L 29.9998,27.0004L 29.9998,25.0004L 27.9998,25.0004 Z M 30.9998,25.0004L 30.9998,27.0004L 32.9998,27.0004L 32.9998,25.0004L 30.9998,25.0004 Z M 33.9998,25.0004L 33.9998,27.0004L 35.9998,27.0004L 35.9998,25.0004L 33.9998,25.0004 Z M 36.9998,25.0004L 36.9998,27.0004L 38.9998,27.0004L 38.9998,25.0004L 36.9998,25.0004 Z M 39.9998,25.0004L 39.9998,27.0004L 41.9998,27.0004L 41.9998,25.0004L 39.9998,25.0004 Z M 42.9996,25.0004L 42.9996,27.0004L 44.9996,27.0004L 44.9996,25.0004L 42.9996,25.0004 Z M 45.9996,25.0004L 45.9996,27.0004L 47.9996,27.0004L 47.9996,25.0004L 45.9996,25.0004 Z M 48.9996,25.0004L 48.9996,27.0004L 50.9996,27.0004L 50.9996,25.0004L 48.9996,25.0004 Z M 51.9996,25.0004L 51.9996,27.0004L 53.9996,27.0004L 53.9996,25.0004L 51.9996,25.0004 Z M 21.9998,28.0004L 21.9998,30.0004L 23.9998,30.0004L 23.9998,28.0004L 21.9998,28.0004 Z M 24.9998,28.0004L 24.9998,30.0004L 26.9998,30.0004L 26.9998,28.0004L 24.9998,28.0004 Z M 27.9998,28.0004L 27.9998,30.0004L 29.9998,30.0004L 29.9998,28.0004L 27.9998,28.0004 Z M 30.9998,28.0004L 30.9998,30.0004L 32.9998,30.0004L 32.9998,28.0004L 30.9998,28.0004 Z M 33.9998,28.0004L 33.9998,30.0004L 35.9998,30.0004L 35.9998,28.0004L 33.9998,28.0004 Z M 36.9998,28.0004L 36.9998,30.0004L 38.9998,30.0004L 38.9998,28.0004L 36.9998,28.0004 Z M 39.9998,28.0004L 39.9998,30.0004L 41.9998,30.0004L 41.9998,28.0004L 39.9998,28.0004 Z M 42.9996,28.0004L 42.9996,30.0004L 44.9996,30.0004L 44.9996,28.0004L 42.9996,28.0004 Z M 45.9997,28.0004L 45.9997,30.0004L 47.9997,30.0004L 47.9997,28.0004L 45.9997,28.0004 Z M 48.9997,28.0003L 48.9997,30.0003L 50.9997,30.0003L 50.9997,28.0003L 48.9997,28.0003 Z M 51.9997,28.0003L 51.9997,30.0003L 53.9997,30.0003L 53.9997,28.0003L 51.9997,28.0003 Z M 21.9998,31.0003L 21.9998,33.0003L 23.9998,33.0003L 23.9998,31.0003L 21.9998,31.0003 Z M 24.9998,31.0003L 24.9998,33.0003L 26.9998,33.0003L 26.9998,31.0003L 24.9998,31.0003 Z M 27.9998,31.0003L 27.9998,33.0003L 29.9998,33.0003L 29.9998,31.0003L 27.9998,31.0003 Z M 45.9997,31.0003L 45.9997,33.0003L 47.9997,33.0003L 47.9997,31.0003L 45.9997,31.0003 Z M 48.9997,31.0003L 48.9997,33.0003L 50.9997,33.0003L 50.9997,31.0003L 48.9997,31.0003 Z M 51.9997,31.0003L 51.9997,33.0003L 53.9997,33.0003L 53.9997,31.0003L 51.9997,31.0003 Z M 21.9998,34.0001L 21.9998,36.0001L 23.9998,36.0001L 23.9998,34.0001L 21.9998,34.0001 Z M 24.9999,34.0001L 24.9999,36.0001L 26.9999,36.0001L 26.9999,34.0001L 24.9999,34.0001 Z M 27.9999,34.0001L 27.9999,36.0001L 29.9999,36.0001L 29.9999,34.0001L 27.9999,34.0001 Z M 45.9997,34.0001L 45.9997,36.0001L 47.9997,36.0001L 47.9997,34.0001L 45.9997,34.0001 Z M 48.9997,34.0001L 48.9997,36.0001L 50.9997,36.0001L 50.9997,34.0001L 48.9997,34.0001 Z M 51.9997,34.0001L 51.9997,36.0001L 53.9997,36.0001L 53.9997,34.0001L 51.9997,34.0001 Z M 21.9999,37.0001L 21.9999,39.0001L 23.9999,39.0001L 23.9999,37.0001L 21.9999,37.0001 Z M 24.9999,37.0001L 24.9999,39.0001L 26.9999,39.0001L 26.9999,37.0001L 24.9999,37.0001 Z M 27.9999,37.0001L 27.9999,39.0001L 29.9999,39.0001L 29.9999,37.0001L 27.9999,37.0001 Z M 45.9997,37.0001L 45.9997,39.0001L 47.9997,39.0001L 47.9997,37.0001L 45.9997,37.0001 Z M 48.9998,37.0001L 48.9998,39.0001L 50.9998,39.0001L 50.9998,37.0001L 48.9998,37.0001 Z M 51.9998,37.0001L 51.9998,39.0001L 53.9998,39.0001L 53.9998,37.0001L 51.9998,37.0001 Z M 21.9999,40.0001L 21.9999,42.0001L 23.9999,42.0001L 23.9999,40.0001L 21.9999,40.0001 Z M 24.9999,40.0001L 24.9999,42.0001L 26.9999,42.0001L 26.9999,40.0001L 24.9999,40.0001 Z M 27.9999,40.0001L 27.9999,42.0001L 29.9999,42.0001L 29.9999,40.0001L 27.9999,40.0001 Z M 45.9998,40.0001L 45.9998,42.0001L 47.9998,42.0001L 47.9998,40.0001L 45.9998,40.0001 Z M 48.9998,40.0001L 48.9998,42.0001L 50.9998,42.0001L 50.9998,40.0001L 48.9998,40.0001 Z M 51.9998,40.0001L 51.9998,42.0001L 53.9998,42.0001L 53.9998,40.0001L 51.9998,40.0001 Z M 21.9999,43.0001L 21.9999,45.0001L 23.9999,45.0001L 23.9999,43.0001L 21.9999,43.0001 Z M 24.9999,43.0001L 24.9999,45.0001L 26.9999,45.0001L 26.9999,43.0001L 24.9999,43.0001 Z M 27.9999,43.0001L 27.9999,45.0001L 29.9999,45.0001L 29.9999,43.0001L 27.9999,43.0001 Z M 45.9998,43.0001L 45.9998,45.0001L 47.9998,45.0001L 47.9998,43.0001L 45.9998,43.0001 Z M 48.9998,43.0001L 48.9998,45.0001L 50.9998,45.0001L 50.9998,43.0001L 48.9998,43.0001 Z M 51.9998,43.0001L 51.9998,45.0001L 53.9998,45.0001L 53.9998,43.0001L 51.9998,43.0001 Z M 21.9999,46.0001L 21.9999,48.0001L 23.9999,48.0001L 23.9999,46.0001L 21.9999,46.0001 Z M 24.9999,46.0001L 24.9999,48.0001L 26.9999,48.0001L 26.9999,46.0001L 24.9999,46.0001 Z M 27.9999,46.0001L 27.9999,48.0001L 29.9999,48.0001L 29.9999,46.0001L 27.9999,46.0001 Z M 30.9999,46.0001L 30.9999,48.0001L 32.9999,48.0001L 32.9999,46.0001L 30.9999,46.0001 Z M 33.9999,46.0001L 33.9999,48.0001L 35.9999,48.0001L 35.9999,46.0001L 33.9999,46.0001 Z M 36.9999,46.0001L 36.9999,48.0001L 38.9999,48.0001L 38.9999,46.0001L 36.9999,46.0001 Z M 39.9999,46.0001L 39.9999,48.0001L 41.9999,48.0001L 41.9999,46.0001L 39.9999,46.0001 Z M 42.9999,46.0001L 42.9999,48.0001L 44.9999,48.0001L 44.9999,46.0001L 42.9999,46.0001 Z M 45.9999,46.0001L 45.9999,48.0001L 47.9999,48.0001L 47.9999,46.0001L 45.9999,46.0001 Z M 48.9999,46.0001L 48.9999,48.0001L 50.9999,48.0001L 50.9999,46.0001L 48.9999,46.0001 Z M 51.9999,46.0001L 51.9999,48.0001L 53.9999,48.0001L 53.9999,46.0001L 51.9999,46.0001 Z M 21.9999,49.0001L 21.9999,51.0001L 23.9999,51.0001L 23.9999,49.0001L 21.9999,49.0001 Z M 24.9999,49.0001L 24.9999,51.0001L 26.9999,51.0001L 26.9999,49.0001L 24.9999,49.0001 Z M 27.9999,49.0001L 27.9999,51.0001L 29.9999,51.0001L 29.9999,49.0001L 27.9999,49.0001 Z M 30.9999,49.0001L 30.9999,51.0001L 33,51.0001L 33,49.0001L 30.9999,49.0001 Z M 34,49.0001L 34,51.0001L 36,51.0001L 36,49.0001L 34,49.0001 Z M 37,49.0001L 37,51.0001L 39,51.0001L 39,49.0001L 37,49.0001 Z M 40,49.0001L 40,51.0001L 42,51.0001L 42,49.0001L 40,49.0001 Z M 42.9999,49.0001L 42.9999,51.0001L 44.9999,51.0001L 44.9999,49.0001L 42.9999,49.0001 Z M 45.9999,49L 45.9999,51L 47.9999,51L 47.9999,49L 45.9999,49 Z M 48.9999,49L 48.9999,51L 50.9999,51L 50.9999,49L 48.9999,49 Z M 51.9999,49L 51.9999,51L 53.9999,51L 53.9999,49L 51.9999,49 Z M 22,52L 22,54L 24,54L 24,52L 22,52 Z M 25,52L 25,54L 27,54L 27,52L 25,52 Z M 28,52L 28,54L 30,54L 30,52L 28,52 Z M 31,52L 31,54L 33,54L 33,52L 31,52 Z M 34,52L 34,54L 36,54L 36,52L 34,52 Z M 37,52L 37,54L 39,54L 39,52L 37,52 Z M 40,52L 40,54L 42,54L 42,52L 40,52 Z M 43,52L 43,54L 45,54L 45,52L 43,52 Z M 46,52L 46,54L 48,54L 48,52L 46,52 Z M 49,52L 49,54L 51,54L 51,52L 49,52 Z M 52,52L 52,54L 54,54L 54,52L 52,52 Z M 31,31L 31,45L 45,45L 45,31L 31,31 Z M 33.6375,36.64L 33.4504,36.565L 33.3733,36.375L 33.4504,36.1829L 33.6375,36.1067L 33.8283,36.1829L 33.9067,36.375L 33.8283,36.5625L 33.6375,36.64 Z M 33.8533,40L 33.4266,40L 33.4266,37.3334L 33.8533,37.3334L 33.8533,40 Z M 36.9467,40L 36.52,40L 36.52,38.4942C 36.52,37.9336 36.3092,37.6533 35.8875,37.6533C 35.6697,37.6533 35.4896,37.7328 35.3471,37.8917C 35.2046,38.0506 35.1333,38.2514 35.1333,38.4942L 35.1333,40L 34.7066,40L 34.7066,37.3333L 35.1333,37.3333L 35.1333,37.7992L 35.1441,37.7992C 35.3486,37.4531 35.6444,37.28 36.0317,37.28C 36.3278,37.28 36.5543,37.3739 36.7112,37.5617C 36.8682,37.7495 36.9467,38.0206 36.9467,38.375L 36.9467,40 Z M 39.0267,39.9642L 38.6208,40.0533C 38.1447,40.0533 37.9067,39.7945 37.9067,39.2767L 37.9067,37.7067L 37.4267,37.7067L 37.4267,37.3333L 37.9067,37.3333L 37.9067,36.6733L 38.3333,36.5333L 38.3333,37.3333L 39.0267,37.3333L 39.0267,37.7067L 38.3333,37.7067L 38.3333,39.1892C 38.3333,39.3658 38.3647,39.4918 38.4275,39.5671C 38.4903,39.6424 38.5942,39.68 38.7392,39.68L 39.0267,39.5733L 39.0267,39.9642 Z M 41.6933,38.7733L 39.8267,38.7733C 39.8339,39.0628 39.9142,39.2863 40.0675,39.4438C 40.2208,39.6013 40.4319,39.68 40.7008,39.68C 41.003,39.68 41.2805,39.5911 41.5333,39.4133L 41.5333,39.8042C 41.3,39.9703 40.9911,40.0533 40.6067,40.0533C 40.2311,40.0533 39.9361,39.9331 39.7217,39.6925C 39.5072,39.452 39.4,39.1133 39.4,38.6767C 39.4,38.2645 39.516,37.9286 39.7479,37.6692C 39.9799,37.4097 40.268,37.28 40.6125,37.28C 40.9564,37.28 41.2225,37.3921 41.4108,37.6163C 41.5992,37.8404 41.6933,38.152 41.6933,38.5508L 41.6933,38.7733 Z M 41.2667,38.4C 41.265,38.1645 41.2058,37.9811 41.0892,37.85C 40.9725,37.7189 40.8103,37.6533 40.6025,37.6533C 40.4019,37.6533 40.2317,37.7222 40.0917,37.86C 39.9517,37.9978 39.8653,38.1778 39.8325,38.4L 41.2667,38.4 Z M 42.76,40L 42.3333,40L 42.3333,36.0533L 42.76,36.0533L 42.76,40 Z",
                        config.Hardware,
                        config.Metrics,
                        config.Params,
                        config.Type.GetHardwareTypes()
                        );

                case MonitorType.RAM:
                    return OHMPanel(
                        config.Type,
                        "M 473.00,193.00 C 473.00,193.00 434.00,193.00 434.00,193.00 434.00,193.00 434.00,245.00 434.00,245.00 434.00,245.00 259.00,245.00 259.00,245.00 259.00,239.01 259.59,235.54 256.67,230.00 247.91,213.34 228.26,212.83 217.65,228.00 213.65,233.71 214.00,238.44 214.00,245.00 214.00,245.00 27.00,245.00 27.00,245.00 27.00,245.00 27.00,193.00 27.00,193.00 27.00,193.00 0.00,193.00 0.00,193.00 0.00,193.00 0.00,20.00 0.00,20.00 12.36,19.43 21.26,13.56 18.00,0.00 18.00,0.00 453.00,0.00 453.00,0.00 453.01,7.85 454.03,15.96 463.00,18.82 465.56,19.42 470.18,19.04 473.00,18.82 473.00,18.82 473.00,193.00 473.00,193.00 Z M 433.00,39.00 C 433.00,39.00 386.00,39.00 386.00,39.00 386.00,39.00 386.00,147.00 386.00,147.00 386.00,147.00 433.00,147.00 433.00,147.00 433.00,147.00 433.00,39.00 433.00,39.00 Z M 423.00,193.00 C 423.00,193.00 399.00,193.00 399.00,193.00 399.00,193.00 399.00,224.00 399.00,224.00 399.00,224.00 387.00,224.00 387.00,224.00 387.00,224.00 387.00,193.00 387.00,193.00 387.00,193.00 377.00,193.00 377.00,193.00 377.00,193.00 377.00,224.00 377.00,224.00 377.00,224.00 365.00,224.00 365.00,224.00 365.00,224.00 365.00,193.00 365.00,193.00 365.00,193.00 354.00,193.00 354.00,193.00 354.00,193.00 354.00,224.00 354.00,224.00 354.00,224.00 343.00,224.00 343.00,224.00 343.00,224.00 343.00,193.00 343.00,193.00 343.00,193.00 333.00,193.00 333.00,193.00 333.00,193.00 333.00,224.00 333.00,224.00 333.00,224.00 322.00,224.00 322.00,224.00 322.00,224.00 322.00,193.00 322.00,193.00 322.00,193.00 311.00,193.00 311.00,193.00 311.00,193.00 311.00,224.00 311.00,224.00 311.00,224.00 300.00,224.00 300.00,224.00 300.00,224.00 300.00,193.00 300.00,193.00 300.00,193.00 289.00,193.00 289.00,193.00 289.00,193.00 289.00,224.00 289.00,224.00 289.00,224.00 277.00,224.00 277.00,224.00 277.00,224.00 277.00,193.00 277.00,193.00 277.00,193.00 191.00,193.00 191.00,193.00 191.00,193.00 191.00,224.00 191.00,224.00 191.00,224.00 179.00,224.00 179.00,224.00 179.00,224.00 179.00,193.00 179.00,193.00 179.00,193.00 169.00,193.00 169.00,193.00 169.00,193.00 169.00,224.00 169.00,224.00 169.00,224.00 157.00,224.00 157.00,224.00 157.00,224.00 157.00,193.00 157.00,193.00 157.00,193.00 146.00,193.00 146.00,193.00 146.00,193.00 146.00,224.00 146.00,224.00 146.00,224.00 134.00,224.00 134.00,224.00 134.00,224.00 134.00,193.00 134.00,193.00 134.00,193.00 125.00,193.00 125.00,193.00 125.00,193.00 125.00,224.00 125.00,224.00 125.00,224.00 114.00,224.00 114.00,224.00 114.00,224.00 114.00,193.00 114.00,193.00 114.00,193.00 103.00,193.00 103.00,193.00 103.00,193.00 103.00,224.00 103.00,224.00 103.00,224.00 91.00,224.00 91.00,224.00 91.00,224.00 91.00,193.00 91.00,193.00 91.00,193.00 81.00,193.00 81.00,193.00 81.00,193.00 81.00,224.00 81.00,224.00 81.00,224.00 69.00,224.00 69.00,224.00 69.00,224.00 69.00,193.00 69.00,193.00 69.00,193.00 39.00,193.00 39.00,193.00 39.00,193.00 39.00,234.00 39.00,234.00 39.00,234.00 203.00,234.00 203.00,234.00 204.62,218.32 219.49,205.67 235.00,205.04 245.28,204.62 255.94,209.24 262.67,217.04 265.14,219.89 267.13,223.51 268.54,227.00 269.28,228.84 269.93,231.78 271.56,232.98 273.27,234.24 276.91,234.00 279.00,234.00 279.00,234.00 423.00,234.00 423.00,234.00 423.00,234.00 423.00,193.00 423.00,193.00 Z M 367.00,39.00 C 367.00,39.00 320.00,39.00 320.00,39.00 320.00,39.00 320.00,147.00 320.00,147.00 320.00,147.00 367.00,147.00 367.00,147.00 367.00,147.00 367.00,39.00 367.00,39.00 Z M 303.00,39.00 C 303.00,39.00 256.00,39.00 256.00,39.00 256.00,39.00 256.00,147.00 256.00,147.00 256.00,147.00 303.00,147.00 303.00,147.00 303.00,147.00 303.00,39.00 303.00,39.00 Z M 215.00,39.00 C 215.00,39.00 168.00,39.00 168.00,39.00 168.00,39.00 168.00,147.00 168.00,147.00 168.00,147.00 215.00,147.00 215.00,147.00 215.00,147.00 215.00,39.00 215.00,39.00 Z M 148.00,39.00 C 148.00,39.00 101.00,39.00 101.00,39.00 101.00,39.00 101.00,147.00 101.00,147.00 101.00,147.00 148.00,147.00 148.00,147.00 148.00,147.00 148.00,39.00 148.00,39.00 Z M 84.00,39.00 C 84.00,39.00 37.00,39.00 37.00,39.00 37.00,39.00 37.00,147.00 37.00,147.00 37.00,147.00 84.00,147.00 84.00,147.00 84.00,147.00 84.00,39.00 84.00,39.00 Z",
                        config.Hardware,
                        config.Metrics,
                        config.Params,
                        config.Type.GetHardwareTypes()
                        );

                case MonitorType.GPU:
                    return OHMPanel(
                        config.Type,
                        "F1 M 20,23.0002L 55.9998,23.0002C 57.1044,23.0002 57.9998,23.8956 57.9998,25.0002L 57.9999,46C 57.9999,47.1046 57.1045,48 55.9999,48L 41,48L 41,53L 45,53C 46.1046,53 47,53.8954 47,55L 47,57L 29,57L 29,55C 29,53.8954 29.8955,53 31,53L 35,53L 35,48L 20,48C 18.8954,48 18,47.1046 18,46L 18,25.0002C 18,23.8956 18.8954,23.0002 20,23.0002 Z M 21,26.0002L 21,45L 54.9999,45L 54.9998,26.0002L 21,26.0002 Z",
                        config.Hardware,
                        config.Metrics,
                        config.Params,
                        config.Type.GetHardwareTypes()
                        );

                case MonitorType.HD:
                    return DrivePanel(
                        config.Type,
                        config.Hardware,
                        config.Metrics,
                        config.Params
                        );

                case MonitorType.Network:
                    return NetworkPanel(
                        config.Type,
                        config.Hardware,
                        config.Metrics,
                        config.Params
                        );

                case MonitorType.Process:
                    return ProcessPanel(
                        config.Type,
                        config.Metrics,
                        config.Params
                        );

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        private MonitorPanel OHMPanel(MonitorType type, string pathData, HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters, params HardwareType[] hardwareTypes)
        {
            return new MonitorPanel(
                type.GetDescription(),
                pathData,
                OHMMonitor.GetInstances(hardwareConfig, metrics, parameters, type, _board, GetHardware(hardwareTypes).ToArray())
                );
        }

        private MonitorPanel DrivePanel(MonitorType type, HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            return new MonitorPanel(
                type.GetDescription(),
                "m12.56977,260.69523l0,63.527l352.937,0l0,-63.527l-352.937,0zm232.938,45.881c-7.797,0 -14.118,-6.318 -14.118,-14.117c0,-7.801 6.321,-14.117 14.118,-14.117c7.795,0 14.117,6.316 14.117,14.117c0.001,7.798 -6.322,14.117 -14.117,14.117zm42.353,0c-7.797,0 -14.118,-6.318 -14.118,-14.117c0,-7.801 6.321,-14.117 14.118,-14.117c7.796,0 14.117,6.316 14.117,14.117c0,7.798 -6.321,14.117 -14.117,14.117zm42.352,0c-7.797,0 -14.117,-6.318 -14.117,-14.117c0,-7.801 6.32,-14.117 14.117,-14.117c7.796,0 14.118,6.316 14.118,14.117c0,7.798 -6.323,14.117 -14.118,14.117 M309.0357666015625,52.46223449707031 69.03976440429688,52.46223449707031 12.569778442382812,246.57623291015625 365.50677490234375,246.57623291015625z",
                DriveMonitor.GetInstances(hardwareConfig, metrics, parameters)
                );
        }

        private MonitorPanel NetworkPanel(MonitorType type, HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            return new MonitorPanel(
                type.GetDescription(),
                "M 40,44L 39.9999,51L 44,51C 45.1046,51 46,51.8954 46,53L 46,57C 46,58.1046 45.1045,59 44,59L 32,59C 30.8954,59 30,58.1046 30,57L 30,53C 30,51.8954 30.8954,51 32,51L 36,51L 36,44L 40,44 Z M 47,53L 57,53L 57,57L 47,57L 47,53 Z M 29,53L 29,57L 19,57L 19,53L 29,53 Z M 19,22L 57,22L 57,31L 19,31L 19,22 Z M 55,24L 53,24L 53,29L 55,29L 55,24 Z M 51,24L 49,24L 49,29L 51,29L 51,24 Z M 47,24L 45,24L 45,29L 47,29L 47,24 Z M 21,27L 21,29L 23,29L 23,27L 21,27 Z M 19,33L 57,33L 57,42L 19,42L 19,33 Z M 55,35L 53,35L 53,40L 55,40L 55,35 Z M 51,35L 49,35L 49,40L 51,40L 51,35 Z M 47,35L 45,35L 45,40L 47,40L 47,35 Z M 21,38L 21,40L 23,40L 23,38L 21,38 Z",
                NetworkMonitor.GetInstances(hardwareConfig, metrics, parameters)
                );
        }

        private MonitorPanel ProcessPanel(MonitorType type, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            int count = parameters.GetValue<int>(ParamKey.ProcessCount);
            bool sortByCpu = parameters.GetValue<bool>(ParamKey.ProcessSortByCpu);
            bool showCpu = metrics.IsEnabled(MetricKey.ProcessCpu);
            bool showRam = metrics.IsEnabled(MetricKey.ProcessRam);
            bool showClose = metrics.IsEnabled(MetricKey.ProcessClose);

            return new MonitorPanel(
                type.GetDescription(),
                "M 19,28L 57,28L 57,50L 19,50L 19,28 Z M 21,30L 21,48L 55,48L 55,30L 21,30 Z M 23,32L 35,32L 35,34L 23,34L 23,32 Z M 23,36L 45,36L 45,38L 23,38L 23,36 Z M 23,40L 40,40L 40,42L 23,42L 23,40 Z M 23,44L 35,44L 35,46L 23,46L 23,44 Z M 47,32L 53,32L 53,46L 47,46L 47,32 Z M 49,34L 51,34L 51,44L 49,44L 49,34 Z",
                new ProcessMonitor(count, sortByCpu, showCpu, showRam, showClose)
                );
        }

        private void UpdateBoard()
        {
            _board.Update();
        }

        private MonitorPanel[] _monitorPanels { get; set; }

        public MonitorPanel[] MonitorPanels
        {
            get
            {
                return _monitorPanels;
            }
            private set
            {
                _monitorPanels = value;

                NotifyPropertyChanged("MonitorPanels");
            }
        }

        private Computer _computer { get; set; }

        private IHardware _board { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class MonitorPanel : INotifyPropertyChanged, IDisposable
    {
        public MonitorPanel(string title, string iconData, params iMonitor[] monitors)
        {
            IconPath = Geometry.Parse(iconData);
            Title = title;

            Monitors = monitors;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (iMonitor _monitor in Monitors)
                    {
                        _monitor.Dispose();
                    }

                    _monitors = null;
                    _iconPath = null;
                }

                _disposed = true;
            }
        }

        ~MonitorPanel()
        {
            Dispose(false);
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private Geometry _iconPath { get; set; }

        public Geometry IconPath
        {
            get
            {
                return _iconPath;
            }
            private set
            {
                _iconPath = value;

                NotifyPropertyChanged("IconPath");
            }
        }

        private string _title { get; set; }

        public string Title
        {
            get
            {
                return _title;
            }
            private set
            {
                _title = value;

                NotifyPropertyChanged("Title");
            }
        }

        private iMonitor[] _monitors { get; set; }

        public iMonitor[] Monitors
        {
            get
            {
                return _monitors;
            }
            private set
            {
                _monitors = value;

                NotifyPropertyChanged("Monitors");
            }
        }

        private bool _disposed { get; set; } = false;
    }

    public interface iMonitor : INotifyPropertyChanged, IDisposable
    {
        string ID { get; }

        string Name { get; }

        bool ShowName { get; }

        iMetric[] Metrics { get; }

        void Update();
    }

    public class BaseMonitor : iMonitor
    {
        public BaseMonitor(string id, string name, bool showName)
        {
            ID = id;
            Name = name;
            ShowName = showName;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (iMetric _metric in Metrics)
                    {
                        _metric.Dispose();
                    }

                    _metrics = null;
                }

                _disposed = true;
            }
        }

        ~BaseMonitor()
        {
            Dispose(false);
        }

        public virtual void Update()
        {
            foreach (iMetric _metric in Metrics)
            {
                _metric.Update();
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _id { get; set; }

        public string ID
        {
            get
            {
                return _id;
            }
            protected set
            {
                _id = value;

                NotifyPropertyChanged("ID");
            }
        }

        private string _name { get; set; }

        public string Name
        {
            get
            {
                return _name;
            }
            protected set
            {
                _name = value;

                NotifyPropertyChanged("Name");
            }
        }

        private bool _showName { get; set; }

        public bool ShowName
        {
            get
            {
                return _showName;
            }
            protected set
            {
                _showName = value;

                NotifyPropertyChanged("ShowName");
            }
        }

        private iMetric[] _metrics { get; set; }

        public iMetric[] Metrics
        {
            get
            {
                return _metrics;
            }
            protected set
            {
                _metrics = value;

                NotifyPropertyChanged("Metrics");
            }
        }

        private bool _disposed { get; set; } = false;
    }

    public class OHMMonitor : BaseMonitor
    {
        public OHMMonitor(MonitorType type, string id, string name, IHardware hardware, IHardware board, MetricConfig[] metrics, ConfigParam[] parameters) : base(id, name, parameters.GetValue<bool>(ParamKey.HardwareNames))
        {
            _hardware = hardware;

            UpdateHardware();

            switch (type)
            {
                case MonitorType.CPU:
                    InitCPU(
                        board,
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll),
                        parameters.GetValue<bool>(ParamKey.AllCoreClocks),
                        parameters.GetValue<bool>(ParamKey.UseGHz),
                        parameters.GetValue<bool>(ParamKey.UseFahrenheit),
                        parameters.GetValue<int>(ParamKey.TempAlert)
                        );
                    break;

                case MonitorType.RAM:
                    InitRAM(
                        board,
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll)
                        );
                    break;

                case MonitorType.GPU:
                    InitGPU(
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll),
                        parameters.GetValue<bool>(ParamKey.UseGHz),
                        parameters.GetValue<bool>(ParamKey.UseFahrenheit),
                        parameters.GetValue<int>(ParamKey.TempAlert)
                        );
                    break;

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    if (_loadBarMetric != null)
                    {
                        _loadBarMetric.Dispose();
                        _loadBarMetric = null;
                    }

                    if (_tempBarMetric != null)
                    {
                        _tempBarMetric.Dispose();
                        _tempBarMetric = null;
                    }

                    if (_vramLoadBarMetric != null)
                    {
                        _vramLoadBarMetric.Dispose();
                        _vramLoadBarMetric = null;
                    }

                    _hardware = null;
                }

                _disposed = true;
            }
        }

        ~OHMMonitor()
        {
            Dispose(false);
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters, MonitorType type, IHardware board, IHardware[] hardware)
        {
            return (
                from hw in hardware
                join c in hardwareConfig on hw.Identifier.ToString() equals c.ID into merged
                from n in merged.DefaultIfEmpty(new HardwareConfig() { ID = hw.Identifier.ToString(), Name = hw.Name, ActualName = hw.Name }).Select(n => { if (n.ActualName != hw.Name) { n.Name = n.ActualName = hw.Name; } return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new OHMMonitor(type, n.ID, n.Name ?? n.ActualName, hw, board, metrics, parameters)
                ).ToArray();
        }

        private void UpdateHardware()
        {
            _hardware.Update();
        }

        private void InitCPU(IHardware board, MetricConfig[] metrics, bool roundAll, bool allCoreClocks, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<OHMMetric> _sensorList = new List<OHMMetric>();

            if (metrics.IsEnabled(MetricKey.CPUClock))
            {
                Regex regex = new Regex(@"^.*(CPU|Core).*#(\d+)$");

                var coreClocks = _hardware.Sensors
                    .Where(s => s.SensorType == SensorType.Clock)
                    .Select(s => new
                    {
                        Match = regex.Match(s.Name),
                        Sensor = s
                    })
                    .Where(s => s.Match.Success)
                    .Select(s => new
                    {
                        Index = int.Parse(s.Match.Groups[2].Value),
                        s.Sensor
                    })
                    .OrderBy(s => s.Index)
                    .ToList();

                if (coreClocks.Count > 0)
                {
                    if (allCoreClocks)
                    {
                        foreach (var coreClock in coreClocks)
                        {
                            _sensorList.Add(new OHMMetric(coreClock.Sensor, MetricKey.CPUClock, DataType.MHz, string.Format("{0} {1}", Resources.CPUCoreClockLabel, coreClock.Index - 1), (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                        }
                    }
                    else
                    {
                        ISensor firstClock = coreClocks
                            .Select(s => s.Sensor)
                            .FirstOrDefault();

                        _sensorList.Add(new OHMMetric(firstClock, MetricKey.CPUClock, DataType.MHz, null, (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUVoltage))
            {
                ISensor _voltage = null;

                if (board != null)
                {
                    _voltage = board.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("CPU")).FirstOrDefault();
                }

                if (_voltage == null)
                {
                    _voltage = _hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage).FirstOrDefault();
                }

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.CPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            bool _cpuTempBarEnabled = metrics.IsEnabled(MetricKey.CPUTempBar);

            if (metrics.IsEnabled(MetricKey.CPUTemp) || _cpuTempBarEnabled)
            {
                ISensor[] _tempSensors = GetCpuTemperatureSensors(board);

                if (_tempSensors.Length > 0)
                {
                    if (metrics.IsEnabled(MetricKey.CPUTemp))
                    {
                        _sensorList.Add(new OHMMetric(_tempSensors, MetricKey.CPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null), IsValidCpuTemperatureSensorValue));
                    }

                    if (_cpuTempBarEnabled)
                    {
                        // Always Celsius (no converter) so value maps directly to 0-100°C scale
                        _tempBarMetric = new OHMMetric(_tempSensors, MetricKey.CPUTempBar, DataType.Celcius, null, roundAll, tempAlert, null, IsValidCpuTemperatureSensorValue);
                        ShowTempBar = true;
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUFan))
            {
                ISensor _fanSensor = null;

                if (board != null)
                {
                    _fanSensor = board.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType) && s.Name.Contains("CPU")).FirstOrDefault();
                }

                if (_fanSensor == null)
                {
                    _fanSensor = _hardware.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType)).FirstOrDefault();
                }

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.CPUFan, DataType.RPM, null, roundAll));
                }
            }

            bool _loadBarEnabled = metrics.IsEnabled(MetricKey.CPULoadBar);
            bool _loadEnabled = metrics.IsEnabled(MetricKey.CPULoad);
            bool _coreLoadEnabled = metrics.IsEnabled(MetricKey.CPUCoreLoad);

            if (_loadBarEnabled || _loadEnabled || _coreLoadEnabled)
            {
                ISensor[] _loadSensors = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load).ToArray();

                if (_loadSensors.Length > 0)
                {
                    ISensor _totalCPU = _loadSensors.Where(s => s.Index == 0).FirstOrDefault();

                    if (_loadBarEnabled && _totalCPU != null)
                    {
                        _loadBarMetric = new OHMMetric(_totalCPU, MetricKey.CPULoadBar, DataType.Percent, null, roundAll);
                        ShowLoadBar = true;
                    }

                    if (_loadEnabled && _totalCPU != null)
                    {
                        _sensorList.Add(new OHMMetric(_totalCPU, MetricKey.CPULoad, DataType.Percent, null, roundAll));
                    }

                    if (_coreLoadEnabled)
                    {
                        for (int i = 1; i <= _loadSensors.Max(s => s.Index); i++)
                        {
                            ISensor _coreLoad = _loadSensors.Where(s => s.Index == i).FirstOrDefault();

                            if (_coreLoad != null)
                            {
                                _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.CPUCoreLoad, DataType.Percent, string.Format("{0} {1}", Resources.CPUCoreLoadLabel, i - 1), roundAll));
                            }
                        }
                    }
                }
            }

            Metrics = _sensorList.ToArray();
        }

        public void InitRAM(IHardware board, MetricConfig[] metrics, bool roundAll)
        {
            List<OHMMetric> _sensorList = new List<OHMMetric>();

            if (metrics.IsEnabled(MetricKey.RAMClock))
            {
                ISensor _ramClock = _hardware.Sensors.Where(s => s.SensorType == SensorType.Clock).FirstOrDefault();

                if (_ramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_ramClock, MetricKey.RAMClock, DataType.MHz, null, true));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMVoltage))
            {
                ISensor _voltage = null;

                if (board != null)
                {
                    _voltage = board.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("RAM")).FirstOrDefault();
                }

                if (_voltage == null)
                {
                    _voltage = _hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage).FirstOrDefault();
                }

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.RAMVoltage, DataType.Voltage, null, roundAll));
                }
            }

            bool _ramLoadBarEnabled = metrics.IsEnabled(MetricKey.RAMLoadBar);

            if (_ramLoadBarEnabled || metrics.IsEnabled(MetricKey.RAMLoad))
            {
                ISensor _loadSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                if (_loadSensor != null)
                {
                    if (_ramLoadBarEnabled)
                    {
                        _loadBarMetric = new OHMMetric(_loadSensor, MetricKey.RAMLoadBar, DataType.Percent, null, roundAll);
                        ShowLoadBar = true;
                    }

                    if (metrics.IsEnabled(MetricKey.RAMLoad))
                    {
                        _sensorList.Add(new OHMMetric(_loadSensor, MetricKey.RAMLoad, DataType.Percent, null, roundAll));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMUsed))
            {
                ISensor _usedSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 0).FirstOrDefault();

                if (_usedSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_usedSensor, MetricKey.RAMUsed, DataType.Gigabyte, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMFree))
            {
                ISensor _freeSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 1).FirstOrDefault();

                if (_freeSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_freeSensor, MetricKey.RAMFree, DataType.Gigabyte, null, roundAll));
                }
            }

            Metrics = _sensorList.ToArray();
        }

        public void InitGPU(MetricConfig[] metrics, bool roundAll, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<iMetric> _sensorList = new List<iMetric>();

            if (metrics.IsEnabled(MetricKey.GPUCoreClock))
            {
                ISensor _coreClock = _hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Core")).FirstOrDefault();

                if (_coreClock != null)
                {
                    _sensorList.Add(new OHMMetric(_coreClock, MetricKey.GPUCoreClock, DataType.MHz, null, (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMClock))
            {
                ISensor _vramClock = _hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Memory")).FirstOrDefault();

                if (_vramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_vramClock, MetricKey.GPUVRAMClock, DataType.MHz, null, (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            bool _gpuLoadBarEnabled = metrics.IsEnabled(MetricKey.GPULoadBar);

            if (metrics.IsEnabled(MetricKey.GPUCoreLoad) || _gpuLoadBarEnabled)
            {
                ISensor _coreLoad = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Core")).FirstOrDefault() ??
                    _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                if (_coreLoad != null)
                {
                    if (metrics.IsEnabled(MetricKey.GPUCoreLoad))
                    {
                        _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.GPUCoreLoad, DataType.Percent, null, roundAll));
                    }

                    if (_gpuLoadBarEnabled)
                    {
                        _loadBarMetric = new OHMMetric(_coreLoad, MetricKey.GPULoadBar, DataType.Percent, null, roundAll);
                        ShowLoadBar = true;
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMLoad))
            {
                ISensor _memoryUsed = _hardware.Sensors.Where(s => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) && s.Name == "GPU Memory Used").FirstOrDefault();
                ISensor _memoryTotal = _hardware.Sensors.Where(s => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) && s.Name == "GPU Memory Total").FirstOrDefault();

                if (_memoryUsed != null && _memoryTotal != null)
                {
                    _sensorList.Add(new GPUVRAMMLoadMetric(_memoryUsed, _memoryTotal, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                }
                else
                {
                    ISensor _vramLoad = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Memory")).FirstOrDefault() ??
                        _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 1).FirstOrDefault();

                    if (_vramLoad != null)
                    {
                        _sensorList.Add(new OHMMetric(_vramLoad, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                    }
                }
            }

            bool _vramLoadBarEnabled = metrics.IsEnabled(MetricKey.VRAMLoadBar);

            if (_vramLoadBarEnabled)
            {
                ISensor _memoryUsed = _hardware.Sensors.Where(s => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) && s.Name == "GPU Memory Used").FirstOrDefault();
                ISensor _memoryTotal = _hardware.Sensors.Where(s => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) && s.Name == "GPU Memory Total").FirstOrDefault();

                if (_memoryUsed != null && _memoryTotal != null)
                {
                    _vramLoadBarMetric = new GPUVRAMMLoadMetric(_memoryUsed, _memoryTotal, MetricKey.VRAMLoadBar, DataType.Percent, null, roundAll);
                    ShowVRAMLoadBar = true;
                }
                else
                {
                    ISensor _vramLoad = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Memory")).FirstOrDefault() ??
                        _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 1).FirstOrDefault();

                    if (_vramLoad != null)
                    {
                        _vramLoadBarMetric = new OHMMetric(_vramLoad, MetricKey.VRAMLoadBar, DataType.Percent, null, roundAll);
                        ShowVRAMLoadBar = true;
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVoltage))
            {
                ISensor _voltage = _hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Index == 0).FirstOrDefault();

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.GPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            bool _gpuTempBarEnabled = metrics.IsEnabled(MetricKey.GPUTempBar);

            if (metrics.IsEnabled(MetricKey.GPUTemp) || _gpuTempBarEnabled)
            {
                ISensor _tempSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Index == 0).FirstOrDefault();

                if (_tempSensor != null)
                {
                    if (metrics.IsEnabled(MetricKey.GPUTemp))
                    {
                        _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.GPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                    }

                    if (_gpuTempBarEnabled)
                    {
                        _tempBarMetric = new OHMMetric(_tempSensor, MetricKey.GPUTempBar, DataType.Celcius, null, roundAll, tempAlert);
                        ShowTempBar = true;
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUFan))
            {
                ISensor _fanSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Control).OrderBy(s => s.Index).FirstOrDefault();

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.GPUFan, DataType.Percent));
                }
            }

            Metrics = _sensorList.ToArray();
        }

        public override void Update()
        {
            UpdateHardware();

            if (_loadBarMetric != null)
            {
                _loadBarMetric.Update();
            }

            if (_tempBarMetric != null)
            {
                _tempBarMetric.Update();
            }

            _vramLoadBarMetric?.Update();

            base.Update();
        }

        private ISensor[] GetCpuTemperatureSensors(IHardware board)
        {
            List<ISensor> sensors = new List<ISensor>();

            AddCpuTemperatureSensors(sensors, _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("CCDs Max (Tdie)")));

            if (board != null)
            {
                AddCpuTemperatureSensors(sensors, board.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("CPU")));
            }

            AddCpuTemperatureSensors(sensors, _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && (s.Name == "CPU Package" || s.Name.Contains("Tdie"))));
            AddCpuTemperatureSensors(sensors, _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature));

            return sensors.ToArray();
        }

        private static void AddCpuTemperatureSensors(List<ISensor> sensors, IEnumerable<ISensor> candidates)
        {
            foreach (ISensor sensor in candidates)
            {
                if (sensor == null || sensors.Any(s => s.Identifier.ToString() == sensor.Identifier.ToString()))
                {
                    continue;
                }

                sensors.Add(sensor);
            }
        }

        private static bool IsValidCpuTemperatureSensorValue(ISensor sensor)
        {
            if (sensor == null || !sensor.Value.HasValue)
            {
                return false;
            }

            double value = sensor.Value.Value;

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return false;
            }

            // A CPU temperature at or below 0 C is a dead sensor on normal desktop hardware.
            return value > 0d;
        }

        private bool _showLoadBar { get; set; }

        public bool ShowLoadBar
        {
            get
            {
                return _showLoadBar;
            }
            private set
            {
                _showLoadBar = value;

                NotifyPropertyChanged("ShowLoadBar");
            }
        }

        private OHMMetric _loadBarMetric { get; set; }

        public iMetric LoadBarMetric
        {
            get
            {
                return _loadBarMetric;
            }
        }

        private bool _showTempBar { get; set; }

        public bool ShowTempBar
        {
            get
            {
                return _showTempBar;
            }
            private set
            {
                _showTempBar = value;

                NotifyPropertyChanged("ShowTempBar");
            }
        }

        private OHMMetric _tempBarMetric { get; set; }

        public iMetric TempBarMetric
        {
            get
            {
                return _tempBarMetric;
            }
        }

        private bool _showVRAMLoadBar { get; set; }

        public bool ShowVRAMLoadBar
        {
            get { return _showVRAMLoadBar; }
            private set
            {
                _showVRAMLoadBar = value;
                NotifyPropertyChanged("ShowVRAMLoadBar");
            }
        }

        private BaseMetric _vramLoadBarMetric { get; set; }

        public iMetric VRAMLoadBarMetric
        {
            get { return _vramLoadBarMetric; }
        }

        private IHardware _hardware { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class DriveMonitor : BaseMonitor
    {
        private const string CATEGORYNAME = "LogicalDisk";

        private const string FREEMB = "Free Megabytes";
        private const string PERCENTFREE = "% Free Space";
        private const string BYTESREADPERSECOND = "Disk Read Bytes/sec";
        private const string BYTESWRITEPERSECOND = "Disk Write Bytes/sec";

        public DriveMonitor(string id, string name, MetricConfig[] metrics, bool roundAll = false, double usedSpaceAlert = 0) : base(id, name, true)
        {
            _loadEnabled = metrics.IsEnabled(MetricKey.DriveLoad);

            bool _loadBarEnabled = metrics.IsEnabled(MetricKey.DriveLoadBar);
            bool _usedEnabled = metrics.IsEnabled(MetricKey.DriveUsed);
            bool _freeEnabled = metrics.IsEnabled(MetricKey.DriveFree);
            bool _readEnabled = metrics.IsEnabled(MetricKey.DriveRead);
            bool _writeEnabled = metrics.IsEnabled(MetricKey.DriveWrite);

            if (_loadBarEnabled)
            {
                if (metrics.Count(m => m.Enabled) == 1 && new Regex("^[A-Z]:$").IsMatch(name))
                {
                    Status = State.LoadBarInline;
                }
                else
                {
                    Status = State.LoadBarStacked;
                }
            }
            else
            {
                Status = State.NoLoadBar;
            }

            if (_loadBarEnabled || _loadEnabled || _usedEnabled || _freeEnabled)
            {
                _counterFreeMB = new PerformanceCounter(CATEGORYNAME, FREEMB, id);
                _counterFreePercent = new PerformanceCounter(CATEGORYNAME, PERCENTFREE, id);
            }

            List<iMetric> _metrics = new List<iMetric>();

            if (_loadBarEnabled || _loadEnabled)
            {
                LoadMetric = new BaseMetric(MetricKey.DriveLoad, DataType.Percent, null, roundAll, usedSpaceAlert);
                _metrics.Add(LoadMetric);
            }

            // Always create Used/Free metrics when load bar is enabled so the badge
            // can display values even if the user has unchecked the text rows.
            if (_loadBarEnabled || _usedEnabled)
            {
                UsedMetric = new BaseMetric(MetricKey.DriveUsed, DataType.Gigabyte, null, roundAll);
                if (_usedEnabled) _metrics.Add(UsedMetric);
            }

            if (_loadBarEnabled || _freeEnabled)
            {
                FreeMetric = new BaseMetric(MetricKey.DriveFree, DataType.Gigabyte, null, roundAll);
                if (_freeEnabled) _metrics.Add(FreeMetric);
            }

            if (_readEnabled)
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESREADPERSECOND, id), MetricKey.DriveRead, DataType.kBps, null, roundAll, 0, BytesPerSecondConverter.Instance));
            }

            if (_writeEnabled)
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESWRITEPERSECOND, id), MetricKey.DriveWrite, DataType.kBps, null, roundAll, 0, BytesPerSecondConverter.Instance));
            }

            Metrics = _metrics.ToArray();
            _driveMetrics = _loadEnabled ? Metrics : Metrics.Where(m => m.Key != MetricKey.DriveLoad).ToArray();
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    if (_loadMetric != null)
                    {
                        _loadMetric.Dispose();
                        _loadMetric = null;
                    }

                    if (_usedMetric != null)
                    {
                        _usedMetric.Dispose();
                        _usedMetric = null;
                    }

                    if (_freeMetric != null)
                    {
                        _freeMetric.Dispose();
                        _freeMetric = null;
                    }

                    if (_counterFreeMB != null)
                    {
                        _counterFreeMB.Dispose();
                        _counterFreeMB = null;
                    }

                    if (_counterFreePercent != null)
                    {
                        _counterFreePercent.Dispose();
                        _counterFreePercent = null;
                    }
                }

                _disposed = true;
            }
        }

        ~DriveMonitor()
        {
            Dispose(false);
        }

        public static IEnumerable<HardwareConfig> GetHardware()
        {
            string[] _instances;

            try
            {
                _instances = new PerformanceCounterCategory(CATEGORYNAME).GetInstanceNames();
            }
            catch (InvalidOperationException)
            {
                _instances = new string[0];

                App.ShowPerformanceCounterError();
            }

            Regex _regex = new Regex("^[A-Z]:$");

            return _instances.Where(n => _regex.IsMatch(n)).OrderBy(d => d[0]).Select(h => new HardwareConfig() { ID = h, Name = h, ActualName = h });
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            bool _roundAll = parameters.GetValue<bool>(ParamKey.RoundAll);
            int _usedSpaceAlert = parameters.GetValue<int>(ParamKey.UsedSpaceAlert);

            return (
                from hw in GetHardware()
                join c in hardwareConfig on hw.ID equals c.ID into merged
                from n in merged.DefaultIfEmpty(hw).Select(n => { n.ActualName = hw.Name; return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new DriveMonitor(n.ID, n.Name ?? n.ActualName, metrics, _roundAll, _usedSpaceAlert)
                ).ToArray();
        }

        public override void Update()
        {
            if (!_countersAvailable)
            {
                return;
            }

            try
            {
                if (_counterFreeMB != null && _counterFreePercent != null)
                {
                    double _freeGB = _counterFreeMB.NextValue() / 1024d;
                    double _freePercent = _counterFreePercent.NextValue();

                    double _usedPercent = 100d - _freePercent;

                    double _totalGB = _freeGB / (_freePercent / 100d);
                    double _usedGB = _totalGB - _freeGB;

                    if (LoadMetric != null)
                    {
                        LoadMetric.Update(_usedPercent);
                    }

                    if (UsedMetric != null)
                    {
                        UsedMetric.Update(_usedGB);
                    }

                    if (FreeMetric != null)
                    {
                        FreeMetric.Update(_freeGB);
                    }
                }

                base.Update();
            }
            catch (InvalidOperationException)
            {
                _countersAvailable = false;
            }
            catch (UnauthorizedAccessException)
            {
                _countersAvailable = false;
            }
            catch (Win32Exception)
            {
                _countersAvailable = false;
            }
        }

        private State _status { get; set; }

        public State Status
        {
            get
            {
                return _status;
            }
            private set
            {
                _status = value;

                NotifyPropertyChanged("Status");
            }
        }

        private iMetric _loadMetric { get; set; }

        public iMetric LoadMetric
        {
            get
            {
                return _loadMetric;
            }
            private set
            {
                _loadMetric = value;

                NotifyPropertyChanged("LoadMetric");
            }
        }

        private iMetric _usedMetric { get; set; }

        public iMetric UsedMetric
        {
            get
            {
                return _usedMetric;
            }
            private set
            {
                _usedMetric = value;

                NotifyPropertyChanged("UsedMetric");
            }
        }

        private iMetric _freeMetric { get; set; }

        public iMetric FreeMetric
        {
            get
            {
                return _freeMetric;
            }
            private set
            {
                _freeMetric = value;

                NotifyPropertyChanged("FreeMetric");
            }
        }

        public iMetric[] DriveMetrics
        {
            get
            {
                return _driveMetrics;
            }
        }

        private PerformanceCounter _counterFreeMB { get; set; }

        private PerformanceCounter _counterFreePercent { get; set; }

        private bool _loadEnabled { get; set; }

        private iMetric[] _driveMetrics { get; set; }

        private bool _countersAvailable { get; set; } = true;

        private bool _disposed { get; set; } = false;

        public enum State : byte
        {
            NoLoadBar,
            LoadBarInline,
            LoadBarStacked
        }
    }

    public class NetworkMonitor : BaseMonitor
    {
        private const string CATEGORYNAME = "Network Interface";

        private const string BYTESRECEIVEDPERSECOND = "Bytes Received/sec";
        private const string BYTESSENTPERSECOND = "Bytes Sent/sec";

        public NetworkMonitor(string id, string name, IDictionary<string, string> adapterIpLookup, bool includeExtIP, MetricConfig[] metrics, bool showName = true, bool roundAll = false, bool useBytes = false, double bandwidthInAlert = 0, double bandwidthOutAlert = 0) : base(id, name, showName)
        {
            iConverter _converter;

            if (useBytes)
            {
                _converter = BytesPerSecondConverter.Instance;
            }
            else
            {
                _converter = BitsPerSecondConverter.Instance;
            }

            List<iMetric> _metrics = new List<iMetric>();

            if (metrics.IsEnabled(MetricKey.NetworkIP))
            {
                string _ipAddress = GetAdapterIPAddress(name, adapterIpLookup);

                if (!string.IsNullOrEmpty(_ipAddress))
                {
                    _metrics.Add(new IPMetric(_ipAddress, MetricKey.NetworkIP, DataType.IP));
                }
            }

            if (includeExtIP)
            {
                _externalIPMetric = new IPMetric("Loading...", MetricKey.NetworkExtIP, DataType.IP);
                _metrics.Add(_externalIPMetric);
            }

            if (metrics.IsEnabled(MetricKey.NetworkIn))
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESRECEIVEDPERSECOND, id), MetricKey.NetworkIn, DataType.kbps, null, roundAll, bandwidthInAlert, _converter));
            }

            if (metrics.IsEnabled(MetricKey.NetworkOut))
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESSENTPERSECOND, id), MetricKey.NetworkOut, DataType.kbps, null, roundAll, bandwidthOutAlert, _converter));
            }

            if (metrics.IsEnabled(MetricKey.NetworkInLoadBar))
            {
                _loadBarInMetric = new NetworkBarMetric(new PerformanceCounter(CATEGORYNAME, BYTESRECEIVEDPERSECOND, id), MetricKey.NetworkInLoadBar, null, roundAll, _converter);
                ShowLoadBarIn = true;
            }

            if (metrics.IsEnabled(MetricKey.NetworkOutLoadBar))
            {
                _loadBarOutMetric = new NetworkBarMetric(new PerformanceCounter(CATEGORYNAME, BYTESSENTPERSECOND, id), MetricKey.NetworkOutLoadBar, null, roundAll, _converter);
                ShowLoadBarOut = true;
            }

            Metrics = _metrics.ToArray();
        }

        ~NetworkMonitor()
        {
            Dispose(false);
        }

        public static IEnumerable<HardwareConfig> GetHardware()
        {
            string[] _instances;

            try
            {
                _instances = new PerformanceCounterCategory(CATEGORYNAME).GetInstanceNames();
            }
            catch (InvalidOperationException)
            {
                _instances = new string[0];

                App.ShowPerformanceCounterError();
            }

            Regex _regex = new Regex(@"^isatap.*$");

            return _instances.Where(i => !_regex.IsMatch(i)).OrderBy(h => h).Select(h => new HardwareConfig() { ID = h, Name = h, ActualName = h });
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            bool _showName = parameters.GetValue<bool>(ParamKey.HardwareNames);
            bool _roundAll = parameters.GetValue<bool>(ParamKey.RoundAll);
            bool _useBytes = parameters.GetValue<bool>(ParamKey.UseBytes);
            int _bandwidthInAlert = parameters.GetValue<int>(ParamKey.BandwidthInAlert);
            int _bandwidthOutAlert = parameters.GetValue<int>(ParamKey.BandwidthOutAlert);

            bool _includeExtIP = metrics.IsEnabled(MetricKey.NetworkExtIP);
            IDictionary<string, string> _adapterIpLookup = metrics.IsEnabled(MetricKey.NetworkIP)
                ? BuildAdapterIPAddressLookup()
                : null;

            NetworkMonitor[] monitors = (
                from hw in GetHardware()
                join c in hardwareConfig on hw.ID equals c.ID into merged
                from n in merged.DefaultIfEmpty(hw).Select(n => { n.ActualName = hw.Name; return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new NetworkMonitor(n.ID, n.Name ?? n.ActualName, _adapterIpLookup, _includeExtIP, metrics, _showName, _roundAll, _useBytes, _bandwidthInAlert, _bandwidthOutAlert)
                ).ToArray();

            if (_includeExtIP)
            {
                RefreshExternalIPAddressAsync(monitors);
            }

            return monitors;
        }

        public override void Update()
        {
            if (!_countersAvailable)
            {
                return;
            }

            try
            {
                if (_loadBarInMetric != null)
                {
                    _loadBarInMetric.Update();
                }

                if (_loadBarOutMetric != null)
                {
                    _loadBarOutMetric.Update();
                }

                base.Update();
            }
            catch (InvalidOperationException)
            {
                _countersAvailable = false;
            }
            catch (UnauthorizedAccessException)
            {
                _countersAvailable = false;
            }
            catch (Win32Exception)
            {
                _countersAvailable = false;
            }
        }

        private static string GetAdapterIPAddress(string name, IDictionary<string, string> adapterIpLookup)
        {
            if (adapterIpLookup == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string configuredName = NormalizeAdapterName(name);
            string ipAddress;
            return adapterIpLookup.TryGetValue(configuredName, out ipAddress) ? ipAddress : null;
        }

        private static IDictionary<string, string> BuildAdapterIPAddressLookup()
        {
            Dictionary<string, string> adapterIpLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
            {
                string ipAddress = GetIPv4Address(netif);
                if (string.IsNullOrEmpty(ipAddress))
                {
                    continue;
                }

                string interfaceDesc = NormalizeAdapterName(netif.Description);
                if (!string.IsNullOrWhiteSpace(interfaceDesc) && !adapterIpLookup.ContainsKey(interfaceDesc))
                {
                    adapterIpLookup.Add(interfaceDesc, ipAddress);
                }

                string interfaceName = NormalizeAdapterName(netif.Name);
                if (!string.IsNullOrWhiteSpace(interfaceName) && !adapterIpLookup.ContainsKey(interfaceName))
                {
                    adapterIpLookup.Add(interfaceName, ipAddress);
                }
            }

            return adapterIpLookup;
        }

        private static string GetIPv4Address(NetworkInterface netif)
        {
            IPInterfaceProperties properties = netif.GetIPProperties();

            foreach (IPAddressInformation unicast in properties.UnicastAddresses)
            {
                if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return unicast.Address.ToString();
                }
            }

            return null;
        }

        private static string NormalizeAdapterName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : Regex.Replace(value, @"[^\w\d\s]", "");
        }

        private static string GetExternalIPAddress()
        {
            try
            {
                HttpWebRequest _request = WebRequest.CreateHttp(Constants.URLs.IPIFY);
                _request.Method = HttpMethod.Get.Method;
                _request.Timeout = 2000; // 2s max to avoid blocking UI thread too long at startup

                using (HttpWebResponse _response = (HttpWebResponse)_request.GetResponse())
                {
                    using (Stream _stream = _response.GetResponseStream())
                    {
                        using (StreamReader _reader = new StreamReader(_stream))
                        {
                            return _reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException)
            {
                return "";
            }
        }

        private static void RefreshExternalIPAddressAsync(NetworkMonitor[] monitors)
        {
            if (monitors == null || monitors.Length == 0)
            {
                return;
            }

            Task.Run(() =>
            {
                string extIP = GetExternalIPAddress();
                if (string.IsNullOrWhiteSpace(extIP))
                {
                    extIP = "Unavailable";
                }

                if (App.Current == null)
                {
                    return;
                }

                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    foreach (NetworkMonitor monitor in monitors)
                    {
                        monitor.SetExternalIPAddress(extIP);
                    }
                }));
            });
        }

        private void SetExternalIPAddress(string extIP)
        {
            if (_externalIPMetric != null)
            {
                _externalIPMetric.SetText(extIP);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    if (_loadBarInMetric != null)
                    {
                        _loadBarInMetric.Dispose();
                        _loadBarInMetric = null;
                    }

                    if (_loadBarOutMetric != null)
                    {
                        _loadBarOutMetric.Dispose();
                        _loadBarOutMetric = null;
                    }
                }

                _disposed = true;
            }
        }

        private bool _showLoadBarIn { get; set; }

        public bool ShowLoadBarIn
        {
            get
            {
                return _showLoadBarIn;
            }
            private set
            {
                _showLoadBarIn = value;

                NotifyPropertyChanged("ShowLoadBarIn");
            }
        }

        private bool _showLoadBarOut { get; set; }

        public bool ShowLoadBarOut
        {
            get
            {
                return _showLoadBarOut;
            }
            private set
            {
                _showLoadBarOut = value;

                NotifyPropertyChanged("ShowLoadBarOut");
            }
        }

        private NetworkBarMetric _loadBarInMetric { get; set; }

        public iMetric LoadBarInMetric
        {
            get
            {
                return _loadBarInMetric;
            }
        }

        private NetworkBarMetric _loadBarOutMetric { get; set; }

        public iMetric LoadBarOutMetric
        {
            get
            {
                return _loadBarOutMetric;
            }
        }

        private bool _disposed { get; set; } = false;

        private bool _countersAvailable { get; set; } = true;

        private IPMetric _externalIPMetric { get; set; }
    }

    public interface iMetric : INotifyPropertyChanged, IDisposable
    {
        MetricKey Key { get; }

        string FullName { get; }

        string Label { get; }

        double Value { get; }

        string Append { get; }

        double nValue { get; }

        string nAppend { get; }

        string Text { get; }

        bool HasValue { get; }

        bool IsAlert { get; }

        bool IsNumeric { get; }

        void Update();

        void Update(double value);
    }

    public class BaseMetric : iMetric
    {
        public BaseMetric(MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null)
        {
            _converter = converter;
            _round = round;
            _alertValue = alertValue;

            Key = key;

            if (label == null)
            {
                FullName = key.GetFullName();
                Label = key.GetLabel();
            }
            else
            {
                FullName = Label = label;
            }

            nAppend = Append = converter == null ? dataType.GetAppend() : converter.TargetType.GetAppend();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_alertColorTimer != null)
                    {
                        _alertColorTimer.Stop();
                        _alertColorTimer = null;
                    }

                    _converter = null;
                }

                _disposed = true;
            }
        }

        ~BaseMetric()
        {
            Dispose(false);
        }

        public virtual void Update() { }

        public void Update(double value)
        {
            double _val = value;

            if (_converter == null)
            {
                nValue = _val;
            }
            else if (_converter.IsDynamic)
            {
                double _nVal;
                DataType _dataType;

                _converter.Convert(ref _val, out _nVal, out _dataType);

                nValue = _nVal;
                Append = _dataType.GetAppend();
            }
            else
            {
                _converter.Convert(ref _val);

                nValue = _val;
            }

            Value = _val;
            HasValue = true;

            if (_alertValue > 0 && _alertValue <= nValue)
            {
                if (!IsAlert)
                {
                    IsAlert = true;
                }
            }
            else if (IsAlert)
            {
                IsAlert = false;
            }

            Text = string.Format(
                "{0:#,##0.##}{1}",
                _val.Round(_round),
                Append
                );
        }

        protected void SetUnavailable(string text = "")
        {
            Value = 0;
            nValue = 0;
            HasValue = false;

            if (IsAlert)
            {
                IsAlert = false;
            }

            Text = text;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private MetricKey _key { get; set; }

        public MetricKey Key
        {
            get
            {
                return _key;
            }
            protected set
            {
                if (_key == value)
                {
                    return;
                }

                _key = value;

                NotifyPropertyChanged("Key");
            }
        }

        private string _fullName { get; set; }

        public string FullName
        {
            get
            {
                return _fullName;
            }
            protected set
            {
                if (_fullName == value)
                {
                    return;
                }

                _fullName = value;

                NotifyPropertyChanged("FullName");
            }
        }

        private string _label { get; set; }

        public string Label
        {
            get
            {
                return _label;
            }
            protected set
            {
                if (_label == value)
                {
                    return;
                }

                _label = value;

                NotifyPropertyChanged("Label");
            }
        }

        private double _value { get; set; }

        public double Value
        {
            get
            {
                return _value;
            }
            protected set
            {
                if (_value == value)
                {
                    return;
                }

                _value = value;

                NotifyPropertyChanged("Value");
            }
        }

        private string _append { get; set; }

        public string Append
        {
            get
            {
                return _append;
            }
            protected set
            {
                if (_append == value)
                {
                    return;
                }

                _append = value;

                NotifyPropertyChanged("Append");
            }
        }

        private double _nValue { get; set; }

        public double nValue
        {
            get
            {
                return _nValue;
            }
            set
            {
                if (_nValue == value)
                {
                    return;
                }

                _nValue = value;

                NotifyPropertyChanged("nValue");
            }
        }

        private string _nAppend { get; set; }

        public string nAppend
        {
            get
            {
                return _nAppend;
            }
            set
            {
                if (_nAppend == value)
                {
                    return;
                }

                _nAppend = value;

                NotifyPropertyChanged("nAppend");
            }
        }

        private string _text { get; set; }

        public string Text
        {
            get
            {
                return _text;
            }
            protected set
            {
                if (_text == value)
                {
                    return;
                }

                _text = value;

                NotifyPropertyChanged("Text");
            }
        }

        private bool _hasValue { get; set; }

        public bool HasValue
        {
            get
            {
                return _hasValue;
            }
            protected set
            {
                if (_hasValue == value)
                {
                    return;
                }

                _hasValue = value;

                NotifyPropertyChanged("HasValue");
            }
        }

        private bool _isAlert { get; set; }

        public bool IsAlert
        {
            get
            {
                return _isAlert;
            }
            protected set
            {
                if (_isAlert == value)
                {
                    return;
                }

                _isAlert = value;

                NotifyPropertyChanged("IsAlert");
                NotifyPropertyChanged("AlertColor");

                if (value)
                {
                    _alertColorFlag = false;

                    if (Framework.Settings.Instance.AlertBlink)
                    {
                        _alertColorTimer = new DispatcherTimer(DispatcherPriority.Normal, App.Current.Dispatcher);
                        _alertColorTimer.Interval = TimeSpan.FromSeconds(0.5d);
                        _alertColorTimer.Tick += new EventHandler(AlertColorTimer_Tick);
                        _alertColorTimer.Start();
                    }
                }
                else if (_alertColorTimer != null)
                {
                    _alertColorTimer.Stop();
                    _alertColorTimer = null;
                }
            }
        }

        public virtual bool IsNumeric
        {
            get { return true; }
        }

        public string AlertColor
        {
            get
            {
                return _alertColorFlag ? Framework.Settings.Instance.FontColor : Framework.Settings.Instance.AlertFontColor;
            }
        }

        private DispatcherTimer _alertColorTimer;

        private void AlertColorTimer_Tick(object sender, EventArgs e)
        {
            _alertColorFlag = !_alertColorFlag;

            NotifyPropertyChanged("AlertColor");
        }

        private bool _alertColorFlag = false;

        protected iConverter _converter { get; set; }

        protected bool _round { get; set; }

        protected double _alertValue { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class OHMMetric : BaseMetric
    {
        public OHMMetric(ISensor sensor, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null, Func<ISensor, bool> valueValidator = null)
            : this(sensor == null ? Enumerable.Empty<ISensor>() : new[] { sensor }, key, dataType, label, round, alertValue, converter, valueValidator)
        {
        }

        public OHMMetric(IEnumerable<ISensor> sensors, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null, Func<ISensor, bool> valueValidator = null)
            : base(key, dataType, label, round, alertValue, converter)
        {
            _sensors = sensors == null ? new ISensor[0] : sensors.Where(s => s != null).ToArray();
            _valueValidator = valueValidator;
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    _sensors = null;
                    _valueValidator = null;
                }

                _disposed = true;
            }
        }

        ~OHMMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            ISensor sensor = _sensors.FirstOrDefault(HasUsableValue);

            if (sensor != null)
            {
                Update(sensor.Value.Value);
            }
            else
            {
                SetUnavailable();
            }
        }

        private bool HasUsableValue(ISensor sensor)
        {
            if (sensor == null || !sensor.Value.HasValue)
            {
                return false;
            }

            double value = sensor.Value.Value;

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return false;
            }

            return _valueValidator == null || _valueValidator(sensor);
        }

        private ISensor[] _sensors { get; set; }

        private Func<ISensor, bool> _valueValidator { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class GPUVRAMMLoadMetric : BaseMetric
    {
        public GPUVRAMMLoadMetric(ISensor memoryUsedSensor, ISensor memoryTotalSensor, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _memoryUsedSensor = memoryUsedSensor;
            _memoryTotalSensor = memoryTotalSensor;
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    _memoryUsedSensor = null;
                    _memoryTotalSensor = null;
                }

                _disposed = true;
            }
        }

        ~GPUVRAMMLoadMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            if (_memoryUsedSensor.Value.HasValue && _memoryTotalSensor.Value.HasValue)
            {
                float load = _memoryUsedSensor.Value.Value / _memoryTotalSensor.Value.Value * 100f;

                Update(load);
            }
            else
            {
                SetUnavailable();
            }
        }

        private ISensor _memoryUsedSensor { get; set; }

        private ISensor _memoryTotalSensor { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class IPMetric : BaseMetric
    {
        public IPMetric(string ipAddress, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            SetText(ipAddress);
        }

        public void SetText(string ipAddress)
        {
            Text = ipAddress;
            HasValue = !string.IsNullOrWhiteSpace(ipAddress) && !string.Equals(ipAddress, "Unavailable", StringComparison.OrdinalIgnoreCase);
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~IPMetric()
        {
            Dispose(false);
        }

        public override bool IsNumeric
        {
            get { return false; }
        }
    }

    public class PCMetric : BaseMetric
    {
        public PCMetric(PerformanceCounter counter, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _counter = counter;
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    if (_counter != null)
                    {
                        _counter.Dispose();
                        _counter = null;
                    }
                }

                _disposed = true;
            }
        }

        ~PCMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            Update(_counter.NextValue());
        }

        private PerformanceCounter _counter { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class NetworkBarMetric : PCMetric
    {
        private PerformanceCounter _bandwidthCounter;
        private iConverter _displayConverter;
        private bool _displayRound;

        public NetworkBarMetric(PerformanceCounter counter, MetricKey key, string label, bool round, iConverter displayConverter)
            : base(counter, key, DataType.Percent, label, round, 0, PercentOf1GbpsConverter.Instance)
        {
            _bandwidthCounter = counter;
            _displayConverter = displayConverter;
            _displayRound = round;
        }

        public override void Update()
        {
            double raw = _bandwidthCounter.NextValue();

            // Bar value: 0-100% of 1Gbps (125,000,000 bytes/sec = 1Gbps)
            Value = Math.Min(100d, raw / 125_000_000d * 100d);
            HasValue = true;

            // Display text: formatted bandwidth using the display converter
            double displayVal = raw;
            string append;
            if (_displayConverter != null && _displayConverter.IsDynamic)
            {
                double nVal;
                DataType dt;
                _displayConverter.Convert(ref displayVal, out nVal, out dt);
                append = dt.GetAppend();
            }
            else if (_displayConverter != null)
            {
                _displayConverter.Convert(ref displayVal);
                append = _displayConverter.TargetType.GetAppend();
            }
            else
            {
                append = DataType.kBps.GetAppend();
            }

            Text = string.Format("{0:#,##0.##}{1}", displayVal.Round(_displayRound), append);
        }
    }

    [Serializable]
    public enum MonitorType : byte
    {
        CPU,
        RAM,
        GPU,
        HD,
        Network,
        Process
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MonitorConfig : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MonitorConfig Clone()
        {
            MonitorConfig _clone = (MonitorConfig)MemberwiseClone();
            _clone.Hardware = _clone.Hardware.Select(h => h.Clone()).ToArray();
            _clone.Params = _clone.Params.Select(p => p.Clone()).ToArray();

            if (_clone.HardwareOC != null)
            {
                _clone.HardwareOC = new ObservableCollection<HardwareConfig>(_clone.HardwareOC.Select(h => h.Clone()));
            }

            return _clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private MonitorType _type { get; set; }

        [JsonProperty]
        public MonitorType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;

                NotifyPropertyChanged("Type");
            }
        }

        private bool _enabled { get; set; }

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;

                NotifyPropertyChanged("Enabled");
            }
        }

        private byte _order { get; set; }

        [JsonProperty]
        public byte Order
        {
            get
            {
                return _order;
            }
            set
            {
                _order = value;

                NotifyPropertyChanged("Order");
            }
        }

        private HardwareConfig[] _hardware { get; set; }

        [JsonProperty]
        public HardwareConfig[] Hardware
        {
            get
            {
                return _hardware;
            }
            set
            {
                _hardware = value;

                NotifyPropertyChanged("Hardware");
            }
        }

        private ObservableCollection<HardwareConfig> _hardwareOC { get; set; }

        public ObservableCollection<HardwareConfig> HardwareOC
        {
            get
            {
                return _hardwareOC;
            }
            set
            {
                _hardwareOC = value;

                NotifyPropertyChanged("HardwareOC");
            }
        }

        private MetricConfig[] _metrics { get; set; }

        [JsonProperty]
        public MetricConfig[] Metrics
        {
            get
            {
                return _metrics;
            }
            set
            {
                _metrics = value;

                NotifyPropertyChanged("Metrics");
            }
        }

        private ConfigParam[] _params { get; set; }

        [JsonProperty]
        public ConfigParam[] Params
        {
            get
            {
                return _params;
            }
            set
            {
                _params = value;

                NotifyPropertyChanged("Params");
            }
        }

        public string Name
        {
            get
            {
                return Type.GetDescription();
            }
        }

        public static MonitorConfig[] CheckConfig(MonitorConfig[] config)
        {
            MonitorConfig[] _default = Default;

            if (config == null)
            {
                return _default;
            }

            config = (
                from def in _default
                join rec in config on def.Type equals rec.Type into merged
                from newrec in merged.DefaultIfEmpty(def)
                select newrec
                ).ToArray();

            foreach (MonitorConfig _record in config)
            {
                MonitorConfig _defaultRecord = _default.Single(d => d.Type == _record.Type);

                if (_record.Hardware == null)
                {
                    _record.Hardware = _defaultRecord.Hardware;
                }

                if (_record.Metrics == null)
                {
                    _record.Metrics = _defaultRecord.Metrics;
                }
                else
                {
                    _record.Metrics = (
                        from def in _defaultRecord.Metrics
                        join metric in _record.Metrics on def.Key equals metric.Key into merged
                        from newmetric in merged.DefaultIfEmpty(def)
                        select newmetric
                        ).ToArray();
                }

                if (_record.Params == null)
                {
                    _record.Params = _defaultRecord.Params;
                }
                else
                {
                    _record.Params = (
                        from def in _defaultRecord.Params
                        join param in _record.Params on def.Key equals param.Key into merged
                        from newparam in merged.DefaultIfEmpty(def)
                        select newparam
                        ).ToArray();
                }
            }

            return config;
        }

        public static MonitorConfig[] Default
        {
            get
            {
                return new MonitorConfig[6]
                {
                    new MonitorConfig()
                    {
                        Type = MonitorType.CPU,
                        Enabled = true,
                        Order = 5,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[8]
                        {
                            new MetricConfig(MetricKey.CPUClock, true),
                            new MetricConfig(MetricKey.CPUTemp, true),
                            new MetricConfig(MetricKey.CPUVoltage, true),
                            new MetricConfig(MetricKey.CPUFan, true),
                            new MetricConfig(MetricKey.CPULoad, true),
                            new MetricConfig(MetricKey.CPUCoreLoad, true),
                            new MetricConfig(MetricKey.CPULoadBar, false),
                            new MetricConfig(MetricKey.CPUTempBar, false)
                        },
                        Params = new ConfigParam[6]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.AllCoreClocks,
                            ConfigParam.Defaults.UseGHz,
                            ConfigParam.Defaults.UseFahrenheit,
                            ConfigParam.Defaults.TempAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.RAM,
                        Enabled = true,
                        Order = 4,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[6]
                        {
                            new MetricConfig(MetricKey.RAMClock, true),
                            new MetricConfig(MetricKey.RAMVoltage, true),
                            new MetricConfig(MetricKey.RAMLoad, true),
                            new MetricConfig(MetricKey.RAMUsed, true),
                            new MetricConfig(MetricKey.RAMFree, true),
                            new MetricConfig(MetricKey.RAMLoadBar, false)
                        },
                        Params = new ConfigParam[2]
                        {
                            ConfigParam.Defaults.NoHardwareNames,
                            ConfigParam.Defaults.RoundAll
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.GPU,
                        Enabled = true,
                        Order = 3,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[10]
                        {
                            new MetricConfig(MetricKey.GPUCoreClock, true),
                            new MetricConfig(MetricKey.GPUVRAMClock, true),
                            new MetricConfig(MetricKey.GPUCoreLoad, true),
                            new MetricConfig(MetricKey.GPUVRAMLoad, true),
                            new MetricConfig(MetricKey.GPUVoltage, true),
                            new MetricConfig(MetricKey.GPUTemp, true),
                            new MetricConfig(MetricKey.GPUFan, true),
                            new MetricConfig(MetricKey.GPULoadBar, false),
                            new MetricConfig(MetricKey.GPUTempBar, false),
                            new MetricConfig(MetricKey.VRAMLoadBar, false)
                        },
                        Params = new ConfigParam[5]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UseGHz,
                            ConfigParam.Defaults.UseFahrenheit,
                            ConfigParam.Defaults.TempAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.HD,
                        Enabled = true,
                        Order = 2,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[6]
                        {
                            new MetricConfig(MetricKey.DriveLoadBar, true),
                            new MetricConfig(MetricKey.DriveLoad, true),
                            new MetricConfig(MetricKey.DriveUsed, true),
                            new MetricConfig(MetricKey.DriveFree, true),
                            new MetricConfig(MetricKey.DriveRead, true),
                            new MetricConfig(MetricKey.DriveWrite, true)
                        },
                        Params = new ConfigParam[2]
                        {
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UsedSpaceAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.Network,
                        Enabled = true,
                        Order = 1,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[6]
                        {
                            new MetricConfig(MetricKey.NetworkIP, true),
                            new MetricConfig(MetricKey.NetworkExtIP, false),
                            new MetricConfig(MetricKey.NetworkIn, true),
                            new MetricConfig(MetricKey.NetworkOut, true),
                            new MetricConfig(MetricKey.NetworkInLoadBar, false),
                            new MetricConfig(MetricKey.NetworkOutLoadBar, false)
                        },
                        Params = new ConfigParam[5]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UseBytes,
                            ConfigParam.Defaults.BandwidthInAlert,
                            ConfigParam.Defaults.BandwidthOutAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.Process,
                        Enabled = false,
                        Order = 0,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[3]
                        {
                            new MetricConfig(MetricKey.ProcessCpu, true),
                            new MetricConfig(MetricKey.ProcessRam, true),
                            new MetricConfig(MetricKey.ProcessClose, true)
                        },
                        Params = new ConfigParam[2]
                        {
                            ConfigParam.Defaults.ProcessCount,
                            ConfigParam.Defaults.ProcessSortByCpu
                        }
                    }
                };
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class HardwareConfig : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public HardwareConfig Clone()
        {
            return (HardwareConfig)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private string _id { get; set; }

        [JsonProperty]
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;

                NotifyPropertyChanged("ID");
            }
        }

        private string _name { get; set; }

        [JsonProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;

                NotifyPropertyChanged("Name");
            }
        }

        private string _actualName { get; set; }

        [JsonProperty]
        public string ActualName
        {
            get
            {
                return _actualName;
            }
            set
            {
                _actualName = value;

                NotifyPropertyChanged("ActualName");
            }
        }

        private bool _enabled { get; set; } = true;

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;

                NotifyPropertyChanged("Enabled");
            }
        }

        private byte _order { get; set; } = 0;

        [JsonProperty]
        public byte Order
        {
            get
            {
                return _order;
            }
            set
            {
                _order = value;

                NotifyPropertyChanged("Order");
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MetricConfig : INotifyPropertyChanged, ICloneable
    {
        public MetricConfig() { }

        public MetricConfig(MetricKey key, bool enabled)
        {
            Key = key;
            Enabled = enabled;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigParam Clone()
        {
            return (ConfigParam)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private MetricKey _key { get; set; }

        [JsonProperty]
        public MetricKey Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;

                NotifyPropertyChanged("Key");
            }
        }

        private bool _enabled { get; set; }

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                NotifyPropertyChanged("Enabled");
            }
        }

        public string Name
        {
            get
            {
                return Key.GetFullName();
            }
        }
    }

    [Serializable]
    public enum MetricKey : byte
    {
        CPUClock = 0,
        CPUTemp = 1,
        CPUVoltage = 2,
        CPUFan = 3,
        CPULoad = 4,
        CPUCoreLoad = 5,

        RAMClock = 6,
        RAMVoltage = 7,
        RAMLoad = 8,
        RAMUsed = 9,
        RAMFree = 10,

        GPUCoreClock = 11,
        GPUVRAMClock = 12,
        GPUCoreLoad = 13,
        GPUVRAMLoad = 14,
        GPUVoltage = 15,
        GPUTemp = 16,
        GPUFan = 17,

        NetworkIP = 26,
        NetworkExtIP = 27,
        NetworkIn = 18,
        NetworkOut = 19,

        DriveLoadBar = 20,
        DriveLoad = 21,
        DriveUsed = 22,
        DriveFree = 23,
        DriveRead = 24,
        DriveWrite = 25,

        CPULoadBar = 28,
        RAMLoadBar = 29,
        NetworkInLoadBar = 30,
        NetworkOutLoadBar = 31,

        GPULoadBar = 32,
        CPUTempBar = 33,
        GPUTempBar = 34,

        VRAMLoadBar = 35,

        ProcessCpu = 36,
        ProcessRam = 37,
        ProcessClose = 38
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ConfigParam : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigParam Clone()
        {
            return (ConfigParam)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private ParamKey _key { get; set; }

        [JsonProperty]
        public ParamKey Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;

                NotifyPropertyChanged("Key");
            }
        }

        private object _value { get; set; }

        [JsonProperty]
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value.GetType() == typeof(long))
                {
                    _value = Convert.ToInt32(value);
                }
                else
                {
                    _value = value;
                }

                NotifyPropertyChanged("Value");
            }
        }

        public Type Type
        {
            get
            {
                return Value.GetType();
            }
        }

        public string TypeString
        {
            get
            {
                return Type.ToString();
            }
        }

        public string Name
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Resources.SettingsShowHardwareNames;

                    case ParamKey.UseFahrenheit:
                        return Resources.SettingsUseFahrenheit;

                    case ParamKey.AllCoreClocks:
                        return Resources.SettingsAllCoreClocks;

                    case ParamKey.CoreLoads:
                        return Resources.SettingsCoreLoads;

                    case ParamKey.TempAlert:
                        return Resources.SettingsTemperatureAlert;

                    case ParamKey.DriveDetails:
                        return Resources.SettingsShowDriveDetails;

                    case ParamKey.UsedSpaceAlert:
                        return Resources.SettingsUsedSpaceAlert;

                    case ParamKey.BandwidthInAlert:
                        return Resources.SettingsBandwidthInAlert;

                    case ParamKey.BandwidthOutAlert:
                        return Resources.SettingsBandwidthOutAlert;

                    case ParamKey.UseBytes:
                        return Resources.SettingsUseBytesPerSecond;

                    case ParamKey.RoundAll:
                        return Resources.SettingsRoundAllDecimals;

                    case ParamKey.DriveSpace:
                        return Resources.SettingsShowDriveSpace;

                    case ParamKey.DriveIO:
                        return Resources.SettingsShowDriveIO;

                    case ParamKey.UseGHz:
                        return Resources.SettingsUseGHz;

                    case ParamKey.ProcessCount:
                        return "Number of Processes";

                    case ParamKey.ProcessSortByCpu:
                        return "Sort by CPU";

                    default:
                        return "Unknown";
                }
            }
        }

        public string Tooltip
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Resources.SettingsShowHardwareNamesTooltip;

                    case ParamKey.UseFahrenheit:
                        return Resources.SettingsUseFahrenheitTooltip;

                    case ParamKey.AllCoreClocks:
                        return Resources.SettingsAllCoreClocksTooltip;

                    case ParamKey.CoreLoads:
                        return Resources.SettingsCoreLoadsTooltip;

                    case ParamKey.TempAlert:
                        return Resources.SettingsTemperatureAlertTooltip;

                    case ParamKey.DriveDetails:
                        return Resources.SettingsDriveDetailsTooltip;

                    case ParamKey.UsedSpaceAlert:
                        return Resources.SettingsUsedSpaceAlertTooltip;

                    case ParamKey.BandwidthInAlert:
                        return Resources.SettingsBandwidthInAlertTooltip;

                    case ParamKey.BandwidthOutAlert:
                        return Resources.SettingsBandwidthOutAlertTooltip;

                    case ParamKey.UseBytes:
                        return Resources.SettingsUseBytesPerSecondTooltip;

                    case ParamKey.RoundAll:
                        return Resources.SettingsRoundAllDecimalsTooltip;

                    case ParamKey.DriveSpace:
                        return Resources.SettingsShowDriveSpaceTooltip;

                    case ParamKey.DriveIO:
                        return Resources.SettingsShowDriveIOTooltip;

                    case ParamKey.UseGHz:
                        return Resources.SettingsUseGHzTooltip;

                    case ParamKey.ProcessCount:
                        return "Controls how many app groups are shown in the Processes section.";

                    case ParamKey.ProcessSortByCpu:
                        return "When enabled, sorts process groups by CPU usage. When disabled, sorts by RAM usage.";

                    default:
                        return "Unknown";
                }
            }
        }

        public static class Defaults
        {
            public static ConfigParam HardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = true };
                }
            }

            public static ConfigParam NoHardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = false };
                }
            }

            public static ConfigParam UseFahrenheit
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseFahrenheit, Value = false };
                }
            }

            public static ConfigParam AllCoreClocks
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.AllCoreClocks, Value = false };
                }
            }

            public static ConfigParam CoreLoads
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.CoreLoads, Value = true };
                }
            }

            public static ConfigParam TempAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.TempAlert, Value = 0 };
                }
            }

            public static ConfigParam DriveDetails
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveDetails, Value = false };
                }
            }

            public static ConfigParam UsedSpaceAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UsedSpaceAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthInAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthInAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthOutAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthOutAlert, Value = 0 };
                }
            }

            public static ConfigParam UseBytes
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseBytes, Value = false };
                }
            }

            public static ConfigParam RoundAll
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.RoundAll, Value = false };
                }
            }

            public static ConfigParam ShowDriveSpace
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveSpace, Value = true };
                }
            }

            public static ConfigParam ShowDriveIO
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveIO, Value = true };
                }
            }

            public static ConfigParam UseGHz
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseGHz, Value = false };
                }
            }

            public static ConfigParam ProcessCount
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.ProcessCount, Value = 5 };
                }
            }

            public static ConfigParam ProcessSortByCpu
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.ProcessSortByCpu, Value = true };
                }
            }
        }
    }

    [Serializable]
    public enum ParamKey : byte
    {
        HardwareNames,
        UseFahrenheit,
        AllCoreClocks,
        CoreLoads,
        TempAlert,
        DriveDetails,
        UsedSpaceAlert,
        BandwidthInAlert,
        BandwidthOutAlert,
        UseBytes,
        RoundAll,
        DriveSpace,
        DriveIO,
        UseGHz,
        ProcessCount,
        ProcessSortByCpu
    }

    public enum DataType : byte
    {
        Dynamic,
        Bit,
        Kilobit,
        Megabit,
        Gigabit,
        Byte,
        Kilobyte,
        Megabyte,
        Gigabyte,
        bps,
        kbps,
        Mbps,
        Gbps,
        Bps,
        kBps,
        MBps,
        GBps,
        MHz,
        GHz,
        Voltage,
        Percent,
        RPM,
        Celcius,
        Fahrenheit,
        IP
    }

    public interface iConverter
    {
        void Convert(ref double value);

        void Convert(ref double value, out double normalized, out DataType targetType);

        DataType TargetType { get; }

        bool IsDynamic { get; }
    }

    public class CelciusToFahrenheit : iConverter
    {
        private CelciusToFahrenheit() { }

        public void Convert(ref double value)
        {
            value = value * 1.8d + 32d;
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            Convert(ref value);
            normalized = value;
            targetType = TargetType;
        }

        public DataType TargetType
        {
            get
            {
                return DataType.Fahrenheit;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return false;
            }
        }

        private static CelciusToFahrenheit _instance { get; set; } = null;

        public static CelciusToFahrenheit Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CelciusToFahrenheit();
                }

                return _instance;
            }
        }
    }

    public class MHzToGHz : iConverter
    {
        private MHzToGHz() { }

        public void Convert(ref double value)
        {
            value = value / 1000d;
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            Convert(ref value);
            normalized = value;
            targetType = TargetType;
        }

        public DataType TargetType
        {
            get
            {
                return DataType.GHz;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return false;
            }
        }

        private static MHzToGHz _instance { get; set; } = null;

        public static MHzToGHz Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MHzToGHz();
                }

                return _instance;
            }
        }
    }

    public class BitsPerSecondConverter : iConverter
    {
        private BitsPerSecondConverter() { }

        public void Convert(ref double value)
        {
            double _normalized;
            DataType _dataType;

            Convert(ref value, out _normalized, out _dataType);
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            normalized = value /= 128d;

            if (value < 1024d)
            {
                targetType = DataType.kbps;
                return;
            }
            else if (value < 1048576d)
            {
                value /= 1024d;
                targetType = DataType.Mbps;
                return;
            }
            else
            {
                value /= 1048576d;
                targetType = DataType.Gbps;
                return;
            }
        }

        public DataType TargetType
        {
            get
            {
                return DataType.kbps;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return true;
            }
        }

        private static BitsPerSecondConverter _instance { get; set; } = null;

        public static BitsPerSecondConverter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BitsPerSecondConverter();
                }

                return _instance;
            }
        }
    }

    public class BytesPerSecondConverter : iConverter
    {
        private BytesPerSecondConverter() { }

        public void Convert(ref double value)
        {
            double _normalized;
            DataType _dataType;

            Convert(ref value, out _normalized, out _dataType);
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            normalized = value /= 1024d;

            if (value < 1024d)
            {
                targetType = DataType.kBps;
                return;
            }
            else if (value < 1048576d)
            {
                value /= 1024d;
                targetType = DataType.MBps;
                return;
            }
            else
            {
                value /= 1048576d;
                targetType = DataType.GBps;
                return;
            }
        }

        public DataType TargetType
        {
            get
            {
                return DataType.kBps;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return true;
            }
        }

        private static BytesPerSecondConverter _instance { get; set; } = null;

        public static BytesPerSecondConverter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BytesPerSecondConverter();
                }

                return _instance;
            }
        }
    }

    public class PercentOf1GbpsConverter : iConverter
    {
        private PercentOf1GbpsConverter() { }

        // 1 Gbps = 1,000,000,000 bits/sec = 125,000,000 bytes/sec
        private const double BYTES_PER_SECOND_1GBPS = 125_000_000d;

        public void Convert(ref double value)
        {
            value = Math.Min(100d, value / BYTES_PER_SECOND_1GBPS * 100d);
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            Convert(ref value);
            normalized = value;
            targetType = DataType.Percent;
        }

        public DataType TargetType
        {
            get
            {
                return DataType.Percent;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return false;
            }
        }

        private static PercentOf1GbpsConverter _instance { get; set; } = null;

        public static PercentOf1GbpsConverter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PercentOf1GbpsConverter();
                }

                return _instance;
            }
        }
    }

    public static class Extensions
    {
        public static bool IsEnabled(this MetricConfig[] metrics, MetricKey key)
        {
            return metrics.Any(m => m.Key == key && m.Enabled);
        }

        public static HardwareType[] GetHardwareTypes(this MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                    return new HardwareType[1] { HardwareType.Cpu };

                case MonitorType.RAM:
                    return new HardwareType[1] { HardwareType.Memory };

                case MonitorType.GPU:
                    return new HardwareType[2] { HardwareType.GpuNvidia, HardwareType.GpuAmd };

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public static string GetDescription(this MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                    return Resources.CPU;

                case MonitorType.RAM:
                    return Resources.RAM;

                case MonitorType.GPU:
                    return Resources.GPU;

                case MonitorType.HD:
                    return Resources.Drives;

                case MonitorType.Network:
                    return Resources.Network;

                case MonitorType.Process:
                    return Resources.Processes;

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public static T GetValue<T>(this ConfigParam[] parameters, ParamKey key)
        {
            return (T)parameters.Single(p => p.Key == key).Value;
        }

        public static string GetFullName(this MetricKey key)
        {
            switch (key)
            {
                case MetricKey.CPUClock:
                    return Resources.CPUClock;

                case MetricKey.CPUTemp:
                    return Resources.CPUTemp;

                case MetricKey.CPUVoltage:
                    return Resources.CPUVoltage;

                case MetricKey.CPUFan:
                    return Resources.CPUFan;

                case MetricKey.CPULoad:
                    return Resources.CPULoad;

                case MetricKey.CPUCoreLoad:
                    return Resources.CPUCoreLoad;

                case MetricKey.RAMClock:
                    return Resources.RAMClock;

                case MetricKey.RAMVoltage:
                    return Resources.RAMVoltage;

                case MetricKey.RAMLoad:
                    return Resources.RAMLoad;

                case MetricKey.RAMUsed:
                    return Resources.RAMUsed;

                case MetricKey.RAMFree:
                    return Resources.RAMFree;

                case MetricKey.GPUCoreClock:
                    return Resources.GPUCoreClock;

                case MetricKey.GPUVRAMClock:
                    return Resources.GPUVRAMClock;

                case MetricKey.GPUCoreLoad:
                    return Resources.GPUCoreLoad;

                case MetricKey.GPUVRAMLoad:
                    return Resources.GPUVRAMLoad;

                case MetricKey.GPUVoltage:
                    return Resources.GPUVoltage;

                case MetricKey.GPUTemp:
                    return Resources.GPUTemp;

                case MetricKey.GPUFan:
                    return Resources.GPUFan;

                case MetricKey.NetworkIP:
                    return Resources.NetworkIP;

                case MetricKey.NetworkExtIP:
                    return Resources.NetworkExtIP;

                case MetricKey.NetworkIn:
                    return Resources.NetworkIn;

                case MetricKey.NetworkOut:
                    return Resources.NetworkOut;

                case MetricKey.DriveLoadBar:
                    return Resources.DriveLoadBar;

                case MetricKey.DriveLoad:
                    return Resources.DriveLoad;

                case MetricKey.DriveUsed:
                    return Resources.DriveUsed;

                case MetricKey.DriveFree:
                    return Resources.DriveFree;

                case MetricKey.DriveRead:
                    return Resources.DriveRead;

                case MetricKey.DriveWrite:
                    return Resources.DriveWrite;

                case MetricKey.CPULoadBar:
                    return Resources.CPULoadBar;

                case MetricKey.RAMLoadBar:
                    return Resources.RAMLoadBar;

                case MetricKey.NetworkInLoadBar:
                    return Resources.NetworkInLoadBar;

                case MetricKey.NetworkOutLoadBar:
                    return Resources.NetworkOutLoadBar;

                case MetricKey.GPULoadBar:
                    return Resources.GPULoadBar;

                case MetricKey.CPUTempBar:
                    return Resources.CPUTempBar;

                case MetricKey.GPUTempBar:
                    return Resources.GPUTempBar;

                case MetricKey.VRAMLoadBar:
                    return Resources.VRAMLoadBar;

                case MetricKey.ProcessCpu:
                    return Resources.CPU;

                case MetricKey.ProcessRam:
                    return Resources.RAM;

                case MetricKey.ProcessClose:
                    return Resources.ButtonClose;

                default:
                    return "Unknown";
            }
        }

        public static string GetLabel(this MetricKey key)
        {
            switch (key)
            {
                case MetricKey.CPUClock:
                    return Resources.CPUClockLabel;

                case MetricKey.CPUTemp:
                    return Resources.CPUTempLabel;

                case MetricKey.CPUVoltage:
                    return Resources.CPUVoltageLabel;

                case MetricKey.CPUFan:
                    return Resources.CPUFanLabel;

                case MetricKey.CPULoad:
                    return Resources.CPULoadLabel;

                case MetricKey.CPUCoreLoad:
                    return Resources.CPUCoreLoadLabel;

                case MetricKey.RAMClock:
                    return Resources.RAMClockLabel;

                case MetricKey.RAMVoltage:
                    return Resources.RAMVoltageLabel;

                case MetricKey.RAMLoad:
                    return Resources.RAMLoadLabel;

                case MetricKey.RAMUsed:
                    return Resources.RAMUsedLabel;

                case MetricKey.RAMFree:
                    return Resources.RAMFreeLabel;

                case MetricKey.GPUCoreClock:
                    return Resources.GPUCoreClockLabel;

                case MetricKey.GPUVRAMClock:
                    return Resources.GPUVRAMClockLabel;

                case MetricKey.GPUCoreLoad:
                    return Resources.GPUCoreLoadLabel;

                case MetricKey.GPUVRAMLoad:
                    return Resources.GPUVRAMLoadLabel;

                case MetricKey.GPUVoltage:
                    return Resources.GPUVoltageLabel;

                case MetricKey.GPUTemp:
                    return Resources.GPUTempLabel;

                case MetricKey.GPUFan:
                    return Resources.GPUFanLabel;

                case MetricKey.NetworkIP:
                    return Resources.NetworkIPLabel;

                case MetricKey.NetworkExtIP:
                    return Resources.NetworkExtIPLabel;

                case MetricKey.NetworkIn:
                    return Resources.NetworkInLabel;

                case MetricKey.NetworkOut:
                    return Resources.NetworkOutLabel;

                case MetricKey.DriveLoadBar:
                    return Resources.DriveLoadBarLabel;

                case MetricKey.DriveLoad:
                    return Resources.DriveLoadLabel;

                case MetricKey.DriveUsed:
                    return Resources.DriveUsedLabel;

                case MetricKey.DriveFree:
                    return Resources.DriveFreeLabel;

                case MetricKey.DriveRead:
                    return Resources.DriveReadLabel;

                case MetricKey.DriveWrite:
                    return Resources.DriveWriteLabel;

                case MetricKey.CPULoadBar:
                    return Resources.CPULoadBarLabel;

                case MetricKey.RAMLoadBar:
                    return Resources.RAMLoadBarLabel;

                case MetricKey.NetworkInLoadBar:
                    return Resources.NetworkInLoadBarLabel;

                case MetricKey.NetworkOutLoadBar:
                    return Resources.NetworkOutLoadBarLabel;

                case MetricKey.GPULoadBar:
                    return Resources.GPULoadBarLabel;

                case MetricKey.CPUTempBar:
                    return Resources.CPUTempBarLabel;

                case MetricKey.GPUTempBar:
                    return Resources.GPUTempBarLabel;

                case MetricKey.VRAMLoadBar:
                    return Resources.VRAMLoadBarLabel;

                case MetricKey.ProcessCpu:
                    return Resources.CPU;

                case MetricKey.ProcessRam:
                    return Resources.RAM;

                case MetricKey.ProcessClose:
                    return Resources.ButtonClose;

                default:
                    return "Unknown";
            }
        }

        public static string GetAppend(this DataType type)
        {
            switch (type)
            {
                case DataType.Bit:
                    return " b";

                case DataType.Kilobit:
                    return " kb";

                case DataType.Megabit:
                    return " mb";

                case DataType.Gigabit:
                    return " gb";

                case DataType.Byte:
                    return " B";

                case DataType.Kilobyte:
                    return " KB";

                case DataType.Megabyte:
                    return " MB";

                case DataType.Gigabyte:
                    return " GB";

                case DataType.bps:
                    return " bps";

                case DataType.kbps:
                    return " kbps";

                case DataType.Mbps:
                    return " Mbps";

                case DataType.Gbps:
                    return " Gbps";

                case DataType.Bps:
                    return " B/s";

                case DataType.kBps:
                    return " kB/s";

                case DataType.MBps:
                    return " MB/s";

                case DataType.GBps:
                    return " GB/s";

                case DataType.MHz:
                    return " MHz";

                case DataType.GHz:
                    return " GHz";

                case DataType.Voltage:
                    return " V";

                case DataType.Percent:
                    return "%";

                case DataType.RPM:
                    return " RPM";

                case DataType.Celcius:
                    return " °C";

                case DataType.Fahrenheit:
                    return " F";

                case DataType.IP:
                    return string.Empty;

                default:
                    throw new ArgumentException("Invalid DataType.");
            }
        }

        public static double Round(this double value, bool doRound)
        {
            if (!doRound)
            {
                return value;
            }

            return Math.Round(value);
        }
    }

    public class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged { add { } remove { } }
    }

    public class ProcessEntry : INotifyPropertyChanged
    {
        private const uint NO_ERROR = 0;
        private const uint ERROR_INSUFFICIENT_BUFFER = 122;
        private static readonly TimeSpan ToolTipCacheLifetime = TimeSpan.FromSeconds(2);
        private readonly object _toolTipLock = new object();
        private Task _toolTipRefreshTask;
        private DateTime _lastToolTipRefreshUtc = DateTime.MinValue;

        public ProcessEntry(int pid, string name, string rowLabel, string cpuText, string ramText, RelayCommand killCommand)
        {
            Pid = pid;
            KillCommand = killCommand;
            Name = name;
            RowLabel = string.IsNullOrWhiteSpace(rowLabel) ? BuildRowLabel(name, pid) : rowLabel;
            CpuText = cpuText;
            RamText = ramText;

            ToolTipTitle = name;
            ToolTipSubtitle = BuildToolTipSubtitle(name, pid);
            ToolTipPath = "Hover to load full process details.";
            ToolTipDisk = "Loading on hover.";
            ToolTipGpu = "Loading on hover.";
            ToolTipNetwork = "Loading on hover.";
            ToolTipOther = "Loading on hover.";
            ToolTipStatus = string.Empty;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Pid { get; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (!SetProperty(ref _name, value, "Name"))
                {
                    return;
                }

                RowLabel = BuildRowLabel(value, Pid);
            }
        }

        private string _rowLabel;
        public string RowLabel
        {
            get { return _rowLabel; }
            private set { SetProperty(ref _rowLabel, value, "RowLabel"); }
        }

        public void SetRowLabel(string rowLabel)
        {
            RowLabel = string.IsNullOrWhiteSpace(rowLabel) ? BuildRowLabel(Name, Pid) : rowLabel;
        }

        private string _cpuText;
        public string CpuText
        {
            get { return _cpuText; }
            set
            {
                if (!SetProperty(ref _cpuText, value, "CpuText"))
                {
                    return;
                }
            }
        }

        private string _ramText;
        public string RamText
        {
            get { return _ramText; }
            set
            {
                if (!SetProperty(ref _ramText, value, "RamText"))
                {
                    return;
                }

                NotifyPropertyChanged("ToolTipMemory");
            }
        }

        private string _toolTipTitle;
        public string ToolTipTitle
        {
            get { return _toolTipTitle; }
            private set { SetProperty(ref _toolTipTitle, value, "ToolTipTitle"); }
        }

        private string _toolTipSubtitle;
        public string ToolTipSubtitle
        {
            get { return _toolTipSubtitle; }
            private set { SetProperty(ref _toolTipSubtitle, value, "ToolTipSubtitle"); }
        }

        private string _toolTipPath;
        public string ToolTipPath
        {
            get { return _toolTipPath; }
            private set { SetProperty(ref _toolTipPath, value, "ToolTipPath"); }
        }

        private string _toolTipMemoryDetails;
        public string ToolTipMemory
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RamText))
                {
                    return string.IsNullOrWhiteSpace(_toolTipMemoryDetails) ? "Memory details unavailable." : _toolTipMemoryDetails;
                }

                return string.IsNullOrWhiteSpace(_toolTipMemoryDetails)
                    ? string.Format("Working set {0}", RamText)
                    : string.Format("Working set {0} | {1}", RamText, _toolTipMemoryDetails);
            }
        }

        private string _toolTipDisk;
        public string ToolTipDisk
        {
            get { return _toolTipDisk; }
            private set { SetProperty(ref _toolTipDisk, value, "ToolTipDisk"); }
        }

        private string _toolTipGpu;
        public string ToolTipGpu
        {
            get { return _toolTipGpu; }
            private set { SetProperty(ref _toolTipGpu, value, "ToolTipGpu"); }
        }

        private string _toolTipNetwork;
        public string ToolTipNetwork
        {
            get { return _toolTipNetwork; }
            private set { SetProperty(ref _toolTipNetwork, value, "ToolTipNetwork"); }
        }

        private string _toolTipOther;
        public string ToolTipOther
        {
            get { return _toolTipOther; }
            private set { SetProperty(ref _toolTipOther, value, "ToolTipOther"); }
        }

        private string _toolTipStatus;
        public string ToolTipStatus
        {
            get { return _toolTipStatus; }
            private set { SetProperty(ref _toolTipStatus, value, "ToolTipStatus"); }
        }

        public RelayCommand KillCommand { get; }

        private bool _showClose = true;
        public bool ShowClose
        {
            get { return _showClose; }
            set { SetProperty(ref _showClose, value, "ShowClose"); }
        }

        public async Task RefreshToolTipAsync()
        {
            Task refreshTask;

            lock (_toolTipLock)
            {
                if (_toolTipRefreshTask != null && !_toolTipRefreshTask.IsCompleted)
                {
                    refreshTask = _toolTipRefreshTask;
                }
                else if (DateTime.UtcNow - _lastToolTipRefreshUtc < ToolTipCacheLifetime)
                {
                    return;
                }
                else
                {
                    ToolTipStatus = "Loading process details...";
                    refreshTask = _toolTipRefreshTask = RefreshToolTipCoreAsync();
                }
            }

            try
            {
                await refreshTask;
            }
            catch
            {
            }
        }

        private async Task RefreshToolTipCoreAsync()
        {
            ProcessToolTipSnapshot snapshot;

            try
            {
                snapshot = await Task.Run(() => CaptureToolTipSnapshot(Pid, Name));
            }
            catch (Exception ex)
            {
                snapshot = ProcessToolTipSnapshot.CreateError(Name, Pid, ex.Message);
            }
            finally
            {
                lock (_toolTipLock)
                {
                    _lastToolTipRefreshUtc = DateTime.UtcNow;
                    _toolTipRefreshTask = null;
                }
            }

            ApplyToolTipSnapshot(snapshot);
        }

        private void ApplyToolTipSnapshot(ProcessToolTipSnapshot snapshot)
        {
            ToolTipTitle = snapshot.Title;
            ToolTipSubtitle = snapshot.Subtitle;
            ToolTipPath = snapshot.Path;
            ToolTipMemoryDetails = snapshot.MemoryDetails;
            ToolTipDisk = snapshot.Disk;
            ToolTipGpu = snapshot.Gpu;
            ToolTipNetwork = snapshot.Network;
            ToolTipOther = snapshot.Other;
            ToolTipStatus = snapshot.Status;
        }

        private string ToolTipMemoryDetails
        {
            get { return _toolTipMemoryDetails; }
            set
            {
                if (!SetProperty(ref _toolTipMemoryDetails, value, "ToolTipMemoryDetails"))
                {
                    return;
                }

                NotifyPropertyChanged("ToolTipMemory");
            }
        }

        private bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        private static ProcessToolTipSnapshot CaptureToolTipSnapshot(int pid, string fallbackName)
        {
            ProcessToolTipSnapshot snapshot = ProcessToolTipSnapshot.CreatePending(fallbackName, pid);

            try
            {
                using (Process process = Process.GetProcessById(pid))
                {
                    process.Refresh();

                    string processName = SafeGetProcessName(process, fallbackName);
                    string processPath = TryGetProcessPath(process);

                    snapshot.Title = GetFriendlyProcessName(processPath, processName);
                    snapshot.Subtitle = BuildToolTipSubtitle(processName, pid);
                    snapshot.Path = string.IsNullOrWhiteSpace(processPath) ? "Path unavailable." : processPath;
                    snapshot.MemoryDetails = BuildMemoryDetails(process);
                    snapshot.Disk = BuildDiskText(process);
                    snapshot.Gpu = BuildGpuText(pid);
                    snapshot.Network = BuildNetworkText(pid);
                    snapshot.Other = BuildOtherText(process);
                    snapshot.Status = string.Format("Updated {0:T}", DateTime.Now);
                }
            }
            catch (ArgumentException)
            {
                snapshot = ProcessToolTipSnapshot.CreateError(fallbackName, pid, "Process exited before details could be loaded.");
            }
            catch (InvalidOperationException)
            {
                snapshot = ProcessToolTipSnapshot.CreateError(fallbackName, pid, "Process details are unavailable.");
            }
            catch (Win32Exception)
            {
                snapshot = ProcessToolTipSnapshot.CreateError(fallbackName, pid, "Access to this process is restricted.");
            }

            return snapshot;
        }

        private static string SafeGetProcessName(Process process, string fallbackName)
        {
            try
            {
                return string.IsNullOrWhiteSpace(process.ProcessName) ? fallbackName : process.ProcessName;
            }
            catch
            {
                return fallbackName;
            }
        }

        private static string TryGetProcessPath(Process process)
        {
            try
            {
                return process.MainModule == null ? null : process.MainModule.FileName;
            }
            catch
            {
                return null;
            }
        }

        private static string GetFriendlyProcessName(string processPath, string fallbackName)
        {
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                try
                {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(processPath);
                    if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
                    {
                        return versionInfo.FileDescription;
                    }
                }
                catch
                {
                }

                string fileName = Path.GetFileNameWithoutExtension(processPath);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    return fileName;
                }
            }

            return fallbackName;
        }

        private static string BuildToolTipSubtitle(string processName, int pid)
        {
            return string.Format("{0} (PID {1})", processName, pid);
        }

        private static string BuildRowLabel(string processName, int pid)
        {
            return string.Format("{0} [{1}]", processName, pid);
        }

        private static string BuildMemoryDetails(Process process)
        {
            List<string> parts = new List<string>(2);

            long privateBytes = TryGetInt64(() => process.PrivateMemorySize64);
            if (privateBytes >= 0)
            {
                parts.Add(string.Format("Private {0}", FormatBytes(privateBytes)));
            }

            long pagedBytes = TryGetInt64(() => process.PagedMemorySize64);
            if (pagedBytes >= 0)
            {
                parts.Add(string.Format("Paged {0}", FormatBytes(pagedBytes)));
            }

            return parts.Count == 0 ? "Memory details unavailable." : string.Join(" | ", parts);
        }

        private static string BuildDiskText(Process process)
        {
            IO_COUNTERS counters;
            try
            {
                if (!GetProcessIoCounters(process.Handle, out counters))
                {
                    return "I/O counters unavailable.";
                }
            }
            catch
            {
                return "I/O counters unavailable.";
            }

            return string.Format(
                "Read {0} | Write {1} | Ops {2}/{3}",
                FormatBytes(counters.ReadTransferCount),
                FormatBytes(counters.WriteTransferCount),
                FormatCount(counters.ReadOperationCount),
                FormatCount(counters.WriteOperationCount));
        }

        private static string BuildGpuText(int pid)
        {
            ProcessGpuSnapshot snapshot = GetProcessGpuSnapshot(pid);
            if (!snapshot.HasData)
            {
                return "No active GPU counters.";
            }

            List<string> parts = new List<string>(3);

            if (snapshot.HasUtilization)
            {
                parts.Add(string.Format("{0:0.#}% usage", snapshot.Utilization));
            }

            if (snapshot.HasMemory)
            {
                parts.Add(string.Format("{0} dedicated", FormatBytes(snapshot.DedicatedBytes)));
                parts.Add(string.Format("{0} shared", FormatBytes(snapshot.SharedBytes)));
            }

            return parts.Count == 0 ? "No active GPU counters." : string.Join(" | ", parts);
        }

        private static ProcessGpuSnapshot GetProcessGpuSnapshot(int pid)
        {
            ProcessGpuSnapshot snapshot = new ProcessGpuSnapshot();
            string prefix = string.Format("pid_{0}_", pid);

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT Name, UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine"))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject result in results)
                    {
                        string name = result["Name"] as string;
                        if (name == null || !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        snapshot.HasUtilization = true;
                        snapshot.Utilization += ToDouble(result["UtilizationPercentage"]);
                    }
                }
            }
            catch (ManagementException)
            {
            }

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT Name, DedicatedUsage, SharedUsage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUProcessMemory"))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject result in results)
                    {
                        string name = result["Name"] as string;
                        if (name == null || !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        snapshot.HasMemory = true;
                        snapshot.DedicatedBytes += ToUInt64(result["DedicatedUsage"]);
                        snapshot.SharedBytes += ToUInt64(result["SharedUsage"]);
                    }
                }
            }
            catch (ManagementException)
            {
            }

            return snapshot;
        }

        private static string BuildNetworkText(int pid)
        {
            ProcessNetworkSnapshot snapshot = GetProcessNetworkSnapshot(pid);
            if (!snapshot.HasData)
            {
                return "Connection data unavailable.";
            }

            return string.Format("{0} TCP | {1} UDP", snapshot.TcpConnections, snapshot.UdpListeners);
        }

        private static ProcessNetworkSnapshot GetProcessNetworkSnapshot(int pid)
        {
            try
            {
                return new ProcessNetworkSnapshot
                {
                    HasData = true,
                    TcpConnections =
                        CountRows<MibTcpRowOwnerPid>(AddressFamily.InterNetwork, TcpTableClass.TCP_TABLE_OWNER_PID_ALL, row => row.owningPid, pid) +
                        CountRows<MibTcp6RowOwnerPid>(AddressFamily.InterNetworkV6, TcpTableClass.TCP_TABLE_OWNER_PID_ALL, row => row.owningPid, pid),
                    UdpListeners =
                        CountUdpRows<MibUdpRowOwnerPid>(AddressFamily.InterNetwork, UdpTableClass.UDP_TABLE_OWNER_PID, row => row.owningPid, pid) +
                        CountUdpRows<MibUdp6RowOwnerPid>(AddressFamily.InterNetworkV6, UdpTableClass.UDP_TABLE_OWNER_PID, row => row.owningPid, pid)
                };
            }
            catch
            {
                return new ProcessNetworkSnapshot();
            }
        }

        private static int CountRows<T>(AddressFamily family, TcpTableClass tableClass, Func<T, uint> getPid, int pid) where T : struct
        {
            int bufferLength = 0;
            uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferLength, false, (int)family, tableClass, 0);
            if (result != NO_ERROR && result != ERROR_INSUFFICIENT_BUFFER)
            {
                return 0;
            }

            IntPtr buffer = Marshal.AllocHGlobal(bufferLength);
            try
            {
                result = GetExtendedTcpTable(buffer, ref bufferLength, false, (int)family, tableClass, 0);
                if (result != NO_ERROR)
                {
                    return 0;
                }

                int count = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = IntPtr.Add(buffer, sizeof(int));
                int rowSize = Marshal.SizeOf(typeof(T));
                int matches = 0;

                for (int i = 0; i < count; i++)
                {
                    T row = (T)Marshal.PtrToStructure(rowPtr, typeof(T));
                    if (getPid(row) == pid)
                    {
                        matches++;
                    }

                    rowPtr = IntPtr.Add(rowPtr, rowSize);
                }

                return matches;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static int CountUdpRows<T>(AddressFamily family, UdpTableClass tableClass, Func<T, uint> getPid, int pid) where T : struct
        {
            int bufferLength = 0;
            uint result = GetExtendedUdpTable(IntPtr.Zero, ref bufferLength, false, (int)family, tableClass, 0);
            if (result != NO_ERROR && result != ERROR_INSUFFICIENT_BUFFER)
            {
                return 0;
            }

            IntPtr buffer = Marshal.AllocHGlobal(bufferLength);
            try
            {
                result = GetExtendedUdpTable(buffer, ref bufferLength, false, (int)family, tableClass, 0);
                if (result != NO_ERROR)
                {
                    return 0;
                }

                int count = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = IntPtr.Add(buffer, sizeof(int));
                int rowSize = Marshal.SizeOf(typeof(T));
                int matches = 0;

                for (int i = 0; i < count; i++)
                {
                    T row = (T)Marshal.PtrToStructure(rowPtr, typeof(T));
                    if (getPid(row) == pid)
                    {
                        matches++;
                    }

                    rowPtr = IntPtr.Add(rowPtr, rowSize);
                }

                return matches;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string BuildOtherText(Process process)
        {
            List<string> parts = new List<string>(5);

            int threads = TryGetInt32(() => process.Threads.Count);
            if (threads >= 0)
            {
                parts.Add(string.Format("Threads {0}", threads));
            }

            int handles = TryGetInt32(() => process.HandleCount);
            if (handles >= 0)
            {
                parts.Add(string.Format("Handles {0}", handles));
            }

            string priority = TryGetString(() => process.PriorityClass.ToString());
            if (!string.IsNullOrWhiteSpace(priority))
            {
                parts.Add(string.Format("Priority {0}", priority));
            }

            DateTime? startTime = TryGetDateTime(() => process.StartTime);
            if (startTime.HasValue)
            {
                parts.Add(string.Format("Started {0:g}", startTime.Value));
            }

            bool? responding = TryGetBool(() => process.Responding);
            if (responding.HasValue)
            {
                parts.Add(string.Format("Responding {0}", responding.Value ? "Yes" : "No"));
            }

            return parts.Count == 0 ? "Additional process stats unavailable." : string.Join(" | ", parts);
        }

        private static string FormatBytes(ulong bytes)
        {
            return FormatBytes((double)bytes);
        }

        private static string FormatBytes(long bytes)
        {
            return FormatBytes((double)bytes);
        }

        private static string FormatBytes(double bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int suffixIndex = 0;

            while (value >= 1024d && suffixIndex < suffixes.Length - 1)
            {
                value /= 1024d;
                suffixIndex++;
            }

            string format = suffixIndex == 0 ? "0" : "0.##";
            return string.Format("{0:" + format + "} {1}", value, suffixes[suffixIndex]);
        }

        private static string FormatCount(ulong count)
        {
            return string.Format("{0:#,##0}", count);
        }

        private static long TryGetInt64(Func<long> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return -1;
            }
        }

        private static int TryGetInt32(Func<int> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return -1;
            }
        }

        private static string TryGetString(Func<string> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return null;
            }
        }

        private static bool? TryGetBool(Func<bool> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? TryGetDateTime(Func<DateTime> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return null;
            }
        }

        private static double ToDouble(object value)
        {
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0;
            }
        }

        private static ulong ToUInt64(object value)
        {
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToUInt64(value);
            }
            catch
            {
                return 0;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS lpIoCounters);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize, bool order, int ipVersion, TcpTableClass tableClass, uint reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize, bool order, int ipVersion, UdpTableClass tableClass, uint reserved);

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpRowOwnerPid
        {
            public uint state;
            public uint localAddr;
            public uint localPort;
            public uint remoteAddr;
            public uint remotePort;
            public uint owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcp6RowOwnerPid
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] localAddr;
            public uint localScopeId;
            public uint localPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] remoteAddr;
            public uint remoteScopeId;
            public uint remotePort;
            public uint state;
            public uint owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibUdpRowOwnerPid
        {
            public uint localAddr;
            public uint localPort;
            public uint owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibUdp6RowOwnerPid
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] localAddr;
            public uint localScopeId;
            public uint localPort;
            public uint owningPid;
        }

        private enum TcpTableClass
        {
            TCP_TABLE_OWNER_PID_ALL = 5
        }

        private enum UdpTableClass
        {
            UDP_TABLE_OWNER_PID = 1
        }

        private struct ProcessGpuSnapshot
        {
            public bool HasUtilization;
            public bool HasMemory;
            public double Utilization;
            public ulong DedicatedBytes;
            public ulong SharedBytes;

            public bool HasData
            {
                get { return HasUtilization || HasMemory; }
            }
        }

        private struct ProcessNetworkSnapshot
        {
            public bool HasData;
            public int TcpConnections;
            public int UdpListeners;
        }

        private sealed class ProcessToolTipSnapshot
        {
            public string Title { get; set; }
            public string Subtitle { get; set; }
            public string Path { get; set; }
            public string MemoryDetails { get; set; }
            public string Disk { get; set; }
            public string Gpu { get; set; }
            public string Network { get; set; }
            public string Other { get; set; }
            public string Status { get; set; }

            public static ProcessToolTipSnapshot CreatePending(string fallbackName, int pid)
            {
                return new ProcessToolTipSnapshot
                {
                    Title = fallbackName,
                    Subtitle = BuildToolTipSubtitle(fallbackName, pid),
                    Path = "Hover to load full process details.",
                    MemoryDetails = string.Empty,
                    Disk = "Loading on hover.",
                    Gpu = "Loading on hover.",
                    Network = "Loading on hover.",
                    Other = "Loading on hover.",
                    Status = string.Empty
                };
            }

            public static ProcessToolTipSnapshot CreateError(string fallbackName, int pid, string message)
            {
                return new ProcessToolTipSnapshot
                {
                    Title = fallbackName,
                    Subtitle = BuildToolTipSubtitle(fallbackName, pid),
                    Path = "Path unavailable.",
                    MemoryDetails = "Memory details unavailable.",
                    Disk = "Unavailable.",
                    Gpu = "Unavailable.",
                    Network = "Unavailable.",
                    Other = "No additional stats available.",
                    Status = message
                };
            }
        }
    }

    public class ProcessGroupEntry : INotifyPropertyChanged
    {
        public ProcessGroupEntry(int rootPid, string displayName, string cpuText, string ramText, int processCount, RelayCommand toggleExpandCommand, RelayCommand killTreeCommand)
        {
            RootPid = rootPid;
            DisplayName = displayName;
            CpuText = cpuText;
            RamText = ramText;
            ProcessCount = processCount;
            ToggleExpandCommand = toggleExpandCommand;
            KillTreeCommand = killTreeCommand;
            Children = new ObservableCollection<ProcessEntry>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int RootPid { get; }

        public RelayCommand ToggleExpandCommand { get; }

        public RelayCommand KillTreeCommand { get; }

        public ObservableCollection<ProcessEntry> Children { get; }

        private string _displayName;
        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                if (!SetProperty(ref _displayName, value, "DisplayName"))
                {
                    return;
                }

                NotifyPropertyChanged("SummaryName");
            }
        }

        private string _cpuText;
        public string CpuText
        {
            get { return _cpuText; }
            set { SetProperty(ref _cpuText, value, "CpuText"); }
        }

        private string _ramText;
        public string RamText
        {
            get { return _ramText; }
            set { SetProperty(ref _ramText, value, "RamText"); }
        }

        private double _cpuBarValue;
        public double CpuBarValue
        {
            get { return _cpuBarValue; }
            set { SetProperty(ref _cpuBarValue, value, "CpuBarValue"); }
        }

        private double _ramBarValue;
        public double RamBarValue
        {
            get { return _ramBarValue; }
            set { SetProperty(ref _ramBarValue, value, "RamBarValue"); }
        }

        private bool _showCpu = true;
        public bool ShowCpu
        {
            get { return _showCpu; }
            set { SetProperty(ref _showCpu, value, "ShowCpu"); }
        }

        private bool _showRam = true;
        public bool ShowRam
        {
            get { return _showRam; }
            set { SetProperty(ref _showRam, value, "ShowRam"); }
        }

        private int _processCount;
        public int ProcessCount
        {
            get { return _processCount; }
            set
            {
                if (!SetProperty(ref _processCount, value, "ProcessCount"))
                {
                    return;
                }

                NotifyPropertyChanged("SummaryName");
                NotifyPropertyChanged("CountText");
                NotifyPropertyChanged("HasChildren");
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (!SetProperty(ref _isExpanded, value, "IsExpanded"))
                {
                    return;
                }

                NotifyPropertyChanged("ExpandGlyph");
            }
        }

        public string SummaryName
        {
            get
            {
                return ProcessCount > 1
                    ? string.Format("{0} ({1})", DisplayName, ProcessCount)
                    : DisplayName;
            }
        }

        public string CountText
        {
            get { return string.Format("({0})", ProcessCount); }
        }

        public bool HasChildren
        {
            get { return ProcessCount > 1; }
        }

        public string ExpandGlyph
        {
            get { return IsExpanded ? "▼" : "▶"; }
        }

        private bool _canEndTree;
        public bool CanEndTree
        {
            get { return _canEndTree; }
            set { SetProperty(ref _canEndTree, value, "CanEndTree"); }
        }

        private bool _showClose = true;
        public bool ShowClose
        {
            get { return _showClose; }
            set { SetProperty(ref _showClose, value, "ShowClose"); }
        }

        private bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProcessMonitor : BaseMonitor
    {
        private const uint TH32CS_SNAPPROCESS = 0x00000002;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private static readonly HashSet<string> LauncherProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explorer",
            "cmd",
            "conhost",
            "powershell",
            "pwsh",
            "wt",
            "services",
            "wininit",
            "winlogon",
            "userinit",
            "smss",
            "csrss",
            "applicationframehost",
            "taskmgr"
        };
        private static readonly HashSet<string> ProtectedRootProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "system",
            "secure system",
            "registry",
            "smss",
            "csrss",
            "wininit",
            "winlogon",
            "services",
            "lsaiso",
            "lsass",
            "svchost",
            "fontdrvhost",
            "dwm",
            "sihost",
            "runtimebroker",
            "dllhost",
            "taskhostw",
            "applicationframehost",
            "shellexperiencehost",
            "startmenuexperiencehost",
            "searchhost",
            "searchindexer",
            "wudfhost",
            "audiodg",
            "taskmgr",
            "explorer"
        };

        private struct ProcessSnapshot
        {
            public TimeSpan CpuTime;
            public DateTime WallTime;
        }

        private struct ProcessInfo
        {
            public string Name;
            public int Pid;
            public int RootPid;
            public double Cpu;
            public double RamMb;
        }

        private struct ProcessTreeNode
        {
            public int Pid;
            public int ParentPid;
            public string Name;
        }

        private sealed class ProcessGroupInfo
        {
            public int RootPid;
            public string RootName;
            public double Cpu;
            public double RamMb;
            public List<ProcessInfo> Children = new List<ProcessInfo>();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        private readonly int _count;
        private readonly bool _sortByCpu;
        private readonly int _processorCount;
        private readonly bool _showCpu;
        private readonly bool _showRam;
        private readonly bool _showClose;
        private readonly Dictionary<int, string> _groupDisplayNameCache = new Dictionary<int, string>();
        private readonly Dictionary<int, bool> _groupCanEndTreeCache = new Dictionary<int, bool>();
        private readonly object _snapshotLock = new object();
        private readonly TimeSpan _treeSnapshotCacheLifetime = TimeSpan.FromSeconds(2);
        private Dictionary<int, ProcessSnapshot> _prevData = new Dictionary<int, ProcessSnapshot>();
        private Dictionary<int, ProcessTreeNode> _cachedTreeNodes = new Dictionary<int, ProcessTreeNode>();
        private DateTime _nextTreeSnapshotUtc = DateTime.MinValue;
        private Task _cpuBaselineWarmupTask;

        public ProcessMonitor(int count, bool sortByCpu, bool showCpu, bool showRam, bool showClose) : base("process", "Processes", false)
        {
            _count = count;
            _sortByCpu = sortByCpu;
            _processorCount = Math.Max(1, Environment.ProcessorCount);
            _showCpu = showCpu;
            _showRam = showRam;
            _showClose = showClose;
            Metrics = new iMetric[0];
            TopProcesses = new ObservableCollection<ProcessGroupEntry>();
            WarmCpuBaseline();
        }

        public override void Update()
        {
            if (_count <= 0)
            {
                if (TopProcesses.Count > 0)
                {
                    TopProcesses.Clear();
                }

                lock (_snapshotLock)
                {
                    _prevData = new Dictionary<int, ProcessSnapshot>();
                }
                _cachedTreeNodes.Clear();
                _groupDisplayNameCache.Clear();
                _groupCanEndTreeCache.Clear();
                _nextTreeSnapshotUtc = DateTime.MinValue;
                return;
            }

            Dictionary<int, ProcessTreeNode> treeNodes = GetProcessTreeSnapshot();
            Process[] procs;
            try { procs = Process.GetProcesses(); }
            catch { return; }

            DateTime now = DateTime.UtcNow;
            Dictionary<int, ProcessSnapshot> previousData;
            lock (_snapshotLock)
            {
                previousData = _prevData;
            }

            bool hasCpuBaseline = previousData.Count > 0;
            Dictionary<int, ProcessSnapshot> currentData = new Dictionary<int, ProcessSnapshot>(procs.Length);
            List<ProcessInfo> entries = new List<ProcessInfo>(procs.Length);
            Dictionary<int, int> rootCache = new Dictionary<int, int>(procs.Length);

            foreach (Process p in procs)
            {
                try
                {
                    double cpuPercent = 0;
                    if (hasCpuBaseline)
                    {
                        TimeSpan totalCpuTime = p.TotalProcessorTime;
                        currentData[p.Id] = new ProcessSnapshot { CpuTime = totalCpuTime, WallTime = now };

                        ProcessSnapshot prev;
                        if (previousData.TryGetValue(p.Id, out prev))
                        {
                            double elapsedCpu = (totalCpuTime - prev.CpuTime).TotalSeconds;
                            double elapsedWall = (now - prev.WallTime).TotalSeconds;
                            if (elapsedWall > 0)
                            {
                                cpuPercent = Math.Min(100, elapsedCpu / elapsedWall / _processorCount * 100);
                            }
                        }
                    }

                    double ramMb = p.WorkingSet64 / 1048576.0;
                    int rootPid = ResolveGroupRootPid(p.Id, treeNodes, rootCache);
                    entries.Add(new ProcessInfo { Name = p.ProcessName, Pid = p.Id, RootPid = rootPid, Cpu = cpuPercent, RamMb = ramMb });
                }
                catch { }
                finally
                {
                    p.Dispose();
                }
            }

            if (hasCpuBaseline)
            {
                lock (_snapshotLock)
                {
                    _prevData = currentData;
                }
            }
            else
            {
                WarmCpuBaseline();
            }

            Dictionary<int, ProcessGroupInfo> groups = BuildGroupInfos(entries, treeNodes);
            List<ProcessGroupInfo> sortedGroups = groups.Values.ToList();
            sortedGroups.Sort(_sortByCpu ? (Comparison<ProcessGroupInfo>)CompareGroupByCpu : CompareGroupByRam);

            PruneDisplayNameCache(groups.Keys);

            int visibleCount = Math.Min(_count, sortedGroups.Count);
            double maxVisibleCpu = 0;
            double maxVisibleRamMb = 0;

            for (int i = 0; i < visibleCount; i++)
            {
                ProcessGroupInfo visibleGroup = sortedGroups[i];
                if (visibleGroup.Cpu > maxVisibleCpu)
                {
                    maxVisibleCpu = visibleGroup.Cpu;
                }

                if (visibleGroup.RamMb > maxVisibleRamMb)
                {
                    maxVisibleRamMb = visibleGroup.RamMb;
                }
            }

            for (int i = 0; i < visibleCount; i++)
            {
                ProcessGroupInfo info = sortedGroups[i];
                ProcessGroupEntry entry;

                if (i < TopProcesses.Count && TopProcesses[i].RootPid == info.RootPid)
                {
                    entry = TopProcesses[i];
                }
                else
                {
                    int existingIndex = FindGroupIndex(info.RootPid);
                    if (existingIndex >= 0)
                    {
                        entry = TopProcesses[existingIndex];
                        if (existingIndex != i)
                        {
                            TopProcesses.Move(existingIndex, i);
                        }
                    }
                    else
                    {
                        entry = CreateProcessEntry(info, maxVisibleCpu, maxVisibleRamMb);
                        TopProcesses.Insert(i, entry);
                    }
                }

                UpdateProcessEntry(entry, info, maxVisibleCpu, maxVisibleRamMb);
            }

            while (TopProcesses.Count > visibleCount)
            {
                TopProcesses.RemoveAt(TopProcesses.Count - 1);
            }
        }

        private static int CompareByCpu(ProcessInfo left, ProcessInfo right)
        {
            int result = right.Cpu.CompareTo(left.Cpu);
            if (result != 0)
            {
                return result;
            }

            return right.RamMb.CompareTo(left.RamMb);
        }

        private static int CompareByRam(ProcessInfo left, ProcessInfo right)
        {
            int result = right.RamMb.CompareTo(left.RamMb);
            if (result != 0)
            {
                return result;
            }

            return right.Cpu.CompareTo(left.Cpu);
        }

        private static int CompareGroupByCpu(ProcessGroupInfo left, ProcessGroupInfo right)
        {
            int result = right.Cpu.CompareTo(left.Cpu);
            if (result != 0)
            {
                return result;
            }

            return right.RamMb.CompareTo(left.RamMb);
        }

        private static int CompareGroupByRam(ProcessGroupInfo left, ProcessGroupInfo right)
        {
            int result = right.RamMb.CompareTo(left.RamMb);
            if (result != 0)
            {
                return result;
            }

            return right.Cpu.CompareTo(left.Cpu);
        }

        private Dictionary<int, ProcessGroupInfo> BuildGroupInfos(List<ProcessInfo> entries, Dictionary<int, ProcessTreeNode> treeNodes)
        {
            Dictionary<int, ProcessGroupInfo> groups = new Dictionary<int, ProcessGroupInfo>();

            foreach (ProcessInfo info in entries)
            {
                ProcessGroupInfo group;
                if (!groups.TryGetValue(info.RootPid, out group))
                {
                    group = new ProcessGroupInfo
                    {
                        RootPid = info.RootPid,
                        RootName = GetTreeNodeName(info.RootPid, treeNodes, info.Name)
                    };
                    groups.Add(info.RootPid, group);
                }

                group.Cpu += info.Cpu;
                group.RamMb += info.RamMb;
                group.Children.Add(info);
            }

            foreach (ProcessGroupInfo group in groups.Values)
            {
                SortChildren(group.Children, group.RootPid);
            }

            return groups;
        }

        private void SortChildren(List<ProcessInfo> children, int rootPid)
        {
            children.Sort((left, right) =>
            {
                if (left.Pid == rootPid && right.Pid != rootPid)
                {
                    return -1;
                }

                if (right.Pid == rootPid && left.Pid != rootPid)
                {
                    return 1;
                }

                return _sortByCpu ? CompareByCpu(left, right) : CompareByRam(left, right);
            });
        }

        private int FindGroupIndex(int rootPid)
        {
            for (int i = 0; i < TopProcesses.Count; i++)
            {
                if (TopProcesses[i].RootPid == rootPid)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindChildIndex(ObservableCollection<ProcessEntry> children, int pid)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].Pid == pid)
                {
                    return i;
                }
            }

            return -1;
        }

        private ProcessGroupEntry CreateProcessEntry(ProcessGroupInfo info, double maxVisibleCpu, double maxVisibleRamMb)
        {
            ProcessGroupEntry entry = null;
            entry = new ProcessGroupEntry(
                info.RootPid,
                ResolveGroupDisplayName(info.RootPid, info.RootName),
                FormatCpuText(info.Cpu),
                FormatRamText(info.RamMb),
                info.Children.Count,
                new RelayCommand(() => entry.IsExpanded = !entry.IsExpanded),
                new RelayCommand(() => KillProcessTree(entry))
            );

            UpdateProcessEntry(entry, info, maxVisibleCpu, maxVisibleRamMb);
            return entry;
        }

        private void UpdateProcessEntry(ProcessGroupEntry entry, ProcessGroupInfo info, double maxVisibleCpu, double maxVisibleRamMb)
        {
            string groupDisplayName = ResolveGroupDisplayName(info.RootPid, info.RootName);
            entry.DisplayName = groupDisplayName;
            entry.CpuText = FormatCpuText(info.Cpu);
            entry.RamText = FormatRamText(info.RamMb);
            entry.CpuBarValue = NormalizeBarValue(info.Cpu, maxVisibleCpu);
            entry.RamBarValue = NormalizeBarValue(info.RamMb, maxVisibleRamMb);
            entry.ShowCpu = _showCpu;
            entry.ShowRam = _showRam;
            entry.ShowClose = _showClose;
            entry.ProcessCount = info.Children.Count;
            entry.CanEndTree = ResolveGroupCanEndTree(info.RootPid, info.RootName);

            for (int i = 0; i < info.Children.Count; i++)
            {
                ProcessInfo childInfo = info.Children[i];
                ProcessEntry childEntry;

                if (i < entry.Children.Count && entry.Children[i].Pid == childInfo.Pid)
                {
                    childEntry = entry.Children[i];
                }
                else
                {
                    int existingIndex = FindChildIndex(entry.Children, childInfo.Pid);
                    if (existingIndex >= 0)
                    {
                        childEntry = entry.Children[existingIndex];
                        if (existingIndex != i)
                        {
                            entry.Children.Move(existingIndex, i);
                        }
                    }
                    else
                    {
                        childEntry = CreateChildEntry(childInfo, groupDisplayName, _showClose);
                        entry.Children.Insert(i, childEntry);
                    }
                }

                UpdateChildEntry(childEntry, childInfo, groupDisplayName, _showClose);
            }

            while (entry.Children.Count > info.Children.Count)
            {
                entry.Children.RemoveAt(entry.Children.Count - 1);
            }
        }

        private static ProcessEntry CreateChildEntry(ProcessInfo info, string groupDisplayName, bool showClose)
        {
            int capturedPid = info.Pid;
            ProcessEntry entry = new ProcessEntry(
                info.Pid,
                info.Name,
                BuildChildRowLabel(groupDisplayName, info),
                FormatCpuText(info.Cpu),
                FormatRamText(info.RamMb),
                new RelayCommand(() => KillProcess(capturedPid))
            );
            entry.ShowClose = showClose;
            return entry;
        }

        private static void UpdateChildEntry(ProcessEntry entry, ProcessInfo info, string groupDisplayName, bool showClose)
        {
            entry.Name = info.Name;
            entry.SetRowLabel(BuildChildRowLabel(groupDisplayName, info));
            entry.CpuText = FormatCpuText(info.Cpu);
            entry.RamText = FormatRamText(info.RamMb);
            entry.ShowClose = showClose;
        }

        private static string BuildChildRowLabel(string groupDisplayName, ProcessInfo info)
        {
            string label = info.Pid == info.RootPid
                ? string.Format("{0} (main)", string.IsNullOrWhiteSpace(groupDisplayName) ? info.Name : groupDisplayName)
                : info.Name;

            if (string.IsNullOrWhiteSpace(label))
            {
                label = "Process";
            }

            return string.Format("{0} [{1}]", label, info.Pid);
        }

        private static double NormalizeBarValue(double value, double maxValue)
        {
            if (value <= 0 || maxValue <= 0)
            {
                return 0;
            }

            return Math.Max(3, Math.Min(100, value / maxValue * 100));
        }

        private static string FormatCpuText(double cpu)
        {
            return string.Format("{0:0.0}%", cpu);
        }

        private static string FormatRamText(double ramMb)
        {
            return ramMb >= 1024
                ? string.Format("{0:0.0} GB", ramMb / 1024.0)
                : string.Format("{0:0} MB", ramMb);
        }

        private static void KillProcessTree(ProcessGroupEntry group)
        {
            if (group == null)
            {
                return;
            }

            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                string.Format("End '{0}' and {1} process{2}?", group.SummaryName, group.ProcessCount, group.ProcessCount == 1 ? "" : "es"),
                "End App Tree",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            List<int> pids = group.Children
                .Select(c => c.Pid)
                .Where(pid => pid != group.RootPid)
                .Distinct()
                .ToList();

            pids.Add(group.RootPid);

            foreach (int pid in pids)
            {
                try
                {
                    using (Process process = Process.GetProcessById(pid))
                    {
                        process.Kill();
                    }
                }
                catch
                {
                }
            }
        }

        private static void KillProcess(int pid)
        {
            try
            {
                using (Process p = Process.GetProcessById(pid))
                {
                    string procName = p.ProcessName;
                    System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                        string.Format("Kill '{0}' (PID {1})?", procName, pid),
                        "Kill Process",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);
                    if (result == System.Windows.MessageBoxResult.Yes)
                        p.Kill();
                }
            }
            catch { }
        }

        private string ResolveGroupDisplayName(int rootPid, string fallbackName)
        {
            string displayName;
            if (_groupDisplayNameCache.TryGetValue(rootPid, out displayName))
            {
                return displayName;
            }

            displayName = fallbackName;

            try
            {
                using (Process process = Process.GetProcessById(rootPid))
                {
                    string processPath = TryGetProcessPath(process);
                    displayName = GetFriendlyProcessName(processPath, fallbackName);
                }
            }
            catch
            {
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = fallbackName;
            }

            _groupDisplayNameCache[rootPid] = displayName;
            return displayName;
        }

        private void PruneDisplayNameCache(IEnumerable<int> activeRootPids)
        {
            HashSet<int> active = new HashSet<int>(activeRootPids);
            List<int> stale = _groupDisplayNameCache.Keys.Where(key => !active.Contains(key)).ToList();
            foreach (int key in stale)
            {
                _groupDisplayNameCache.Remove(key);
            }

            stale = _groupCanEndTreeCache.Keys.Where(key => !active.Contains(key)).ToList();
            foreach (int key in stale)
            {
                _groupCanEndTreeCache.Remove(key);
            }
        }

        private bool ResolveGroupCanEndTree(int rootPid, string fallbackName)
        {
            bool canEndTree;
            if (_groupCanEndTreeCache.TryGetValue(rootPid, out canEndTree))
            {
                return canEndTree;
            }

            canEndTree = true;
            try
            {
                using (Process process = Process.GetProcessById(rootPid))
                {
                    string processName = process.ProcessName;
                    string processPath = TryGetProcessPath(process);
                    canEndTree = IsKillableRootProcess(processName, processPath);
                }
            }
            catch
            {
                canEndTree = IsKillableRootProcess(fallbackName, null);
            }

            _groupCanEndTreeCache[rootPid] = canEndTree;
            return canEndTree;
        }

        private static bool IsKillableRootProcess(string processName, string processPath)
        {
            if (!string.IsNullOrWhiteSpace(processName) && ProtectedRootProcessNames.Contains(processName))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(processPath))
            {
                return true;
            }

            string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            return string.IsNullOrWhiteSpace(windowsDirectory) ||
                !processPath.StartsWith(windowsDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static string TryGetProcessPath(Process process)
        {
            try
            {
                return process.MainModule == null ? null : process.MainModule.FileName;
            }
            catch
            {
                return null;
            }
        }

        private static string GetFriendlyProcessName(string processPath, string fallbackName)
        {
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                try
                {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(processPath);
                    if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
                    {
                        return versionInfo.FileDescription;
                    }
                }
                catch
                {
                }

                string fileName = Path.GetFileNameWithoutExtension(processPath);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    return fileName;
                }
            }

            return fallbackName;
        }

        private static Dictionary<int, ProcessTreeNode> CollectProcessTreeSnapshot()
        {
            Dictionary<int, ProcessTreeNode> nodes = new Dictionary<int, ProcessTreeNode>();
            IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (snapshot == INVALID_HANDLE_VALUE)
            {
                return nodes;
            }

            try
            {
                PROCESSENTRY32 entry = new PROCESSENTRY32();
                entry.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));

                if (!Process32FirstW(snapshot, ref entry))
                {
                    return nodes;
                }

                do
                {
                    string name = string.IsNullOrWhiteSpace(entry.szExeFile)
                        ? string.Empty
                        : Path.GetFileNameWithoutExtension(entry.szExeFile);

                    nodes[(int)entry.th32ProcessID] = new ProcessTreeNode
                    {
                        Pid = (int)entry.th32ProcessID,
                        ParentPid = (int)entry.th32ParentProcessID,
                        Name = name
                    };

                    entry.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
                }
                while (Process32NextW(snapshot, ref entry));

                return nodes;
            }
            finally
            {
                CloseHandle(snapshot);
            }
        }

        private Dictionary<int, ProcessTreeNode> GetProcessTreeSnapshot()
        {
            DateTime now = DateTime.UtcNow;
            if (_cachedTreeNodes.Count > 0 && now < _nextTreeSnapshotUtc)
            {
                return _cachedTreeNodes;
            }

            _cachedTreeNodes = CollectProcessTreeSnapshot();
            _nextTreeSnapshotUtc = now + _treeSnapshotCacheLifetime;
            return _cachedTreeNodes;
        }

        private void WarmCpuBaseline()
        {
            lock (_snapshotLock)
            {
                if (_prevData.Count > 0 || (_cpuBaselineWarmupTask != null && !_cpuBaselineWarmupTask.IsCompleted))
                {
                    return;
                }

                // Baseline process CPU counters off the UI thread so the first render can complete quickly.
                _cpuBaselineWarmupTask = Task.Run(() =>
                {
                    Process[] processes;
                    try
                    {
                        processes = Process.GetProcesses();
                    }
                    catch
                    {
                        return;
                    }

                    DateTime sampleTime = DateTime.UtcNow;
                    Dictionary<int, ProcessSnapshot> snapshots = new Dictionary<int, ProcessSnapshot>(processes.Length);

                    foreach (Process process in processes)
                    {
                        try
                        {
                            snapshots[process.Id] = new ProcessSnapshot
                            {
                                CpuTime = process.TotalProcessorTime,
                                WallTime = sampleTime
                            };
                        }
                        catch
                        {
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }

                    lock (_snapshotLock)
                    {
                        _prevData = snapshots;
                    }
                });
            }
        }

        private static int ResolveGroupRootPid(int pid, Dictionary<int, ProcessTreeNode> treeNodes, Dictionary<int, int> rootCache)
        {
            int cachedRoot;
            if (rootCache.TryGetValue(pid, out cachedRoot))
            {
                return cachedRoot;
            }

            int current = pid;
            List<int> visited = new List<int>();
            HashSet<int> seen = new HashSet<int>();

            while (seen.Add(current))
            {
                visited.Add(current);

                ProcessTreeNode node;
                ProcessTreeNode parentNode;
                if (!treeNodes.TryGetValue(current, out node) ||
                    node.ParentPid <= 0 ||
                    !treeNodes.TryGetValue(node.ParentPid, out parentNode) ||
                    IsLauncherProcess(parentNode.Name))
                {
                    break;
                }

                current = node.ParentPid;
            }

            foreach (int visitedPid in visited)
            {
                rootCache[visitedPid] = current;
            }

            return current;
        }

        private static bool IsLauncherProcess(string processName)
        {
            return !string.IsNullOrWhiteSpace(processName) && LauncherProcessNames.Contains(processName);
        }

        private static string GetTreeNodeName(int pid, Dictionary<int, ProcessTreeNode> treeNodes, string fallbackName)
        {
            ProcessTreeNode node;
            if (treeNodes.TryGetValue(pid, out node) && !string.IsNullOrWhiteSpace(node.Name))
            {
                return node.Name;
            }

            return fallbackName;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "Process32FirstW", SetLastError = true)]
        private static extern bool Process32FirstW(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "Process32NextW", SetLastError = true)]
        private static extern bool Process32NextW(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        public ObservableCollection<ProcessGroupEntry> TopProcesses { get; private set; }
    }
}
