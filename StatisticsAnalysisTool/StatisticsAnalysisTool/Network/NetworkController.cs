﻿using Albion.Network;
using PacketDotNet;
using SharpPcap;
using StatisticsAnalysisTool.Network.Handler;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StatisticsAnalysisTool.Network
{
    public class NetworkController
    {
        private static IPhotonReceiver _receiver;
        private static readonly List<ICaptureDevice> _capturedDevices = new List<ICaptureDevice>();

        public static void StartNetworkCapture()
        {
            var builder = ReceiverBuilder.Create();

            //builder.AddRequestHandler(new GuildVaultInfoHandler());
            //builder.AddEventHandler(new MoveEventHandler());
            //builder.AddEventHandler(new NewCharacterEventHandler());
            
            builder.AddEventHandler(new TakeSilverEventHandler());
            builder.AddEventHandler(new PartySilverGainedEventHandler());
            builder.AddEventHandler(new GuildVaultInfoHandler());

            //builder.AddEventHandler(new NewLootEventHandler());
            //builder.AddEventHandler(new NewLootChestEventHandler());

            _receiver = builder.Build();

            _capturedDevices.AddRange(CaptureDeviceList.Instance);
            StartDeviceCapture();
        }

        private static void StartDeviceCapture()
        {
            foreach (var device in _capturedDevices)
            {
                Task.Run(() =>
                {
                    device.OnPacketArrival += PacketHandler;
                    device.Open(DeviceMode.Promiscuous, 1000);
                    device.StartCapture();
                });
            }
        }

        public static void StopNetworkCapture()
        {
            foreach (var device in _capturedDevices.Where(device => device.Started))
            {
                Task.Run(() =>
                {
                    device.StopCapture();
                });
            }
        }

        private static void PacketHandler(object sender, CaptureEventArgs e)
        {
            var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data).Extract<UdpPacket>();
            if (packet != null && (packet.SourcePort == 5056 || packet.DestinationPort == 5056))
            {
                _receiver.ReceivePacket(packet.PayloadData);
            }
        }
    }
}