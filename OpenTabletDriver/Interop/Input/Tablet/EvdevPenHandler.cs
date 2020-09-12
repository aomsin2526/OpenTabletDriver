﻿using System;
using System.Numerics;
using OpenTabletDriver.Native.Linux;
using OpenTabletDriver.Native.Linux.Evdev;
using OpenTabletDriver.Native.Linux.Evdev.Structs;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Platform.Pointer;

namespace OpenTabletDriver.Interop.Input.Tablet
{
    public class EvdevPenHandler : IVirtualTablet, IPressureHandler
    {
        public unsafe EvdevPenHandler()
        {
            Device = new EvdevDevice("OpenTabletDriver Virtual Artist Tablet");

            Device.EnableType(EventType.INPUT_PROP_DIRECT);
            Device.EnableType(EventType.EV_ABS);

            var xAbs = new input_absinfo
            {
                maximum = (int)Platform.VirtualScreen.Width,
                resolution = short.MaxValue
            };
            input_absinfo* xPtr = &xAbs;
            Device.EnableCustomCode(EventType.EV_ABS, EventCode.ABS_X, (IntPtr)xPtr);

            var yAbs = new input_absinfo
            {
                maximum = (int)Platform.VirtualScreen.Height,
                resolution = short.MaxValue
            };
            input_absinfo* yPtr = &yAbs;
            Device.EnableCustomCode(EventType.EV_ABS, EventCode.ABS_Y, (IntPtr)yPtr);

            var pressure = new input_absinfo
            {
                maximum = MaxPressure
            };
            input_absinfo* pressurePtr = &pressure;
            Device.EnableCustomCode(EventType.EV_ABS, EventCode.ABS_PRESSURE, (IntPtr)pressurePtr);

            Device.EnableTypeCodes(
                EventType.EV_KEY,
                EventCode.BTN_TOUCH,
                EventCode.BTN_STYLUS,
                EventCode.BTN_TOOL_PEN
            );

            var result = Device.Initialize();
            switch (result)
            {
                case ERRNO.NONE:
                    Log.Debug("Evdev", $"Successfully initialized virtual pressure sensitive tablet. (code {result})");
                    break;
                default:
                    Log.Write("Evdev", $"Failed to initialize virtual pressure sensitive tablet. (error code {result})", LogLevel.Error);
                    break;
            }
        }

        private EvdevDevice Device { set; get; }
        private const int MaxPressure = ushort.MaxValue;

        private EventCode? GetCode(MouseButton button)
        {
            return button switch
            {
                MouseButton.Right => EventCode.BTN_STYLUS2,
                MouseButton.Middle => EventCode.BTN_STYLUS3,
                _                 => null
            };
        }

        public void MouseDown(MouseButton button)
        {
            if (GetCode(button) is EventCode code)
            {
                Device.Write(EventType.EV_KEY, code, 1);
                Device.Sync();
            }
        }

        public void MouseUp(MouseButton button)
        {
            if (GetCode(button) is EventCode code)
            {
                Device.Write(EventType.EV_KEY, code, 0);
                Device.Sync();
            }
        }

        public void SetPosition(Vector2 pos)
        {
            Device.Write(EventType.EV_ABS, EventCode.ABS_X, (int)pos.X);
            Device.Write(EventType.EV_ABS, EventCode.ABS_Y, (int)pos.Y);
            Device.Sync();
        }

        public void SetPressure(float percentage)
        {
            Device.Write(EventType.EV_ABS, EventCode.ABS_PRESSURE, (int)(MaxPressure * percentage));
            Device.Write(EventType.EV_KEY, EventCode.BTN_TOUCH, percentage > 0 ? 1 : 0);
            Device.Write(EventType.EV_KEY, EventCode.BTN_TOOL_PEN, 1);
            Device.Sync();
        }
    }
}