﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CityAndSeek.Common.Packets;

namespace CityAndSeek.Client.RequestHandlers
{
    public interface IRequestHandler
    {
        void OnPacket(Packet packet);
    }
}